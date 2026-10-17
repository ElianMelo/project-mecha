using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Hovl
{
    public class HS_DemoShooting : MonoBehaviour
    {
        [Header("Fire rate")]
        [Range(0.01f, 1f)]
        public float fireRate = 0.1f;

        private float fireCountdown;

        [Header("Rotation")]
        [SerializeField] private bool rotate = true;

        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera cam;
        [SerializeField] private Animation camAnim;

        [Header("Projectile settings")]
        [SerializeField] private float maxLength = 100f;
        [SerializeField] private GameObject[] prefabs;

        [Header("Pooling")]
        [SerializeField] private int maxPoolSizePerPrefab = 40;

        [Header("Projectile switching")]
        [SerializeField] private float switchDelay = 0.4f;

        private int currentPrefabIndex;
        private float buttonSaver;

        private static Transform globalPoolRoot;

        // Prefab is used directly as the dictionary key.
        private static readonly Dictionary<GameObject, List<GameObject>> pools =
            new Dictionary<GameObject, List<GameObject>>();

        private static readonly Dictionary<GameObject, Transform> poolParents =
            new Dictionary<GameObject, Transform>();

        private void Awake()
        {
            EnsureGlobalPool();

            if (cam == null)
                cam = Camera.main;
        }

        private void Start()
        {
            Counter(0);
        }

        private void Update()
        {
            HandleShooting();
            HandleProjectileSwitch();

            if (rotate)
                HandleRotation();

            if (fireCountdown > 0f)
                fireCountdown -= Time.deltaTime;

            buttonSaver += Time.deltaTime;
        }

        private void HandleShooting()
        {
            if (IsFirePressedThisFrame())
            {
                Shoot();
            }

            if (IsFastFireHeld() && fireCountdown <= 0f)
            {
                Shoot();
                fireCountdown = fireRate;
            }
        }

        private void HandleProjectileSwitch()
        {
            float horizontal = GetHorizontalInput();

            if (horizontal < 0f && buttonSaver >= switchDelay)
            {
                buttonSaver = 0f;
                Counter(-1);
            }
            else if (horizontal > 0f && buttonSaver >= switchDelay)
            {
                buttonSaver = 0f;
                Counter(1);
            }
        }

        private void HandleRotation()
        {
            if (cam == null)
                return;

            Vector2 pointerPosition = GetPointerScreenPosition();
            Ray ray = cam.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxLength))
            {
                RotateToMouseDirection(hit.point);
            }
        }

        private void Shoot()
        {
            if (prefabs == null || prefabs.Length == 0)
                return;

            if (firePoint == null)
            {
                Debug.LogWarning("HS_DemoShooting: FirePoint is not assigned.");
                return;
            }

            GameObject prefab = prefabs[currentPrefabIndex];

            if (prefab == null)
                return;

            GameObject projectile = GetProjectile(prefab);

            if (projectile == null)
                return;

            if (camAnim != null && camAnim.clip != null)
            {
                camAnim.Play(camAnim.clip.name);
            }

            Transform projectileTransform = projectile.transform;

            projectileTransform.SetParent(null, false);
            projectileTransform.SetPositionAndRotation(
                firePoint.position,
                firePoint.rotation
            );

            Rigidbody rb = projectile.GetComponent<Rigidbody>();

            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif

                rb.angularVelocity = Vector3.zero;
            }

            projectile.SetActive(true);

            IPooledProjectile pooledProjectile =
                projectile.GetComponent<IPooledProjectile>();

            if (pooledProjectile != null)
            {
                pooledProjectile.OnSpawnedFromPool();
            }
        }

        private GameObject GetProjectile(GameObject prefab)
        {
            if (prefab == null)
                return null;

            if (!pools.TryGetValue(prefab, out List<GameObject> pool))
            {
                pool = new List<GameObject>();
                pools.Add(prefab, pool);
            }

            CleanupDestroyedObjects(pool);

            for (int i = 0; i < pool.Count; i++)
            {
                GameObject pooledObject = pool[i];

                if (pooledObject == null)
                    continue;

                if (!pooledObject.activeInHierarchy)
                    return pooledObject;
            }

            if (pool.Count >= maxPoolSizePerPrefab)
                return null;

            GameObject newProjectile = CreateProjectile(prefab);

            if (newProjectile != null)
            {
                pool.Add(newProjectile);
            }

            return newProjectile;
        }

        private void CleanupDestroyedObjects(List<GameObject> pool)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (pool[i] == null)
                {
                    pool.RemoveAt(i);
                }
            }
        }

        private GameObject CreateProjectile(GameObject prefab)
        {
            Transform parent = GetOrCreatePoolParent(prefab);

            GameObject newProjectile = Instantiate(prefab, parent);
            newProjectile.SetActive(false);

            return newProjectile;
        }

        private Transform GetOrCreatePoolParent(GameObject prefab)
        {
            if (poolParents.TryGetValue(prefab, out Transform existingParent) &&
                existingParent != null)
            {
                return existingParent;
            }

            GameObject parentObject = new GameObject(prefab.name + "_Pool");
            parentObject.transform.SetParent(globalPoolRoot);

            poolParents[prefab] = parentObject.transform;

            return parentObject.transform;
        }

        private static void EnsureGlobalPool()
        {
            if (globalPoolRoot != null)
                return;

            GameObject existing = GameObject.Find(
                "Hovl_GlobalProjectilePool"
            );

            if (existing != null)
            {
                globalPoolRoot = existing.transform;
                DontDestroyOnLoad(existing);
                return;
            }

            GameObject poolObject = new GameObject(
                "Hovl_GlobalProjectilePool"
            );

            DontDestroyOnLoad(poolObject);
            globalPoolRoot = poolObject.transform;
        }

        private void Counter(int count)
        {
            if (prefabs == null || prefabs.Length == 0)
                return;

            currentPrefabIndex += count;

            if (currentPrefabIndex >= prefabs.Length)
            {
                currentPrefabIndex = 0;
            }
            else if (currentPrefabIndex < 0)
            {
                currentPrefabIndex = prefabs.Length - 1;
            }
        }

        private void RotateToMouseDirection(Vector3 destination)
        {
            Vector3 direction = destination - transform.position;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        private bool IsFirePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetButtonDown("Fire1"))
            {
                return true;
            }
#endif

            return false;
        }

        private bool IsFastFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null &&
                Mouse.current.rightButton.isPressed)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(1))
            {
                return true;
            }
#endif

            return false;
        }

        private float GetHorizontalInput()
        {
            float horizontal = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed)
                    horizontal -= 1f;

                if (Keyboard.current.dKey.isPressed)
                    horizontal += 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Approximately(horizontal, 0f))
            {
                if (Input.GetKey(KeyCode.A))
                    horizontal -= 1f;

                if (Input.GetKey(KeyCode.D))
                    horizontal += 1f;

                if (Mathf.Approximately(horizontal, 0f))
                {
                    horizontal = Input.GetAxisRaw("Horizontal");
                }
            }
#endif

            return Mathf.Clamp(horizontal, -1f, 1f);
        }

        private Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }

    public interface IPooledProjectile
    {
        void OnSpawnedFromPool();
    }
}