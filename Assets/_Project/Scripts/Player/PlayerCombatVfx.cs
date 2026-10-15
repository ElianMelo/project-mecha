using UnityEngine;

public class PlayerCombatVfx : MonoBehaviour
{
    [SerializeField] private CombatVfxSettings settings;
    [SerializeField] private PlayerCombatSettings combat;
    private PlayerMovement movement;
    private CharacterHitFeedback feedback;
    private float nextSmoke;
    private bool wasGrounded;
    private float previousVerticalSpeed;
    private void Awake() { movement=GetComponent<PlayerMovement>(); feedback=GetComponent<CharacterHitFeedback>(); }
    private void Update()
    {
        if(movement==null || settings==null) return;
        if (!wasGrounded && movement.IsGrounded && previousVerticalSpeed < -settings.landingSpeedThreshold)
        {
            var foot = transform.position + settings.dashOffset;
            ParticleBurstEmitter.Emit(settings.landingSmoke, foot, Vector3.left);
            ParticleBurstEmitter.Emit(settings.landingSmoke, foot, Vector3.right);
        }
        wasGrounded = movement.IsGrounded;
        previousVerticalSpeed = movement.Velocity.y;
        if (movement.IsDashing && Time.time>=nextSmoke) Dash(movement.Facing);
    }
    public void Strike(PlayerAttack.ActionKind kind,int facing,int pulse)
    {
        var action=combat.Get(kind);
        Vector3 offset=new Vector3(facing*(action.reach+pulse*action.pulseAdvance),action.height,settings.strikeOffset.z);
        Vector3 direction=kind==PlayerAttack.ActionKind.RisingSlash ? new Vector3(facing*0.5f,1f,0f) : new Vector3(facing,kind==PlayerAttack.ActionKind.Slash2?-0.3f:0.3f,0f);
        ParticleBurstEmitter.Emit(PlayerAttack.IsSkill(kind)?settings.skillWind:settings.wind,transform.position+offset,direction);
        ParticleBurstEmitter.Emit(settings.slashEdges,transform.position+offset,direction);
        if (PlayerAttack.IsSkill(kind))
            ParticleBurstEmitter.Emit(settings.slashEdges,transform.position+offset,new Vector3(facing,-direction.y,0f));
    }
    public void Dash(int facing, bool beginning = false)
    {
        nextSmoke=Time.time+settings.dashEmissionInterval;
        ParticleBurstEmitter.Emit(settings.dashSmoke,transform.position+settings.dashOffset,new Vector3(-facing,0.25f,0f));
        ParticleBurstEmitter.Emit(settings.dashStreaks,transform.position+settings.strikeOffset.y*Vector3.up,new Vector3(-facing,0.05f,0f));
        if (beginning) ParticleBurstEmitter.Emit(settings.dashDebris,transform.position+settings.dashOffset,new Vector3(-facing,0.65f,0f));
    }
    public void Hit(Vector3 direction) => feedback?.Hit(0f,direction);
}
