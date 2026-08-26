using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class PotDrainTests
    {
        [Test]
        public void Tick_DrainsGoldWhenOverlappingIdlePot()
        {
            GameObject traveller = new GameObject("Traveller");
            GameObject pot = new GameObject("Pot");

            try
            {
                CircleCollider2D sensorCollider = traveller.AddComponent<CircleCollider2D>();
                sensorCollider.isTrigger = true;
                sensorCollider.radius = 0.5f;
                PotDrain potDrain = traveller.AddComponent<PotDrain>();
                potDrain.Configure(null, sensorCollider);

                pot.AddComponent<Rigidbody2D>().gravityScale = 0f;
                pot.AddComponent<CircleCollider2D>().radius = 0.4f;
                PotController potController = pot.AddComponent<PotController>();

                System.Reflection.FieldInfo storedGoldField = typeof(PotController).GetField(
                    "_storedGold",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(storedGoldField, Is.Not.Null);
                storedGoldField.SetValue(potController, 2f);

                traveller.transform.position = Vector3.zero;
                pot.transform.position = Vector3.zero;
                Physics2D.SyncTransforms();

                bool isDraining = potDrain.Tick(1f, 2f);

                Assert.That(isDraining, Is.True);
                Assert.That(potController.StoredGold, Is.EqualTo(0f));
                Assert.That(potDrain.StolenGold, Is.EqualTo(2f));
                Assert.That(potDrain.CurrentPot, Is.SameAs(potController));
            }
            finally
            {
                Object.DestroyImmediate(pot);
                Object.DestroyImmediate(traveller);
            }
        }
    }
}