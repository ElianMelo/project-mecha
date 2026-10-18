using System.Collections.Generic;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public struct ChatRequest : IBroadcast { public string Text; }
public struct ChatDelivery : IBroadcast { public string Pilot; public string Text; }

// The presenter stays enabled while the native content is hidden outside co-op gameplay.
[DefaultExecutionOrder(-100)]
public sealed class MechaChat : MonoBehaviour
{
    [SerializeField] private ChatSettings settings;
    [SerializeField] private NetworkManager network;
    [SerializeField] private TMP_Text messages, hint;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private GameObject content;
    private readonly ChatHistory history = new ChatHistory();
    private readonly Dictionary<NetworkConnection, float> lastSent = new Dictionary<NetworkConnection, float>();
    private float nextSend;
    private int changedFrame = -1;
    private GameObject previousSelection;
    private bool hiddenByPlayer;
    public static MechaChat Instance { get; private set; }
    public static bool IsTyping => Instance != null && Instance.Editing;
    public bool Editing { get; private set; }
    public string VisibleMessages => messages.text;
    public TMP_InputField InputField => input;
    public bool ContentVisible => content != null && content.activeSelf;
    public bool Available
    {
        get
        {
            if (network == null || !network.ClientManager.Started || GameManager.Instance != null && GameManager.Instance.LobbyOpen ||
                LevelRouteDirector.Instance != null && LevelRouteDirector.Instance.IsRouting) return false;
            int players = 0;
            foreach (var player in PlayerAttack.ActiveActors) if (player.IsSpawned && ++players > 1) return true;
            return false;
        }
    }
    public void ToggleVisibility()
    {
        if (!Available) return;
        if (Editing) Close();
        hiddenByPlayer = !hiddenByPlayer;
        RefreshVisibility();
    }
    private void RefreshVisibility()
    {
        bool visible = Available && !hiddenByPlayer;
        if (!visible && Editing) Close();
        if (content != null && content.activeSelf != visible) content.SetActive(visible);
    }
    private void Awake()
    {
        Instance = this;
        input.characterLimit = settings.characterLimit; input.richText = false; messages.richText = false;
        input.onSubmit.AddListener(_ => Submit());
        network.ClientManager.RegisterBroadcast<ChatDelivery>(Receive);
        network.ServerManager.RegisterBroadcast<ChatRequest>(ReceiveRequest);
        network.ServerManager.OnRemoteConnectionState += ConnectionChanged;
        RefreshVisibility();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (network == null) return;
        network.ClientManager.UnregisterBroadcast<ChatDelivery>(Receive);
        network.ServerManager.UnregisterBroadcast<ChatRequest>(ReceiveRequest);
        network.ServerManager.OnRemoteConnectionState -= ConnectionChanged;
    }
    private void ConnectionChanged(NetworkConnection connection, RemoteConnectionStateArgs args)
    { if (args.ConnectionState == RemoteConnectionState.Stopped) lastSent.Remove(connection); }
    private void Update()
    {
        messages.text = history.Render(Time.unscaledTime);
        hint.text = Editing ? GameText.Get("Chat", "controls.editing") : GameText.Get("Chat", "controls.idle", GameInputPresentation.Hint("Chat"), GameInputPresentation.Hint("ChatVisibility"));
        RefreshVisibility();
        if (!Available || GameControlBindings.Capturing) return;
        if (changedFrame == Time.frameCount) return;
        var keyboard = Keyboard.current;
        if (Editing && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            if (Editing) Submit();
        }
        // Clicking the input is also a supported entry path.
        if (!Editing && input.isFocused) Open();
    }
    public void Open()
    {
        if (Editing || !Available || GameManager.Instance != null && GameManager.Instance.OptionsMenuOpen) return;
        hiddenByPlayer = false; RefreshVisibility();
        previousSelection = EventSystem.current?.currentSelectedGameObject;
        if (previousSelection == input.gameObject) previousSelection = null;
        Editing = true; changedFrame = Time.frameCount;
        EventSystem.current?.SetSelectedGameObject(input.gameObject); input.ActivateInputField();
    }
    public void Submit()
    {
        if (!Editing || changedFrame == Time.frameCount) return;
        string message = ChatHistory.Clean(input.text, settings.characterLimit);
        if (message.Length > 0)
        {
            if (Time.unscaledTime < nextSend) { GameAudio.Play(GameAudioCue.UiError); return; } // Keep the draft for the next attempt.
            nextSend = Time.unscaledTime + settings.sendInterval;
            if (network.ClientManager.Started) network.ClientManager.Broadcast(new ChatRequest { Text = message });
            else Receive(new ChatDelivery { Pilot = PlayerIdentity.LocalName, Text = message }, Channel.Reliable);
        }
        input.text = ""; Close();
    }
    public void Close()
    {
        Editing = false; changedFrame = Time.frameCount; input.DeactivateInputField();
        input.text = "";
        EventSystem.current?.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        GameManager.Instance?.SuppressGameplayInput();
    }
    private void ReceiveRequest(NetworkConnection sender, ChatRequest request, Channel channel)
    {
        if (!sender.IsAuthenticated || lastSent.TryGetValue(sender, out float at) && Time.unscaledTime < at) return;
        string text = ChatHistory.Clean(request.Text, settings.characterLimit);
        if (text.Length == 0) return;
        lastSent[sender] = Time.unscaledTime + settings.sendInterval;
        string name = GameText.Get("Chat", "pilot.prefix") + (sender.ClientId + 1);
        foreach (var owned in sender.Objects)
            if (owned != null && owned.TryGetComponent<PlayerIdentity>(out var identity)) { name = identity.PilotName; break; }
        network.ServerManager.Broadcast(new ChatDelivery { Pilot = name, Text = text });
    }
    private void Receive(ChatDelivery delivery, Channel channel)
    {
        history.Add(delivery.Pilot, ChatHistory.Clean(delivery.Text, settings.characterLimit), Time.unscaledTime, settings.messageLifetime, settings.visibleMessages);
        if (Available) GameAudio.Play(GameAudioCue.Notification);
    }
}
