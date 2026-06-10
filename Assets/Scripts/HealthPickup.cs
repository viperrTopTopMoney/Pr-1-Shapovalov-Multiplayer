using FishNet.Object;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;

    private PickupManager _manager;
    private Vector3 _spawnPosition;

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized || other == null)
            return;

        PlayerNetwork player = other.GetComponentInParent<PlayerNetwork>();
        if (player == null)
            return;

        if (!player.IsAlive.Value || player.HP.Value >= 100)
            return;

        player.Heal(_healAmount);

        if (_manager != null)
            _manager.OnPickedUp(_spawnPosition);

        base.Despawn();
    }
}
