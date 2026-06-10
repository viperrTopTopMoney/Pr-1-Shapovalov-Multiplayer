using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 25f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _lifeTime = 3f;

    private int _ownerId = -1;

    public void Init(int ownerId)
    {
        _ownerId = ownerId;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Invoke(nameof(DespawnProjectile), _lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized || other == null)
            return;

        // Friendly fire в коопе отключён: пуля не наносит урон игрокам.
        if (other.GetComponentInParent<PlayerNetwork>() != null)
            return;

        ZombieNetwork zombie = other.GetComponentInParent<ZombieNetwork>();
        if (zombie != null)
        {
            zombie.TakeDamageServer(_damage, _ownerId);
            DespawnProjectile();
            return;
        }

        // Баррикады не ломаются от выстрелов игроков.
        if (other.GetComponentInParent<BarricadeController>() != null)
        {
            DespawnProjectile();
            return;
        }

        if (!other.isTrigger)
            DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        if (!base.IsServerInitialized)
            return;

        if (base.IsSpawned)
            base.Despawn();
        else
            Destroy(gameObject);
    }
}
