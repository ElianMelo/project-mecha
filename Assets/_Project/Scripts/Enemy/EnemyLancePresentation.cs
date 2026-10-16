using UnityEngine;

// The server owns weapon availability; every observer presents the same hand/rearm state.
[RequireComponent(typeof(EnemyLanceAttack), typeof(EnemyController))]
public sealed class EnemyLancePresentation : MonoBehaviour
{
    private EnemyLanceAttack attack;
    private EnemyController actor;
    private Renderer[] weaponRenderers;

    private void Awake()
    {
        attack = GetComponent<EnemyLanceAttack>(); actor = GetComponent<EnemyController>();
        weaponRenderers = attack.HeldLance != null
            ? attack.HeldLance.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
    }

    private void LateUpdate()
    {
        foreach (var renderer in weaponRenderers) renderer.enabled = attack.HasLance && !actor.IsDead;
    }
}
