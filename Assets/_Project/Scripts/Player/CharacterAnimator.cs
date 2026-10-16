using FishNet.Component.Animating;
using UnityEngine;

// Shared animation presentation. Physics and combat own the actual gameplay timing.
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private CharacterAnimationSettings settings;
    private Animator animator;
    private NetworkAnimator networkAnimator;
    private PlayerMovement player;
    private PlayerAttack attack;
    private EnemyController enemy;
    private Rigidbody body;
    private string current;
    private float lockedUntil;
    private bool dead;
    private Vector3 previousPosition;
    public Animator Animator => animator;
    private bool DrivesAnimator => networkAnimator == null || !networkAnimator.IsSpawned ||
        (networkAnimator.ClientAuthoritative ? networkAnimator.IsOwner ||
            !networkAnimator.Owner.IsValid && networkAnimator.IsServerInitialized : networkAnimator.IsServerInitialized);
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(); player = GetComponent<PlayerMovement>(); enemy = GetComponent<EnemyController>(); body = GetComponent<Rigidbody>();
        attack = GetComponent<PlayerAttack>();
        networkAnimator = GetComponent<NetworkAnimator>();
        if (animator != null && settings != null) { animator.runtimeAnimatorController = settings.controller; animator.applyRootMotion = false; }
        previousPosition = transform.position;
    }
    private void Update()
    {
        Vector3 motion = (transform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.001f);
        previousPosition = transform.position;
        // Observers consume NetworkAnimator state. Local locomotion/RPC callbacks must
        // not overwrite the authority's states, playback parameters or hit-stop speed.
        if (!DrivesAnimator) return;
        if (animator != null)
            animator.speed = attack != null && attack.IsInHitStop ? 0f : enemy != null ? enemy.Tempo : 1f;
        if (attack != null && attack.IsInHitStop && Time.time < lockedUntil) lockedUntil += Time.deltaTime;
        if (enemy != null && Time.time < lockedUntil) lockedUntil += Time.deltaTime * (1f - enemy.Tempo);
        if (animator == null || settings == null || dead || Time.time < lockedUntil) return;
        if (enemy != null && enemy.Tempo <= 0f) return;
        bool grounded = player != null ? player.IsGrounded : enemy != null ? enemy.IsGrounded : Mathf.Abs(body.linearVelocity.y) < 0.5f;
        Vector3 velocity = player != null ? player.Velocity : enemy != null && enemy.IsSpawned && !enemy.IsServerInitialized ? motion : body != null ? body.linearVelocity : motion;
        // Enemy motion and Animator.speed already share tempo. Normalize here so slowdown applies once.
        float horizontal = Mathf.Abs(velocity.x) / (enemy != null ? enemy.Tempo : 1f);
        float vertical = velocity.y;
        string state = !grounded ? (vertical > 0.2f ? "Jump" : "Fall") :
            player != null && player.IsDashing ? "Dash" : horizontal > settings.movementThreshold ? "Run" : "Idle";
        float playback = 1f;
        if (state == "Run" || state == "Dash")
        {
            float reference = state == "Dash" ? settings.dashReferenceSpeed : settings.runReferenceSpeed;
            playback = Mathf.Clamp(horizontal / Mathf.Max(reference, 0.01f), settings.minimumLocomotionPlayback,
                Mathf.Max(settings.minimumLocomotionPlayback, settings.maximumLocomotionPlayback));
        }
        Play(state, playback: playback);
    }
    private void Play(string state, float duration = 0f, float normalizedTime = 0f, float playback = 1f)
    {
        if (animator == null || settings == null || !DrivesAnimator) return;
        var clip = settings.Clip(state);
        if (clip == null) return;
        float speed = duration > 0f ? clip.length / duration : playback;
        if (current == state && duration <= 0f && (state == "Run" || state == "Dash"))
            animator.SetFloat("PlaybackSpeed", speed, settings.locomotionDamping, Time.deltaTime);
        else animator.SetFloat("PlaybackSpeed", speed);
        if (current == state && duration <= 0f) return;
        current = state;
        if (networkAnimator != null && networkAnimator.IsSpawned)
            networkAnimator.CrossFadeInFixedTime(state, settings.crossFade, 0, normalizedTime * clip.length);
        else animator.CrossFadeInFixedTime(state, settings.crossFade, 0, normalizedTime * clip.length);
    }
    public void Action(PlayerAttack.ActionKind kind, float duration) {
        if (kind == PlayerAttack.ActionKind.DiveStrike) kind = PlayerAttack.ActionKind.AirSlash;
        else if (kind == PlayerAttack.ActionKind.CriticalStrike) kind = PlayerAttack.ActionKind.DashSlash;
        else if (kind == PlayerAttack.ActionKind.MagneticPull) kind = PlayerAttack.ActionKind.RisingSlash;
        lockedUntil = Time.time + duration; Play(kind.ToString(), duration); }
    public void ReleaseAction() { lockedUntil = 0f; current = null; }
    public void Reaction(float stun) { lockedUntil = Time.time + stun; Play("Hit", stun, settings.hitPoseTime); }
    public void Telegraph(float duration) { lockedUntil = Time.time + duration; Play("Attack", duration); }
    public void Death(float duration) { dead = true; Play("Death", duration); }
    public void ResetPresentation() { dead=false; lockedUntil=0f; current=null; }
}
