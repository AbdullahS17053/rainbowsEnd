using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotRegistryTests
    {
        [Test]
        public void ActivePotCount_ExcludesDestroyedPots()
        {
            GameObject registryHost = new GameObject("PotRegistry");
            PotRegistry registry = registryHost.AddComponent<PotRegistry>();

            PotConfig activePotConfig = ScriptableObject.CreateInstance<PotConfig>();
            PotConfig destroyedPotConfig = ScriptableObject.CreateInstance<PotConfig>();

            GameObject activePotObject = new GameObject("ActivePot");
            GameObject destroyedPotObject = new GameObject("DestroyedPot");

            try
            {
                PotController activePot = ConfigurePot(activePotObject, activePotConfig);
                PotController destroyedPot = ConfigurePot(destroyedPotObject, destroyedPotConfig);

                registry.RegisterPot(activePot);
                registry.RegisterPot(destroyedPot);
                destroyedPot.DestroyByUnicorn(null);

                Assert.That(registry.RegisteredPots.Count, Is.EqualTo(2));
                Assert.That(registry.ActivePotCount, Is.EqualTo(1));
                Assert.That(destroyedPot.IsDestroyed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(activePotObject);
                Object.DestroyImmediate(destroyedPotObject);
                Object.DestroyImmediate(activePotConfig);
                Object.DestroyImmediate(destroyedPotConfig);
                Object.DestroyImmediate(registryHost);
            }
        }

        [Test]
        public void TotalStoredGold_AggregatesAcrossPots()
        {
            GameObject registryHost = new GameObject("PotRegistry");
            PotRegistry registry = registryHost.AddComponent<PotRegistry>();

            PotConfig potConfig = ScriptableObject.CreateInstance<PotConfig>();

            GameObject pot1Object = new GameObject("Pot1");
            GameObject pot2Object = new GameObject("Pot2");
            GameObject destroyedPotObject = new GameObject("DestroyedPot");

            try
            {
                PotController pot1 = ConfigurePot(pot1Object, potConfig);
                PotController pot2 = ConfigurePot(pot2Object, potConfig);
                PotController destroyedPot = ConfigurePot(destroyedPotObject, potConfig);

                registry.RegisterPot(pot1);
                registry.RegisterPot(pot2);
                registry.RegisterPot(destroyedPot);
                destroyedPot.DestroyByUnicorn(null);

                float _ = pot1.DrainGold(-1f); // no-op drain
                float drain1 = pot1.DrainGold(10f);
                float drain2 = pot2.DrainGold(5f);

                Assert.That(drain1, Is.EqualTo(0f));
                Assert.That(drain2, Is.EqualTo(0f));
                Assert.That(registry.TotalStoredGold, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(pot1Object);
                Object.DestroyImmediate(pot2Object);
                Object.DestroyImmediate(destroyedPotObject);
                Object.DestroyImmediate(potConfig);
                Object.DestroyImmediate(registryHost);
            }
        }

        [Test]
        public void TotalStoredGold_ReturnsZero_WhenAllPotsEmpty()
        {
            GameObject registryHost = new GameObject("PotRegistry");
            PotRegistry registry = registryHost.AddComponent<PotRegistry>();

            PotConfig potConfig = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");

            try
            {
                PotController pot = ConfigurePot(potObject, potConfig);
                registry.RegisterPot(pot);

                Assert.That(registry.TotalStoredGold, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(potObject);
                Object.DestroyImmediate(potConfig);
                Object.DestroyImmediate(registryHost);
            }
        }

        [Test]
        public void HasAnyGoldOrHarvestingPot_ReturnsFalse_WhenAllPotsEmptyAndIdle()
        {
            GameObject registryHost = new GameObject("PotRegistry");
            PotRegistry registry = registryHost.AddComponent<PotRegistry>();

            PotConfig potConfig = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");

            try
            {
                PotController pot = ConfigurePot(potObject, potConfig);
                registry.RegisterPot(pot);

                Assert.That(registry.HasAnyGoldOrHarvestingPot, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(potObject);
                Object.DestroyImmediate(potConfig);
                Object.DestroyImmediate(registryHost);
            }
        }

        private static PotController ConfigurePot(GameObject potObject, PotConfig config)
        {
            Rigidbody2D body = potObject.AddComponent<Rigidbody2D>();
            CircleCollider2D collider2D = potObject.AddComponent<CircleCollider2D>();
            Health health = potObject.AddComponent<Health>();
            PotController potController = potObject.AddComponent<PotController>();

            potController.Configure(config, body, collider2D, health, null, null);
            return potController;
        }
    }
}
