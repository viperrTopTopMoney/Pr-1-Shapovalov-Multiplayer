using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        ShowingResults
    }

    [Header("Настройки матча")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;

    public int RequiredPlayers => _requiredPlayers;

    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(60f);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.WaitingForPlayers;
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        if (ServerManager != null && ServerManager.Clients != null)
        {
            ConnectedPlayers.Value = ServerManager.Clients.Count;
        }

        if (CurrentState.Value == GameState.WaitingForPlayers)
        {
            if (ConnectedPlayers.Value >= _requiredPlayers)
            {
                CurrentState.Value = GameState.InProgress;
            }
        }
        else if (CurrentState.Value == GameState.InProgress)
        {
            MatchTimer.Value -= Time.deltaTime;
            if (MatchTimer.Value <= 0f)
            {
                CurrentState.Value = GameState.ShowingResults;
                StartCoroutine(ResetLobbyTimer(5f));
            }
        }
    }

    private IEnumerator ResetLobbyTimer(float delay)
    {
        yield return new WaitForSeconds(delay);

        // ХУК НОВОГО РАУНДА: Находим всех сетевых игроков и сбрасываем их состояние на сервере
        PlayerNetwork[] allPlayers = FindObjectsOfType<PlayerNetwork>();
        foreach (PlayerNetwork player in allPlayers)
        {
            player.ResetRoundState();
        }

        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.WaitingForPlayers;
    }
}