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
        foreach (var node in route.Nodes)
        {
            var control = Instantiate(nodePrefab, content);
            control.name = "Route " + node.id;
            control.transform.localPosition = Position(node);
            control.Bind(owner, route, node, view);
            nodes.Add(control);
            foreach (int next in node.next) CreateEdge(node.id, next);
        }
        foreach (var child in content.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = route.Settings.boardLayer;
    }
    public Vector3 Position(LevelRouteNode node) => new Vector3((node.lane - (node.width - 1) * 0.5f) * route.Settings.nodeSpacing.x, 0.2f, node.depth * route.Settings.nodeSpacing.y);
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
        Vector2 point = view.WorldToViewportPoint(origin.transform.position);
        int best = id; float score = float.MaxValue;
        foreach (var node in nodes)
        {
            if (node.NodeId == id || !node.gameObject.activeInHierarchy) continue;
            Vector2 delta = (Vector2)view.WorldToViewportPoint(node.transform.position) - point;
            float forward = Vector2.Dot(delta, direction);
            float side = Mathf.Abs(Vector2.Dot(delta, new Vector2(-direction.y, direction.x)));
            if (forward <= 0.005f || forward < side * 0.6f) continue;
            float value = forward + side * 3f;
            if (value < score) { score = value; best = node.NodeId; }
        }
        return best;
    }
    public void Refresh(int focused)
    {
        foreach (var node in nodes) node.Refresh(node.NodeId == focused);
        foreach (var edge in edges)
        {
            bool travelled = route.Visited.Contains(edge.from) && route.Visited.Contains(edge.to);
            bool reachable = edge.from == route.CurrentNode && route.IsAvailable(edge.to);
            bool preview = reachable && (edge.from == focused || edge.to == focused);
            bool chosen = reachable && edge.to == route.Winner && route.IsRouting;
            Color color = travelled || chosen ? route.Settings.visitedColor : preview ? Color.white : reachable ? route.Settings.reachableColor : route.Settings.lockedColor;
            foreach (var dash in edge.dashes) dash.color = color;
        }
    }
    public void ShowDetails(int id, int pending)
    {
        if (id < 0 || id >= route.Nodes.Length) return;
        var node = route.Nodes[id]; var room = route.Encounter(node.encounterId);
        if (room == null) return;
        title.text = "EXPEDITION / " + route.Sector.ToString("00");
        previewTitle.text = room.displayName.ToUpperInvariant();
        previewBody.text = "ENCOUNTER " + (node.depth + 1).ToString("00") + " / " + room.shape.ToString().ToUpperInvariant() + "\n\n" + room.description + "\n\n" + room.rewardPreview;
        bool voted = route.Pilots.Any(x => x.playerId == route.ClientManager.Connection.ClientId && x.nodeId == id);
        previewState.text = route.Winner == id ? "DESTINATION CONFIRMED" : voted ? "YOUR VOTE" : pending == id ? "SENDING VOTE..." : route.IsAvailable(id) ? "AVAILABLE / CONFIRM TO SELECT" : route.Visited.Contains(id) ? "CLEARED" : "SCOUTING AHEAD";
        previewState.color = route.IsAvailable(id) ? route.Settings.reachableColor :
            route.Visited.Contains(id) ? route.Settings.visitedColor : route.Settings.lockedLabelColor;
        roster.text = "SQUAD INTENT\n" + string.Join("\n", route.Pilots.Select((pilot, index) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(route.Settings.squad.PilotColor(pilot.colorSlot)) + ">P" + (index + 1) + "</color> " + pilot.name.Replace("<", "").Replace(">", "") + " / " +
            (pilot.nodeId < 0 ? "Choosing" : route.Encounter(route.Nodes[pilot.nodeId].encounterId)?.displayName)));
    }
    public void UpdateClock()
    {
        int seconds = Mathf.Max(1, Mathf.CeilToInt(route.SecondsRemaining));
        countdown.text = route.Phase == RouteVotePhase.SoftWait ? "WAITING FOR VOTES / " + seconds : route.Phase == RouteVotePhase.Locked ?
            (route.Candidates.Length > 1 ? "TIED VOTE / ROLL IN " : "DEPLOYING IN ") + seconds : route.Phase == RouteVotePhase.TieReveal ? "ROUTE CONFIRMED" : "SELECT NEXT ENCOUNTER";
        if (countdownAnimator != null)
        {
            countdownAnimator.enabled = route.Settings.feel == null || route.Settings.feel.motionEnabled;
            if (countdownAnimator.enabled && seconds != displayedSecond && route.Phase == RouteVotePhase.Locked) countdownAnimator.SetTrigger("Pulse");
        }
        displayedSecond = seconds;
        countdown.color = route.Phase == RouteVotePhase.Locked || route.Phase == RouteVotePhase.TieReveal ? route.Settings.visitedColor : route.Settings.reachableColor;
        help.text = "D-PAD / STICK / ARROWS  Scout\n" + GameInputPresentation.Hint("Submit") + " / CLICK  Select\n" +
            (GameInputPresentation.UsingGamepad ? GameInputPresentation.Hint("Cancel") + "  Withdraw vote\n" : "") + "START / ESC  Options";
    }
    public void Confirm(int node) => Node(node)?.Confirm();
    public void Clear()
    {
        edges.Clear(); nodes.Clear();
        if (content != null) foreach (Transform child in content) Destroy(child.gameObject);
    }
}
