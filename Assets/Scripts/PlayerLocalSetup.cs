using Unity.Netcode;
using UnityEngine;

public class PlayerLocalSetup : NetworkBehaviour
{
    [SerializeField] private Camera _playerCamera;

    public override void OnNetworkSpawn()
    {
        // Включаем камеру только если этот объект — наш локальный персонаж (Owner)
        if (_playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(IsOwner);
        }
    }
}