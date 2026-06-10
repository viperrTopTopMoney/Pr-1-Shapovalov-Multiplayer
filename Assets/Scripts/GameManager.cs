using System.Collections;
using System.Text;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Match")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _preparationDuration = 15f;
    [SerializeField] private float _intruderCountdownDuration = 10f;
    [SerializeField] private float _resultsDelay = 5f;

    [Header("Fallbacks")]
    [SerializeField] private Transform _houseCenterFallback;

    public int RequiredPlayers => _requiredPlayers;
    public bool CanHostStartGame => base.IsServerInitialized && ConnectedPlayers.Value >= _requiredPlayers && CurrentState.Value == MatchFlowState.Lobby;
    public bool IntruderWarningActive => CurrentState.Value == MatchFlowState.IntruderWarning;

    public readonly SyncVar<MatchFlowState> CurrentState = new SyncVar<MatchFlowState>(MatchFlowState.Lobby);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> StateTimer = new SyncVar<float>(0f);
    public readonly SyncVar<int> CurrentWave = new SyncVar<int>(0);
    public readonly SyncVar<int> RemainingZombies = new SyncVar<int>(0);
    public readonly SyncVar<bool> MatchHasEnded = new SyncVar<bool>(false);

    private Coroutine _resultsCoroutine;
    private MatchFlowState _stateBeforeIntruder = MatchFlowState.InWave;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ResetServerState();

        if (ServerManager != null)
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (ServerManager != null)
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;

        if (_resultsCoroutine != null)
        {
            StopCoroutine(_resultsCoroutine);
            _resultsCoroutine = null;
        }
    }

    private void Update()
    {
        if (!base.IsServerInitialized)
            return;

        UpdateConnectedPlayersCount();

        switch (CurrentState.Value)
        {
            case MatchFlowState.Preparation:
                TickTimer(_preparationDuration, BeginNextWave);
                break;

            case MatchFlowState.IntruderWarning:
                TickTimer(_intruderCountdownDuration, TriggerDefeat);
                break;
        }

        if (IsGameplayState(CurrentState.Value) && AreAllPlayersDead())
            TriggerDefeat();
    }

    private void OnRemoteConnectionState(FishNet.Connection.NetworkConnection connection, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (!base.IsServerInitialized)
            return;

        UpdateConnectedPlayersCount();

        if (ConnectedPlayers.Value == 0)
            ResetServerState();
    }

    private void UpdateConnectedPlayersCount()
    {
        ConnectedPlayers.Value = ServerManager != null && ServerManager.Clients != null ? ServerManager.Clients.Count : 0;
    }

    private static bool IsGameplayState(MatchFlowState state)
    {
        return state == MatchFlowState.InWave ||
               state == MatchFlowState.Preparation ||
               state == MatchFlowState.IntruderWarning;
    }

    public void StartMatchFromLobby()
    {
        if (!base.IsServerInitialized || !CanHostStartGame)
            return;

        MatchHasEnded.Value = false;
        CurrentWave.Value = 0;
        RemainingZombies.Value = 0;
        StateTimer.Value = 0f;
        CurrentState.Value = MatchFlowState.InWave;

        ZombieManager.Instance?.ClearAllServer();
        ResetRoundStateForAllPlayers();

        if (WaveManager.Instance != null)
            WaveManager.Instance.StartFirstWave();
        else
            Debug.LogError("[GameManager] WaveManager not found.");
    }

    public void OnWaveStarted(int waveNumber, int totalZombies)
    {
        if (!base.IsServerInitialized)
            return;

        CurrentWave.Value = waveNumber;
        RemainingZombies.Value = totalZombies;
        CurrentState.Value = MatchFlowState.InWave;
        StateTimer.Value = 0f;
    }

    public void OnWaveCleared()
    {
        if (!base.IsServerInitialized || MatchHasEnded.Value)
            return;

        if (CurrentWave.Value >= 5)
        {
            TriggerVictory();
            return;
        }

        CurrentState.Value = MatchFlowState.Preparation;
        StateTimer.Value = _preparationDuration;
        WaveManager.Instance?.BeginPreparation();
    }

    public void EnterIntruderWarning()
    {
        if (!base.IsServerInitialized)
            return;

        if (CurrentState.Value == MatchFlowState.Victory ||
            CurrentState.Value == MatchFlowState.Defeat ||
            CurrentState.Value == MatchFlowState.Lobby)
            return;

        if (CurrentState.Value == MatchFlowState.IntruderWarning)
            return;

        _stateBeforeIntruder = CurrentState.Value;
        CurrentState.Value = MatchFlowState.IntruderWarning;
        StateTimer.Value = _intruderCountdownDuration;
    }

    public void ClearIntruderWarning()
    {
        if (!base.IsServerInitialized)
            return;

        if (CurrentState.Value != MatchFlowState.IntruderWarning)
            return;

        CurrentState.Value = _stateBeforeIntruder == MatchFlowState.IntruderWarning
            ? MatchFlowState.InWave
            : _stateBeforeIntruder;

        StateTimer.Value = CurrentState.Value == MatchFlowState.Preparation ? Mathf.Max(1f, StateTimer.Value) : 0f;
    }

    public void NotifyZombieKilled()
    {
        if (!base.IsServerInitialized)
            return;

        RemainingZombies.Value = Mathf.Max(0, RemainingZombies.Value - 1);

        if (RemainingZombies.Value <= 0)
            OnWaveCleared();
    }

    public void AwardKill(int ownerId)
    {
        if (!base.IsServerInitialized || ownerId < 0)
            return;

        PlayerNetwork[] players = FindAll<PlayerNetwork>();
        foreach (PlayerNetwork player in players)
        {
            if (player != null && player.OwnerId == ownerId)
            {
                player.AddKill();
                break;
            }
        }
    }

    public void NotifyIntruderResolved()
    {
        if (!base.IsServerInitialized)
            return;

        ClearIntruderWarning();
    }

    public void TriggerVictory()
    {
        if (!base.IsServerInitialized || MatchHasEnded.Value)
            return;

        MatchHasEnded.Value = true;
        CurrentState.Value = MatchFlowState.Victory;
        StateTimer.Value = _resultsDelay;
        BeginResultsCountdown();
    }

    public void TriggerDefeat()
    {
        if (!base.IsServerInitialized || MatchHasEnded.Value)
            return;

        MatchHasEnded.Value = true;
        CurrentState.Value = MatchFlowState.Defeat;
        StateTimer.Value = _resultsDelay;
        BeginResultsCountdown();
    }

    private void BeginResultsCountdown()
    {
        if (_resultsCoroutine != null)
            StopCoroutine(_resultsCoroutine);

        _resultsCoroutine = StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(_resultsDelay);

        ZombieManager.Instance?.ClearAllServer();
        ResetRoundStateForAllPlayers();
        ResetServerState();
        SessionManager.Instance?.LoadLobbyScene();

        _resultsCoroutine = null;
    }

    public void ResetServerState()
    {
        MatchHasEnded.Value = false;
        CurrentState.Value = MatchFlowState.Lobby;
        UpdateConnectedPlayersCount();
        StateTimer.Value = 0f;
        CurrentWave.Value = 0;
        RemainingZombies.Value = 0;
        _stateBeforeIntruder = MatchFlowState.InWave;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.CurrentWave.Value = 0;
            WaveManager.Instance.RemainingZombies.Value = 0;
            WaveManager.Instance.PreparationTimer.Value = 0f;
            WaveManager.Instance.WaveRunning.Value = false;
        }
    }

    private void ResetRoundStateForAllPlayers()
    {
        PlayerNetwork[] allPlayers = FindAll<PlayerNetwork>();
        foreach (PlayerNetwork player in allPlayers)
        {
            if (player != null)
                player.ResetRoundState();
        }
    }

    private void TickTimer(float duration, System.Action onFinished)
    {
        if (StateTimer.Value <= 0f)
            StateTimer.Value = duration;

        StateTimer.Value = Mathf.Max(0f, StateTimer.Value - Time.deltaTime);

        if (StateTimer.Value <= 0f)
        {
            StateTimer.Value = 0f;
            onFinished?.Invoke();
        }
    }

    public void BeginNextWave()
    {
        if (!base.IsServerInitialized || MatchHasEnded.Value)
            return;

        CurrentState.Value = MatchFlowState.InWave;
        StateTimer.Value = 0f;
        WaveManager.Instance?.StartNextWave();
    }

    private bool AreAllPlayersDead()
    {
        PlayerNetwork[] players = FindAll<PlayerNetwork>();
        if (players.Length == 0)
            return false;

        foreach (PlayerNetwork player in players)
        {
            if (player != null && player.LifeState.Value != PlayerLifeState.Dead)
                return false;
        }

        return true;
    }

    public Transform GetHouseTargetFallback()
    {
        if (_houseCenterFallback != null)
            return _houseCenterFallback;

        GameObject houseCenter = GameObject.Find("HouseCenter_TARGET");
        if (houseCenter != null)
            return houseCenter.transform;

        return transform;
    }

    public string FormatStateTimer()
    {
        switch (CurrentState.Value)
        {
            case MatchFlowState.Preparation:
                return $"Preparation: {Mathf.CeilToInt(StateTimer.Value)}";

            case MatchFlowState.IntruderWarning:
                return $"Intruder: {Mathf.CeilToInt(StateTimer.Value)}";

            case MatchFlowState.Victory:
                return "Victory";

            case MatchFlowState.Defeat:
                return "Defeat";

            default:
                return "Lobby";
        }
    }

    public string FormatIntruderTimer()
    {
        return CurrentState.Value == MatchFlowState.IntruderWarning
            ? $"INTRUDER {Mathf.CeilToInt(StateTimer.Value)}"
            : string.Empty;
    }

    public string FormatDoorText()
    {
        DoorController door = FindOne<DoorController>();
        return door != null ? $"Door {door.CurrentHP.Value}/{door.MaxHP}" : "Door n/a";
    }

    public string FormatWindowText()
    {
        WindowController[] windows = FindAll<WindowController>();
        if (windows == null || windows.Length == 0)
            return "Windows n/a";

        int total = 0;
        int max = 0;

        foreach (WindowController window in windows)
        {
            if (window == null)
                continue;

            total += window.CurrentHP.Value;
            max += window.MaxHP;
        }

        return $"Windows {total}/{max}";
    }

    public string FormatPlayersSummary()
    {
        PlayerNetwork[] players = FindAll<PlayerNetwork>();
        if (players == null || players.Length == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (PlayerNetwork player in players)
        {
            if (player == null)
                continue;

            sb.AppendLine($"{player.Nickname.Value} {player.LifeState.Value} HP:{player.HP.Value} K:{player.Kills.Value}");
        }

        return sb.ToString();
    }

    public string FormatResultsText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(CurrentState.Value == MatchFlowState.Victory ? "VICTORY" : "DEFEAT");
        sb.AppendLine($"Wave: {CurrentWave.Value}");

        PlayerNetwork[] players = FindAll<PlayerNetwork>();
        foreach (PlayerNetwork player in players)
        {
            if (player == null)
                continue;

            sb.AppendLine($"{player.Nickname.Value}: {player.Kills.Value} kills");
        }

        return sb.ToString();
    }

    private static T[] FindAll<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
    }

    private static T FindOne<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindFirstObjectByType<T>();
    }
}
