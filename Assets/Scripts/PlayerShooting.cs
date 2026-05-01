using FishNet.Object;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _bulletPrefab; 
    [SerializeField] private int _maxAmmo = 10; 
    [SerializeField] private float _cooldown = 0.5f;

    private int _currentAmmo;
    private float _lastShootTime;

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient) _currentAmmo = _maxAmmo;
    }

    private void Update()
    {
        if (!base.IsOwner) return; 

        if (Input.GetKeyDown(KeyCode.Space) && _currentAmmo > 0 && Time.time > _lastShootTime + _cooldown)
        {
            _currentAmmo--;
            _lastShootTime = Time.time;
            ShootServerRpc();
        }
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 1.5f;
        GameObject bullet = Instantiate(_bulletPrefab, spawnPos, transform.rotation);
        
        if (bullet.TryGetComponent(out Projectile projectile))
        {
            projectile.Init(base.OwnerId);
        }

        // Спавн в FishNet
        base.Spawn(bullet);
    }
}