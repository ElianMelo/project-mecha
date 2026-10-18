using UnityEngine;

// Per-instance timed state; shared character and skill assets are never mutated.
public sealed class CharacterModifiers : MonoBehaviour
{
    private float until, movement = 1f, attack = 1f;
    private PlayerHealth health;
    public bool Active => Time.time < until && (health == null || health.Health > 0f);
    public float MoveSpeed => Active ? movement : 1f;
    public float AttackSpeed => Active ? attack : 1f;
    public float Remaining => Active ? until - Time.time : 0f;
    private void Awake() => health = GetComponent<PlayerHealth>();
    public void Activate(float duration, float moveMultiplier, float attackMultiplier)
    { until = Time.time + duration; movement = moveMultiplier; attack = attackMultiplier; }
    public void Clear() => until = 0f;
    private void OnDisable() => until = 0f;
}
