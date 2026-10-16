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
    private readonly HashSet<Collider> supported = new HashSet<Collider>();
    private Collider surface;
    private void Awake() => surface = GetComponent<Collider>();
    public void Configure(OneWayPlatformSettings profile) => settings = profile;
    private void OnEnable() => Platforms.Add(this);
    public static void Register(Collider actor) { if (actor != null) Actors.Add(actor); }
    public static void Unregister(Collider actor) { if (actor != null) Actors.Remove(actor); }
    public void Drop(Collider actor)
    {
        drops[actor] = Time.time + settings.dropDuration;
        supported.Remove(actor);
        SetIgnored(actor, true);
    }
    public bool IsDropping(Collider actor) => drops.TryGetValue(actor, out float until) && Time.time < until;
    public void ConfirmSupport(Collider actor)
    {
        if (IsDropping(actor)) return;
        supported.Add(actor); SetIgnored(actor, false);
    }
    private void FixedUpdate()
    {
        foreach (var actor in Actors)
        {
            if (actor == null || !actor.enabled || actor.attachedRigidbody == null) continue;
            Bounds b = actor.bounds, p = surface.bounds;
            bool overlaps = b.max.x > p.min.x && b.min.x < p.max.x && b.max.z > p.min.z && b.min.z < p.max.z;
            bool dropping = IsDropping(actor);
            bool rising = actor.attachedRigidbody.linearVelocity.y > settings.risingThreshold;
            // A rounded capsule can settle slightly below the top at an edge. Keep the
            // established support until it actually leaves, jumps, or explicitly drops.
            bool retained = supported.Contains(actor) && overlaps && b.min.y >= p.max.y - settings.supportReleaseDepth;
            bool above = b.min.y >= p.max.y - settings.surfaceTolerance;
            bool solid = !dropping && !rising && (above || retained);
            if (solid && overlaps) supported.Add(actor); else supported.Remove(actor);
            SetIgnored(actor, !solid);
            if (!dropping) drops.Remove(actor);
        }
        ignored.RemoveWhere(actor =>
        {
            if (actor != null && Actors.Contains(actor)) return false;
            if (actor != null) Physics.IgnoreCollision(actor, surface, false);
            return true;
        });
        supported.RemoveWhere(actor => actor == null || !Actors.Contains(actor));
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
        ignored.Clear(); drops.Clear(); supported.Clear();
    }
}
