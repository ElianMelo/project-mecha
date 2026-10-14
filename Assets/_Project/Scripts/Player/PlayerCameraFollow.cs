using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private float orthographicSize = 3.2f;
    [SerializeField] private float verticalOffset = 1.1f;
    [SerializeField] private float lookAhead = 0.75f;
    [SerializeField] private float smoothTime = 0.16f;
    [SerializeField] private Vector2 horizontalBounds = new Vector2(-13.2f, 13.2f);
    [SerializeField] private Vector2 verticalBounds = new Vector2(-1.7f, 3.5f);
    private Camera view;
    private PlayerMovement player;
    private Vector3 smoothingVelocity;
    private float nextSearch;

    private void Awake() { view = GetComponent<Camera>(); view.orthographic = true; view.orthographicSize = orthographicSize; }
    private void LateUpdate()
    {
        if (player == null)
        {
            if (Time.unscaledTime < nextSearch) return;
            nextSearch = Time.unscaledTime + 0.25f;
            foreach (var candidate in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
                if (candidate.HasControl) { Focus(candidate); break; }
            if (player == null) return;
        }
        view.orthographicSize = orthographicSize;
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref smoothingVelocity, smoothTime, 50f, Time.deltaTime);
    }
    public void Focus(PlayerMovement target)
    {
        player = target; smoothingVelocity = Vector3.zero;
        transform.position = DesiredPosition();
    }
    private Vector3 DesiredPosition()
    {
        float halfWidth = orthographicSize * view.aspect;
        float x = player.transform.position.x + player.Facing * lookAhead;
        x = halfWidth * 2f >= horizontalBounds.y - horizontalBounds.x ? (horizontalBounds.x + horizontalBounds.y) * 0.5f : Mathf.Clamp(x, horizontalBounds.x + halfWidth, horizontalBounds.y - halfWidth);
        float y = Mathf.Clamp(player.transform.position.y + verticalOffset, verticalBounds.x, verticalBounds.y);
        return new Vector3(x, y, -10f);
    }
}
