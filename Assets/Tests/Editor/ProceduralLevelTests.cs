using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mecha.Tests
{
    [Category("LocalGameplay")]
    public class ProceduralLevelTests
    {
        private ScriptableObject settings;

        [SetUp]
        public void SetUp()
        {
            settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Settings/ProceduralLevels.asset");
            Assert.That(settings, Is.Not.Null, "The actual authored room profile must exist.");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void SameSeedProducesSameRoomWithoutMutatingSharedProfile(int roomNumber)
        {
            string profile = EditorJsonUtility.ToJson(settings);
            string first = JsonUtility.ToJson(Generate(104729, roomNumber));
            Assert.That(JsonUtility.ToJson(Generate(104729, roomNumber)), Is.EqualTo(first));
            Assert.That(EditorJsonUtility.ToJson(settings), Is.EqualTo(profile));
        }

        [TestCase(1, "Big")] [TestCase(2, "Rising")] [TestCase(3, "Long")] [TestCase(4, "Big")]
        public void RoomFlowSelectsExpectedShape(int roomNumber, string shape)
        {
            Assert.That(GameCode.Get<object>(Generate(73, roomNumber), "Shape").ToString(), Is.EqualTo(shape));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void GeneratedRoomsKeepSpawnsSeparatedAndOnSupports(int roomNumber)
        {
            foreach (int seed in new[] { 7, 73, 104729, 8675309 })
            {
                object room = Generate(seed, roomNumber);
                var bounds = GameCode.Get<Rect>(room, "Bounds");
                var platforms = GameCode.Get<Array>(room, "Platforms");
                var start = GameCode.Get<Vector3>(room, "PlayerStart");
                var portal = GameCode.Get<Vector3>(room, "Portal");
                var enemies = GameCode.Get<Vector3[]>(room, "Enemies");
                Assert.That(enemies.Length, Is.GreaterThan(0), $"Room {roomNumber}, seed {seed}");
                Assert.That(enemies.Length, Is.LessThanOrEqualTo(GameCode.Get<Vector2Int>(settings, "enemyCount").y));
                AssertSupported(start, platforms, bounds); AssertSupported(portal, platforms, bounds);
                Assert.That(Vector3.Distance(start, portal), Is.GreaterThan(3f));
                foreach (var deck in platforms)
                {
                    float width = GameCode.Get<float>(deck, "width");
                    var position = GameCode.Get<Vector2>(deck, "position");
                    Assert.That(width, Is.GreaterThan(0f));
                    Assert.That(position.x - width * 0.5f, Is.GreaterThanOrEqualTo(bounds.xMin - 0.001f));
                    Assert.That(position.x + width * 0.5f, Is.LessThanOrEqualTo(bounds.xMax + 0.001f));
                }
                for (int i = 0; i < enemies.Length; i++)
                {
                    AssertSupported(enemies[i], platforms, bounds);
                    Assert.That(Vector3.Distance(enemies[i], start),
                        Is.GreaterThanOrEqualTo(GameCode.Get<float>(settings, "minimumSpawnDistance") - 0.001f));
                    for (int j = 0; j < i; j++)
                        Assert.That(Vector3.Distance(enemies[i], enemies[j]),
                            Is.GreaterThanOrEqualTo(GameCode.Get<float>(settings, "minimumEnemySpacing") - 0.001f));
                }
            }
        }

        [Test]
        public void RisingRoomsFitCameraWidthAndVaryPlatformWidths()
        {
            var camera = GameCode.Get<ScriptableObject>(settings, "cameraSettings");
            float expected = 2f * GameCode.Get<float>(camera, "orthographicSize") * 16f / 9f + 0.1f;
            var room = Generate(104729, 2);
            Assert.That(GameCode.Get<Rect>(room, "Bounds").width, Is.EqualTo(expected).Within(0.001f));
            var widths = new System.Collections.Generic.HashSet<int>();
            foreach (var deck in GameCode.Get<Array>(room, "Platforms"))
                widths.Add(Mathf.RoundToInt(GameCode.Get<float>(deck, "width") * 100));
            Assert.That(widths.Count, Is.GreaterThan(3), "Rising rooms must avoid three identical small decks.");
            Assert.That(JsonUtility.ToJson(Generate(73, 2)), Is.Not.EqualTo(JsonUtility.ToJson(room)));
        }

        private object Generate(int seed, int number) => GameCode.Call(GameCode.Type("ProceduralLevelLayout"), "Generate", settings, seed, number, 16f / 9f);
        private void AssertSupported(Vector3 point, Array platforms, Rect bounds)
        {
            Assert.That(point.x, Is.InRange(bounds.xMin, bounds.xMax));
            Assert.That(point.y, Is.InRange(bounds.yMin, bounds.yMax));
            Assert.That(point.z, Is.EqualTo(0f));
            if (Mathf.Abs(point.y - GameCode.Get<float>(settings, "floorHeight") - 0.08f) < 0.001f) return;
            foreach (var deck in platforms)
            {
                var position = GameCode.Get<Vector2>(deck, "position");
                float width = GameCode.Get<float>(deck, "width");
                float top = position.y + GameCode.Get<float>(settings, "platformThickness") * 0.5f;
                if (Mathf.Abs(point.y - top - 0.08f) < 0.001f && Mathf.Abs(point.x - position.x) <= width * 0.5f) return;
            }
            Assert.Fail("Spawn/portal has no supporting surface: " + point);
        }
    }
}
