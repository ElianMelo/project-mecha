using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private PlayerInputs inputs;
    private Animator animator;

    private const string AttackAnim = "Attack";

    void Start()
    {
        inputs = GetComponent<PlayerInputs>();
        animator = GetComponentInChildren<Animator>();
        inputs.OnAttackInput += HandleAttack;
    }

    private void HandleAttack()
    {
        animator.Play(AttackAnim);
    }
}
