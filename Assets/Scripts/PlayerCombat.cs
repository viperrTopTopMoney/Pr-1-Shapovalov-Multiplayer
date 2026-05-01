using FishNet.Object;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // base.IsOwner вместо IsOwner
        if (!base.IsOwner) return;

        if (hit.gameObject.CompareTag("Player"))
        {
            PlayerNetwork target = hit.gameObject.GetComponent<PlayerNetwork>();
            
            // Проверяем, что цель — это не мы сами и она существует
            if (target != null && target != _playerNetwork)
            {
                // В FishNet можно передавать ссылки на NetworkBehaviour напрямую в RPC!
                DealDamageServerRpc(target, _damage);
            }
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(PlayerNetwork targetPlayer, int damage)
    {
        // Если объект нашелся и он "живой" в сети
        if (targetPlayer != null)
        {
            // Используем метод TakeDamage, который мы уже переписали в PlayerNetwork
            targetPlayer.TakeDamage(damage);
        }
    }
}