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
    public bool HasControl => (!NetworkObject.IsSpawned || IsOwner) && !(GameManager.Instance != null && GameManager.Instance.MenuOpen);
    public event Action OnJumpInput;
    public event Action OnDownInput;
    public event Action OnAttackInput;
    public event Action OnInteractInput;
    public event Action<int> OnDashInput;
    public event Action<int> OnSkillInput;
    public event Action OnTechInput;
    public event Action<string> OnCommand;
    public float LastJumpPressed { get; private set; } = -10f;
    private float lastLeft = -10f, lastRight = -10f;
    private int previousDirection;
    private PlayerInput input;
    private InputActionAsset cachedActions;
    private InputAction jumpAction, downAction, interactAction;
    private bool typing;
    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        interactAction = new InputAction("Portal interaction", InputActionType.Button, settings.interactBinding);
        interactAction.AddBinding(settings.alternateInteractBinding);
        interactAction.performed += HandleInteract;
    }
    private void OnEnable() => interactAction.Enable();
    public void OnTech(InputValue value) { if (HasControl && !typing && value.isPressed) { OnCommand?.Invoke("X / TECH"); OnTechInput?.Invoke(); } }
    private void HandleInteract(InputAction.CallbackContext context)
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (HasControl && (selected == null || selected.GetComponent<TMPro.TMP_InputField>() == null)) OnInteractInput?.Invoke();
    }
    private void OnDestroy() { interactAction.performed -= HandleInteract; interactAction.Dispose(); }
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
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool nowTyping = selected != null && selected.GetComponent<TMPro.TMP_InputField>() != null;
        if (nowTyping && !typing) ResetInput();
        typing = nowTyping;
        if (!HasControl || typing || input == null || !input.isActiveAndEnabled)
        {
            MoveInput = Vector2.zero;
            JumpHeld = DownHeld = false;
            return;
        }
        if (cachedActions != input.actions)
        {
            cachedActions = input.actions;
            jumpAction = cachedActions?.FindAction("Player/Jump", false);
            downAction = cachedActions?.FindAction("Player/Down", false);
        }
        // SendMessages only reports performed events for buttons; poll held state
        // from this player's actions so releases are handled for every device.
        JumpHeld = jumpAction != null && jumpAction.IsPressed();
        DownHeld = downAction != null && downAction.IsPressed();
    }
    private void OnDisable() { interactAction.Disable(); ResetInput(); }
    private void ResetInput() { MoveInput = Vector2.zero; JumpHeld = DownHeld = false; previousDirection = 0; lastLeft = lastRight = LastJumpPressed = -10f; }
    public void OnMove(InputValue value)
    {
        if (!HasControl || typing) return;
        MoveInput = value.Get<Vector2>();
        int direction = Mathf.Abs(MoveInput.x) < settings.movementDeadzone ? 0 : (MoveInput.x > 0f ? 1 : -1);
        if (direction != 0 && direction != previousDirection)
        {
            OnCommand?.Invoke(direction > 0 ? ">" : "<");
            float previous = direction > 0 ? lastRight : lastLeft;
            if (Time.time - previous <= settings.doubleTapWindow)
            {
                OnCommand?.Invoke(direction > 0 ? ">> / DASH" : "<< / DASH"); OnDashInput?.Invoke(direction);
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
        if (JumpHeld) { LastJumpPressed = Time.time; OnCommand?.Invoke("UP"); OnJumpInput?.Invoke(); }
    }
    public void OnDown(InputValue value)
    {
        if (!HasControl || typing) return;
        DownHeld = value.isPressed;
        if (DownHeld) { OnCommand?.Invoke("DOWN"); OnDownInput?.Invoke(); }
    }
    public void OnAttack(InputValue value)
    {
        if (!HasControl || typing || !value.isPressed) return;
        // Read the action state so a simultaneous Down+Z is independent of callback order.
        if (downAction != null) DownHeld = downAction.IsPressed();
        OnCommand?.Invoke(DownHeld ? "DOWN + Z" : "Z"); OnAttackInput?.Invoke();
    }
    public void OnDash(InputValue value) { if (HasControl && !typing && value.isPressed) { OnCommand?.Invoke("DASH"); OnDashInput?.Invoke(MoveInput.x == 0f ? 0 : (MoveInput.x > 0f ? 1 : -1)); } }
    public void OnSkillA(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(0); }
    public void OnSkillS(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(1); }
    public void OnSkillD(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(2); }
}
