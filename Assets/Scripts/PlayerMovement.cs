using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _runMultiplier = 1.35f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _cc;
    private PlayerNetwork _playerNetwork;
    private PlayerView _playerView;
    private float _verticalVelocity;
    private float _lastSentMagnitude = -1f;
    private bool _lastSentRunning;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerNetwork = GetComponent<PlayerNetwork>();
        _playerView = GetComponentInChildren<PlayerView>();
    }

    private void Update()
    {
        if (!base.IsOwner || _playerNetwork == null || !_playerNetwork.IsControllable())
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool running = Input.GetKey(KeyCode.LeftShift);

        Vector3 move = transform.right * h + transform.forward * v;
        float moveSpeed = _speed * (running ? _runMultiplier : 1f);
        move = move.normalized * moveSpeed;

        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += _gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        _cc.Move(move * Time.deltaTime);

        if (_playerView != null)
            _playerView.SetMoveState(new Vector2(h, v).magnitude, running);

        float magnitude = new Vector2(h, v).magnitude;
        if (Mathf.Abs(magnitude - _lastSentMagnitude) > 0.05f || running != _lastSentRunning)
        {
            _lastSentMagnitude = magnitude;
            _lastSentRunning = running;
            _playerNetwork.ReportMovementServerRpc(magnitude, running);
        }
    }
}
