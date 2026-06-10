using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(PlayerNetwork))]
public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private Camera _interactionCamera;
    [SerializeField] private float _interactionDistance = 3f;
    [SerializeField] private LayerMask _interactionMask = ~0;

    private PlayerNetwork _playerNetwork;
    private PlayerView _playerView;
    private PlayerNetwork _reviveTarget;
    private BarricadeController _repairTarget;
    private float _reviveTimer;

    private void Awake()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
        _playerView = GetComponentInChildren<PlayerView>();
    }

    private void Update()
    {
        if (!base.IsOwner || _playerNetwork == null || _playerNetwork.LifeState.Value != PlayerLifeState.Alive)
            return;

        if (_interactionCamera == null)
            _interactionCamera = Camera.main;

        if (!Input.GetKey(KeyCode.E))
        {
            ClearTargets();
            return;
        }

        if (!TryGetTarget(out RaycastHit hit))
        {
            ClearTargets();
            return;
        }

        if (hit.collider == null)
            return;

        PlayerNetwork targetPlayer = hit.collider.GetComponentInParent<PlayerNetwork>();
        if (targetPlayer != null)
        {
            HandleRevive(targetPlayer);
            return;
        }

        BarricadeController barricade = hit.collider.GetComponentInParent<BarricadeController>();
        if (barricade != null)
        {
            HandleRepair(barricade);
            return;
        }

        ClearTargets();
    }

    private bool TryGetTarget(out RaycastHit hit)
    {
        hit = default;
        if (_interactionCamera == null)
            return false;

        Ray ray = new Ray(_interactionCamera.transform.position, _interactionCamera.transform.forward);
        return Physics.Raycast(ray, out hit, _interactionDistance, _interactionMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleRevive(PlayerNetwork targetPlayer)
    {
        if (!ReviveSystem.CanRevive(_playerNetwork, targetPlayer))
        {
            ClearTargets();
            return;
        }

        if (_reviveTarget != targetPlayer)
        {
            _reviveTarget = targetPlayer;
            _repairTarget = null;
            _reviveTimer = 0f;
        }

        _reviveTimer += Time.deltaTime;
        if (_reviveTimer >= ReviveSystem.ReviveHoldSeconds)
        {
            ReviveTargetServerRpc(targetPlayer);
            ClearTargets();
        }
    }

    private void HandleRepair(BarricadeController barricade)
    {
        if (!ReviveSystem.CanRepair(_playerNetwork, barricade))
        {
            ClearTargets();
            return;
        }

        if (_repairTarget != barricade)
        {
            _repairTarget = barricade;
            _reviveTarget = null;
            _reviveTimer = 0f;
        }

        RepairTargetServerRpc(barricade, Time.deltaTime);
    }

    private void ClearTargets()
    {
        _reviveTimer = 0f;
        _reviveTarget = null;
        _repairTarget = null;
    }

    [ServerRpc]
    private void ReviveTargetServerRpc(PlayerNetwork targetPlayer)
    {
        if (!ReviveSystem.CanRevive(_playerNetwork, targetPlayer))
            return;

        ReviveSystem.ApplyRevive(targetPlayer);
    }

    [ServerRpc]
    private void RepairTargetServerRpc(BarricadeController barricade, float deltaTime)
    {
        if (!ReviveSystem.CanRepair(_playerNetwork, barricade))
            return;

        ReviveSystem.ApplyRepair(barricade, deltaTime);
    }
}
