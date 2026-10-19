using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// Bindings become intentions here; movement and combat remain separate.
public class PlayerInputs : NetworkBehaviour
{
    [SerializeField] private PlayerInputSettings settings;
    public Vector2 MoveInput { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool DownHeld { get; private set; }
    public bool HasControl => (!NetworkObject.IsSpawned || IsOwner) && !PvpMatchDirector.BlocksCombat && !GameInputPresentation.Disconnected && !MechaChat.IsTyping &&
        (GetComponent<CharacterRuntime>() == null || GetComponent<CharacterRuntime>().Ready) &&
        !(GameManager.Instance != null && GameManager.Instance.GameplayInputBlocked);
    public event Action OnJumpInput;
    public event Action OnDownInput;
    public event Action OnAttackInput;
    public event Action OnInteractInput;
    public event Action<int> OnDashInput;
    public event Action<int> OnSkillInput;
    public event Action OnTechInput;
    public event Action OnSecondaryFireInput;
    public event Action<string> OnCommand;
    public bool FireHeld { get; private set; }
    public bool AttackHeld { get; private set; }
    public float LastJumpPressed { get; private set; } = -10f;
    private float lastLeft = -10f, lastRight = -10f;
    private int previousDirection;
    private PlayerInput input;
    private InputActionAsset ownedActions;
    private InputActionAsset cachedActions;
    private InputAction jumpAction, downAction, moveAction, fireAction, attackAction;
    private bool typing;
    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        if (input != null)
        {
            ownedActions = Instantiate(input.actions); input.actions = ownedActions;
            GameControlBindings.Register(ownedActions);
        }
    }
    public void OnTech(InputValue value) { if (HasControl && !typing && value.isPressed) { OnCommand?.Invoke(GameText.Get("Input", "command.heavy", GameInputPresentation.Hint("Tech"))); OnTechInput?.Invoke(); } }
    public void OnInteract(InputValue value)
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (value.isPressed && HasControl && (selected == null || selected.GetComponent<TMPro.TMP_InputField>() == null)) OnInteractInput?.Invoke();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (input != null) input.enabled = IsOwner;
        ResetInput();
    }
    // The prefab keeps PlayerInput disabled until ownership is known, so remote
    // replicas never auto-pair with (and take) the local keyboard/gamepad.
    private void Start()
    {
        if (input != null && !NetworkObject.IsSpawned) input.enabled = true;
    }
    public override void OnOwnershipClient(FishNet.Connection.NetworkConnection previousOwner)
    {
        base.OnOwnershipClient(previousOwner);
        if (input != null) input.enabled = IsOwner;
        ResetInput();
    }
    public override void OnStopClient()
    {
        if (input != null) input.enabled = false;
        ResetInput();
        base.OnStopClient();
    }
    private void Update()
    {
        RefreshDevicePairing();
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool nowTyping = selected != null && selected.GetComponent<TMPro.TMP_InputField>() != null;
        if (nowTyping && !typing) ResetInput();
        typing = nowTyping;
        if (!HasControl || typing || input == null || !input.isActiveAndEnabled)
        {
            ResetInput();
            return;
        }
        if (cachedActions != input.actions)
        {
            cachedActions = input.actions;
            jumpAction = cachedActions?.FindAction("Player/Jump", false);
            downAction = cachedActions?.FindAction("Player/Down", false);
            moveAction = cachedActions?.FindAction("Player/Move", false);
            fireAction = cachedActions?.FindAction("Player/SecondaryFire", false);
            attackAction = cachedActions?.FindAction("Player/Attack", false);
        }
        // SendMessages only reports performed events for buttons; poll held state
        // from this player's actions so releases are handled for every device.
        JumpHeld = jumpAction != null && jumpAction.IsPressed();
        DownHeld = downAction != null && downAction.IsPressed();
        FireHeld = fireAction != null && fireAction.IsPressed();
        AttackHeld = attackAction != null && attackAction.IsPressed();
        // Polling restores a held stick after closing UI and clears it on device loss.
        ApplyMove(moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero);
    }
    private void RefreshDevicePairing()
    {
        if (input == null || !input.isActiveAndEnabled || NetworkObject.IsSpawned && !IsOwner || GameInputPresentation.Revision == 0) return;
        var pad = GameInputPresentation.ActiveGamepad;
        if (pad != null && pad.added)
        {
            bool paired = false;
            foreach (var device in input.devices) if (device == pad) paired = true;
            if (!paired) input.SwitchCurrentControlScheme("Gamepad", pad);
        }
        else if (!GameInputPresentation.UsingGamepad && Keyboard.current != null &&
            (input.currentControlScheme != "Keyboard&Mouse" || input.hasMissingRequiredDevices))
        {
            // A removed controller can leave PlayerInput waiting for its old
            // device. Ask the native API to pair the keyboard on explicit use.
            if (Mouse.current != null) input.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
            else input.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current);
        }
    }
    private void OnDisable() => ResetInput();
    public void OnDeviceLost(PlayerInput playerInput) => ResetInput();
    public void OnControlsChanged(PlayerInput playerInput) { GameControlBindings.Register(playerInput.actions); ResetInput(); }
    private void OnDestroy() { GameControlBindings.Unregister(ownedActions); if (ownedActions != null) Destroy(ownedActions); }
    private void ResetInput() { MoveInput = Vector2.zero; AttackHeld = FireHeld = JumpHeld = DownHeld = false; previousDirection = 0; lastLeft = lastRight = LastJumpPressed = -10f; }
    public void OnSecondaryFire(InputValue value)
    {
        if (!HasControl || typing) return;
        FireHeld = value.isPressed;
        if (value.isPressed) OnSecondaryFireInput?.Invoke();
    }
    public void OnMove(InputValue value)
    {
        if (!HasControl || typing) return;
        ApplyMove(value.Get<Vector2>());
    }
    private void ApplyMove(Vector2 value)
    {
        MoveInput = value;
        int direction = Mathf.Abs(MoveInput.x) < settings.movementDeadzone ? 0 : (MoveInput.x > 0f ? 1 : -1);
        if (direction != 0 && direction != previousDirection)
        {
            OnCommand?.Invoke(direction > 0 ? ">" : "<");
            float previous = direction > 0 ? lastRight : lastLeft;
            if (Time.time - previous <= settings.doubleTapWindow && !GetComponent<PlayerHealth>().IsStunned && GetComponent<PlayerMovement>().Settings.style == PlayerMovementSettings.MovementStyle.Dasher)
            {
                OnCommand?.Invoke(GameText.Get("Input", "command.direction.dash", direction > 0 ? ">>" : "<<")); OnDashInput?.Invoke(direction);
                if (direction > 0) lastRight = -10f; else lastLeft = -10f;
            }
            else if (direction > 0) lastRight = Time.time; else lastLeft = Time.time;
        }
        previousDirection = direction;
    }
    public void OnJump(InputValue value)
    {
        if (!HasControl || typing) return;
        JumpHeld = value.isPressed;
        if (JumpHeld) { LastJumpPressed = Time.time; OnCommand?.Invoke(GameText.Get("Input", "command.up")); OnJumpInput?.Invoke(); }
    }
    public void OnDown(InputValue value)
    {
        if (!HasControl || typing) return;
        DownHeld = value.isPressed;
        if (DownHeld) { OnCommand?.Invoke(GameText.Get("Input", "command.down")); OnDownInput?.Invoke(); }
    }
    public void OnAttack(InputValue value)
    {
        if (!HasControl || typing || !value.isPressed) return;
        if (GetComponent<PlayerLevelTravel>()?.CanUsePortal == true) return;
        // Read the action state so a simultaneous Down+Z is independent of callback order.
        if (downAction != null) DownHeld = downAction.IsPressed();
        OnCommand?.Invoke((DownHeld ? GameText.Get("Input", "command.down.prefix") : "") + GameInputPresentation.Hint("Attack")); OnAttackInput?.Invoke();
    }
    public void OnDash(InputValue value)
    {
        if (!HasControl || typing || !value.isPressed) return;
        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : MoveInput;
        MoveInput = move;
        OnCommand?.Invoke(GameText.Get("Input", "command.dash"));
        OnDashInput?.Invoke(Mathf.Abs(move.x) < settings.movementDeadzone ? 0 : (move.x > 0f ? 1 : -1));
    }
    public void OnSkillA(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(0); }
    public void OnSkillS(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(1); }
    public void OnSkillD(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(2); }
}
