using UnityEngine;

public enum EnemyRole { Hunter, Guard, Lancer }

[RequireComponent(typeof(EnemyController))]
public sealed class EnemyTactics : MonoBehaviour
{
    [SerializeField] private EnemyTacticsSettings settings;
    private EnemyStateMachine brain;
    private Collider homeSurface;
    public float GuardPatrolRadius => settings != null ? settings.guardPatrolRadius : 1.3f;
    private void Awake() => brain = GetComponent<EnemyStateMachine>();
    public bool HoldPosition(PlayerAwareness target)
    {
        if (settings == null || target == null) return false;
        if (homeSurface == null && Physics.Raycast(brain.Home + Vector3.up * 0.35f, Vector3.down, out var home,
            0.8f, brain.Motor.Settings.groundLayers, QueryTriggerInteraction.Ignore)) homeSurface = home.collider;
        bool hold = false;
        if (brain.Actor.Role == EnemyRole.Guard)
            hold = Vector3.Distance(target.transform.position, brain.Home) > settings.guardEngageRadius ||
                Vector3.Distance(transform.position, brain.Home) > settings.guardLeash;
        else
        {
            float distance = (target.transform.position - transform.position).sqrMagnitude;
            int ahead = 0;
            foreach (var other in EnemyController.ActiveEnemies)
            {
                if (other == null || other == brain.Actor || other.IsDead || other.Role != EnemyRole.Hunter) continue;
                float d = (target.transform.position - other.transform.position).sqrMagnitude;
                if (d < distance || Mathf.Approximately(d, distance) && other.GetInstanceID() < brain.Actor.GetInstanceID()) ahead++;
            }
            hold = ahead >= settings.simultaneousHunters;
        }
        if (!hold) return false;
        brain.Navigation.MoveTo(brain.Home, homeSurface, brain.Actor.ChaseSpeed, settings.homeArrival);
        return true;
    }
}
