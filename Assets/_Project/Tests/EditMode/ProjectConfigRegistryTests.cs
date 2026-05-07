using NUnit.Framework;
using RainbowsEnd.Core.Config;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class ProjectConfigRegistryTests
    {
        [Test]
        public void HasCoreReferences_IsFalse_WhenRegistryIsEmpty()
        {
            ProjectConfigRegistry registry = ScriptableObject.CreateInstance<ProjectConfigRegistry>();

            Assert.That(registry.HasCoreReferences, Is.False);
        }

        [Test]
        public void Configure_AssignsAllCoreReferences()
        {
            ProjectConfigRegistry registry = ScriptableObject.CreateInstance<ProjectConfigRegistry>();
            PlayerConfig player = ScriptableObject.CreateInstance<PlayerConfig>();
            EnemyConfig traveller = ScriptableObject.CreateInstance<EnemyConfig>();
            UnicornConfig unicorn = ScriptableObject.CreateInstance<UnicornConfig>();
            PotConfig pot = ScriptableObject.CreateInstance<PotConfig>();
            WeaponConfig weapon = ScriptableObject.CreateInstance<WeaponConfig>();
            PortalConfig portal = ScriptableObject.CreateInstance<PortalConfig>();
            EconomyConfig economy = ScriptableObject.CreateInstance<EconomyConfig>();
            WaveConfig wave = ScriptableObject.CreateInstance<WaveConfig>();

            registry.Configure(player, traveller, unicorn, pot, weapon, portal, economy, wave);

            Assert.That(registry.HasCoreReferences, Is.True);
            Assert.That(registry.PlayerConfig, Is.SameAs(player));
            Assert.That(registry.TravellerConfig, Is.SameAs(traveller));
            Assert.That(registry.UnicornConfig, Is.SameAs(unicorn));
            Assert.That(registry.PotConfig, Is.SameAs(pot));
            Assert.That(registry.DefaultWeaponConfig, Is.SameAs(weapon));
            Assert.That(registry.PortalConfig, Is.SameAs(portal));
            Assert.That(registry.EconomyConfig, Is.SameAs(economy));
            Assert.That(registry.WaveConfig, Is.SameAs(wave));
        }
    }
}