using FishNet.Component.Animating;
using FishNet.Object;

public class PlayerAttack : NetworkBehaviour
{
    private PlayerInputs inputs;
    private NetworkAnimator animator;

    private const string AttackAnim = "Attack";

    public override void OnStartClient()
    {
        if (!IsOwner) return;
        base.OnStartClient();
        inputs = GetComponent<PlayerInputs>();
        animator = GetComponentInChildren<NetworkAnimator>();
        inputs.OnAttackInput += HandleAttack;
    }

    private void HandleAttack()
    {
        animator.Play(AttackAnim);
    }
}
