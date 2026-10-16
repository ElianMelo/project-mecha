using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mecha.Tests
{
    [Category("LocalGameplay")]
    public class OneWayPlatformTests
    {
        private GameObject platformObject, actorObject;
        private Component platform;
        private BoxCollider surface;
        private CapsuleCollider actor;
        private Rigidbody body;
        private ScriptableObject settings;
        private const float Top = 1000f;

        [SetUp]
        public void SetUp()
        {
            settings = GameCode.Asset("OneWayPlatformSettings");
            platformObject = new GameObject("Test one-way platform");
            platformObject.transform.position = new Vector3(0, Top - 0.1f, 0);
            surface = platformObject.AddComponent<BoxCollider>();
            surface.size = new Vector3(6f, 0.2f, 2f);
            platform = platformObject.AddComponent(GameCode.Type("OneWayPlatform"));
            GameCode.Call(platform, "Configure", settings);
            actorObject = new GameObject("Test capsule actor");
            actorObject.transform.position = new Vector3(0, Top + 1.01f, 0);
            actor = actorObject.AddComponent<CapsuleCollider>();
            actor.height = 2f; actor.radius = 0.4f;
            body = actorObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            GameCode.Call(GameCode.Type("OneWayPlatform"), "Register", actor);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (actor != null) GameCode.Call(GameCode.Type("OneWayPlatform"), "Unregister", actor);
            Object.DestroyImmediate(platformObject);
            Object.DestroyImmediate(actorObject);
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator FallingActorLandsAndStaysSupported()
        {
            body.useGravity = true;
            yield return new WaitForSeconds(0.35f);
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.False);
            Assert.That(actor.bounds.min.y, Is.EqualTo(Top).Within(0.04f));
            Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(0.15f));
        }

        [UnityTest]
        public IEnumerator RoundedCapsuleKeepsEstablishedSupportAfterSmallPenetration()
        {
            yield return new WaitForFixedUpdate();
            body.position = new Vector3(0, Top + 0.88f, 0); // Feet 0.12 below the surface.
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.False);
        }

        [UnityTest]
        public IEnumerator JumpPassesThroughThenDescendingActorCanLand()
        {
            body.position = new Vector3(0, Top + 0.6f, 0);
            body.linearVelocity = Vector3.up * 2f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.True);
            body.position = new Vector3(0, Top + 1.05f, 0);
            body.linearVelocity = Vector3.down;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.False);
        }

        [UnityTest]
        public IEnumerator ExplicitDropCannotBeCancelledBySupportConfirmation()
        {
            yield return new WaitForFixedUpdate();
            GameCode.Call(platform, "Drop", actor);
            GameCode.Call(platform, "ConfirmSupport", actor);
            yield return new WaitForFixedUpdate();
            Assert.That(GameCode.Call(platform, "IsDropping", actor), Is.True);
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.True);
        }

        [UnityTest]
        public IEnumerator ExpiredDropRestoresCollisionWhenActorReturnsAbovePlatform()
        {
            GameCode.Set(settings, "dropDuration", 0.06f);
            GameCode.Call(platform, "Drop", actor);
            yield return new WaitForSeconds(0.12f);
            body.position = new Vector3(0, Top + 1.05f, 0);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(GameCode.Call(platform, "IsDropping", actor), Is.False);
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.False);
        }

        [UnityTest]
        public IEnumerator DisablingPlatformRestoresIgnoredCollisionPairs()
        {
            GameCode.Call(platform, "Drop", actor);
            yield return new WaitForFixedUpdate();
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.True);
            ((Behaviour)platform).enabled = false;
            Assert.That(Physics.GetIgnoreCollision(actor, surface), Is.False);
        }
    }
}
