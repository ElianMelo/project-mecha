using UnityEngine;

// Observe replicated movement on each client; no input sounds and no extra network traffic.
public sealed class ActorFootstepAudio : MonoBehaviour
{
    private PlayerMovement player;
    private PlayerHealth playerHealth;
    private PlayerAttack attack;
    private EnemyController enemy;
    private Vector3 previous;
    private float travelled, previousVertical, nextStep;
    private bool grounded, initialized;
    private void Awake() { player = GetComponent<PlayerMovement>(); playerHealth = GetComponent<PlayerHealth>(); attack = GetComponent<PlayerAttack>(); enemy = GetComponent<EnemyController>(); }
    private void OnEnable() { initialized = false; travelled = 0f; previous = transform.position; }
    private void LateUpdate()
    {
        var audio = GameAudio.Instance;
        if (audio == null) return;
        bool alive = playerHealth != null ? playerHealth.Health > 0f : enemy != null && !enemy.IsDead;
        bool onGround = player != null ? player.IsGrounded : enemy != null && enemy.IsGrounded;
        Vector3 delta = transform.position - previous; previous = transform.position;
        float vertical = player != null ? player.Velocity.y : delta.y / Mathf.Max(0.001f, Time.deltaTime);
        bool discontinuity = delta.sqrMagnitude > 9f || !alive;
        if (!initialized || discontinuity) { initialized = true; grounded = onGround; previousVertical = 0f; travelled = 0f; return; }
        if (onGround && !grounded && previousVertical < -audio.Settings.landingSpeed)
            GameAudio.Play(GameAudioCue.Land, transform.position, transform);
        else if (!onGround && grounded && vertical > audio.Settings.landingSpeed)
            GameAudio.Play(GameAudioCue.Jump, transform.position, transform);
        if (onGround && (player == null || !player.IsDashing && !playerHealth.IsStunned && !attack.IsBusy) && (enemy == null || !enemy.IsStunned && !enemy.IsAttacking))
        {
            travelled += Mathf.Abs(delta.x);
            if (travelled >= audio.Settings.footstepDistance && Time.time >= nextStep)
            {
                GameAudio.Play(player != null ? GameAudioCue.Footstep : GameAudioCue.EnemyFootstep, transform.position, transform);
                travelled = 0f; nextStep = Time.time + audio.Settings.minimumStepInterval;
            }
        }
        else travelled = 0f;
        grounded = onGround;
        // Retain fall speed across render frames that occur between physics/network updates.
        previousVertical = onGround ? 0f : Mathf.Min(previousVertical, vertical);
    }
}
