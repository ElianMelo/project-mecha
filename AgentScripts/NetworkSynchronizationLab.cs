using System;
using System.Linq;
using System.Threading.Tasks;
using FishNet.Managing;
using UnityEditor;
using UnityEngine;

// Operator-invoked local session inspection. Uses the official Pipeline run_script.
public static class NetworkSynchronizationLab
{
    public static object Snapshot()
    {
        var network = GameManager.Instance.Network;
        return new { frame = Time.frameCount, time = Time.time, background = Application.runInBackground,
            tickRate = network.TimeManager.TickRate, rtt = network.TimeManager.RoundTripTime,
            clients = network.ServerManager.Clients.Count,
            players = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Select(p => new {
                p.ObjectId, p.OwnerId, p.IsOwner, p.IsGrounded, pos = p.transform.position.ToString("F3"),
                kinematic = p.GetComponent<Rigidbody>().isKinematic, interpolation = p.GetComponent<Rigidbody>().interpolation.ToString(),
                p.GetComponent<PlayerAttack>().TotalDamage, animation = p.GetComponent<CharacterAnimator>().Animator.GetCurrentAnimatorStateInfo(0).shortNameHash
            }).ToArray(),
            enemies = EnemyController.ActiveEnemies.Where(e => e.IsSpawned).Select(e => new {
                e.ObjectId, e.Health, e.CurrentState, pos = e.transform.position.ToString("F3"),
                kinematic = e.GetComponent<Rigidbody>().isKinematic, interpolation = e.GetComponent<Rigidbody>().interpolation.ToString(),
                animation = e.GetComponent<CharacterAnimator>().Animator.GetCurrentAnimatorStateInfo(0).shortNameHash
            }).ToArray() };
    }
    public static object StageRemoteCombat()
    {
        var remote = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).First(p => p.IsSpawned && !p.IsOwner);
        foreach (var e in EnemyController.ActiveEnemies) { e.PracticePassive = true; e.CancelAttack(); }
        var enemy = EnemyController.ActiveEnemies.First(e => !e.IsDead && e.Role != EnemyRole.Lancer);
        remote.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-8f, -3.4f, 0f));
        var body = enemy.GetComponent<Rigidbody>();
        body.position = new Vector3(-6.8f, -3.4f, 0f); enemy.transform.position = body.position;
        enemy.GetComponent<EnemyMotor>().SetImpact(Vector3.zero);
        enemy.GetComponent<FishNet.Component.Transforming.NetworkTransform>().Teleport();
        Physics.SyncTransforms();
        return new { target = enemy.ObjectId, snapshot = Snapshot() };
    }
    public static object Impair(long latency, double loss = 0, double reorder = 0)
    {
        var simulator = GameManager.Instance.Network.TransportManager.LatencySimulator;
        simulator.SetLatency(latency); simulator.SetPacketLoss(loss); simulator.SetOutOfOrder(reorder);
        simulator.SetEnabled(latency > 0 || loss > 0 || reorder > 0);
        return new { latency, loss, reorder };
    }
    public static async Task<object> Observe(int milliseconds = 2000)
    {
        var before = Snapshot(); await Task.Delay(milliseconds);
        return new { before, after = Snapshot() };
    }
}
