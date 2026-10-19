using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Keeps network/gameplay objects resident; loads only the local 3D routing presentation additively.
public sealed class RouteMapMenu : MonoBehaviour
{
    private LevelRouteDirector route;
    private RouteMapBoard board;
    private LevelRouteNode[] graph;
    private readonly Dictionary<Camera,bool> cameras = new Dictionary<Camera,bool>();
    private readonly Dictionary<CinemachineBrain,bool> cameraBrains = new Dictionary<CinemachineBrain,bool>();
    private readonly Dictionary<Canvas,bool> canvases = new Dictionary<Canvas,bool>();
    private readonly Dictionary<int,bool> layerCollisions = new Dictionary<int,bool>();
    private int routingLayer;
    private readonly Dictionary<int,int> previousVotes = new Dictionary<int,int>();
    private bool changingScene;
    private int focused = -1, round = -1, displayedSecond = -1, pendingNode = -1;
    private float pendingUntil, hapticUntil;
    private Gamepad hapticPad;
    private RouteVotePhase lastPhase;
    private GameObject previousSelection;
    private bool open;
    private string loadedScene;
    private Vector2 lastPointerPosition;
    private bool pointerTracked;
    public bool IsVisible => open && board != null;
    public int FocusedNode => focused;
    public bool CanInteract => IsVisible && (GameManager.Instance == null || !GameManager.Instance.MenuOpen) && !MechaChat.IsTyping;
    private void Update()
    {
        if (route == null)
        {
            route = LevelRouteDirector.Instance;
            if (route != null) route.Changed += Refresh;
        }
        if (hapticPad != null && Time.unscaledTime >= hapticUntil) StopHaptic();
        if (pendingNode != -1 && Time.unscaledTime >= pendingUntil) pendingNode = -1;
        bool wanted = route != null && route.IsRouting && route.IsClientInitialized;
        if (!changingScene && wanted != open) { changingScene = true; StartCoroutine(ChangeScene(wanted)); }
        if (!IsVisible) return;
        if (CanInteract) EnsureFocus();
        board.UpdateClock();
        int seconds = Mathf.CeilToInt(route.SecondsRemaining);
        if (seconds != displayedSecond)
        {
            displayedSecond = seconds;
            if (route.Phase == RouteVotePhase.Locked && seconds > 0) GameAudio.Play(GameAudioCue.UiMove);
        }
    }
    private IEnumerator ChangeScene(bool show)
    {
        if (show)
        {
            // Selection colliders must never push actors in the still-resident combat scene.
            routingLayer = route.Settings.boardLayer;
            for (int layer = 0; layer < 32; layer++)
            {
                layerCollisions[layer] = Physics.GetIgnoreLayerCollision(routingLayer,layer);
                Physics.IgnoreLayerCollision(routingLayer,layer,true);
            }
            loadedScene = route.Settings.routingScenePath;
            var existing = SceneManager.GetSceneByPath(loadedScene);
            if (!existing.isLoaded) yield return SceneManager.LoadSceneAsync(loadedScene, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(loadedScene);
            board = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RouteMapBoard>(true)).FirstOrDefault();
            if (board == null) { Debug.LogError("The routing scene has no RouteMapBoard."); RestoreCollisions(); changingScene = false; yield break; }
            previousSelection = EventSystem.current?.currentSelectedGameObject;
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera.gameObject.scene != scene)
                {
                    cameras[camera] = camera.enabled; camera.enabled = false;
                    if (camera.TryGetComponent<CinemachineBrain>(out var brain)) { cameraBrains[brain] = brain.enabled; brain.enabled = false; }
                }
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.gameObject.scene != scene && canvas.name != "Game Options" && canvas.name != "Audio Configuration" && canvas.name != "Mecha Lobby UI" && canvas.name != "Controller Keyboard")
                { canvases[canvas] = canvas.enabled; canvas.enabled = false; }
            open = true; graph = null; round = -1; pointerTracked = false;
            Refresh(); if (CanInteract) EnsureFocus(); GameAudio.Play(GameAudioCue.UiOpen);
        }
        else
        {
            open = false; board?.SuspendCamera(); board = null; graph = null; pendingNode = -1; previousVotes.Clear(); StopHaptic();
            RestoreGameplay();
            var scene = SceneManager.GetSceneByPath(loadedScene);
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            RestoreCollisions();
            GameManager.Instance?.SuppressGameplayInput();
            EventSystem.current?.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        }
        changingScene = false;
    }
    private void RestoreGameplay()
    {
        foreach (var pair in cameras) if (pair.Key != null) pair.Key.enabled = pair.Value;
        foreach (var pair in cameraBrains) if (pair.Key != null) pair.Key.enabled = pair.Value;
        foreach (var pair in canvases) if (pair.Key != null) pair.Key.enabled = pair.Value;
        cameras.Clear(); cameraBrains.Clear(); canvases.Clear();
    }
    private void RestoreCollisions()
    {
        foreach (var pair in layerCollisions) Physics.IgnoreLayerCollision(routingLayer,pair.Key,pair.Value);
        layerCollisions.Clear();
    }
    private void Refresh()
    {
        if (!IsVisible || !route.IsRouting) return;
        if (graph != route.Nodes) { graph = route.Nodes; focused = -1; board.Build(route, this); }
        if (round != route.Round)
        {
            round = route.Round; displayedSecond = -1; pendingNode = -1; previousVotes.Clear();
            var first = board.Nodes.FirstOrDefault(x => route.IsAvailable(x.NodeId));
            if (first != null) { focused = first.NodeId; if (CanInteract) EventSystem.current?.SetSelectedGameObject(first.gameObject); }
        }
        foreach (var pilot in route.Pilots)
        {
            if (previousVotes.TryGetValue(pilot.playerId, out int old) && old != pilot.nodeId && pilot.nodeId >= 0)
            { board.Confirm(pilot.nodeId); GameAudio.Play(GameAudioCue.Notification); }
            previousVotes[pilot.playerId] = pilot.nodeId;
        }
        int localId = route.ClientManager.Connection.ClientId;
        if (route.Pilots.Any(x => x.playerId == localId && (pendingNode == -2 ? x.nodeId < 0 : x.nodeId == pendingNode))) pendingNode = -1;
        board.Refresh(focused); board.ShowDetails(focused,pendingNode);
        if (lastPhase != route.Phase)
        {
            displayedSecond = -1;
            if (route.Phase == RouteVotePhase.Locked || route.Phase == RouteVotePhase.TieReveal)
            {
                if (route.Winner >= 0) { Focus(route.Winner); if (CanInteract) EventSystem.current?.SetSelectedGameObject(board.Node(route.Winner).gameObject); }
                if (route.Phase == RouteVotePhase.TieReveal) board.Confirm(route.Winner);
                GameAudio.Play(route.Phase == RouteVotePhase.TieReveal ? GameAudioCue.UiConfirm : GameAudioCue.Notification); Pulse();
            }
            lastPhase = route.Phase;
        }
        board.UpdateClock();
    }
    private void EnsureFocus()
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        if(board.RandomRouteButton != null && board.RandomRouteButton.IsInteractable() && selected == board.RandomRouteButton.gameObject) return;
        if (selected != null && selected.GetComponent<RouteMapNodeView>() != null) return;
        var fallback = board.Node(focused) ?? board.Nodes.FirstOrDefault(x => route.IsAvailable(x.NodeId));
        if (fallback != null) EventSystem.current?.SetSelectedGameObject(fallback.gameObject);
    }
    public void Focus(int node)
    {
        if (board == null) return;
        focused = node; board.FocusDepth(node); board.Refresh(focused); board.ShowDetails(focused,pendingNode);
    }
    public void PointerFocus(int node, Vector2 position, Vector2 delta, bool force = false)
    {
        if (!CanInteract) return;
        // Moving the camera can raycast a different miniature under a stationary cursor.
        // Require real pointer motion so hovering once cannot scroll through the whole map.
        if (!force && (delta.sqrMagnitude < 0.01f || pointerTracked && (position - lastPointerPosition).sqrMagnitude < 1f)) return;
        pointerTracked = true; lastPointerPosition = position;
        var target = board.Node(node);
        if (target != null && EventSystem.current?.currentSelectedGameObject != target.gameObject)
            EventSystem.current?.SetSelectedGameObject(target.gameObject);
    }
    public void Move(int from, MoveDirection direction)
    {
        Vector2 vector = direction == MoveDirection.Up ? Vector2.up : direction == MoveDirection.Down ? Vector2.down : direction == MoveDirection.Left ? Vector2.left : direction == MoveDirection.Right ? Vector2.right : Vector2.zero;
        if (vector == Vector2.zero) return;
        var next = board.Node(board.Neighbour(from, vector));
        if(next != null && next.NodeId == from && direction == MoveDirection.Left && board.RandomRouteButton.IsInteractable()) { board.RandomRouteButton.Select(); return; }
        if (next != null) EventSystem.current?.SetSelectedGameObject(next.gameObject);
    }
    public void Confirm(int node)
    {
        if (!CanInteract || !route.CanVote || !route.IsAvailable(node)) { GameAudio.Play(GameAudioCue.UiError); return; }
        int localId = route.ClientManager.Connection.ClientId;
        if (pendingNode != -1) return;
        bool withdraw = route.Pilots.Any(x => x.playerId == localId && x.nodeId == node);
        pendingNode = withdraw ? -2 : node; pendingUntil = Time.unscaledTime + 1.5f;
        board.ShowDetails(node,pendingNode); GameAudio.Play(GameAudioCue.UiConfirm);
        if (withdraw) route.Withdraw();
        else { route.Select(node); board.Confirm(node); }
        Pulse();
    }
    public void RandomRoute()
    {
        if(!CanInteract || !route.CanVote) return;
        route.RandomRoute(); GameAudio.Play(GameAudioCue.UiConfirm);
    }
    private void Pulse()
    {
        if (route.Settings.feel != null && !route.Settings.feel.motionEnabled || Gamepad.current == null || !Gamepad.current.added) return;
        hapticPad = Gamepad.current; hapticPad.SetMotorSpeeds(route.Settings.hapticStrength,route.Settings.hapticStrength * 0.5f);
        hapticUntil = Time.unscaledTime + route.Settings.hapticSeconds;
    }
    private void StopHaptic() { if (hapticPad != null && hapticPad.added) hapticPad.SetMotorSpeeds(0,0); hapticPad = null; }
    private void OnDisable() => StopHaptic();
    private void OnDestroy()
    {
        if (route != null) route.Changed -= Refresh;
        StopHaptic(); board?.SuspendCamera(); RestoreGameplay(); RestoreCollisions();
        if (Application.isPlaying && !string.IsNullOrEmpty(loadedScene))
        { var scene = SceneManager.GetSceneByPath(loadedScene); if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene); }
    }
}
