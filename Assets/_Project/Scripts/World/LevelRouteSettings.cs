using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Routing/Settings")]
public sealed class LevelRouteSettings : ScriptableObject
{
    public RouteEncounterDefinition openingEncounter;
    [Tooltip("One entry per depth. 1 creates a forced corridor or merge; larger values create branches.")]
    public int[] rowWidths = { 1, 3, 2, 1, 3, 1 };
    [Min(0.1f)] public float softVoteSeconds = 5f;
    [Min(1f)] public float confirmedVoteSeconds = 3f;
    [Min(0.1f)] public float tieRevealSeconds = 1f;
    [Min(0.1f)] public float soloConfirmSeconds = 0.3f;
    [Min(0.05f)] public float snapshotInterval = 0.15f;
    [Min(0.05f)] public float voteRepeatSeconds = 0.15f;
    public Vector2 nodeSpacing = new Vector2(9f, 5.1f);
    public GameObject boardPrefab;
    public string routingScenePath = "Assets/Scenes/LevelRouting.unity";
    public Sprite pathSprite;
    public Sprite focusSprite;
    public Material pathMaterial;
    public UiFeelSettings feel;
    public SquadPresentationSettings squad;
    public Color reachableColor = new Color(0.24f, 0.92f, 0.93f);
    public Color visitedColor = new Color(0.98f, 0.7f, 0.25f);
    public Color lockedColor = new Color(0.46f, 0.55f, 0.64f);
    [Range(24, 31)] public int boardLayer = 30;
    [Min(0.1f)] public float pathDashSpacing = 0.65f;
    [Min(0.01f)] public float hapticSeconds = 0.065f;
    [Range(0f, 1f)] public float hapticStrength = 0.13f;
}
