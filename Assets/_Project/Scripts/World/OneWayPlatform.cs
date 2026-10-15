using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Collider))]
public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private OneWayPlatformSettings settings;
    private static readonly HashSet<Collider> Actors = new HashSet<Collider>();
    private static readonly HashSet<OneWayPlatform> Platforms = new HashSet<OneWayPlatform>();
    public static IReadOnlyCollection<OneWayPlatform> ActivePlatforms => Platforms;
    public Collider Surface => surface;
    private readonly Dictionary<Collider, float> drops = new Dictionary<Collider, float>();
    private readonly HashSet<Collider> ignored = new HashSet<Collider>();
    private Collider surface;
    private void Awake() => surface = GetComponent<Collider>();
    private void OnEnable() => Platforms.Add(this);
    public static void Register(Collider actor) { if (actor != null) Actors.Add(actor); }
    public static void Unregister(Collider actor) { if (actor != null) Actors.Remove(actor); }
    public void Drop(Collider actor)
    {
        drops[actor] = Time.time + settings.dropDuration;
        SetIgnored(actor, true);
    }
    private void FixedUpdate()
    {
        foreach (var actor in Actors)
        {
            if (actor == null || !actor.enabled || actor.attachedRigidbody == null) continue;
            Bounds b = actor.bounds, p = surface.bounds;
            bool overlaps = b.max.x > p.min.x && b.min.x < p.max.x && b.max.z > p.min.z && b.min.z < p.max.z;
            bool dropping = drops.TryGetValue(actor, out float until) && Time.time < until;
            bool fromBelow = b.min.y < p.max.y - settings.surfaceTolerance;
            bool rising = actor.attachedRigidbody.linearVelocity.y > settings.risingThreshold;
            SetIgnored(actor, overlaps && (dropping || fromBelow || rising));
            if (!dropping) drops.Remove(actor);
        }
        ignored.RemoveWhere(actor =>
        {
            if (actor != null && Actors.Contains(actor)) return false;
            if (actor != null) Physics.IgnoreCollision(actor, surface, false);
            return true;
        });
    }
    private void SetIgnored(Collider actor, bool value)
    {
        bool changed = value ? ignored.Add(actor) : ignored.Remove(actor);
        if (changed && actor != null && surface != null) Physics.IgnoreCollision(actor, surface, value);
    }
    private void OnDisable()
    {
        Platforms.Remove(this);
        foreach (var actor in ignored) if (actor != null && surface != null) Physics.IgnoreCollision(actor, surface, false);
        ignored.Clear(); drops.Clear();
    }
}
