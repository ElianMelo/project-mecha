using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ApplyReferenceCadence
{
    public static object Apply()
    {
        MechaCadenceSetup.Apply();
        var hud = Object.FindFirstObjectByType<MechaGameplayHud>();
        return new { hud = hud.name, pieces = MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat").pieces.Select(p => p.name).ToArray(),
            buttons = hud.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b => new { b.name, listeners = b.onClick.GetPersistentEventCount() }).ToArray() };
    }
}
