using UnityEngine;

public class PlayerCombatVfx : MonoBehaviour
{
    private static readonly Color Ruby = new Color(1f, 0.18f, 0.12f);
    private static readonly Color Fire = new Color(1f, 0.62f, 0.08f);
    private static readonly Color Violet = new Color(0.65f, 0.3f, 1f);
    public void Begin(PlayerAttack.ActionKind kind, int facing)
    {
        if (PlayerAttack.IsSkill(kind)) CombatEffect.Burst(transform.position + Vector3.up * 0.65f, kind == PlayerAttack.ActionKind.CriticalX ? Violet : Ruby, 12);
    }
    public void Strike(PlayerAttack.ActionKind kind, int facing, int pulse)
    {
        Vector3 center = transform.position + new Vector3(0f, 0.65f, -0.35f);
        if (kind == PlayerAttack.ActionKind.RisingSlash)
        {
            CombatEffect.Arc(center, facing, 1.1f, -65f, 100f, new Color(0.3f, 0.85f, 1f), 0.13f);
            CombatEffect.Arc(center, facing, 0.85f, -50f, 95f, Color.white, 0.04f);
        }
        else if (kind == PlayerAttack.ActionKind.SwordFire)
        {
            center.x += facing * (1.7f + pulse * 0.7f);
            CombatEffect.Arc(center, facing, 0.7f, -80f, 80f, Fire, 0.2f, Vector3.right * facing * 2f);
            CombatEffect.Burst(center, Fire, 16);
        }
        else if (kind == PlayerAttack.ActionKind.CriticalX)
        {
            center += new Vector3(facing * 0.7f, 0.4f, 0f);
            CombatEffect.Arc(center, facing, 1.6f, pulse == 0 ? -140f : 40f, pulse == 0 ? 40f : 220f, Violet, 0.2f);
            CombatEffect.Arc(center, facing, 1.2f, -75f, 105f, Color.white, 0.07f);
        }
        else
        {
            bool alternate = pulse % 2 == 1 || kind == PlayerAttack.ActionKind.Slash2;
            CombatEffect.Arc(center, facing, kind == PlayerAttack.ActionKind.MegaSlash ? 1.2f : 0.95f, alternate ? -85f : -35f, alternate ? 55f : 95f, PlayerAttack.IsSkill(kind) ? Ruby : Fire);
            if (kind == PlayerAttack.ActionKind.MegaSlash && pulse == 3) CombatEffect.Burst(center + Vector3.right * facing * 0.8f, Ruby, 28);
        }
    }
    public void Dash(int facing)
    {
        Vector3 center = transform.position + Vector3.up * 0.5f + Vector3.back * 0.3f;
        CombatEffect.Arc(center, facing, 0.5f, 110f, 250f, new Color(0.2f, 0.8f, 1f), 0.08f, Vector3.left * facing);
    }
    public void Hit(Vector3 position) => CombatEffect.Burst(position, Ruby, 18);
}
