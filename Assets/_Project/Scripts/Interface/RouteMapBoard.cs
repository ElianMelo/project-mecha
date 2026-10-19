using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// Lives in LevelRouting.unity. The camera renders this scene directly to the display.
public sealed class RouteMapBoard : MonoBehaviour
{
    [SerializeField] private Camera view;
    [SerializeField] private Transform content;
    [SerializeField] private RouteMapNodeView nodePrefab;
    [SerializeField] private TMP_Text title, previewTitle, previewBody, previewState, roster, countdown, help;
    [SerializeField] private Animator countdownAnimator;
    [SerializeField] private RouteMapCamera depthCamera;
    [SerializeField] private Transform tableSurface;
    [SerializeField] private Transform[] tableRails, depthDividers;
    [SerializeField] private RouteRandomButton randomRoute;
    public UnityEngine.UI.Button RandomRouteButton => randomRoute;
    private readonly List<Transform> dividers = new List<Transform>();
    private int displayedSecond = -1;
    private sealed class Edge { public int from, to; public SpriteRenderer[] dashes; }
    private readonly List<Edge> edges = new List<Edge>();
    private readonly List<RouteMapNodeView> nodes = new List<RouteMapNodeView>();
    private LevelRouteDirector route;
    public Camera View => view;
    public IReadOnlyList<RouteMapNodeView> Nodes => nodes;
    public void Build(LevelRouteDirector director, RouteMapMenu owner)
    {
        Clear(); route = director; displayedSecond = -1;
        randomRoute.onClick.RemoveAllListeners(); randomRoute.onClick.AddListener(owner.RandomRoute);
        int steps = route.Nodes.Length == 0 ? 2 : route.Nodes.Max(node => node.depth) + 1;
        ConfigureTable(route.Settings, steps);
        depthCamera.Configure(route.Settings, steps);
        foreach (var node in route.Nodes)
        {
            var control = Instantiate(nodePrefab, content);
            control.name = GameText.Get("Routing", "node.name") + node.id;
            control.transform.localPosition = Position(node);
            control.Bind(owner, route, node, view);
            nodes.Add(control);
            foreach (int next in node.next) CreateEdge(node.id, next);
        }
        foreach (var child in content.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = route.Settings.boardLayer;
        randomRoute.Board=this;
        randomRoute.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.None};
    }
    public void ConfigureTable(LevelRouteSettings settings, int steps)
    {
        float span = (steps - 1) * settings.nodeSpacing.y;
        foreach (var part in new[] { tableSurface }.Concat(tableRails))
        {
            var position = part.localPosition; position.z = span * 0.5f; part.localPosition = position;
            var scale = part.localScale; scale.z = span + settings.tableDepthPadding * 2f; part.localScale = scale;
        }
        if (dividers.Count == 0) dividers.AddRange(depthDividers);
        for (int depth = 0; depth < steps; depth++)
        {
            if (depth >= dividers.Count) { var extra = Instantiate(dividers[0], dividers[0].parent); extra.name = "Depth Divider"; dividers.Add(extra); }
            var line = dividers[depth]; line.gameObject.SetActive(true);
            var position = line.localPosition; position.z = depth * settings.nodeSpacing.y - settings.tableDepthPadding * 0.6f; line.localPosition = position;
        }
        for (int depth = steps; depth < dividers.Count; depth++) dividers[depth].gameObject.SetActive(false);
    }
    public void FocusDepth(int node) { if (node >= 0 && node < route.Nodes.Length) depthCamera.Focus(route.Nodes[node]); }
    public void FocusAvailable()
    {
        var first=nodes.FirstOrDefault(n=>route.IsAvailable(n.NodeId));
        if(first != null) UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(first.gameObject);
    }
    public void SuspendCamera() => depthCamera.Suspend();
    public Vector3 Position(LevelRouteNode node) => MapPosition(route.Settings, node);
    public static Vector3 MapPosition(LevelRouteSettings settings, LevelRouteNode node)
    {
        // Four lanes share the existing three-lane footprint and camera framing.
        float spacing = settings.nodeSpacing.x * Mathf.Min(1f, 2f / Mathf.Max(1, node.width - 1));
        return new Vector3((node.lane - (node.width - 1) * 0.5f) * spacing, 0.2f, node.depth * settings.nodeSpacing.y);
    }
    private void CreateEdge(int from, int to)
    {
        Vector3 a = Position(route.Nodes[from]), b = Position(route.Nodes[to]); a.y = b.y = 0.12f;
        int count = Mathf.Max(2, Mathf.FloorToInt(Vector3.Distance(a, b) / route.Settings.pathDashSpacing));
        var dashes = new SpriteRenderer[count - 1];
        for (int i = 1; i < count; i++)
        {
            var go = new GameObject("Route Path Dot"); go.transform.SetParent(content, false);
            go.transform.localPosition = Vector3.Lerp(a, b, i / (float)count);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); go.transform.localScale = Vector3.one * 1.6f;
            var sprite = go.AddComponent<SpriteRenderer>(); sprite.sprite = route.Settings.pathSprite;
            sprite.sharedMaterial = route.Settings.pathMaterial; dashes[i - 1] = sprite;
        }
        edges.Add(new Edge { from = from, to = to, dashes = dashes });
    }
    public RouteMapNodeView Node(int id) => nodes.FirstOrDefault(x => x.NodeId == id);
    public int Neighbour(int id, Vector2 direction)
    {
        var origin = Node(id); if (origin == null) return id;
        int depth = route.Nodes[id].depth;
        float x = origin.transform.localPosition.x;
        bool vertical = Mathf.Abs(direction.y) > Mathf.Abs(direction.x);
        int targetDepth = depth + (vertical ? direction.y > 0f ? 1 : -1 : 0);
        // Navigate one row at a time, even when earlier rows are outside the camera.
        // Screen projection would invert neighbours once a long map extends behind the view.
        var candidates = nodes.Where(node => node.NodeId != id && node.gameObject.activeInHierarchy && route.Nodes[node.NodeId].depth == targetDepth &&
            (vertical || (node.transform.localPosition.x - x) * direction.x > 0f));
        return candidates.OrderBy(node => Mathf.Abs(node.transform.localPosition.x - x)).ThenBy(node => node.NodeId).FirstOrDefault()?.NodeId ?? id;
    }
    public void Refresh(int focused)
    {
        randomRoute.interactable=route.CanVote;
        foreach (var node in nodes) node.Refresh(node.NodeId == focused);
        foreach (var edge in edges)
        {
            bool travelled = route.Visited.Contains(edge.from) && route.Visited.Contains(edge.to);
            bool reachable = edge.from == route.ChoiceOrigin && route.IsAvailable(edge.to);
            int plannedIndex = System.Array.IndexOf(route.Planned, edge.to);
            bool planned = plannedIndex >= 0 && (plannedIndex > 0 ? route.Planned[plannedIndex - 1] == edge.from : edge.from == route.CurrentNode);
            bool preview = reachable && (edge.from == focused || edge.to == focused);
            bool chosen = reachable && edge.to == route.Winner && route.IsRouting;
            Color color = travelled || planned || chosen ? route.Settings.visitedColor : preview ? Color.white : reachable ? route.Settings.reachableColor : route.Settings.lockedColor;
            foreach (var dash in edge.dashes) dash.color = color;
        }
    }
    public void ShowDetails(int id, int pending)
    {
        if (id < 0 || id >= route.Nodes.Length) return;
        var node = route.Nodes[id]; var room = route.Encounter(node.encounterId);
        if (room == null) return;
        title.text = GameText.Get("Routing", "expedition.heading") + route.Sector.ToString("00");
        if (!route.IsRevealed(id))
        {
            previewTitle.text = GameText.Get("Routing", "node.unknown");
            previewBody.text = string.Empty;
            previewState.text = string.Empty;
            roster.text = string.Empty;
            return;
        }
        previewTitle.text = GameText.Get(room.displayNameLocalized).ToUpperInvariant();
        previewBody.text = GameText.Get("Routing", "encounter.heading") + (node.depth + 1).ToString("00") + " / " + GameText.Term("Routing", room.shape).ToUpperInvariant() + "\n\n" + GameText.Get(room.descriptionLocalized) + "\n\n" + GameText.Get(room.rewardPreviewLocalized);
        bool voted = route.Pilots.Any(x => x.playerId == route.ClientManager.Connection.ClientId && x.nodeId == id);
        previewState.text = pending == -2 && voted ? GameText.Get("Routing", "vote.withdrawing") : route.Winner == id ? GameText.Get("Routing", "destination.confirmed") : voted ? GameText.Get("Routing", "vote.yours") : pending == id ? GameText.Get("Routing", "vote.sending") : route.IsAvailable(id) ? GameText.Get("Routing", "destination.available") : route.Visited.Contains(id) ? GameText.Get("Routing", "destination.cleared") : route.Planned.Contains(id) ? GameText.Get("Routing", "destination.planned") : GameText.Get("Routing", "destination.scouting");
        previewState.color = route.IsAvailable(id) ? route.Settings.reachableColor :
            route.Visited.Contains(id) || route.Planned.Contains(id) ? route.Settings.visitedColor : route.Settings.lockedLabelColor;
        roster.text = GameText.Get("Routing", "squad.heading") + string.Join("\n", route.Pilots.Select((pilot, index) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(route.Settings.squad.PilotColor(pilot.colorSlot)) + ">P" + (index + 1) + "</color> " + pilot.name.Replace("<", "").Replace(">", "") + " / " +
            (pilot.nodeId < 0 ? GameText.Get("Routing", "vote.choosing") : route.Encounter(route.Nodes[pilot.nodeId].encounterId) is RouteEncounterDefinition intent ? GameText.Get(intent.displayNameLocalized) : string.Empty)));
    }
    public void UpdateClock()
    {
        int seconds = Mathf.Max(1, Mathf.CeilToInt(route.SecondsRemaining));
        int path = route.Planned.Length + 1;
        countdown.text = route.Phase == RouteVotePhase.SoftWait ? GameText.Get("Routing", "planning.waiting", path, route.PlanningTarget) : route.Phase == RouteVotePhase.Locked ?
            (path == route.PlanningTarget ? GameText.Get("Routing", "deployment.countdown") + seconds : GameText.Get("Routing", "planning.confirmed", path, route.PlanningTarget, seconds)) : GameText.Get("Routing", "planning.select", path, route.PlanningTarget);
        if (countdownAnimator != null)
        {
            countdownAnimator.enabled = route.Settings.feel == null || route.Settings.feel.motionEnabled;
            if (countdownAnimator.enabled && seconds != displayedSecond && route.Phase == RouteVotePhase.Locked) countdownAnimator.SetTrigger("Pulse");
        }
        displayedSecond = seconds;
        countdown.color = route.Phase == RouteVotePhase.Locked || route.Phase == RouteVotePhase.TieReveal ? route.Settings.visitedColor : route.Settings.reachableColor;
        help.text = GameText.Get("Routing", "controls.scout") + GameInputPresentation.Hint("Submit") + GameText.Get("Routing", "controls.select") +
            (GameInputPresentation.UsingGamepad ? GameInputPresentation.Hint("Cancel") + GameText.Get("Routing", "controls.withdraw") : "") + GameText.Get("Routing", "controls.options", GameInputPresentation.Hint("Menu"));
    }
    public void Confirm(int node) => Node(node)?.Confirm();
    public void Clear()
    {
        edges.Clear(); nodes.Clear();
        if (content != null) foreach (Transform child in content) Destroy(child.gameObject);
    }
}
