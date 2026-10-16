using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Mecha.Tests
{
    [Category("LocalGameplay")]
    public class ComboSequenceTests
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private ScriptableObject sequence, first, second;
        private object player;

        [SetUp]
        public void SetUp()
        {
            first = Asset("AttackPiece"); second = Asset("AttackPiece");
            sequence = Asset("ComboDefinition");
            GameCode.Set(sequence, "attacks", GameCode.Array("AttackPiece", first, second));
            GameCode.Set(sequence, "inputWindow", 0.9f);
            player = GameCode.New("ComboSequencePlayer");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test]
        public void InputsConsumeAuthoredOrderAndLoopWithoutChangingAsset()
        {
            string before = JsonUtility.ToJson(sequence);
            Assert.That(Consume(0f), Is.SameAs(first));
            Assert.That(Consume(0.2f), Is.SameAs(second));
            Assert.That(Consume(0.4f), Is.SameAs(first));
            Assert.That(JsonUtility.ToJson(sequence), Is.EqualTo(before));
        }

        [Test]
        public void LateInputRestartsAtFirstPiece()
        {
            Consume(0f);
            Assert.That(Consume(0.91f), Is.SameAs(first));
        }

        [Test]
        public void NonLoopingComboStopsUntilWindowExpires()
        {
            GameCode.Set(sequence, "loop", false);
            Consume(0f); Consume(0.2f);
            Assert.That(Consume(0.4f), Is.Null);
            Assert.That(Consume(1.2f), Is.SameAs(first));
        }

        [Test]
        public void SwitchingGroundAndAirRestartsEachSequence()
        {
            var air = Asset("ComboDefinition");
            GameCode.Set(air, "attacks", GameCode.Array("AttackPiece", second, first));
            Consume(0f);
            Assert.That(GameCode.Call(player, "Consume", air, 0.1f), Is.SameAs(second));
            Assert.That(Consume(0.2f), Is.SameAs(first));
        }

        [Test]
        public void PlayersHaveIndependentComboCursors()
        {
            var other = GameCode.New("ComboSequencePlayer");
            Consume(0f);
            Assert.That(GameCode.Call(other, "Consume", sequence, 0.1f), Is.SameAs(first));
            Assert.That(Consume(0.2f), Is.SameAs(second));
        }

        [Test]
        public void ResetClearsPendingStep()
        {
            Consume(0f);
            GameCode.Call(player, "Reset");
            Assert.That(Consume(0.1f), Is.SameAs(first));
        }

        [Test]
        public void MissingAndEmptyCombosProduceNoAttack()
        {
            Assert.That(GameCode.Call(player, "Consume", null, 0f), Is.Null);
            GameCode.Set(sequence, "attacks", GameCode.Array("AttackPiece"));
            Assert.That(Consume(0f), Is.Null);
        }

        private object Consume(float time) => GameCode.Call(player, "Consume", sequence, time);
        private ScriptableObject Asset(string type)
        {
            var asset = GameCode.Asset(type); assets.Add(asset); return asset;
        }
    }
}
