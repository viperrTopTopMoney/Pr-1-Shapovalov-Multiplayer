using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private void Start()
    {
        // Подписываемся на событие запуска сервера
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            
            // Если сервер уже запущен (например, при переходе между сценами)
            if (NetworkManager.Singleton.IsServer) OnServerStarted();
        }
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся, чтобы не было утечек памяти
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void OnServerStarted()
    {
        SpawnAll();
    }

    private void SpawnAll()
    {
        foreach (var point in _spawnPoints)
            SpawnPickup(point.position);
    }

    public void OnPickedUp(Vector3 position)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        var go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        
        // Передаем ссылку на менеджер, чтобы аптечка знала, кому сообщить о "смерти"
        if (go.TryGetComponent(out HealthPickup pickup))
        {
            pickup.Init(this);
        }

        // Регистрируем объект в сети
        go.GetComponent<NetworkObject>().Spawn();
    }
}