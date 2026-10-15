using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(BoxCollider))]
public class DestructibleProp : NetworkBehaviour, ICombatTarget
{
    [SerializeField] private DestructibleSettings settings;
    private readonly SyncVar<float> health = new SyncVar<float>();
    private Renderer[] visuals;
    private Collider hitbox;
    private CharacterHitFeedback feedback;
    private float soloHealth;
    public bool IsDead => (NetworkObject.IsSpawned ? health.Value : soloHealth) <= 0f;
    public float Health => NetworkObject.IsSpawned ? health.Value : soloHealth;
    private void Awake()
    {
        visuals = GetComponentsInChildren<Renderer>(); hitbox = GetComponent<Collider>();
        feedback = GetComponent<CharacterHitFeedback>(); soloHealth = settings.health;
        health.OnChange += OnHealthChanged;
    }
    private void OnDestroy() => health.OnChange -= OnHealthChanged;
    public override void OnStartServer() { base.OnStartServer(); health.Value = settings.health; }
    public override void OnStartClient() { base.OnStartClient(); SetVisible(!IsDead); }
    private void OnHealthChanged(float previous, float next, bool asServer) => SetVisible(next > 0f);
    private void SetVisible(bool visible)
    {
        hitbox.enabled = visible;
        foreach (var renderer in visuals) renderer.enabled = visible;
    }
    public void TakeHit(float damage, Vector3 knockback, float stun)
    {
        if (IsDead || NetworkObject.IsSpawned && !IsServerInitialized || damage <= 0f) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else { soloHealth = Mathf.Max(0f, soloHealth - damage); SetVisible(!IsDead); }
        if (NetworkObject.IsSpawned) ShowHit(knockback, IsDead); else ApplyHit(knockback, IsDead);
    }
    [ObserversRpc(RunLocally = true)] private void ShowHit(Vector3 direction, bool broken) => ApplyHit(direction, broken);
    private void ApplyHit(Vector3 direction, bool broken)
    {
        if (!broken) feedback?.Hit(0f, direction);
        else
        {
            ParticleBurstEmitter.Emit(settings.debris, transform.position, direction + Vector3.up);
            ParticleBurstEmitter.Emit(settings.smoke, transform.position, Vector3.up);
        }
    }
}
