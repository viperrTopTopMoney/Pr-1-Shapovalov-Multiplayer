using System.Collections.Generic;
using FishNet;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HouseIntrusionZone : MonoBehaviour
{
    private readonly HashSet<ZombieNetwork> _zombiesInside = new HashSet<ZombieNetwork>();

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        ZombieNetwork zombie = other.GetComponentInParent<ZombieNetwork>();
        if (zombie != null)
            _zombiesInside.Add(zombie);
    }

    private void OnTriggerExit(Collider other)
    {
        ZombieNetwork zombie = other.GetComponentInParent<ZombieNetwork>();
        if (zombie != null)
            _zombiesInside.Remove(zombie);
    }

    private void Update()
    {
        if (!InstanceFinder.IsServerStarted)
            return;

        CleanupDeadZombies();

        if (GameManager.Instance == null)
            return;

        if (_zombiesInside.Count > 0)
            GameManager.Instance.EnterIntruderWarning();
        else
            GameManager.Instance.NotifyIntruderResolved();
    }

    private void CleanupDeadZombies()
    {
        _zombiesInside.RemoveWhere(zombie => zombie == null || !zombie.Alive);
    }
}
