using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Platform Traversal")]
public class EnemyTraversalSettings : ScriptableObject
{
    public float jumpSpeed = 11.6f, risingGravity = 24f, fallingGravity = 40f, terminalVelocity = 22f;
    public float acceleration = 38f, airAcceleration = 24f, dropSpeed = 4.5f;
    public float jumpCooldown = 0.32f, groundProbe = 0.16f, probeInset = 0.08f;
    public float routeRefresh = 0.35f, landingInset = 0.42f, launchTolerance = 0.22f;
    public float maximumDrop = 7f, heightSafety = 0.2f, horizontalSafety = 0.35f;
    public float airborneArrival = 0.18f, groundedArrival = 0.15f;
    public LayerMask groundLayers = (1 << 6) | (1 << 7);
}
