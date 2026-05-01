using FishNet;
using FishNet.Object;
using UnityEngine;
using System.Collections;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        // Подписка на старт сервера в FishNet
        InstanceFinder.ServerManager.OnServerConnectionState += OnServerState;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerState;
    }

    private void OnServerState(FishNet.Transporting.ServerConnectionStateArgs args)
    {
        // Если сервер запустился (Started)
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            SpawnAll();
        }
    }

    private void SpawnAll()
    {
        foreach (var point in _spawnPoints)
            SpawnPickup(point.position);
    }

    public void OnPickedUp(Vector3 position)
    {
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        GameObject go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        if (go.TryGetComponent(out HealthPickup pickup)) pickup.Init(this);
        
        // Глобальный спавн через InstanceFinder, если скрипт не NetworkBehaviour
        InstanceFinder.ServerManager.Spawn(go);
    }
}