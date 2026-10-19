using UnityEngine;
using UnityEngine.InputSystem;

// Native particles/trail/light observe combat state; presentation never drives hits.
public sealed class SwordSwingPresentation : MonoBehaviour
{
    [SerializeField] private SwordSwingSettings settings;
    [SerializeField] private TrailRenderer bladeTrail;
    [SerializeField] private Unity.Cinemachine.CinemachineImpulseSource impactSource;
    [SerializeField] private Light bladeLight;
    [SerializeField] private ParticleSystem bladePeel;
    [SerializeField] private GameObject cutPrefab;
    private PlayerAttack attack;
    private PlayerMovement movement;
    private GameObject cutInstance;
    private ParticleSystem[] cutSystems;
    private PlayerCombatSettings.ActionData lastAction;
    private float previousElapsed;
    private bool cutPlayed;
    private float lightPeakUntil;
    private Gamepad hapticPad;
    private float hapticUntil;
    private float nextImpact;
    private void OnEnable()
    {
        attack = GetComponentInParent<PlayerAttack>();
        movement = GetComponentInParent<PlayerMovement>();
        if (attack != null) attack.HitConfirmed += Impact;
        if (bladeTrail != null) { bladeTrail.Clear(); bladeTrail.emitting = false; }
        if (cutInstance == null && cutPrefab != null)
        {
            // One reusable, unit-scale world envelope per equipped sword; no network particle objects.
            cutInstance = Instantiate(cutPrefab);
            cutSystems = cutInstance.GetComponentsInChildren<ParticleSystem>(true);
        }
        ClearCut();lastAction=null;previousElapsed=0f;cutPlayed=false;
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
        {cutPlayed=false;lastAction=action;}
        previousElapsed=elapsed;
        bool emitting = melee && elapsed >= action.impact-settings.leadIn && elapsed <= action.impact+Mathf.Max(.1f,action.hitActiveTime);
        if(bladeTrail != null)
        {
            bladeTrail.widthMultiplier = heavy ? settings.heavyWidth : settings.lightWidth;
            bladeTrail.time = heavy ? settings.heavyTail : settings.lightTail;
            if (emitting && !bladeTrail.emitting) bladeTrail.Clear();
            bladeTrail.emitting = emitting;
        }
        if(bladePeel != null)
        {
            if(emitting && !bladePeel.isPlaying)bladePeel.Play();
            else if(!emitting && bladePeel.isPlaying)bladePeel.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        if(melee && !cutPlayed && elapsed >= action.impact-settings.cutLead)
        {
            cutPlayed=true;lightPeakUntil=Time.time+settings.lightDecay;
            if(cutInstance != null)
            {
                ClearCut();
                FollowBlade();
                cutInstance.transform.localScale=Vector3.one*(heavy?settings.heavyRadius:settings.lightRadius);
                foreach(var ps in cutSystems)ps.Play(false);
            }
        }
        if(cutPlayed && emitting && cutInstance != null)FollowBlade();
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
    private void ClearCut(){if(cutSystems!=null)foreach(var ps in cutSystems)if(ps != null)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
    private void FollowBlade()
    {
        // The source mesh's +X radial axis follows the blade's long +Y axis in its actual 3D plane.
        cutInstance.transform.SetPositionAndRotation(transform.position,Quaternion.LookRotation(transform.forward,-transform.right));
    }
    private void OnDisable()
    {
        if (attack != null) attack.HitConfirmed -= Impact;
        if (bladeTrail != null) { bladeTrail.emitting = false; bladeTrail.Clear(); }
        if(bladePeel != null)bladePeel.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(bladeLight != null && settings != null)bladeLight.intensity=settings.idleLight;
        lightPeakUntil=0f;ClearCut();
        StopHaptic();
    }
    private void OnDestroy(){if(cutInstance!=null)Destroy(cutInstance);}
}
