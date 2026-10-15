using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private ParallaxSettings settings;
    private Camera view;
    private Vector3 origin, cameraOrigin;
    private void Start()
    {
        view = Camera.main; origin = transform.position;
        if (view != null) cameraOrigin = view.transform.position;
    }
    private void LateUpdate()
    {
        if (view == null || settings == null) return;
        Vector3 delta = view.transform.position - cameraOrigin;
        transform.position = origin + new Vector3(delta.x * (1f - settings.movement.x), delta.y * (1f - settings.movement.y), 0f);
    }
}
