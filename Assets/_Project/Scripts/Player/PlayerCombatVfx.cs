using UnityEngine;

public class PlayerCombatVfx : MonoBehaviour
{
    [SerializeField] private CombatVfxSettings settings;
    [SerializeField] private PlayerCombatSettings combat;
    private PlayerMovement movement;
    private CharacterHitFeedback feedback;
    private FireSkillPresentation fireSkills;
    private CharacterSkills characterSkills;
    public void Configure(PlayerCombatSettings profile) => combat = profile;
    private float nextSmoke;
    private bool wasGrounded;
    private float previousVerticalSpeed;
    private void Awake() { movement=GetComponent<PlayerMovement>(); feedback=GetComponent<CharacterHitFeedback>(); fireSkills=GetComponent<FireSkillPresentation>(); characterSkills=GetComponent<CharacterSkills>(); }
    public void EndAction() => fireSkills?.StopFeeding();
    public void SkillContact(PlayerAttack.ActionKind kind, Vector3 point, Vector3 incoming, Vector3 normal, bool final)
        => fireSkills?.Contact(kind, point, incoming, normal, final);
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
        => Strike(combat.Get(kind), facing, pulse);
    public void BeginAction(PlayerCombatSettings.ActionData action, int facing)
    {
        if (characterSkills != null && characterSkills.Begin(action, facing)) { GameAudio.Play(GameAudioCue.SkillPulse, transform.position, transform); return; }
        var cue = action.kind == PlayerAttack.ActionKind.MegaSlash ? GameAudioCue.MegaSlash :
            action.kind == PlayerAttack.ActionKind.SwordFire ? GameAudioCue.SwordFire :
            action.kind == PlayerAttack.ActionKind.CriticalX ? GameAudioCue.CriticalX :
            action.kind == PlayerAttack.ActionKind.MagneticPull ? GameAudioCue.MagneticPull : (GameAudioCue?)null;
        if (cue.HasValue) GameAudio.Play(cue.Value, transform.position, transform);
        if (settings == null) return;
        if (fireSkills != null && fireSkills.Handles(action.kind)) { fireSkills.Begin(action, facing); return; }
        if (PlayerAttack.IsSkill(action.kind))
            ParticleBurstEmitter.Emit(settings.skillCharge, transform.position + Vector3.up * 0.85f, Vector3.up);
    }
    public void Strike(PlayerCombatSettings.ActionData action,int facing,int pulse)
    {
        if (characterSkills != null && characterSkills.PresentPulse(action, facing, pulse)) { GameAudio.Play(GameAudioCue.SkillPulse, transform.position, transform); return; }
        var kind=action.kind;
        if (PlayerAttack.IsSkill(kind) && pulse == action.pulses - 1)
            GameAudio.Play(kind == PlayerAttack.ActionKind.MegaSlash ? GameAudioCue.MegaSlashImpact :
                kind == PlayerAttack.ActionKind.SwordFire ? GameAudioCue.SwordFireImpact : GameAudioCue.CriticalXImpact, transform.position, transform);
        var sound = PlayerAttack.IsSkill(kind) ? GameAudioCue.SkillPulse :
            kind == PlayerAttack.ActionKind.Slash2 ? GameAudioCue.Combo :
            kind == PlayerAttack.ActionKind.Slash3 ? GameAudioCue.Finisher :
            kind == PlayerAttack.ActionKind.AirSlash ? GameAudioCue.AirSlash :
            kind == PlayerAttack.ActionKind.DashSlash ? GameAudioCue.DashSlash :
            kind == PlayerAttack.ActionKind.RisingSlash ? GameAudioCue.RisingSlash :
            kind == PlayerAttack.ActionKind.DiveStrike ? GameAudioCue.DiveStrike :
            kind == PlayerAttack.ActionKind.CriticalStrike ? GameAudioCue.CriticalStrike : GameAudioCue.Slash;
        if (kind != PlayerAttack.ActionKind.MagneticPull) GameAudio.Play(sound, transform.position, transform);
        Vector3 offset=new Vector3(facing*(action.reach+pulse*action.pulseAdvance),action.height,settings.strikeOffset.z);
        if (kind == PlayerAttack.ActionKind.MagneticPull)
        {
            ParticleBurstEmitter.Emit(settings.magneticRecall, transform.position + offset, new Vector3(-facing, 0f, 0f));
            return;
        }
        if (fireSkills != null && fireSkills.Strike(action, facing, pulse)) return;
        var fire = kind == PlayerAttack.ActionKind.SwordFire ? settings.swordFire
            : kind == PlayerAttack.ActionKind.MegaSlash ? settings.megaSlashFire
            : kind == PlayerAttack.ActionKind.CriticalX ? settings.criticalXFire : null;
        if (fire != null)
        {
            SwordFireVfxEmitter.Emit(fire, transform.position + offset, facing, pulse);
            return;
        }
        Vector3 direction=kind==PlayerAttack.ActionKind.DiveStrike ? new Vector3(facing*0.15f,-1f,0f) : kind==PlayerAttack.ActionKind.RisingSlash ? new Vector3(facing*0.5f,1f,0f) : new Vector3(facing,kind==PlayerAttack.ActionKind.Slash2?-0.3f:0.3f,0f);
        ParticleBurstEmitter.Emit(PlayerAttack.IsSkill(kind)?settings.skillWind:settings.wind,transform.position+offset,direction);
        ParticleBurstEmitter.Emit(settings.slashEdges,transform.position+offset,direction);
        if (PlayerAttack.IsSkill(kind))
            ParticleBurstEmitter.Emit(settings.slashEdges,transform.position+offset,new Vector3(facing,-direction.y,0f));
    }
    public void Dash(int facing, bool beginning = false)
    {
        if (beginning) GameAudio.Play(GameAudioCue.Dash, transform.position, transform);
        nextSmoke=Time.time+settings.dashEmissionInterval;
        ParticleBurstEmitter.Emit(settings.dashSmoke,transform.position+settings.dashOffset,new Vector3(-facing,0.25f,0f));
        ParticleBurstEmitter.Emit(settings.dashStreaks,transform.position+settings.strikeOffset.y*Vector3.up,new Vector3(-facing,0.05f,0f));
        if (beginning) ParticleBurstEmitter.Emit(settings.dashDebris,transform.position+settings.dashOffset,new Vector3(-facing,0.65f,0f));
    }
    public void Hit(Vector3 direction) => feedback?.Hit(0f,direction);
}
