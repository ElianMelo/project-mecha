using FishNet.Object;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private GameObject visuals;

    private PlayerInputs inputs;
    private Rigidbody rb;
    private Animator animator;

    private Vector2 moveInputFiltered;

    private const string RunningAnim = "Running";

    public override void OnStartClient()
    {
        if (!IsOwner) return;
        base.OnStartClient();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        inputs = GetComponent<PlayerInputs>();
        inputs.OnJumpInput += HandleJump;
    }

    void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    public void HandleMovement()
    {
        moveInputFiltered = inputs.MoveInput.normalized;
        animator.SetBool(RunningAnim, false);
        if (moveInputFiltered == Vector2.zero) return;
        // if (moveInputFiltered.x == 0) return;
        HandleDirection();
        animator.SetBool(RunningAnim, true);
        moveInputFiltered = moveInputFiltered * movementSpeed;
        moveInputFiltered.y = rb.linearVelocity.y;
        rb.linearVelocity = moveInputFiltered;
    }

    public void HandleDirection()
    {
        var direction = moveInputFiltered.x > 0 ? 1f : -1f;
        visuals.transform.rotation = Quaternion.Euler(0f, 90f * direction, 0f);
    }

    public void HandleJump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}
