using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public abstract class BarricadeController : NetworkBehaviour, IDamageable
{
    [Header("Barricade")]
    [SerializeField] protected int _maxHP = 100;
    [SerializeField] private GameObject _intactRoot;
    [SerializeField] private GameObject _destroyedRoot;

    public readonly SyncVar<int> CurrentHP = new SyncVar<int>(100);
    private float _repairAccumulator;
    private Collider _blockingCollider;
    public int MaxHP => _maxHP;
    public bool IsDestroyed => CurrentHP.Value <= 0;

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentHP.Value = _maxHP;
        _repairAccumulator = 0f;
        ApplyVisuals(_maxHP);
    }

    private void Awake()
    {
        _blockingCollider = GetComponent<Collider>();
        CurrentHP.OnChange += OnHpChanged;
    }

    private void OnDestroy()
    {
        CurrentHP.OnChange -= OnHpChanged;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_maxHP < 1)
            _maxHP = 1;
    }

    public void TakeDamageServer(int amount)
    {
        if (!base.IsServerInitialized || amount <= 0 || IsDestroyed)
            return;

        CurrentHP.Value = Mathf.Max(0, CurrentHP.Value - amount);
    }

    public void RepairServer(float amountPerSecond)
    {
        if (!base.IsServerInitialized || amountPerSecond <= 0f || CurrentHP.Value >= _maxHP)
            return;

        _repairAccumulator += amountPerSecond;
        int delta = Mathf.FloorToInt(_repairAccumulator);
        if (delta <= 0)
            return;

        _repairAccumulator -= delta;

        CurrentHP.Value = Mathf.Min(_maxHP, CurrentHP.Value + delta);
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        ApplyVisuals(next);
    }

    protected virtual void ApplyVisuals(int hp)
    {
        bool intact = hp > 0;
        if (_intactRoot != null) _intactRoot.SetActive(intact);
        if (_destroyedRoot != null) _destroyedRoot.SetActive(!intact);
        if (_blockingCollider != null) _blockingCollider.isTrigger = !intact;
    }
}
