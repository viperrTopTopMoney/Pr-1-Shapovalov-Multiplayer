using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 25f;
    [SerializeField] private int _damage = 25;
    private ulong _ownerId;

    public void Init(ulong ownerId)
    {
        _ownerId = ownerId;
        if (IsServer) Invoke(nameof(Despawn), 3f);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // ЛОГИКА ПО ТЕГУ "Player"
        if (other.CompareTag("Player"))
        {
            var target = other.GetComponentInParent<PlayerNetwork>();
            
            if (target != null && target.OwnerClientId != _ownerId)
            {
                target.TakeDamage(_damage);
                Debug.Log($"<color=red>УРОН НАНЕСЕН!</color> Попали в игрока {target.OwnerClientId}");
                Despawn();
            }
        }
        else if (!other.isTrigger) // Попали в стену
        {
            Despawn();
        }
    }

    private void Despawn() => NetworkObject.Despawn();
}