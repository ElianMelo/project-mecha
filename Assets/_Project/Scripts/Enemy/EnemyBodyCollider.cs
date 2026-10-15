using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public sealed class EnemyBodyCollider : MonoBehaviour
{
    private static readonly HashSet<EnemyBodyCollider> Bodies = new HashSet<EnemyBodyCollider>();
    public static IEnumerable<EnemyBodyCollider> Active => Bodies;
    public CapsuleCollider Collider { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Bodies.Clear();
    private void Awake() => Collider = GetComponent<CapsuleCollider>();
    private void OnEnable() => Bodies.Add(this);
    private void OnDisable() => Bodies.Remove(this);
}
