using UnityEngine;

// The gun is parented to the animated left forearm; only its world aim is independent.
public sealed class SecondaryWeaponPresentation : MonoBehaviour
{
    [SerializeField] private SecondaryWeaponSettings settings;
    [SerializeField] private Transform gun, leftArm;
    [SerializeField] private SpriteRenderer reticle;
    private MechaAutoAim aim;
    private PlayerIdentity identity;
    private void Awake() { aim = GetComponent<MechaAutoAim>(); identity = GetComponent<PlayerIdentity>(); }
    private void LateUpdate()
    {
        Vector3 direction = aim.Direction;
        Vector3 mount = leftArm != null ? leftArm.TransformPoint(settings.armMountOffset) : aim.Origin;
        gun.SetPositionAndRotation(mount, Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, direction)));
        if (reticle == null) return;
        bool visible = identity.LocallyControlled && aim.Target != null;
        reticle.enabled = visible;
        if (visible) reticle.transform.SetPositionAndRotation(aim.Origin + direction * Vector3.Distance(aim.Origin, aim.TargetPoint), Quaternion.identity);
    }
}
