using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class WaveManager : NetworkBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private float _preparationTime = 15f;

    public readonly SyncVar<int> CurrentWave = new SyncVar<int>(0);
    public readonly SyncVar<int> RemainingZombies = new SyncVar<int>(0);
    public readonly SyncVar<float> PreparationTimer = new SyncVar<float>(0f);
    public readonly SyncVar<bool> WaveRunning = new SyncVar<bool>(false);

    private static readonly (int normal, int runner, int boss)[] _waves =
    {
        (10, 0, 0),
        (15, 0, 0),
        (15, 5, 0),
        (20, 10, 0),
        (15, 10, 1)
    };

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentWave.Value = 0;
        RemainingZombies.Value = 0;
        WaveRunning.Value = false;
        PreparationTimer.Value = 0f;
    }

    private void Update()
    {
        if (!base.IsServerInitialized)
            return;

        if (!WaveRunning.Value && PreparationTimer.Value > 0f)
        {
            PreparationTimer.Value = Mathf.Max(0f, PreparationTimer.Value - Time.deltaTime);
        }
    }

    public void StartFirstWave()
    {
        if (!base.IsServerInitialized)
            return;

        StartWave(1);
    }

    public void StartNextWave()
    {
        if (!base.IsServerInitialized)
            return;

        int nextWave = Mathf.Clamp(CurrentWave.Value + 1, 1, _waves.Length);
        StartWave(nextWave);
    }

    public void StartWave(int waveNumber)
    {
        if (!base.IsServerInitialized)
            return;

        if (waveNumber < 1 || waveNumber > _waves.Length)
            return;

        CurrentWave.Value = waveNumber;
        WaveRunning.Value = true;
        PreparationTimer.Value = 0f;

        var definition = _waves[waveNumber - 1];
        int spawned = ZombieManager.Instance != null
            ? ZombieManager.Instance.SpawnWave(definition.normal, definition.runner, definition.boss)
            : 0;

        if (spawned <= 0)
        {
            Debug.LogError("Wave cannot start because no zombie prefabs were spawned.");
            RemainingZombies.Value = 0;
            WaveRunning.Value = false;
            GameManager.Instance?.ResetServerState();
            return;
        }

        RemainingZombies.Value = spawned;
        GameManager.Instance?.OnWaveStarted(waveNumber, spawned);
    }

    public void NotifyZombieKilled()
    {
        if (!base.IsServerInitialized)
            return;

        RemainingZombies.Value = Mathf.Max(0, RemainingZombies.Value - 1);
        if (GameManager.Instance != null)
            GameManager.Instance.RemainingZombies.Value = RemainingZombies.Value;
        if (RemainingZombies.Value <= 0)
            GameManager.Instance?.OnWaveCleared();
    }

    public void BeginPreparation()
    {
        if (!base.IsServerInitialized)
            return;

        WaveRunning.Value = false;
        PreparationTimer.Value = _preparationTime;
    }

    public bool HasMoreWaves => CurrentWave.Value < _waves.Length;
    public int LastWaveReached => CurrentWave.Value;
}
