using FishNet.Object;
using UnityEngine;

public class PlayerLocalSetup : NetworkBehaviour
{
    [SerializeField] private Camera _playerCamera;

    public override void OnStartNetwork()
    {
        // Включаем камеру только для владельца
        if (_playerCamera != null)
        {
            _playerCamera.gameObject.SetActive(base.Owner.IsLocalClient);
        }
    }
}