using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _bulletPrefab; 
    [SerializeField] private int _maxAmmo = 10; 
    [SerializeField] private float _cooldown = 0.5f;

    private int _currentAmmo;
    private float _lastShootTime;

    public override void OnNetworkSpawn()
    {
        if (IsOwner) _currentAmmo = _maxAmmo;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space) && _currentAmmo > 0 && Time.time > _lastShootTime + _cooldown)
        {
            _currentAmmo--;
            _lastShootTime = Time.time;
            ShootServerRpc();
            Debug.Log($"Выстрел! Патронов осталось: {_currentAmmo}");
        }
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        // Смещение, чтобы пуля не застревала в игроке
        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 1.5f;
        GameObject bullet = Instantiate(_bulletPrefab, spawnPos, transform.rotation);
        
        if (bullet.TryGetComponent(out Projectile projectile))
        {
            projectile.Init(OwnerClientId);
        }

        bullet.GetComponent<NetworkObject>().Spawn();
    }
}