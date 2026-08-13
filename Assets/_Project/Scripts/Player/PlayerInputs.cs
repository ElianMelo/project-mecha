using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : NetworkBehaviour
{
    private Vector2 _moveInput;

    public Vector2 MoveInput => _moveInput;
    public Action OnJumpInput;
    public Action OnDownInput;
    public Action OnAttackInput;

    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        OnJumpInput?.Invoke();
    }

    public void OnDown(InputValue value)
    {
        OnDownInput?.Invoke();
    }

    public void OnAttack(InputValue value)
    {
        OnAttackInput?.Invoke();
    }
}
