using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public sealed class EnemyLanceAttack : NetworkBehaviour
{
    [SerializeField] private EnemyLanceSettings settings;
    [SerializeField] private Transform heldLance;
    private readonly SyncVar<bool> holdingLance = new SyncVar<bool>(true);
    private EnemyController actor;
    private EnemyStateMachine brain;
    private PlayerAwareness target;
    private float fireAt, nextShot;
    public bool IsCharging { get; private set; }
    public bool HasLance => holdingLance.Value;
    public Transform HeldLance => heldLance;
    public Vector3 ReleasePosition => heldLance != null ? heldLance.position : transform.position + Vector3.up * settings.launchHeight;
    private void Awake()
    {
        actor = GetComponent<EnemyController>(); brain = GetComponent<EnemyStateMachine>();
    }
    public void Cancel()
    {
        if (IsCharging) holdingLance.Value = false;
        IsCharging = false;
        if (settings != null) nextShot = Mathf.Max(nextShot, Time.time + settings.windup);
    }
    public void DelayForTempo(float duration)
    {
        if (IsCharging) fireAt += duration;
        if (nextShot > Time.time) nextShot += duration;
    }
    public bool Tick()
    {
        if (settings == null) return false;
        if (target == null || !target.IsAlive || !IsCharging) target = brain.Target;
        if (target == null || !target.IsAlive) { IsCharging = false; return false; }
        float distance = Vector3.Distance(target.transform.position, transform.position);
        actor.Face(target.transform.position.x - transform.position.x);
        if (distance > settings.detectionRange) { IsCharging = false; return false; }
        if (IsCharging)
        {
            brain.Motor.Move(0f);
            if (Time.time >= fireAt) { Fire(); IsCharging = false; nextShot = Time.time + settings.cooldown; }
            return true;
        }
        if (!brain.Motor.IsGrounded) { brain.Navigation.Pursue(target.Movement, actor.ChaseSpeed, settings.preferredRange); return true; }
        if (distance < settings.retreatRange)
        {
            var support = brain.Motor.Support;
            float direction = Mathf.Sign(transform.position.x - target.transform.position.x);
            float x = Mathf.Clamp(transform.position.x + direction * 2f, support.bounds.min.x + 0.5f, support.bounds.max.x - 0.5f);
            brain.Navigation.MoveTo(new Vector3(x, support.bounds.max.y, 0f), support, actor.ChaseSpeed, 0.2f);
            if (Mathf.Abs(x - transform.position.x) > 0.3f) return true;
        }
        if (distance > settings.preferredRange + 2f)
        { brain.Navigation.Pursue(target.Movement, actor.ChaseSpeed, settings.preferredRange); return true; }
        brain.Motor.Move(0f);
        if (Time.time >= nextShot)
        {
            holdingLance.Value = true;
            IsCharging = true; fireAt = Time.time + Mathf.Max(0.18f, settings.windup);
            actor.ShowLanceWindup(Mathf.Max(0.18f, settings.windup));
        }
        return true;
    }
    private void Fire()
    {
        if (target == null || !target.IsAlive || settings.projectile == null) return;
        Vector3 origin = ReleasePosition;
        Vector3 end = target.transform.position + Vector3.up * 0.8f;
        end.x += Mathf.Clamp(target.Movement.Velocity.x * settings.leadTime, -settings.maximumLead, settings.maximumLead);
        float apex = Mathf.Max(origin.y, end.y) + settings.arcHeight;
        float vy = Mathf.Sqrt(2f * settings.gravity * (apex - origin.y));
        float flight = vy / settings.gravity + Mathf.Sqrt(2f * (apex - end.y) / settings.gravity);
        Vector3 velocity = new Vector3((end.x - origin.x) / flight, vy, (end.z - origin.z) / flight);
        var projectile = Instantiate(settings.projectile, origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg));
        projectile.GetComponent<EnemyLanceProjectile>().Launch(settings, velocity);
        holdingLance.Value = false;
        actor.ServerManager.Spawn(projectile);
    }
}
