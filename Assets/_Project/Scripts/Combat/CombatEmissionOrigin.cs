using UnityEngine;

// A rig binding shared by combat geometry and presentation; neither owns the other.
public sealed class CombatEmissionOrigin : MonoBehaviour
{
    [SerializeField] private Transform skillHand;
    public Transform SkillHand => skillHand;
    public void Bind(Transform hand) => skillHand = hand;

    public Vector3 Resolve(PlayerCombatSettings.ActionData action, int facing, int pulse)
    {
        Vector3 point = action.hitFromHand && skillHand != null ? skillHand.position :
            transform.position + new Vector3(facing * action.hitOrigin.x, action.hitOrigin.y + pulse * action.hitPulseHeight, 0f);
        point.z = transform.position.z; // Native combat physics stays on the X/Y plane.
        return point;
    }

    public static Vector3 Direction(PlayerCombatSettings.ActionData action, int facing, int pulse)
    {
        float angle = (action.hitFirstAngle + pulse * action.hitAngleStep) * Mathf.Deg2Rad;
        return new Vector3(facing * Mathf.Cos(angle), Mathf.Sin(angle), 0f);
    }
}
