using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// PhysicsRaycaster selects the same world GameObject that controller navigation selects.
public sealed class RouteMapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler
{
    [SerializeField] private SpriteRenderer ring, focusRing;
    [SerializeField] private SpriteRenderer[] votePips;
    [SerializeField] private TMP_Text label, state, votes;
    private RouteMapMenu menu;
    private LevelRouteDirector route;
    private Animator model;
    private GameObject miniature;
    private string encounterLabel;
    private Vector3 labelPosition;
    private Vector2 labelSize;
    private float labelFontSize;
    private sealed class ModelTint
    {
        public MeshRenderer renderer;
        public int materialIndex, colorProperty;
        public Color originalColor;
    }
    private readonly List<ModelTint> modelTints = new List<ModelTint>();
    private MaterialPropertyBlock modelProperties;
    private float displayedBrightness = -1f;
    public int NodeId { get; private set; }
    public void Bind(RouteMapMenu owner, LevelRouteDirector director, LevelRouteNode node, Camera camera)
    {
        menu = owner; route = director; NodeId = node.id;
        var room = route.Encounter(node.encounterId);
        encounterLabel = room != null ? node.depth == 0 && room.encounterId == "insertion" ? GameText.Get("Routing", "node.insertion") : GameText.Term("Routing", room.shape).ToUpperInvariant() + " / " + (node.depth + 1).ToString("00") : GameText.Get("Routing", "node.unavailable");
        labelPosition = label.transform.localPosition;
        labelSize = label.rectTransform.sizeDelta;
        labelFontSize = label.fontSize;
        foreach (var text in new[] {label,state,votes}) text.transform.rotation = camera.transform.rotation;
        if (room != null && room.mapModel != null)
        {
            miniature = Instantiate(room.mapModel, transform); miniature.transform.localPosition = Vector3.zero;
            model = miniature.GetComponent<Animator>();
            if (model != null) model.enabled = route.Settings.feel == null || route.Settings.feel.motionEnabled;
            CacheModelColors(miniature);
        }
        Refresh(false);
    }
    public void Refresh(bool focused)
    {
        bool revealed = route.IsRevealed(NodeId);
        if (miniature != null) miniature.SetActive(revealed);
        label.text = revealed ? encounterLabel : GameText.Get("Routing", "node.unknown");
        label.transform.localPosition = revealed ? labelPosition : new Vector3(labelPosition.x, 2.2f, 0f);
        label.rectTransform.sizeDelta = revealed ? labelSize : new Vector2(labelSize.x, labelSize.y * 3f);
        label.fontSize = revealed ? labelFontSize : labelFontSize * 2f;
        bool available = route.IsAvailable(NodeId), visited = route.Visited.Contains(NodeId), planned = route.Planned.Contains(NodeId);
        bool winner = route.Winner == NodeId && route.IsRouting;
        SetModelBrightness(winner || available ? route.Settings.reachableModelBrightness :
            visited ? route.Settings.visitedModelBrightness : planned ? 1f : route.Settings.lockedModelBrightness);
        ring.color = winner || visited || planned ? route.Settings.visitedColor : available ? route.Settings.reachableColor : route.Settings.lockedColor;
        ring.enabled = revealed;
        focusRing.enabled = focused; // Persistent even with every Animator disabled.
        state.text = !revealed ? string.Empty : winner ? GameText.Get("Routing", "node.confirmed") : available ? GameText.Get("Routing", "node.select") : visited ? GameText.Get("Routing", "destination.cleared") : planned ? GameText.Get("Routing", "destination.planned") : GameText.Get("Routing", "node.scout");
        state.color = ring.color;
        var supporters = route.Pilots.Where(x => x.nodeId == NodeId).ToArray();
        votes.text = !revealed ? string.Empty : string.Join("  ", supporters.Select(x => GameText.Get("Routing", "pilot.prefix") + (System.Array.FindIndex(route.Pilots, p => p.playerId == x.playerId) + 1)));
        for (int i = 0; i < votePips.Length; i++)
        {
            votePips[i].enabled = revealed && i < supporters.Length;
            if (i < supporters.Length) votePips[i].color = route.Settings.squad.PilotColor(supporters[i].colorSlot);
        }
        if (model != null && model.enabled) model.SetBool("Focused", focused);
        // Scouting focus must not make an unavailable destination look selectable.
        label.color = winner || available ? Color.white : visited || planned ? route.Settings.visitedColor : route.Settings.lockedLabelColor;
    }
    private void CacheModelColors(GameObject miniature)
    {
        int baseColor = Shader.PropertyToID(GameText.Get("Routing", "shader.baseColor")), legacyColor = Shader.PropertyToID(GameText.Get("Routing", "shader.color"));
        foreach (var renderer in miniature.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null) continue;
                int property = material.HasProperty(baseColor) ? baseColor : legacyColor;
                if (!material.HasProperty(property)) continue;
                modelTints.Add(new ModelTint { renderer = renderer, materialIndex = i,
                    colorProperty = property, originalColor = material.GetColor(property) });
            }
        }
    }
    private void SetModelBrightness(float brightness)
    {
        if (Mathf.Approximately(displayedBrightness, brightness)) return;
        displayedBrightness = brightness;
        if (modelProperties == null) modelProperties = new MaterialPropertyBlock();
        foreach (var tint in modelTints)
        {
            var color = tint.originalColor;
            color.r *= brightness; color.g *= brightness; color.b *= brightness;
            tint.renderer.GetPropertyBlock(modelProperties, tint.materialIndex);
            modelProperties.SetColor(tint.colorProperty, color);
            tint.renderer.SetPropertyBlock(modelProperties, tint.materialIndex);
        }
    }
    public void Confirm() { if (model != null && model.enabled) model.SetTrigger("Confirm"); }
    public void OnPointerEnter(PointerEventData data) => menu?.PointerFocus(NodeId, data.position, data.delta);
    public void OnPointerMove(PointerEventData data) => menu?.PointerFocus(NodeId, data.position, data.delta);
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || menu == null || !menu.CanInteract) return;
        menu.PointerFocus(NodeId, data.position, data.delta, true); menu.Confirm(NodeId);
    }
    public void OnSelect(BaseEventData data) { menu?.Focus(NodeId); GameAudio.Play(GameAudioCue.UiMove); }
    public void OnDeselect(BaseEventData data) { if (focusRing != null) focusRing.enabled = false; }
    public void OnMove(AxisEventData data)
    {
        if (menu == null || !menu.CanInteract) return;
        menu.Move(NodeId, data.moveDir); data.Use();
    }
    public void OnSubmit(BaseEventData data) { if (menu != null && menu.CanInteract) menu.Confirm(NodeId); data.Use(); }
}
