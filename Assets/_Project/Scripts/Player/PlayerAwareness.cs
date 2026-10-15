using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerMovement))]
public class PlayerAwareness : MonoBehaviour
{
    [SerializeField] private PlayerAwarenessSettings settings;
    private static readonly HashSet<PlayerAwareness> Players = new HashSet<PlayerAwareness>();
    public static IReadOnlyCollection<PlayerAwareness> ActivePlayers => Players;
    public PlayerAwarenessSettings Settings => settings;
    public PlayerHealth Health { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public bool IsAlive => isActiveAndEnabled && Health != null && Health.Health > 0f;
    private void Awake() { Health = GetComponent<PlayerHealth>(); Movement = GetComponent<PlayerMovement>(); }
    private void OnEnable() => Players.Add(this);
    private void OnDisable() => Players.Remove(this);
    public float DistanceFrom(Vector3 point) => Vector2.Distance(transform.position, point);
    public EnemyStateId StateAtDistance(float distance, EnemyStateId current, float hysteresis)
    {
        if (distance <= settings.hardRadius + (current == EnemyStateId.Alert || current == EnemyStateId.Prediction ? hysteresis : 0f)) return EnemyStateId.Alert;
        if (distance <= settings.softRadius + (current == EnemyStateId.Suspicious ? hysteresis : 0f)) return EnemyStateId.Suspicious;
        return EnemyStateId.Wander;
    }
    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        Gizmos.color = settings.hardColor; Gizmos.DrawWireSphere(transform.position, settings.hardRadius);
        Gizmos.color = settings.softColor; Gizmos.DrawWireSphere(transform.position, settings.softRadius);
        Gizmos.color = settings.largeColor; Gizmos.DrawWireSphere(transform.position, settings.largeRadius);
    }
}
