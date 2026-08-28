using System.Collections.Generic;
using UnityEditor;
using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Orchestration;

namespace RainbowsEnd.Tests.EditMode.Orchestration
{
    public sealed class RunWaveControllerTests
    {
        [Test]
        public void Tick_SpawnsExactCounts_ForConfiguredWave()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Test", 0f, 0.1f, 10, 3, 2)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            for (int index = 0; index < 5; index++)
            {
                controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            }

            Assert.That(spawnState.SpawnedTravellerCount, Is.EqualTo(3));
            Assert.That(spawnState.SpawnedPoliceCount, Is.EqualTo(2));
        }

        [Test]
        public void Tick_DoesNotSpawn_WhenNoPotHasGoldOrHarvesting()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Gated", 0f, 0.1f, 10, 2, 0)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            RunWaveTickResult gateResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, false, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, false, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, false, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(gateResult.SpawnedType, Is.EqualTo(RunSpawnType.None));
            Assert.That(spawnState.SpawnedTravellerCount, Is.Zero);

            RunWaveTickResult openResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(openResult.SpawnedType, Is.EqualTo(RunSpawnType.Traveller));
            Assert.That(spawnState.SpawnedTravellerCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AlternatesMixedWave_TravellerFirst()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Mixed", 0f, 0.1f, 10, 3, 2)
            });
            FakeSpawnState spawnState = new FakeSpawnState();
            List<RunSpawnType> spawnedTypes = new List<RunSpawnType>();

            for (int index = 0; index < 5; index++)
            {
                RunWaveTickResult result = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
                spawnedTypes.Add(result.SpawnedType);
            }

            Assert.That(spawnedTypes, Is.EqualTo(new[]
            {
                RunSpawnType.Traveller,
                RunSpawnType.Police,
                RunSpawnType.Traveller,
                RunSpawnType.Police,
                RunSpawnType.Traveller
            }));
        }

        [Test]
        public void Tick_StallsSpawning_WhenAtMaxConcurrentEnemies()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Limited", 0f, 0.1f, 1, 2, 0)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            RunWaveTickResult firstResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            RunWaveTickResult stalledResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            spawnState.ClearActiveEnemies();
            RunWaveTickResult resumedResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(firstResult.SpawnedType, Is.EqualTo(RunSpawnType.Traveller));
            Assert.That(stalledResult.SpawnedType, Is.EqualTo(RunSpawnType.None));
            Assert.That(resumedResult.SpawnedType, Is.EqualTo(RunSpawnType.Traveller));
            Assert.That(spawnState.SpawnedTravellerCount, Is.EqualTo(2));
        }

        [Test]
        public void Tick_WaitsForActiveEnemiesToClear_BeforeWin()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Clear", 0f, 0.1f, 3, 1, 0)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            RunWaveTickResult waitingResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            spawnState.ClearActiveEnemies();
            RunWaveTickResult winResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(waitingResult.Outcome, Is.EqualTo(RunOutcome.None));
            Assert.That(winResult.Outcome, Is.EqualTo(RunOutcome.Win));
        }

        [Test]
        public void Tick_AdvancesToNextWave_AfterSpawnedEnemiesClear()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Travellers", 0f, 0.1f, 3, 1, 0),
                new WaveDefinition("Police", 0f, 0.1f, 3, 0, 1)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            spawnState.ClearActiveEnemies();
            RunWaveTickResult advanceResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);
            RunWaveTickResult policeSpawnResult = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 1, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(advanceResult.WaveAdvanced, Is.True);
            Assert.That(controller.CurrentWaveNumber, Is.EqualTo(2));
            Assert.That(policeSpawnResult.SpawnedType, Is.EqualTo(RunSpawnType.Police));
        }

        [Test]
        public void Tick_ReturnsLose_WhenAllPotsAreDestroyed()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Unsafe", 0f, 0.1f, 3, 1, 0)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            RunWaveTickResult result = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 0, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(result.Outcome, Is.EqualTo(RunOutcome.Lose));
            Assert.That(spawnState.SpawnedTravellerCount, Is.Zero);
        }

        [Test]
        public void Tick_PotLossWins_WhenFinalWaveWouldAlsoComplete()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Empty", 0f, 0.1f, 3, 0, 0)
            });
            FakeSpawnState spawnState = new FakeSpawnState();

            RunWaveTickResult result = controller.Tick(0.1f, spawnState.ActiveTravellerCount, spawnState.ActivePoliceCount, 0, 0, true, spawnState.TrySpawnTraveller, spawnState.TrySpawnPolice, null);

            Assert.That(result.Outcome, Is.EqualTo(RunOutcome.Lose));
        }

        private sealed class FakeSpawnState
        {
            public int ActiveTravellerCount { get; private set; }

            public int ActivePoliceCount { get; private set; }

            public int SpawnedTravellerCount { get; private set; }

            public int SpawnedPoliceCount { get; private set; }

            public int SpawnedMilitaryCount { get; private set; }

            public int ActiveMilitaryCount { get; private set; }

            public bool TrySpawnTraveller()
            {
                SpawnedTravellerCount++;
                ActiveTravellerCount++;
                return true;
            }

            public bool TrySpawnPolice()
            {
                SpawnedPoliceCount++;
                ActivePoliceCount++;
                return true;
            }

            public bool TrySpawnMilitary()
            {
                SpawnedMilitaryCount++;
                ActiveMilitaryCount++;
                return true;
            }

            public void ClearActiveEnemies()
            {
                ActiveTravellerCount = 0;
                ActivePoliceCount = 0;
                ActiveMilitaryCount = 0;
            }
        }


[Test]
        public void Tick_RotatesTravellerPoliceMilitary_WhenAllTypesRemain()
        {
            RunWaveController controller = new RunWaveController(new[]
            {
                new WaveDefinition("Mixed", 0f, 0.1f, 10, 2, 2, 2)
            });
            FakeSpawnState spawnState = new FakeSpawnState();
            List<RunSpawnType> spawnedTypes = new List<RunSpawnType>();

            for (int index = 0; index < 6; index++)
            {
                RunWaveTickResult result = controller.Tick(
                    0.1f,
                    spawnState.ActiveTravellerCount,
                    spawnState.ActivePoliceCount,
                    spawnState.ActiveMilitaryCount,
                    1,
                    true,
                    spawnState.TrySpawnTraveller,
                    spawnState.TrySpawnPolice,
                    spawnState.TrySpawnMilitary);
                spawnedTypes.Add(result.SpawnedType);
            }

            Assert.That(spawnedTypes, Is.EqualTo(new[]
            {
                RunSpawnType.Traveller,
                RunSpawnType.Police,
                RunSpawnType.Military,
                RunSpawnType.Traveller,
                RunSpawnType.Police,
                RunSpawnType.Military
            }));
        }


        [Test]
        public void DefaultWaveAssetUsesApprovedPoliceTotals()
        {
            WaveConfig waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>(
                "Assets/_Project/ScriptableObjects/Defaults/SO_Wave_Default.asset");

            Assert.That(waveConfig, Is.Not.Null);
            Assert.That(waveConfig.Waves, Has.Length.GreaterThanOrEqualTo(5));
            Assert.That(
                new[]
                {
                    waveConfig.Waves[0].PoliceCount,
                    waveConfig.Waves[1].PoliceCount,
                    waveConfig.Waves[2].PoliceCount,
                    waveConfig.Waves[3].PoliceCount,
                    waveConfig.Waves[4].PoliceCount
                },
                Is.EqualTo(new[] { 0, 0, 3, 4, 5 }));
        }
}
}
