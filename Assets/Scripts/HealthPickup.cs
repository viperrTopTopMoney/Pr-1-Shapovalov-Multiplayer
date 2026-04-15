using Unity.Netcode;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;
    private PickupManager _manager;
    private Vector3 _spawnPosition;

    // Тот самый метод, который искал PickupManager
    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Только сервер обрабатывает подбор аптечки
        if (!IsServer) return;

        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Проверки из практики: мертвый не подбирает, при полном HP не лечим
        if (!player.IsAlive.Value) return;
        if (player.HP.Value >= 100) return;

        // Лечим игрока (не больше 100)
        player.HP.Value = Mathf.Min(100, player.HP.Value + _healAmount);

        // Сообщаем менеджеру, что аптечка подобрана, чтобы он запустил таймер респавна
        if (_manager != null)
        {
            _manager.OnPickedUp(_spawnPosition);
        }

        // Удаляем аптечку из сети
        GetComponent<NetworkObject>().Despawn(true);
    }
    
}