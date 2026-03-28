using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;

    // Этот метод вызывается Unity автоматически, когда один коллайдер входит в другой
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsOwner) return;
        if (hit.gameObject.CompareTag("Player"))
        {
            PlayerNetwork target = hit.gameObject.GetComponent<PlayerNetwork>();
            if (target != null && target != _playerNetwork)
            {
                DealDamageServerRpc(target.NetworkObjectId, _damage);
            }
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetObjectId, int damage)
    {
        // Сервер находит объект по ID
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
            return;

        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();
        
        if (targetPlayer != null)
        {
            // Вычитаем HP и ограничиваем снизу нулем (Mathf.Max)
            int nextHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
            targetPlayer.HP.Value = nextHp;
        }
    }
}