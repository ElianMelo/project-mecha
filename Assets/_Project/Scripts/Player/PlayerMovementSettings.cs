using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Movement")]
public class PlayerMovementSettings : ScriptableObject
{
    public enum MovementStyle { Dasher, Agile }
    public MovementStyle style;
    [Header("Agile traversal")]
    public bool runByDefault, directionalDash, variableJump;
    public float jumpCutGravity = 65f;
    public float walkSpeed = 4.5f, runSpeed = 7f, jumpSpeed = 10f;
    public float dashSpeed = 11.5f, dashDuration = 0.18f, dashCooldown = 0.12f, dashBuffer = 0.14f;
    public float dashNetworkGrace = 0.08f;
    public float risingGravity = 24f, fallingGravity = 40f, apexGravity = 14f, terminalVelocity = 20f;
    public float coyoteTime = 0.1f, jumpBuffer = 0.12f, apexSpeed = 1.5f;
    public float groundAcceleration = 90f, airAcceleration = 45f, airAttackAcceleration = 20f;
    public float rocketMomentum = 0.28f, dropSpeed = 4f, fastFallSpeed = 6f;
    [Header("Impulse cadence (seconds, not render frames)")]
    public float rocketSpeed = 14f, rocketRiseSpeed = 7.8f, rocketDuration = 0.25f;
    public float mushidonSpeed = 18.5f, mushidonDuration = 0.36f, mushidonFallSpeed = 12f;
    public float chordWindow = 0.0833f, mushidonWindow = 0.25f, brakeLock = 0.0667f;
    public float impulseEndRatio = 0.52f, slideDrag = 22f, attackDrag = 22f;
    public float groundProbe = 0.15f, groundProbeInset = 0.08f;
    public float vaultHeight = 0.28f, vaultReach = 0.3f, vaultMaxRiseSpeed = 2f, vaultCooldown = 0.2f;
    public LayerMask groundLayers = (1 << 6) | (1 << 7);
    public float capsuleHeight = 1.75f, capsuleRadius = 0.27f;
    public Vector3 capsuleCenter = new Vector3(0f,0.875f,0f);
}
