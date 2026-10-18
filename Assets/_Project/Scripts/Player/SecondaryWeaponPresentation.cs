using UnityEngine;

// The gun is parented to the animated left forearm; only its world aim is independent.
public sealed class SecondaryWeaponPresentation : MonoBehaviour
{
    [SerializeField] private SecondaryWeaponSettings settings;
    [SerializeField] private Transform gun, leftArm;
    [SerializeField] private SpriteRenderer reticle;
    [SerializeField] private Transform reticlePivot;
    [SerializeField] private Animator reticleAnimator;
    private MechaAutoAim aim;
    public void Bind(Transform arm, Transform weapon) { leftArm = arm; gun = weapon; }
    private PlayerIdentity identity;
    private ITargetable previousTarget;
    private void Awake() { aim = GetComponent<MechaAutoAim>(); identity = GetComponent<PlayerIdentity>(); }
    private void LateUpdate()
    {
        Vector3 direction = aim.Direction;
        Vector3 mount = leftArm != null ? leftArm.TransformPoint(settings.armMountOffset) : aim.Origin;
        gun.SetPositionAndRotation(mount, Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, direction)));
        if (reticle == null) return;
        bool visible = identity.LocallyControlled && aim.Target != null;
        reticle.enabled = visible;
        if (!visible) { previousTarget = null; return; }
        Vector3 position = aim.IsLocked ? aim.TargetPoint
            : aim.Origin + direction * Vector3.Distance(aim.Origin, aim.TargetPoint);
        position.z = Mathf.Min(transform.position.z, aim.Target.TargetBounds.min.z) + settings.reticleVisualDepth;
        var pivot = reticlePivot != null ? reticlePivot : reticle.transform;
        pivot.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, aim.Angle));
        if (reticleAnimator != null)
        {
            if (previousTarget != aim.Target) reticleAnimator.Play("Scan", 0, 0f);
            reticleAnimator.SetBool("Locked", aim.IsLocked);
        }
        previousTarget = aim.Target;
    }
}
