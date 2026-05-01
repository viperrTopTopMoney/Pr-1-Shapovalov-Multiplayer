using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 25f;
    [SerializeField] private int _damage = 25;
    private int _ownerId; // В FishNet ID игрока - это int

    public void Init(int ownerId)
    {
        _ownerId = ownerId;
        // На сервере уничтожаем объект через 3 секунды
        if (base.IsServerInitialized) Invoke(nameof(Despawn), 3f);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        if (other.CompareTag("Player"))
        {
            var target = other.GetComponentInParent<PlayerNetwork>();
            
            // Сравниваем ID владельца
            if (target != null && target.OwnerId != _ownerId)
            {
                target.TakeDamage(_damage);
                Despawn();
            }
        }
        else if (!other.isTrigger) 
        {
            Despawn();
        }
    }

    private void Despawn() => base.Despawn();
}