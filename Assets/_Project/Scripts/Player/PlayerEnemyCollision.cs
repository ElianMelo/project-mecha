using System.Collections.Generic;
using UnityEngine;

// Run after movement has accepted or ended the dash, before the native physics step.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(PlayerMovement), typeof(CapsuleCollider), typeof(Rigidbody))]
public sealed class PlayerEnemyCollision : MonoBehaviour
{
    [SerializeField] private PlayerBodyCollisionSettings settings;
    private readonly HashSet<Collider> ignored = new HashSet<Collider>();
    private readonly List<Collider> released = new List<Collider>();
    private PlayerMovement movement;
    private CapsuleCollider capsule;
    private float restoreDeadline;
    private bool wasDashing;
    private void Awake()
    {
        movement = GetComponent<PlayerMovement>(); capsule = GetComponent<CapsuleCollider>();
        if (settings != null) GetComponent<Rigidbody>().maxDepenetrationVelocity = settings.maximumSeparationSpeed;
    }
    private void FixedUpdate()
    {
        if (settings == null) return;
        bool dashing = movement.IsPassingThrough;
        if (dashing)
        {
            foreach (var enemy in EnemyBodyCollider.Active)
            {
                var other = enemy.Collider;
                if (other == null || !other.enabled || !other.gameObject.activeInHierarchy) continue;
                ignored.Add(other);
                if (!Physics.GetIgnoreCollision(capsule, other)) Physics.IgnoreCollision(capsule, other, true);
            }
        }
        else
        {
            if (wasDashing) restoreDeadline = Time.time + settings.reentryGrace;
            released.Clear();
            foreach (var other in ignored)
            {
                bool overlapping = other != null && other.enabled && other.gameObject.activeInHierarchy &&
                    Physics.ComputePenetration(capsule, capsule.transform.position, capsule.transform.rotation,
                        other, other.transform.position, other.transform.rotation, out _, out _);
                if (overlapping && Time.time < restoreDeadline) continue;
                if (other != null) Physics.IgnoreCollision(capsule, other, false);
                released.Add(other);
            }
            foreach (var other in released) ignored.Remove(other);
        }
        wasDashing = dashing;
    }
    private void OnDisable()
    {
        foreach (var other in ignored) if (other != null && capsule != null) Physics.IgnoreCollision(capsule, other, false);
        ignored.Clear(); wasDashing = false;
    }
}
