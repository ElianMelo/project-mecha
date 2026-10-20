using UnityEngine;
using UnityEngine.InputSystem;

// Native particles/trail/light observe combat state; presentation never drives hits.
[DefaultExecutionOrder(100)]
public sealed class SwordSwingPresentation : MonoBehaviour
{
    [SerializeField] private SwordSwingSettings settings;
    [SerializeField] private TrailRenderer bladeTrail;
    [SerializeField] private TrailRenderer[] bladeBodyTrails;
    [SerializeField] private Unity.Cinemachine.CinemachineImpulseSource impactSource;
    [SerializeField] private Light bladeLight;
    [SerializeField] private ParticleSystem bladePeel;
    private PlayerAttack attack;
    private PlayerMovement movement;
    private PlayerCombatSettings.ActionData lastAction;
    private float previousElapsed;
    private bool strikePlayed;
    private float lightPeakUntil;
    private Gamepad hapticPad;
    private float hapticUntil;
    private float nextImpact;
    private void OnEnable()
    {
        attack = GetComponentInParent<PlayerAttack>();
        movement = GetComponentInParent<PlayerMovement>();
        if (attack != null) attack.HitConfirmed += Impact;
        ClearTrails();lastAction=null;previousElapsed=0f;strikePlayed=false;
    }
    private void LateUpdate()
    {
        if (hapticPad != null && Time.unscaledTime >= hapticUntil) StopHaptic();
        if (attack == null || settings == null) return;
        var action = attack.ActiveData;
        bool melee = action != null && !PlayerAttack.IsSkill(action.kind) && action.kind != PlayerAttack.ActionKind.MagneticPull;
        bool heavy = melee && PlayerAttack.IsHeavy(action.kind);
        float elapsed = attack.ActionElapsed;
        if(action != lastAction || elapsed < previousElapsed)
        {ClearTrails();strikePlayed=false;lastAction=action;}
        previousElapsed=elapsed;
        // Combat queries the animated blade segment in this same LateUpdate phase.
        bool emitting = attack.MeleeContactActive;
        PresentTrail(bladeTrail, emitting, heavy, 1f);
        if (bladeBodyTrails != null)
            foreach (var trail in bladeBodyTrails) PresentTrail(trail, emitting, heavy, settings.bodyTrailWidth);
        if(bladePeel != null)
        {
            if(emitting && !bladePeel.isPlaying)bladePeel.Play();
            else if(!emitting && bladePeel.isPlaying)bladePeel.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        if(emitting && !strikePlayed)
        {
            strikePlayed=true;lightPeakUntil=Time.time+settings.lightDecay;
        }
        if (!melee) lightPeakUntil=0f;
        if(bladeLight != null)
        {
            float envelope=Mathf.Clamp01((lightPeakUntil-Time.time)/Mathf.Max(.01f,settings.lightDecay));
            bladeLight.intensity=Mathf.Lerp(settings.idleLight,heavy?settings.heavyStrikeLight:settings.lightStrikeLight,envelope);
        }
    }
    private void Impact(float damage)
    {
        if (settings == null || attack == null || !attack.IsBusy || PlayerAttack.IsSkill(attack.ActiveData.kind) ||
            attack.NetworkObject.IsSpawned && !attack.IsOwner || settings.optionalMotion != null && !settings.optionalMotion.motionEnabled || Time.unscaledTime < nextImpact) return;
        nextImpact = Time.unscaledTime + .08f;
        bool heavy = PlayerAttack.IsHeavy(attack.ActiveData.kind);
        float impulse = heavy ? settings.heavyImpulse : settings.lightImpulse;
        impactSource?.GenerateImpulseWithVelocity(new Vector3(-(movement != null ? movement.Facing : 1), -.35f, 0f) * impulse);
        var pad = GameInputPresentation.ActiveGamepad;
        if (pad == null || !pad.added || !GameInputPresentation.UsingGamepad) return;
        float strength = heavy ? settings.heavyRumble : settings.lightRumble;
        hapticPad = pad; pad.SetMotorSpeeds(strength,strength*.6f); hapticUntil = Time.unscaledTime+settings.rumbleSeconds;
    }
    private void StopHaptic() { if (hapticPad != null && hapticPad.added) hapticPad.SetMotorSpeeds(0f,0f); hapticPad = null; }
    private void PresentTrail(TrailRenderer trail, bool emitting, bool heavy, float width)
    {
        if (trail == null) return;
        trail.widthMultiplier=(heavy?settings.heavyWidth:settings.lightWidth)*width;
        trail.time=heavy?settings.heavyTail:settings.lightTail;
        if(emitting && !trail.emitting) trail.Clear();
        trail.emitting=emitting;
    }
    private void ClearTrails()
    {
        if (bladeTrail != null) {bladeTrail.emitting=false;bladeTrail.Clear();}
        if (bladeBodyTrails != null) foreach(var trail in bladeBodyTrails)
            if(trail != null){trail.emitting=false;trail.Clear();}
    }
    private void OnDisable()
    {
        if (attack != null) attack.HitConfirmed -= Impact;
        ClearTrails();
        if(bladePeel != null)bladePeel.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(bladeLight != null && settings != null)bladeLight.intensity=settings.idleLight;
        lightPeakUntil=0f;
        StopHaptic();
    }
}
