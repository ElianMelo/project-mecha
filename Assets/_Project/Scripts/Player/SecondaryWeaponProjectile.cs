using FishNet.Object;
using UnityEngine;

// Native Rigidbody/CCD and Collider contacts own flight and impact; FishNet owns replication.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class SecondaryWeaponProjectile : NetworkBehaviour
{
    [SerializeField] private SecondaryWeaponSettings settings;
    private PlayerHealth shooter;
    private Rigidbody body;
    private Collider hitbox;
    private Vector3 origin;
    private float expires;
    private bool launched, spent;
    private void Awake() { body = GetComponent<Rigidbody>(); hitbox = GetComponent<Collider>(); }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServerInitialized) { body.isKinematic = true; hitbox.enabled = false; }
    }
    public void Launch(SecondaryWeaponSettings profile, PlayerHealth source, Vector3 direction)
    {
        settings = profile; shooter = source; origin = body.position;
        hitbox.excludeLayers = ~CombatTargetRules.TargetLayers(settings.hitLayers.value);
        foreach (var actor in PlayerAttack.ActiveActors)
        {
            if (actor == null) continue;
            var health = actor.GetComponent<PlayerHealth>();
            if (health == shooter || !CombatTargetRules.CanHit(shooter, health))
                foreach (var collider in actor.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(hitbox, collider);
        }
        expires = Time.time + settings.projectileLifetime; launched = true;
        body.isKinematic = false; body.linearVelocity = direction.normalized * settings.boltSpeed;
    }
    private void Update()
    {
        if (!launched || IsSpawned && !IsServerInitialized) return;
        if (Time.time >= expires || (transform.position - origin).sqrMagnitude >= settings.range * settings.range) Finish();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (spent || !launched || IsSpawned && !IsServerInitialized) return;
        if (PvpMatchDirector.BlocksCombat) { Finish(); return; }
        var target = collision.collider.GetComponentInParent<IDamageable>();
        Vector3 force = body.linearVelocity.normalized * 0.5f;
        if (CombatTargetRules.CanHit(shooter, target))
            target.TakeDamage(settings.damage, force, 0.04f, shooter);
        Vector3 position = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        if (IsSpawned) ImpactAudio(position); else GameAudio.Play(GameAudioCue.BulletImpact, position);
        Finish();
    }
    [ObserversRpc(RunLocally = true)] private void ImpactAudio(Vector3 position) => GameAudio.Play(GameAudioCue.BulletImpact, position);
    private void Finish()
    {
        if (spent) return;
        spent = true; hitbox.enabled = false;
        if (IsSpawned) Despawn(); else Destroy(gameObject);
    }
}
