using FishNet.Object;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{
    private PlayerInputs inputs;
    private Animator animator;

    private const string AttackAnim = "Attack";

    public override void OnStartClient()
    {
        if (!IsOwner) return;
        base.OnStartClient();
        inputs = GetComponent<PlayerInputs>();
        animator = GetComponentInChildren<Animator>();
        inputs.OnAttackInput += HandleAttack;
    }

    private void HandleAttack()
    {
        animator.Play(AttackAnim);
    }
}
