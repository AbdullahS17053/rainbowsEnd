using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotRegistryTests
    {
        [Test]
        public void ActivePotCount_ExcludesDestroyedPots()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject activeObject = new GameObject("ActivePot");
            GameObject destroyedObject = new GameObject("DestroyedPot");

            try
            {
                PotController activePot = ConfigurePot(activeObject, config);
                PotController destroyedPot = ConfigurePot(destroyedObject, config);
                registry.RegisterPot(activePot);
                registry.RegisterPot(destroyedPot);
                destroyedPot.DestroyByUnicorn(null);

                Assert.That(registry.ActivePotCount, Is.EqualTo(1));
                Assert.That(destroyedPot.IsDestroyed, Is.True);
            }
            finally
            {
                DestroyAll(registryObject, activeObject, destroyedObject, config);
            }
        }

        [Test]
        public void TotalStoredGold_FloorsCombinedFractionalValue()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject firstObject = new GameObject("Pot1");
            GameObject secondObject = new GameObject("Pot2");

            try
            {
                PotController first = ConfigurePot(firstObject, config);
                PotController second = ConfigurePot(secondObject, config);
                registry.RegisterPot(first);
                registry.RegisterPot(second);
                first.TryStoreGold(1.75f);
                second.TryStoreGold(2.5f);

                Assert.That(registry.TotalStoredGoldValue, Is.EqualTo(4.25f).Within(0.0001f));
                Assert.That(registry.TotalStoredGold, Is.EqualTo(4));
            }
            finally
            {
                DestroyAll(registryObject, firstObject, secondObject, config);
            }
        }

        [Test]
        public void TrySpendGold_UsesOldestRegisteredPotFirst()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject firstObject = new GameObject("Pot1");
            GameObject secondObject = new GameObject("Pot2");

            try
            {
                PotController first = ConfigurePot(firstObject, config);
                PotController second = ConfigurePot(secondObject, config);
                registry.RegisterPot(first);
                registry.RegisterPot(second);
                first.TryStoreGold(3.25f);
                second.TryStoreGold(4.75f);

                Assert.That(registry.TrySpendGold(5), Is.True);
                Assert.That(first.StoredGold, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(second.StoredGold, Is.EqualTo(3f).Within(0.0001f));
            }
            finally
            {
                DestroyAll(registryObject, firstObject, secondObject, config);
            }
        }

        [Test]
        public void TrySpendGold_WhenInsufficientLeavesPotsUnchanged()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");

            try
            {
                PotController pot = ConfigurePot(potObject, config);
                registry.RegisterPot(pot);
                pot.TryStoreGold(4.9f);

                Assert.That(registry.TrySpendGold(5), Is.False);
                Assert.That(pot.StoredGold, Is.EqualTo(4.9f).Within(0.0001f));
            }
            finally
            {
                DestroyAll(registryObject, potObject, config);
            }
        }

        [Test]
        public void StoreGold_FillsOldestCapacityBeforeNewerPots()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig firstConfig = ScriptableObject.CreateInstance<PotConfig>();
            PotConfig secondConfig = ScriptableObject.CreateInstance<PotConfig>();
            SetMaxStoredGold(firstConfig, 2f);
            SetMaxStoredGold(secondConfig, 3f);
            GameObject firstObject = new GameObject("Pot1");
            GameObject secondObject = new GameObject("Pot2");

            try
            {
                PotController first = ConfigurePot(firstObject, firstConfig);
                PotController second = ConfigurePot(secondObject, secondConfig);
                registry.RegisterPot(first);
                registry.RegisterPot(second);
                first.TryStoreGold(1.5f);

                Assert.That(registry.StoreGold(4f), Is.EqualTo(3.5f).Within(0.0001f));
                Assert.That(first.StoredGold, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(second.StoredGold, Is.EqualTo(3f).Within(0.0001f));
            }
            finally
            {
                DestroyAll(registryObject, firstObject, secondObject, firstConfig, secondConfig);
            }
        }

        [Test]
        public void DestroyedPotsAreIgnoredForGoldOperations()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");

            try
            {
                PotController pot = ConfigurePot(potObject, config);
                registry.RegisterPot(pot);
                pot.TryStoreGold(7f);
                pot.DestroyByUnicorn(null);

                Assert.That(registry.TotalStoredGold, Is.EqualTo(0));
                Assert.That(registry.StoreGold(1f), Is.EqualTo(0f));
                Assert.That(registry.TrySpendGold(1), Is.False);
            }
            finally
            {
                DestroyAll(registryObject, potObject, config);
            }
        }

        [Test]
        public void TotalStoredGold_ReturnsZero_WhenAllPotsEmpty()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");

            try
            {
                PotController pot = ConfigurePot(potObject, config);
                registry.RegisterPot(pot);
                Assert.That(registry.TotalStoredGold, Is.EqualTo(0));
            }
            finally
            {
                DestroyAll(registryObject, potObject, config);
            }
        }

        [Test]
        public void HasAnyGoldOrHarvestingPot_ReturnsFalse_WhenAllPotsEmptyAndIdle()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");

            try
            {
                PotController pot = ConfigurePot(potObject, config);
                registry.RegisterPot(pot);
                Assert.That(registry.HasAnyGoldOrHarvestingPot, Is.False);
            }
            finally
            {
                DestroyAll(registryObject, potObject, config);
            }
        }


        [Test]
        public void HasHarvestingPot_ReturnsTrueOnlyForLiveUncarriedHarvestingPot()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            PotRegistry registry = registryObject.AddComponent<PotRegistry>();
            PotConfig config = ScriptableObject.CreateInstance<PotConfig>();
            GameObject potObject = new GameObject("Pot");
            GameObject playerObject = new GameObject("Player");

            try
            {
                PotController pot = ConfigurePot(potObject, config);
                PlayerRoot playerRoot = playerObject.AddComponent<PlayerRoot>();
                registry.RegisterPot(pot);

                Assert.That(registry.HasHarvestingPot, Is.False);
                SetHarvesting(pot);
                Assert.That(registry.HasHarvestingPot, Is.True);

                Assert.That(pot.TryPickUp(playerRoot), Is.True);
                Assert.That(registry.HasHarvestingPot, Is.False);

                pot.Drop(Vector2.zero);
                SetHarvesting(pot);
                pot.DestroyByUnicorn(null);
                Assert.That(registry.HasHarvestingPot, Is.False);
            }
            finally
            {
                DestroyAll(registryObject, potObject, playerObject, config);
            }
        }

        private static PotController ConfigurePot(GameObject potObject, PotConfig config)
        {
            Rigidbody2D body = potObject.AddComponent<Rigidbody2D>();
            CircleCollider2D collider2D = potObject.AddComponent<CircleCollider2D>();
            Health health = potObject.AddComponent<Health>();
            PotController pot = potObject.AddComponent<PotController>();
            pot.Configure(config, body, collider2D, health, null, null);
            return pot;
        }


        private static void SetHarvesting(PotController pot)
        {
            FieldInfo field = typeof(PotController).GetField("_harvestState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            PotHarvestState state = (PotHarvestState)field.GetValue(pot);
            state.CaptureTrail(1f, 2f, 100f);
            state.UpdateState(0.1f, false, 0f);
        }

        private static void SetMaxStoredGold(PotConfig config, float amount)
        {
            System.Reflection.FieldInfo field = typeof(PotConfig).GetField(
                "<MaxStoredGold>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(config, amount);
        }

        private static void DestroyAll(params Object[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null)
                {
                    Object.DestroyImmediate(objects[index]);
                }
            }
        }
    }
}