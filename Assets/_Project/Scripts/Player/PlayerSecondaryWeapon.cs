using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(PlayerInputs), typeof(MechaAutoAim), typeof(SecondaryWeaponPresentation))]
public sealed class PlayerSecondaryWeapon : NetworkBehaviour
{
    [SerializeField] private SecondaryWeaponSettings settings;
    private readonly SyncVar<float> ammunition = new SyncVar<float>(25f);
    private readonly SyncVar<float> aimAngle = new SyncVar<float>();
    private float soloAmmo, nextLocalShot, nextServerShot, refillAt, nextAimSync, lastAim;
    private PlayerInputs inputs;
    private PlayerHealth health;
    private PlayerAttack attack;
    private MechaAutoAim aim;
    public int Ammo => Mathf.FloorToInt((IsSpawned ? ammunition.Value : soloAmmo) + 0.001f);
    public int Capacity => settings.capacity;
    private void Awake()
    {
        inputs = GetComponent<PlayerInputs>(); health = GetComponent<PlayerHealth>(); attack = GetComponent<PlayerAttack>();
        aim = GetComponent<MechaAutoAim>(); soloAmmo = settings.capacity;
    }
    private void OnEnable() => inputs.OnSecondaryFireInput += FirePressed;
    private void OnDisable() => inputs.OnSecondaryFireInput -= FirePressed;
    public override void OnStartServer() { base.OnStartServer(); ammunition.Value = settings.capacity; }
    private bool CanFire => health != null && health.Health > 0f && !health.IsStunned && !attack.IsBusy && !attack.ServerBusy;
    private void Update()
    {
        if ((!IsSpawned || IsServerInitialized) && Time.time >= refillAt)
        {
            float amount = settings.refillPerSecond * Time.deltaTime;
            if (IsSpawned) ammunition.Value = Mathf.Min(settings.capacity, ammunition.Value + amount);
            else soloAmmo = Mathf.Min(settings.capacity, soloAmmo + amount);
        }
        // The press callback fires immediately; this continues only while the native action is held.
        // Both paths use the same deadline, so they cannot spawn two bullets in one cadence slot.
        if ((!IsSpawned || IsOwner) && inputs.FireHeld) FirePressed();
        if (!IsSpawned) return;
        if (!IsOwner) { aim.PresentRemote(aimAngle.Value); return; }
        if (Time.time >= nextAimSync && Mathf.Abs(Mathf.DeltaAngle(lastAim, aim.Angle)) > 0.25f)
        {
            nextAimSync = Time.time + settings.aimSyncInterval; lastAim = aim.Angle; RequestAim(lastAim);
        }
    }
    [ServerRpc]
    private void RequestAim(float value)
    {
        if (!float.IsNaN(value) && !float.IsInfinity(value)) aimAngle.Value = Mathf.Repeat(value, 360f);
    }
    private void FirePressed()
    {
        if (!inputs.HasControl || !CanFire || Ammo < 1 || Time.time < nextLocalShot) return;
        nextLocalShot = NextShotTime(nextLocalShot);
        if (IsSpawned) RequestShot(aim.Angle); else Fire(aim.Angle);
    }
    [ServerRpc]
    private void RequestShot(float angle)
    {
        if (!CanFire || Ammo < 1 || Time.time + settings.arrivalGrace < nextServerShot || float.IsNaN(angle) || float.IsInfinity(angle)) return;
        aimAngle.Value = Mathf.Repeat(angle, 360f); Fire(aimAngle.Value);
    }
    private void Fire(float angle)
    {
        if (settings.projectile == null) return;
        nextServerShot = NextShotTime(nextServerShot);
        refillAt = Time.time + settings.refillDelay;
        if (IsSpawned) ammunition.Value -= 1f; else soloAmmo -= 1f;
        // Only the authority creates a projectile. There is no second owner tracer.
        var instance = Instantiate(settings.projectile, aim.Origin, Quaternion.Euler(0f, 0f, angle));
        if (IsSpawned) Spawn(instance.gameObject);
        instance.GetComponent<SecondaryWeaponProjectile>().Launch(settings, health, Quaternion.Euler(0f, 0f, angle) * Vector3.right);
    }
    private float NextShotTime(float scheduled)
    {
        float interval = 1f / Mathf.Max(0.01f, settings.roundsPerSecond);
        // Keep cadence across frame/tick jitter without replaying a backlog after a long frame.
        float next = scheduled + interval;
        return next > Time.time ? next : Time.time + interval;
    }
}
