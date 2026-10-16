using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Combat/Configurator")]
public sealed class ComboConfiguratorSettings : ScriptableObject
{
    public PlayerCombatSettings combat;
    public ComboLoadout loadout;
    public ComboDefinition[] presets;
    public int maximumSteps = 8;
    public Vector3 previewSpawn = new Vector3(2f, 0.03f, 0f);
}
