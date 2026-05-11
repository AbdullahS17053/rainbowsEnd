using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Systems.Economy;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class PotDrainTests
    {
        [Test]
        public void Tick_DrainsGoldWhenOverlappingIdlePot()
        {
            GameObject economyHost = new GameObject("EconomyStateHost");
            GameObject traveller = new GameObject("Traveller");
            GameObject pot = new GameObject("Pot");

            EconomyConfig economyConfig = ScriptableObject.CreateInstance<EconomyConfig>();

            try
            {
                EconomyState economyState = economyHost.AddComponent<EconomyState>();
                economyState.Configure(economyConfig);

                CircleCollider2D sensorCollider = traveller.AddComponent<CircleCollider2D>();
                sensorCollider.isTrigger = true;
                sensorCollider.radius = 0.5f;
                PotDrain potDrain = traveller.AddComponent<PotDrain>();
                potDrain.Configure(null, sensorCollider);

                pot.AddComponent<Rigidbody2D>().gravityScale = 0f;
                pot.AddComponent<CircleCollider2D>().radius = 0.4f;
                pot.AddComponent<PotController>();

                traveller.transform.position = Vector3.zero;
                pot.transform.position = Vector3.zero;
                Physics2D.SyncTransforms();

                bool isDraining = potDrain.Tick(1f, 2f);

                Assert.That(isDraining, Is.True);
                Assert.That(economyState.CurrentGold, Is.EqualTo(19));
                Assert.That(potDrain.CurrentPot, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(pot);
                Object.DestroyImmediate(traveller);
                Object.DestroyImmediate(economyConfig);
                Object.DestroyImmediate(economyHost);
            }
        }
    }
}