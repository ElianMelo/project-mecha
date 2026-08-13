using FishNet.Object;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private GameObject visuals;

    [Header("Platform")]
    [SerializeField] private float sphereRadius = 0.5f;
    [SerializeField] private float castDistance = 10f;
    [SerializeField] private LayerMask platformLayer;

    private Collider pCollider;
    private PlayerInputs inputs;
    private Rigidbody rb;
    private Animator animator;

    private Vector2 moveInputFiltered;
    private Coroutine restorePlatformBelowRoutine;

    private bool canJump = true;

    private const string RunningAnim = "Running";

    public override void OnStartClient()
    {
        pCollider = transform.GetComponent<Collider>();
        if (!IsOwner) return;
        base.OnStartClient();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        inputs = GetComponent<PlayerInputs>();
        inputs.OnJumpInput += HandleJump;
        inputs.OnDownInput += HandleDown;
    }

    void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Platform") ||
            collision.gameObject.CompareTag("Ground"))
            canJump = true;
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
        transform.rotation = Quaternion.Euler(0f, 90f * direction, 0f);
    }

    public void HandleJump()
    {
        if (!canJump) return;
        canJump = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void HandleDown()
    {
        DisableClosestPlatformBelow(true);
    }

    private void DisableClosestPlatformBelow(bool callRpc = false)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            sphereRadius,
            Vector3.down,
            castDistance,
            platformLayer);

        if (hits.Length == 0)
            return;

        RaycastHit closest = hits
            .OrderBy(h => h.distance)
            .First();

        Physics.IgnoreCollision(pCollider, closest.collider, true);
        if(restorePlatformBelowRoutine != null) StopCoroutine(restorePlatformBelowRoutine);
        restorePlatformBelowRoutine = StartCoroutine(RestorePlatformBelow(closest.collider));
        if(callRpc) RequestDownPlatform();
    }

    private IEnumerator RestorePlatformBelow(Collider otherCollider)
    {
        yield return new WaitForSeconds(1f);
        Physics.IgnoreCollision(pCollider, otherCollider, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDownPlatform()
    {
        ApplyDownPlatform();
    }

    [ObserversRpc]
    private void ApplyDownPlatform()
    {
        DisableClosestPlatformBelow(false);
    }
}
