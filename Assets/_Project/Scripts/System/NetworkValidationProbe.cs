#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;

// Opt-in, bounded local network-validation gesture and telemetry. No control endpoint.
// Launch a development client with --mecha-network-probe; add --mecha-network-probe-attack
// to request ordinary attacks through the same owner combat entry point as input.
public sealed class NetworkValidationProbe : MonoBehaviour
{
    private float started = -1f, nextSample, nextAttack;
    private int samples, stillFrames;
    private float maximumFrameMs, maximumStep, maximumColliderOffset;
    private Vector3 previousPosition;
    private PlayerMovement owner;
    private bool attacking;
    private bool hosting, staged, configured;
    private int previousTargetId = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!Environment.GetCommandLineArgs().Contains("--mecha-network-probe")) return;
        DontDestroyOnLoad(new GameObject("Network Validation Probe", typeof(NetworkValidationProbe)));
    }

    private void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        attacking = args.Contains("--mecha-network-probe-attack");
        hosting = args.Contains("--mecha-network-probe-host");
    }
    private void LateUpdate()
    {
        if (!configured && GameManager.Instance != null)
        {
            var args = Environment.GetCommandLineArgs();
            var simulator = GameManager.Instance.Network.TransportManager.LatencySimulator;
            int latency = Array.IndexOf(args, "--mecha-network-probe-latency");
            if (latency >= 0 && latency + 1 < args.Length && long.TryParse(args[latency + 1], out long delay))
            {
                simulator.SetLatency(Math.Max(0, delay)); simulator.SetPacketLoss(0.02); simulator.SetOutOfOrder(0.02); simulator.SetEnabled(true);
            }
            if (hosting) GameManager.Instance.StartHost();
            configured = true;
        }
        if (owner == null)
        {
            owner = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && p.IsOwner);
            if (owner == null) return;
            started = Time.unscaledTime; nextAttack = started + 10f;
        }
        if (Time.unscaledTime - started > 35f) { enabled = false; return; }
        if (hosting && !staged && Time.unscaledTime - started >= 8f)
        {
            var remote = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && !p.IsOwner);
            if (remote != null)
            {
                foreach (var actor in EnemyController.ActiveEnemies) { actor.PracticePassive = true; actor.CancelAttack(); }
                var enemy = EnemyController.ActiveEnemies.First(e => e.IsSpawned && !e.IsDead && e.Role != EnemyRole.Lancer);
                remote.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-8f, -3.4f, 0f));
                var body = enemy.GetComponent<Rigidbody>(); body.position = new Vector3(-6.8f, -3.4f, 0f);
                enemy.transform.position = body.position; enemy.GetComponent<EnemyMotor>().SetImpact(Vector3.zero);
                enemy.GetComponent<FishNet.Component.Transforming.NetworkTransform>().Teleport(); Physics.SyncTransforms();
                Debug.Log($"MECHA_PROBE_STAGE target={enemy.ObjectId} health={enemy.Health}"); staged = true;
            }
        }
        maximumFrameMs = Mathf.Max(maximumFrameMs, Time.unscaledDeltaTime * 1000f);
        var enemies = EnemyController.ActiveEnemies.Where(e => e.IsSpawned && !e.IsDead).OrderBy(e => e.ObjectId).ToArray();
        var target = enemies.FirstOrDefault();
        if (target != null)
        {
            float step = Vector3.Distance(previousPosition, target.transform.position);
            if (samples > 0 && previousTargetId == target.ObjectId) { maximumStep = Mathf.Max(maximumStep, step); if (step < 0.00001f) stillFrames++; }
            previousTargetId = target.ObjectId;
            previousPosition = target.transform.position;
            var capsule = target.GetComponent<CapsuleCollider>();
            maximumColliderOffset = Mathf.Max(maximumColliderOffset, Vector3.Distance(capsule.bounds.center, target.transform.TransformPoint(capsule.center)));
        }
        samples++;
        if (attacking && Time.unscaledTime >= nextAttack)
        {
            owner.Face(1); owner.GetComponent<PlayerAttack>().HandleAttack(); nextAttack = Time.unscaledTime + 0.6f;
        }
        if (Time.unscaledTime < nextSample) return;
        nextSample = Time.unscaledTime + 0.2f;
        var network = owner.NetworkManager;
        Debug.Log("MECHA_PROBE " + JsonUtility.ToJson(new Snapshot {
            elapsed = Time.unscaledTime - started, frame = Time.frameCount, tickRate = network.TimeManager.TickRate,
            rtt = network.TimeManager.RoundTripTime, background = Application.runInBackground,
            maxFrameMs = maximumFrameMs, maxStep = maximumStep, maxColliderOffset = maximumColliderOffset, stillFrames = stillFrames,
            ownerPosition = owner.transform.position, damage = owner.GetComponent<PlayerAttack>().TotalDamage,
            action = owner.GetComponent<PlayerAttack>().CurrentAction,
            actors = enemies.Select(e => $"id={e.ObjectId} hp={e.Health:F1} pos={e.transform.position:F3} kine={e.GetComponent<Rigidbody>().isKinematic} interp={e.GetComponent<Rigidbody>().interpolation} anim={e.GetComponent<CharacterAnimator>().Animator.GetCurrentAnimatorStateInfo(0).shortNameHash}").ToArray()
        }));
    }
    [Serializable] private sealed class Snapshot
    {
        public float elapsed, maxFrameMs, maxStep, maxColliderOffset, damage;
        public int frame, tickRate, stillFrames;
        public long rtt;
        public bool background;
        public Vector3 ownerPosition;
        public string action;
        public string[] actors;
    }
}
#endif
