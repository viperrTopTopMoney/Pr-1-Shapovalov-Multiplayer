using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieNetwork : NetworkBehaviour, IDamageable
{
    [Header("Zombie Stats")]
    [SerializeField] private ZombieKind _kind = ZombieKind.Normal;
    [SerializeField] private int _maxHP = 2;
    [SerializeField] private float _moveSpeed = 1.4f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackCooldown = 1.2f;
    [SerializeField] private int _barricadeDamage = 5;
    [SerializeField] private int _playerDamage = 15;
    [SerializeField] private float _modelScale = 1f;
    [SerializeField] private float _despawnDelay = 1.25f;

    public readonly SyncVar<int> HP = new SyncVar<int>(2);
    public readonly SyncVar<bool> IsDead = new SyncVar<bool>(false);
    public readonly SyncVar<float> MoveMagnitude = new SyncVar<float>(0f);
    public readonly SyncVar<bool> IsAttacking = new SyncVar<bool>(false);

    private NavMeshAgent _agent;
    private ZombieView _view;
    private float _nextAttackTime;
    private float _attackVisualUntil;
    private Transform _currentTarget;

    public ZombieKind Kind => _kind;
    public int PlayerDamage => _playerDamage;
    public int BarricadeDamage => _barricadeDamage;
    public bool Alive => !IsDead.Value;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _view = GetComponent<ZombieView>();

        HP.OnChange += OnHpChanged;
        IsDead.OnChange += OnDeadChanged;
        MoveMagnitude.OnChange += OnMotionChanged;
        IsAttacking.OnChange += OnMotionChanged;
    }

    private void OnDestroy()
    {
        HP.OnChange -= OnHpChanged;
        IsDead.OnChange -= OnDeadChanged;
        MoveMagnitude.OnChange -= OnMotionChanged;
        IsAttacking.OnChange -= OnMotionChanged;
    }

    public void Configure(ZombieKind kind, int maxHP, float moveSpeed, int barricadeDamage, int playerDamage, float modelScale)
    {
        _kind = kind;
        _maxHP = Mathf.Max(1, maxHP);
        _moveSpeed = Mathf.Max(0.1f, moveSpeed);
        _barricadeDamage = Mathf.Max(0, barricadeDamage);
        _playerDamage = Mathf.Max(0, playerDamage);
        _modelScale = Mathf.Max(0.1f, modelScale);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        HP.Value = _maxHP;
        IsDead.Value = false;
        MoveMagnitude.Value = 0f;
        IsAttacking.Value = false;
        _nextAttackTime = 0f;
        _attackVisualUntil = 0f;

        ApplyServerStats();
        SnapToNavMesh();
        ZombieManager.Instance?.RegisterZombie(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ZombieManager.Instance?.UnregisterZombie(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisualScale();
        UpdateViewMotion();
    }

    private void Update()
    {
        if (!base.IsServerInitialized || IsDead.Value)
            return;

        IsAttacking.Value = Time.time < _attackVisualUntil;

        UpdateTarget();
        MoveToTarget();
        TryAttack();
    }

    public void TakeDamageServer(int amount)
    {
        TakeDamageServer(amount, -1);
    }

    public void TakeDamageServer(int amount, int attackerOwnerId)
    {
        if (!base.IsServerInitialized || amount <= 0 || IsDead.Value)
            return;

        HP.Value = Mathf.Max(0, HP.Value - amount);

        if (HP.Value <= 0 && !IsDead.Value)
        {
            if (attackerOwnerId >= 0)
                GameManager.Instance?.AwardKill(attackerOwnerId);

            KillServer();
        }
    }

    private void KillServer()
    {
        if (!base.IsServerInitialized || IsDead.Value)
            return;

        IsDead.Value = true;
        MoveMagnitude.Value = 0f;
        IsAttacking.Value = false;

        ZombieManager.Instance?.NotifyZombieKilled(this);
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        if (asServer && next <= 0 && !IsDead.Value)
            KillServer();
    }

    private void OnDeadChanged(bool prev, bool next, bool asServer)
    {
        if (!next)
        {
            if (_agent != null && base.IsServerInitialized)
                _agent.isStopped = false;

            return;
        }

        if (_agent != null && base.IsServerInitialized)
            _agent.isStopped = true;

        if (_view != null)
            _view.PlayDeath();

        if (base.IsServerInitialized)
            Invoke(nameof(DespawnSelf), _despawnDelay);
    }

    private void DespawnSelf()
    {
        if (!base.IsServerInitialized)
            return;

        if (base.IsSpawned)
            base.Despawn();
        else
            Destroy(gameObject);
    }

    private void ApplyServerStats()
    {
        if (_agent == null)
            return;

        _agent.speed = _moveSpeed;
        _agent.acceleration = 20f;
        _agent.angularSpeed = 720f;
        _agent.stoppingDistance = Mathf.Max(0.1f, _attackRange * 0.85f);
    }

    private void SnapToNavMesh()
    {
        if (_agent == null)
            return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            _agent.Warp(hit.position);
    }

    private void ApplyVisualScale()
    {
        if (_view != null)
            _view.SetScale(_modelScale);
    }

    private void UpdateTarget()
    {
        _currentTarget = ZombieManager.Instance != null ? ZombieManager.Instance.GetTargetForZombie(this) : null;

        if (_currentTarget == null && GameManager.Instance != null)
            _currentTarget = GameManager.Instance.GetHouseTargetFallback();
    }

    private void MoveToTarget()
    {
        if (_agent == null || !_agent.isOnNavMesh || _currentTarget == null)
        {
            MoveMagnitude.Value = 0f;
            return;
        }

        _agent.SetDestination(_currentTarget.position);
        MoveMagnitude.Value = _agent.velocity.magnitude;
        UpdateViewMotion();
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
            return;

        PlayerNetwork nearbyPlayer = FindNearestAttackablePlayer();
        if (nearbyPlayer != null)
        {
            DoAttack();
            nearbyPlayer.TakeDamage(_playerDamage);
            return;
        }

        if (_currentTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        if (distance > _attackRange)
            return;

        if (_currentTarget.TryGetComponent(out BarricadeController barricade) && !barricade.IsDestroyed)
        {
            DoAttack();
            barricade.TakeDamageServer(_barricadeDamage);
        }
    }

    private PlayerNetwork FindNearestAttackablePlayer()
    {
        PlayerNetwork[] players = UnityEngine.Object.FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);

        PlayerNetwork best = null;
        float bestDistance = float.MaxValue;

        foreach (PlayerNetwork player in players)
        {
            if (player == null || player.LifeState.Value != PlayerLifeState.Alive)
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= _attackRange && distance < bestDistance)
            {
                best = player;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void DoAttack()
    {
        _nextAttackTime = Time.time + _attackCooldown;
        _attackVisualUntil = Time.time + 0.25f;

        MoveMagnitude.Value = 0f;
        IsAttacking.Value = true;

        if (_view != null)
            _view.PlayAttack();

        PlayAttackObserversRpc();
    }

    [ObserversRpc]
    private void PlayAttackObserversRpc()
    {
        if (_view != null)
            _view.PlayAttack();
    }

    private void OnMotionChanged(float prev, float next, bool asServer)
    {
        UpdateViewMotion();
    }

    private void OnMotionChanged(bool prev, bool next, bool asServer)
    {
        UpdateViewMotion();
    }

    private void UpdateViewMotion()
    {
        if (_view != null)
            _view.SetMotion(MoveMagnitude.Value, IsAttacking.Value);
    }
}
