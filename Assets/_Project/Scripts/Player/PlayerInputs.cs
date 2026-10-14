using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// Bindings become intentions here; movement and combat remain separate.
public class PlayerInputs : NetworkBehaviour
{
    [SerializeField] private float doubleTapWindow = 0.23f;
    public Vector2 MoveInput { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool DownHeld { get; private set; }
    public bool HasControl => !NetworkObject.IsSpawned || IsOwner;
    public event Action OnJumpInput;
    public event Action OnDownInput;
    public event Action OnAttackInput;
    public event Action<int> OnDashInput;
    public event Action<int> OnSkillInput;
    private float lastLeft = -10f, lastRight = -10f;
    private int previousDirection;
    private PlayerInput input;
    private InputActionAsset cachedActions;
    private InputAction jumpAction, downAction;
    private bool typing;
    private void Awake() => input = GetComponent<PlayerInput>();
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (input != null) input.enabled = IsOwner;
        ResetInput();
    }
    private void Update()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool nowTyping = selected != null && selected.GetComponent<TMPro.TMP_InputField>() != null;
        if (nowTyping && !typing) ResetInput();
        typing = nowTyping;
        if (!HasControl || typing || input == null || !input.isActiveAndEnabled)
        {
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
    private void OnDisable() => ResetInput();
    private void ResetInput() { MoveInput = Vector2.zero; JumpHeld = DownHeld = false; previousDirection = 0; lastLeft = lastRight = -10f; }
    public void OnMove(InputValue value)
    {
        if (!HasControl || typing) return;
        MoveInput = value.Get<Vector2>();
        int direction = Mathf.Abs(MoveInput.x) < 0.3f ? 0 : (MoveInput.x > 0f ? 1 : -1);
        if (direction != 0 && direction != previousDirection)
        {
            float previous = direction > 0 ? lastRight : lastLeft;
            if (Time.time - previous <= doubleTapWindow)
            {
                OnDashInput?.Invoke(direction);
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
        if (JumpHeld) OnJumpInput?.Invoke();
    }
    public void OnDown(InputValue value)
    {
        if (!HasControl || typing) return;
        DownHeld = value.isPressed;
        if (DownHeld) OnDownInput?.Invoke();
    }
    public void OnAttack(InputValue value) { if (HasControl && !typing && value.isPressed) OnAttackInput?.Invoke(); }
    public void OnDash(InputValue value) { if (HasControl && !typing && value.isPressed) OnDashInput?.Invoke(MoveInput.x == 0f ? 0 : (MoveInput.x > 0f ? 1 : -1)); }
    public void OnSkillA(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(0); }
    public void OnSkillS(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(1); }
    public void OnSkillD(InputValue value) { if (HasControl && !typing && value.isPressed) OnSkillInput?.Invoke(2); }
}
