using System.Collections.Generic;
using UnityEngine;

// Small surface graph for this side-view arena. The motor owns all actual physics.
[RequireComponent(typeof(EnemyMotor))]
public class EnemyPlatformNavigator : MonoBehaviour
{
    private EnemyMotor motor;
    private Collider goal, nextSurface;
    private float refreshAt, landingX;
    private readonly List<Collider> surfaces = new List<Collider>();
    private readonly List<float> costs = new List<float>();
    private readonly List<int> previous = new List<int>();
    private readonly List<bool> visited = new List<bool>();
    public string Destination => nextSurface != null ? nextSurface.name : "Direct pursuit";
    private void Awake() => motor = GetComponent<EnemyMotor>();

    public void Pursue(PlayerMovement player, float speed, float stopDistance)
    {
        if (player == null) { motor.Move(0f); return; }
        MoveTo(player.transform.position, player.Support, speed, stopDistance);
    }
    public void ResetRoute()
    {
        goal = null; refreshAt = 0f;
        if (motor.IsGrounded) nextSurface = null;
    }
    public bool CanNavigateTo(Collider destination, float speed) => destination == motor.Support || FindNext(motor.Support, destination, speed) != null;
    public void MoveTo(Vector3 targetPosition, Collider targetSupport, float speed, float stopDistance)
    {
        float targetX = targetPosition.x;
        if (!motor.IsGrounded)
        {
            float dx = (nextSurface != null ? landingX : targetX) - transform.position.x;
            motor.Move(Mathf.Abs(dx) > motor.Settings.airborneArrival ? Mathf.Sign(dx) * speed : 0f, false);
            return;
        }
        Collider desiredGoal = targetSupport != null ? targetSupport : goal;
        if (desiredGoal == null && Physics.Raycast(targetPosition + Vector3.up * motor.Settings.probeInset,
            Vector3.down, out var below, motor.Settings.maximumDrop, motor.Settings.groundLayers, QueryTriggerInteraction.Ignore)) desiredGoal = below.collider;
        if (desiredGoal != goal || Time.time >= refreshAt || nextSurface == motor.Support)
        {
            goal = desiredGoal; refreshAt = Time.time + motor.Settings.routeRefresh;
            nextSurface = FindNext(motor.Support, goal, speed);
        }
        if (goal == motor.Support || nextSurface == null)
        {
            float dx = targetX - transform.position.x;
            motor.Move(Mathf.Abs(dx) > stopDistance ? Mathf.Sign(dx) * speed : 0f);
            return;
        }
        var from = motor.Support.bounds;
        var to = nextSurface.bounds;
        float inset = motor.Settings.landingInset;
        landingX = Mathf.Clamp(transform.position.x, to.min.x + inset, to.max.x - inset);
        float launchX = Mathf.Clamp(landingX, from.min.x + inset, from.max.x - inset);
        float distance = launchX - transform.position.x;
        if (Mathf.Abs(distance) > motor.Settings.launchTolerance)
        {
            motor.Move(Mathf.Sign(distance) * speed, Mathf.Abs(distance) > motor.Settings.launchTolerance * 2f);
            return;
        }
        motor.Move(Mathf.Abs(landingX - transform.position.x) > motor.Settings.airborneArrival ? Mathf.Sign(landingX - transform.position.x) * speed : 0f, false);
        if (to.max.y < from.max.y - motor.Settings.heightSafety && landingX >= from.min.x && landingX <= from.max.x)
            motor.Drop();
        else motor.Jump();
    }

    private Collider FindNext(Collider start, Collider destination, float speed)
    {
        if (start == null || destination == null || start == destination) return null;
        surfaces.Clear(); surfaces.Add(start);
        foreach (var platform in OneWayPlatform.ActivePlatforms)
            if (platform != null && platform.Surface != null && platform.Surface.enabled && !surfaces.Contains(platform.Surface)) surfaces.Add(platform.Surface);
        if (!surfaces.Contains(destination)) surfaces.Add(destination);
        costs.Clear(); previous.Clear(); visited.Clear();
        for (int i = 0; i < surfaces.Count; i++) { costs.Add(float.PositiveInfinity); previous.Add(-1); visited.Add(false); }
        costs[0] = 0f;
        int finish = surfaces.IndexOf(destination);
        for (int step = 0; step < surfaces.Count; step++)
        {
            int current = -1;
            for (int i = 0; i < surfaces.Count; i++)
                if (!visited[i] && (current < 0 || costs[i] < costs[current])) current = i;
            if (current < 0 || float.IsPositiveInfinity(costs[current])) break;
            if (current == finish) break;
            visited[current] = true;
            for (int i = 1; i < surfaces.Count; i++)
            {
                if (visited[i] || !CanReach(surfaces[current], surfaces[i], speed)) continue;
                float x = current == 0 ? transform.position.x : surfaces[current].bounds.center.x;
                var bounds = surfaces[i].bounds;
                float approach = Mathf.Abs(x - Mathf.Clamp(x, bounds.min.x + motor.Settings.landingInset, bounds.max.x - motor.Settings.landingInset));
                float cost = costs[current] + 1f + approach / speed + Mathf.Abs(bounds.max.y - surfaces[current].bounds.max.y) / motor.Settings.jumpSpeed;
                if (cost >= costs[i]) continue;
                costs[i] = cost; previous[i] = current;
            }
        }
        if (previous[finish] < 0) return null;
        int next = finish;
        while (previous[next] > 0) next = previous[next];
        return surfaces[next];
    }

    private bool CanReach(Collider source, Collider destination, float speed)
    {
        var settings = motor.Settings;
        var a = source.bounds; var b = destination.bounds;
        float height = b.max.y - a.max.y;
        float apex = settings.jumpSpeed * settings.jumpSpeed / (2f * settings.risingGravity);
        if (height > apex - settings.heightSafety || height < -settings.maximumDrop) return false;
        float gap = Mathf.Max(0f, Mathf.Max(b.min.x - a.max.x + settings.landingInset * 2f, a.min.x - b.max.x + settings.landingInset * 2f));
        float flightTime = settings.jumpSpeed / settings.risingGravity + Mathf.Sqrt(2f * Mathf.Max(0f, apex - height) / settings.fallingGravity);
        return gap <= speed * flightTime - settings.horizontalSafety;
    }
}
