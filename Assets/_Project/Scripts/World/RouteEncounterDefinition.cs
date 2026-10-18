using FishNet.Object;
using UnityEngine;

// Adding an enabled definition under Resources/RouteEncounters registers a room automatically.
[CreateAssetMenu(menuName = "Mecha/Routing/Encounter")]
public sealed class RouteEncounterDefinition : ScriptableObject
{
    public UnityEngine.Localization.LocalizedString displayNameLocalized = new UnityEngine.Localization.LocalizedString();
    public UnityEngine.Localization.LocalizedString descriptionLocalized = new UnityEngine.Localization.LocalizedString();
    public UnityEngine.Localization.LocalizedString rewardPreviewLocalized = new UnityEngine.Localization.LocalizedString();
    public string encounterId;
    [HideInInspector] public string displayName = "Enemy patrol";
    [TextArea] [HideInInspector] public string description = "Defeat every enemy to open the exit.";
    [HideInInspector] public string rewardPreview = "Clear the encounter to continue.";
    public LevelShape shape;
    public ProceduralLevelSettings roomSettings;
    [Tooltip("Optional server-spawned encounter. Empty uses the existing enemy encounter.")]
    public NetworkObject encounterPrefab;
    public GameObject mapModel;
    public Sprite icon;
    public Color color = new Color(0.25f, 0.85f, 1f);
    public bool includeInRoutes = true;
    [Min(0.01f)] public float weight = 1f;
    [Min(1)] public int minimumDepth = 1;
    [Min(1)] public int maximumDepth = 99;
}
