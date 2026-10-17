using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
public static class CombatInputInspection
{
    public static object State()
    {
        var actors = Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        return new { server = FishNet.InstanceFinder.IsServerStarted, client = FishNet.InstanceFinder.IsClientStarted,
            actors = actors.Select(a => new { a.IsOwner, a.CurrentAction, a.Mana, health = a.GetComponent<PlayerHealth>().Health, stunned = a.GetComponent<PlayerHealth>().IsStunned, cooldown = a.Cooldown(2), criticalPiece = a.Settings.IndexOf(PlayerAttack.ActionKind.CriticalX),
                costs = new[] {a.Cost(0),a.Cost(1),a.Cost(2)},
                devices = a.GetComponent<PlayerInput>().devices.Select(d=>d.name).ToArray(),
                bindings = new[] {"Attack","Tech","SkillA","SkillS","SkillD"}.Select(n => new { action=n, paths=a.GetComponent<PlayerInput>().actions.FindAction("Player/"+n).bindings.Select(b=>b.effectivePath).ToArray(), hint = GameInputPresentation.Hint(n) }).ToArray() }).ToArray() };
    }
    public static string MakePracticePassive()
    {
        foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) enemy.PracticePassive = true;
        var player = Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Single(p => p.IsOwner);
        player.GetComponent<PlayerHealth>().EnterLevel(player.transform.position);
        return "Native practice-passive state enabled for current enemies; player revived for input inspection";
    }
    public static string Observe()
    {
        var player = Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Single(p=>p.IsOwner);
        var input = player.GetComponent<PlayerInputs>();
        input.OnTechInput += () => Debug.Log("MANUAL INPUT: Heavy Attack -> " + player.CurrentAction);
        input.OnAttackInput += () => Debug.Log("MANUAL INPUT: Light Attack -> " + player.CurrentAction);
        input.OnSkillInput += index => Debug.Log("MANUAL INPUT: Skill " + index + " cost " + player.Cost(index) + " -> " + player.CurrentAction + ", MP " + player.Mana);
        return "Manual input observation connected until Play Mode ends";
    }
}
