using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Network Variables")]
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<bool> IsDowned = new SyncVar<bool>(false);
    public readonly SyncVar<PlayerLifeState> LifeState = new SyncVar<PlayerLifeState>(PlayerLifeState.Alive);
    public readonly SyncVar<string> Nickname = new SyncVar<string>("");
    public readonly SyncVar<int> Kills = new SyncVar<int>(0);
    public readonly SyncVar<float> MoveMagnitude = new SyncVar<float>(0f);
    public readonly SyncVar<bool> IsRunningState = new SyncVar<bool>(false);

    [Header("Settings")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _downedTime = 20f;

    private PlayerView _playerView;
    private Coroutine _downedCoroutine;

    private const int MaxHP = 100;

    private void Awake()
    {
        _playerView = GetComponentInChildren<PlayerView>();

        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
        IsDowned.OnChange += OnIsDownedChanged;
        LifeState.OnChange += OnLifeStateChanged;
        Nickname.OnChange += OnNicknameChanged;
        Kills.OnChange += OnKillsChanged;
        MoveMagnitude.OnChange += OnMovementChanged;
        IsRunningState.OnChange += OnMovementChanged;
    }

    private void OnDestroy()
    {
        HP.OnChange -= OnHpChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
        IsDowned.OnChange -= OnIsDownedChanged;
        LifeState.OnChange -= OnLifeStateChanged;
        Nickname.OnChange -= OnNicknameChanged;
        Kills.OnChange -= OnKillsChanged;
        MoveMagnitude.OnChange -= OnMovementChanged;
        IsRunningState.OnChange -= OnMovementChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RefreshUI();

        if (base.IsOwner)
            StartCoroutine(DelayedNicknameSubmit());
    }

    private IEnumerator DelayedNicknameSubmit()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
    }

    public void TakeDamage(int amount)
    {
        if (!base.IsServerInitialized || amount <= 0 || LifeState.Value == PlayerLifeState.Dead)
            return;

        HP.Value = Mathf.Max(0, HP.Value - amount);

        if (HP.Value <= 0 && LifeState.Value == PlayerLifeState.Alive)
        {
            EnterDownedState();
            return;
        }

        PlayHitObserversRpc();
    }

    public void Heal(int amount)
    {
        if (!base.IsServerInitialized || amount <= 0 || LifeState.Value == PlayerLifeState.Dead)
            return;

        HP.Value = Mathf.Min(MaxHP, HP.Value + amount);
    }

    public void AddKill()
    {
        if (!base.IsServerInitialized)
            return;

        Kills.Value++;
    }

    [ServerRpc]
    public void ReportMovementServerRpc(float moveMagnitude, bool isRunning)
    {
        if (!base.IsServerInitialized || LifeState.Value != PlayerLifeState.Alive)
            return;

        MoveMagnitude.Value = Mathf.Clamp01(moveMagnitude);
        IsRunningState.Value = isRunning;
    }

    [ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{base.OwnerId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    [ObserversRpc]
    private void PlayHitObserversRpc()
    {
        if (_playerView != null)
            _playerView.PlayHit();
    }

    public void EnterDownedState()
    {
        if (!base.IsServerInitialized || LifeState.Value != PlayerLifeState.Alive)
            return;

        StopDownedTimer();

        HP.Value = 0;
        IsAlive.Value = false;
        IsDowned.Value = true;
        LifeState.Value = PlayerLifeState.Downed;

        if (_downedCoroutine != null)
            StopCoroutine(_downedCoroutine);

        _downedCoroutine = StartCoroutine(DownedTimerRoutine());
    }

    public void ReviveFromDowned()
    {
        if (!base.IsServerInitialized || LifeState.Value != PlayerLifeState.Downed)
            return;

        StopDownedTimer();

        HP.Value = MaxHP / 2;
        IsAlive.Value = true;
        IsDowned.Value = false;
        LifeState.Value = PlayerLifeState.Alive;
    }

    public void ForceDeath()
    {
        if (!base.IsServerInitialized || LifeState.Value == PlayerLifeState.Dead)
            return;

        StopDownedTimer();

        HP.Value = 0;
        IsAlive.Value = false;
        IsDowned.Value = false;
        LifeState.Value = PlayerLifeState.Dead;
    }

    public void ResetRoundState()
    {
        if (!base.IsServerInitialized)
            return;

        StopDownedTimer();

        HP.Value = MaxHP;
        IsAlive.Value = true;
        IsDowned.Value = false;
        LifeState.Value = PlayerLifeState.Alive;
        Kills.Value = 0;
        MoveMagnitude.Value = 0f;
        IsRunningState.Value = false;

        CharacterController cc = null;
        bool hasController = TryGetComponent(out cc);

        if (hasController)
            cc.enabled = false;

        Vector3 targetPosition = transform.position;
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, _spawnPoints.Length);
            if (_spawnPoints[idx] != null)
                targetPosition = _spawnPoints[idx].position;
        }
        else
        {
            Transform namedSpawn = GameObject.Find($"PlayerSpawn_{Random.Range(1, 3)}")?.transform;
            if (namedSpawn != null)
                targetPosition = namedSpawn.position;
        }

        transform.position = targetPosition;

        if (hasController && cc != null)
            cc.enabled = true;

        if (TryGetComponent(out PlayerShooting shooting))
            shooting.RestoreAmmo();

        RefreshUI();
    }

    public bool IsControllable()
    {
        return LifeState.Value == PlayerLifeState.Alive;
    }

    public void RefreshUI()
    {
        UpdateHPLocal(HP.Value);
        UpdateNicknameLocal(Nickname.Value);
        UpdateLifeStateLocal(LifeState.Value);
        UpdateMovementLocal(MoveMagnitude.Value, IsRunningState.Value);
    }

    private void StopDownedTimer()
    {
        if (_downedCoroutine != null)
        {
            StopCoroutine(_downedCoroutine);
            _downedCoroutine = null;
        }
    }

    private IEnumerator DownedTimerRoutine()
    {
        yield return new WaitForSeconds(_downedTime);

        if (base.IsServerInitialized && LifeState.Value == PlayerLifeState.Downed)
            ForceDeath();

        _downedCoroutine = null;
    }

    private void UpdateHPLocal(int value)
    {
        if (_playerView != null)
        {
            _playerView.UpdateHP(value);
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text txt in texts)
        {
            if (txt.gameObject.name == "HpText")
            {
                txt.text = $"HP: {value}";
                break;
            }
        }
    }

    private void UpdateNicknameLocal(string name)
    {
        if (_playerView != null)
        {
            _playerView.UpdateNickname(name);
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text txt in texts)
        {
            if (txt.gameObject.name == "NicknameText")
            {
                txt.text = name;
                break;
            }
        }
    }

    private void UpdateLifeStateLocal(PlayerLifeState state)
    {
        if (_playerView != null)
        {
            _playerView.UpdateLifeState(state);
            return;
        }

        ToggleVisuals(state);
    }

    private void ToggleVisuals(PlayerLifeState state)
    {
        bool visible = state != PlayerLifeState.Dead;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                renderer.enabled = visible;
        }

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic ui in graphics)
                ui.enabled = visible;
        }

        // Важно: при Downed коллайдер оставляем включённым,
        // чтобы напарник мог навести луч и зажать E для revive.
        if (TryGetComponent(out CharacterController cc))
            cc.enabled = state != PlayerLifeState.Dead;
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        UpdateHPLocal(next);

        if (asServer && next <= 0 && LifeState.Value == PlayerLifeState.Alive)
            EnterDownedState();
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
    }

    private void OnIsDownedChanged(bool prev, bool next, bool asServer)
    {
    }

    private void OnLifeStateChanged(PlayerLifeState prev, PlayerLifeState next, bool asServer)
    {
        if (_playerView != null && prev == PlayerLifeState.Downed && next == PlayerLifeState.Alive)
            _playerView.PlayRevive();

        UpdateLifeStateLocal(next);
    }

    private void OnNicknameChanged(string prev, string next, bool asServer)
    {
        UpdateNicknameLocal(next);
    }

    private void OnKillsChanged(int prev, int next, bool asServer)
    {
    }

    private void OnMovementChanged(float prev, float next, bool asServer)
    {
        UpdateMovementLocal(MoveMagnitude.Value, IsRunningState.Value);
    }

    private void OnMovementChanged(bool prev, bool next, bool asServer)
    {
        UpdateMovementLocal(MoveMagnitude.Value, IsRunningState.Value);
    }

    private void UpdateMovementLocal(float moveMagnitude, bool running)
    {
        if (_playerView != null)
            _playerView.SetMoveState(moveMagnitude, running);
    }
}
