using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private CharacterController _controller;

    private void Update()
    {
        // Двигаться может только владелец
        if (!IsOwner) return;

        Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 moveDirection = transform.TransformDirection(moveInput);
            _controller.Move(moveDirection * _moveSpeed * Time.deltaTime);
        }
    }
}