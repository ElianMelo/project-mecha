using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Routing/Settings")]
public sealed class LevelRouteSettings : ScriptableObject
{
    public RouteEncounterDefinition openingEncounter;
    [Tooltip("Total steps, including the single starting node. Later rows are randomized for each sector.")]
    [Range(2, 30)] public int stepCount = 10;
    [Range(1, 4)] public int minimumRowWidth = 1;
    [Range(2, 4)] public int maximumRowWidth = 4;
    [Tooltip("Maximum consecutive rows with the same node count. The width range must permit at least two different counts for longer routes.")]
    [Range(1, 2)] public int maximumRowWidthStreak = 2;
    [Tooltip("Maximum consecutive nodes with only one outgoing choice along any path.")]
    [Range(1, 2)] public int maximumStraightStreak = 2;
    [Tooltip("Visible path length starting at each available choice, inclusive. Three reveals the available node and two connected steps beyond it.")]
    [Min(1)] public int revealedStepsAhead = 3;
    [Min(0.1f)] public float softVoteSeconds = 5f;
    [Min(1f)] public float confirmedVoteSeconds = 3f;
    [Min(0.1f)] public float tieRevealSeconds = 1f;
    [Min(0.1f)] public float soloConfirmSeconds = 0.3f;
    [Min(0.05f)] public float snapshotInterval = 0.15f;
    [Min(0.05f)] public float voteRepeatSeconds = 0.15f;
    public Vector2 nodeSpacing = new Vector2(9f, 5.1f);
    [Header("Map depth scrolling")]
    [Tooltip("SmoothDamp time in seconds for camera depth scouting. Applies independently of decorative UI motion.")]
    [Min(0.05f)] public float cameraDepthDamping = 0.65f;
    [Range(0.2f, 0.8f)] public float cameraFocusViewport = 0.6f;
    [Range(0.6f, 0.95f)] public float cameraFarViewport = 0.86f;
    [Tooltip("Extra forward camera travel beyond the far viewport limit, in board units. Lets distant nodes reach the focus viewport.")]
    [Min(0f)] public float cameraFarDepthOffset = 18f;
    [Min(0.1f)] public float tableDepthPadding = 3f;
    public GameObject boardPrefab;
    public string routingScenePath = "Assets/Scenes/LevelRouting.unity";
    public Sprite pathSprite;
    public Sprite focusSprite;
    public Material pathMaterial;
    public UiFeelSettings feel;
    public SquadPresentationSettings squad;
    public Color reachableColor = new Color(0.3f, 1f, 1f);
    public Color visitedColor = new Color(0.98f, 0.7f, 0.25f);
    public Color lockedColor = new Color(0.17f, 0.23f, 0.28f);
    public Color lockedLabelColor = new Color(0.38f, 0.45f, 0.51f);
    [Header("Miniature availability")]
    [Tooltip("Multiplies each miniature's original material color without changing shared materials.")]
    [Range(0f, 2f)] public float reachableModelBrightness = 1.4f;
    [Range(0f, 2f)] public float visitedModelBrightness = 0.45f;
    [Range(0f, 2f)] public float lockedModelBrightness = 0.16f;
    [Range(24, 31)] public int boardLayer = 30;
    [Min(0.1f)] public float pathDashSpacing = 0.65f;
    [Min(0.01f)] public float hapticSeconds = 0.065f;
    [Range(0f, 1f)] public float hapticStrength = 0.13f;
}
