using UnityEngine;

// Shared animation presentation. Physics and combat own the actual gameplay timing.
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private CharacterAnimationSettings settings;
    private Animator animator;
    private PlayerMovement player;
    private EnemyController enemy;
    private Rigidbody body;
    private string current;
    private float lockedUntil;
    private bool dead;
    private Vector3 previousPosition;
    public Animator Animator => animator;
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(); player = GetComponent<PlayerMovement>(); enemy = GetComponent<EnemyController>(); body = GetComponent<Rigidbody>();
        if (animator != null && settings != null) { animator.runtimeAnimatorController = settings.controller; animator.applyRootMotion = false; }
        previousPosition = transform.position;
    }
    private void Update()
    {
        Vector3 motion = (transform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.001f);
        previousPosition = transform.position;
        if (animator == null || settings == null || dead || Time.time < lockedUntil) return;
        bool grounded = player != null ? player.IsGrounded : enemy != null ? enemy.IsGrounded : Mathf.Abs(body.linearVelocity.y) < 0.5f;
        float vertical = player != null ? player.Velocity.y : enemy != null && enemy.IsSpawned && !enemy.IsServerInitialized ? motion.y : body.linearVelocity.y;
        string state = !grounded ? (vertical > 0.2f ? "Jump" : "Fall") :
            player != null && player.IsDashing ? "Dash" : Mathf.Abs(motion.x) > settings.movementThreshold ? "Run" : "Idle";
        Play(state);
    }
    private void Play(string state, float duration = 0f, float normalizedTime = 0f)
    {
        if (animator == null || settings == null) return;
        var clip = settings.Clip(state);
        if (clip == null) return;
        animator.SetFloat("PlaybackSpeed", duration > 0f ? clip.length / duration : 1f);
        if (current == state && duration <= 0f) return;
        current = state;
        animator.CrossFadeInFixedTime(state, settings.crossFade, 0, normalizedTime * clip.length);
    }
    public void Action(PlayerAttack.ActionKind kind, float duration) { lockedUntil = Time.time + duration; Play(kind.ToString(), duration); }
    public void ReleaseAction() { lockedUntil = 0f; current = null; }
    public void Reaction(float stun) { lockedUntil = Time.time + stun; Play("Hit", stun, settings.hitPoseTime); }
    public void Telegraph(float duration) { lockedUntil = Time.time + duration; Play("Attack", duration); }
    public void Death(float duration) { dead = true; Play("Death", duration); }
    public void ResetPresentation() { dead=false; lockedUntil=0f; current=null; }
}
