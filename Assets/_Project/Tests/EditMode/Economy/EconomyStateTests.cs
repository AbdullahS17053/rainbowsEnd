using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Systems.Economy;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Economy
{
    public sealed class EconomyStateTests
    {
        [Test]
        public void ActiveHarvesterCount_UpdatesCurrentHarvestMultiplier()
        {
            GameObject host = new GameObject("EconomyStateHost");
            EconomyState economyState = host.AddComponent<EconomyState>();
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();
            GameObject harvesterOne = new GameObject("HarvesterOne");
            GameObject harvesterTwo = new GameObject("HarvesterTwo");

            economyState.Configure(config);
            economyState.SetHarvesterActive(harvesterOne, true);

            Assert.That(economyState.CurrentHarvestMultiplier, Is.EqualTo(1f));

            economyState.SetHarvesterActive(harvesterTwo, true);

            Assert.That(economyState.CurrentHarvestMultiplier, Is.EqualTo(2.5f));

            Object.DestroyImmediate(harvesterTwo);
            Object.DestroyImmediate(harvesterOne);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void RemoveGold_ClampsAtZeroAndReturnsActualAmountRemoved()
        {
            GameObject host = new GameObject("EconomyStateHost");
            EconomyState economyState = host.AddComponent<EconomyState>();
            EconomyConfig config = ScriptableObject.CreateInstance<EconomyConfig>();

            economyState.Configure(config);

            float removedOne = economyState.RemoveGold(3f);
            float removedTwo = economyState.RemoveGold(100f);

            Assert.That(removedOne, Is.EqualTo(3f));
            Assert.That(removedTwo, Is.EqualTo(18f));
            Assert.That(economyState.CurrentGold, Is.EqualTo(0));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(host);
        }
    }
}