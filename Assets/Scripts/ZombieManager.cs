using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ZombieManager : NetworkBehaviour
{
    public static ZombieManager Instance { get; private set; }

    [Header("Zombie Prefabs")]
    [SerializeField] private GameObject _normalZombiePrefab;
    [SerializeField] private GameObject _runnerZombiePrefab;
    [SerializeField] private GameObject _bossZombiePrefab;

    [Header("Spawn")]
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _houseCenterFallback;
    [SerializeField] private int _maxAliveZombies = 60;

    private readonly List<ZombieNetwork> _aliveZombies = new List<ZombieNetwork>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _aliveZombies.Clear();
    }

    public void RegisterZombie(ZombieNetwork zombie)
    {
        if (zombie != null && !_aliveZombies.Contains(zombie))
            _aliveZombies.Add(zombie);
    }

    public void UnregisterZombie(ZombieNetwork zombie)
    {
        if (zombie != null)
            _aliveZombies.Remove(zombie);
    }

    public void NotifyZombieKilled(ZombieNetwork zombie)
    {
        UnregisterZombie(zombie);
        WaveManager.Instance?.NotifyZombieKilled();
    }

    public int SpawnWave(int normalCount, int runnerCount, int bossCount)
    {
        if (!base.IsServerInitialized)
            return 0;

        int spawned = 0;
        spawned += SpawnKind(ZombieKind.Normal, normalCount);
        spawned += SpawnKind(ZombieKind.Runner, runnerCount);
        spawned += SpawnKind(ZombieKind.Boss, bossCount);

        return spawned;
    }

    private int SpawnKind(ZombieKind kind, int count)
    {
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (_aliveZombies.Count >= _maxAliveZombies)
                break;

            GameObject prefab = GetPrefab(kind);
            if (prefab == null)
            {
                Debug.LogError($"[ZombieManager] Missing prefab for {kind} zombie.");
                continue;
            }

            Transform spawnPoint = GetSpawnPoint(i + spawned);
            GameObject zombieObject = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            if (!zombieObject.TryGetComponent(out ZombieNetwork zombieNetwork))
            {
                Debug.LogError($"[ZombieManager] Zombie prefab '{prefab.name}' is missing ZombieNetwork.");
                Destroy(zombieObject);
                continue;
            }

            ApplyStats(zombieNetwork, kind);
            base.Spawn(zombieObject);
            spawned++;
        }

        return spawned;
    }

    private void ApplyStats(ZombieNetwork zombie, ZombieKind kind)
    {
        switch (kind)
        {
            case ZombieKind.Normal:
                zombie.Configure(kind, maxHP: 2, moveSpeed: 1.2f, barricadeDamage: 5, playerDamage: 15, modelScale: 1f);
                break;

            case ZombieKind.Runner:
                zombie.Configure(kind, maxHP: 1, moveSpeed: 3.8f, barricadeDamage: 3, playerDamage: 20, modelScale: 1f);
                break;

            case ZombieKind.Boss:
                zombie.Configure(kind, maxHP: 5, moveSpeed: 2.1f, barricadeDamage: 15, playerDamage: 30, modelScale: 1.4f);
                break;
        }
    }

    private GameObject GetPrefab(ZombieKind kind)
    {
        return kind switch
        {
            ZombieKind.Normal => _normalZombiePrefab,
            ZombieKind.Runner => _runnerZombiePrefab,
            ZombieKind.Boss => _bossZombiePrefab,
            _ => _normalZombiePrefab
        };
    }

    private Transform GetSpawnPoint(int index)
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int safeIndex = Mathf.Abs(index) % _spawnPoints.Length;
            if (_spawnPoints[safeIndex] != null)
                return _spawnPoints[safeIndex];
        }

        Transform namedSpawn = GameObject.Find($"ZombieSpawn_{Mathf.Abs(index % 8) + 1}")?.transform;
        if (namedSpawn != null)
            return namedSpawn;

        if (_houseCenterFallback != null)
            return _houseCenterFallback;

        return transform;
    }

    public Transform GetTargetForZombie(ZombieNetwork zombie)
    {
        if (zombie == null)
            return _houseCenterFallback != null ? _houseCenterFallback : transform;

        DoorController door = FindOne<DoorController>();
        WindowController[] windows = FindAll<WindowController>();

        BarricadeController bestTarget = null;
        float bestDistance = float.MaxValue;

        if (door != null && !door.IsDestroyed)
        {
            bestTarget = door;
            bestDistance = Vector3.Distance(zombie.transform.position, door.transform.position);
        }

        foreach (WindowController window in windows)
        {
            if (window == null || window.IsDestroyed)
                continue;

            float distance = Vector3.Distance(zombie.transform.position, window.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = window;
            }
        }

        if (bestTarget != null)
            return bestTarget.transform;

        Transform namedHouseCenter = GameObject.Find("HouseCenter_TARGET")?.transform;
        if (namedHouseCenter != null)
            return namedHouseCenter;

        return _houseCenterFallback != null ? _houseCenterFallback : transform;
    }

    public void ClearAllServer()
    {
        if (!base.IsServerInitialized)
            return;

        foreach (ZombieNetwork zombie in new List<ZombieNetwork>(_aliveZombies))
        {
            if (zombie == null)
                continue;

            if (zombie.IsSpawned)
                zombie.Despawn();
            else
                Destroy(zombie.gameObject);
        }

        _aliveZombies.Clear();
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
