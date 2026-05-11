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