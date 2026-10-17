using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// PhysicsRaycaster selects the same world GameObject that controller navigation selects.
public sealed class RouteMapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler
{
    [SerializeField] private SpriteRenderer ring, focusRing;
    [SerializeField] private SpriteRenderer[] votePips;
    [SerializeField] private TMP_Text label, state, votes;
    private RouteMapMenu menu;
    private LevelRouteDirector route;
    private Animator model;
    public int NodeId { get; private set; }
    public void Bind(RouteMapMenu owner, LevelRouteDirector director, LevelRouteNode node, Camera camera)
    {
        menu = owner; route = director; NodeId = node.id;
        var room = route.Encounter(node.encounterId);
        label.text = room != null ? node.depth == 0 && room.encounterId == "insertion" ? "INSERTION" : room.shape.ToString().ToUpperInvariant() + " / " + (node.depth + 1).ToString("00") : "UNAVAILABLE";
        foreach (var text in new[] {label,state,votes}) text.transform.rotation = camera.transform.rotation;
        if (room != null && room.mapModel != null)
        {
            var miniature = Instantiate(room.mapModel, transform); miniature.transform.localPosition = Vector3.zero;
            model = miniature.GetComponent<Animator>();
            if (model != null) model.enabled = route.Settings.feel == null || route.Settings.feel.motionEnabled;
        }
        Refresh(false);
    }
    public void Refresh(bool focused)
    {
        bool available = route.IsAvailable(NodeId), visited = route.Visited.Contains(NodeId);
        bool winner = route.Winner == NodeId && route.IsRouting;
        ring.color = winner || visited ? route.Settings.visitedColor : available ? route.Settings.reachableColor : route.Settings.lockedColor;
        focusRing.enabled = focused; // Persistent even with every Animator disabled.
        state.text = winner ? "CONFIRMED" : available ? "SELECT ROUTE" : visited ? "CLEARED" : "SCOUT AHEAD";
        state.color = ring.color;
        var supporters = route.Pilots.Where(x => x.nodeId == NodeId).ToArray();
        votes.text = string.Join("  ", supporters.Select(x => "P" + (System.Array.FindIndex(route.Pilots, p => p.playerId == x.playerId) + 1)));
        for (int i = 0; i < votePips.Length; i++)
        {
            votePips[i].enabled = i < supporters.Length;
            if (i < supporters.Length) votePips[i].color = route.Settings.squad.PilotColor(supporters[i].colorSlot);
        }
        if (model != null && model.enabled) model.SetBool("Focused", focused);
        label.color = focused || available || visited ? Color.white : new Color(0.67f,0.75f,0.82f);
    }
    public void Confirm() { if (model != null && model.enabled) model.SetTrigger("Confirm"); }
    public void OnPointerEnter(PointerEventData data) { if (menu != null && menu.CanInteract) EventSystem.current?.SetSelectedGameObject(gameObject); }
    public void OnPointerClick(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left && menu != null && menu.CanInteract) menu.Confirm(NodeId); }
    public void OnSelect(BaseEventData data) { menu?.Focus(NodeId); GameAudio.Play(GameAudioCue.UiMove); }
    public void OnDeselect(BaseEventData data) { if (focusRing != null) focusRing.enabled = false; }
    public void OnMove(AxisEventData data)
    {
        if (menu == null || !menu.CanInteract) return;
        menu.Move(NodeId, data.moveDir); data.Use();
    }
    public void OnSubmit(BaseEventData data) { if (menu != null && menu.CanInteract) menu.Confirm(NodeId); data.Use(); }
}
