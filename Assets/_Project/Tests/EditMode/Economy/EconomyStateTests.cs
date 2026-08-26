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

            try
            {
                economyState.Configure(config);
                economyState.SetHarvesterActive(harvesterOne, true);
                Assert.That(economyState.CurrentHarvestMultiplier, Is.EqualTo(1f));

                economyState.SetHarvesterActive(harvesterTwo, true);
                Assert.That(economyState.CurrentHarvestMultiplier, Is.EqualTo(2.5f));
            }
            finally
            {
                Object.DestroyImmediate(harvesterTwo);
                Object.DestroyImmediate(harvesterOne);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(host);
            }
        }
    }
}