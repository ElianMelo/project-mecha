#if UNITY_EDITOR
using System.Collections;
using FishNet.Managing;
using FishNet.Object;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mecha.Tests
{
    // A single local server, no clients or Relay. Exercises the real projectile's
    // authority gate and Rigidbody simulation together; a math-only test missed the freeze.
    [Category("TargetedNetwork")]
    public class LanceProjectileTests
    {
        private NetworkManager network;
        private GameObject networkObject;
        private NetworkObject lance;
        private ScriptableObject profile;
        private bool background;
        private int frameRate;

        [UnityTest]
        public IEnumerator SpawnedLanceTravelsThroughApexAndDespawnsAfterLifetime()
        {
            background = Application.runInBackground;
            frameRate = Application.targetFrameRate;
            networkObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tests/Fixtures/LanceTestServer.prefab"));
            network = networkObject.GetComponent<NetworkManager>();
            networkObject.SetActive(true);
            network.TransportManager.Transport.SetPort(0); // OS assigns a free port.
            network.TransportManager.Transport.SetServerBindAddress("127.0.0.1", FishNet.Transporting.IPAddressType.IPv4);
            Assert.That(network.ServerManager.StartConnection(), Is.True);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!network.ServerManager.Started && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(network.ServerManager.Started, Is.True, "Local regression server did not start.");

            profile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Settings/EnemyLance.asset"));
            GameCode.Set(profile, "collisionLayers", (LayerMask)0);
            GameCode.Set(profile, "lifetime", 0.8f);
            GameCode.Set(profile, "gravity", 20f);
            var prefab = GameCode.Get<NetworkObject>(profile, "projectile");
            var origin = new Vector3(0f, 30f, 0f);
            lance = Object.Instantiate(prefab, origin, Quaternion.identity);
            var projectile = lance.GetComponent(GameCode.Type("EnemyLanceProjectile"));
            GameCode.Call(projectile, "Launch", profile, new Vector3(4f, 4f, 0f));
            network.ServerManager.Spawn(lance);
            var body = lance.GetComponent<Rigidbody>();
            Assert.That(((NetworkBehaviour)projectile).IsServerInitialized, Is.True);

            yield return new WaitForSeconds(0.12f);
            Assert.That(body.position.x, Is.GreaterThan(origin.x + 0.2f), "Lance froze despite its changing velocity.");
            Assert.That(body.position.y, Is.GreaterThan(origin.y + 0.1f));
            Assert.That(lance.transform.right.y, Is.GreaterThan(0f), "Lance must point along the rising trajectory.");
            float risingX = body.position.x;
            yield return new WaitForSeconds(0.38f);
            Assert.That(body.position.x, Is.GreaterThan(risingX + 0.5f));
            Assert.That(body.position.y, Is.LessThan(origin.y));
            Assert.That(lance.transform.right.y, Is.LessThan(0f), "Lance must turn along the falling trajectory.");
            yield return new WaitForSeconds(0.4f);
            Assert.That(lance == null || !lance.IsSpawned, Is.True, "Expired lance remained in the level.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (lance != null && lance.IsSpawned) network.ServerManager.Despawn(lance);
            if (network != null) network.ServerManager.StopConnection(true);
            yield return null;
            if (lance != null) Object.Destroy(lance.gameObject);
            if (networkObject != null) Object.Destroy(networkObject);
            if (profile != null) Object.Destroy(profile);
            yield return null;
            Application.runInBackground = background;
            Application.targetFrameRate = frameRate;
        }
    }
}
#endif
