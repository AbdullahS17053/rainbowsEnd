using NUnit.Framework;
using RainbowsEnd.Gameplay.Pots;

namespace RainbowsEnd.Tests.EditMode.Pots
{
    public sealed class PotHarvestStateTests
    {
        [Test]
        public void CaptureTrail_ThenGenerateGold_UsesHarvestMultiplier()
        {
            PotHarvestState harvestState = new PotHarvestState();
            harvestState.CaptureTrail(2f, 3f);
            harvestState.UpdateState(0.1f, isMoving: false, harvestCancelDelaySeconds: 0f);

            float generatedGold = harvestState.GenerateGold(0.5f, 2f);

            Assert.That(harvestState.Status, Is.EqualTo(PotHarvestStatus.Harvesting));
            Assert.That(generatedGold, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void MovingPastCancelDelay_InterruptsHarvest()
        {
            PotHarvestState harvestState = new PotHarvestState();
            harvestState.CaptureTrail(1f, 2f);

            harvestState.UpdateState(0.1f, isMoving: true, harvestCancelDelaySeconds: 0.2f);
            harvestState.UpdateState(0.1f, isMoving: true, harvestCancelDelaySeconds: 0.2f);
            harvestState.UpdateState(0.1f, isMoving: true, harvestCancelDelaySeconds: 0.2f);

            Assert.That(harvestState.Status, Is.EqualTo(PotHarvestStatus.Interrupted));
            Assert.That(harvestState.HasCapturedTrail, Is.True);
            Assert.That(harvestState.GenerateGold(0.5f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void ReleaseTrail_StopsHarvestAndReturnsIdle()
        {
            PotHarvestState harvestState = new PotHarvestState();
            harvestState.CaptureTrail(1f, 2f);
            harvestState.UpdateState(0.1f, isMoving: false, harvestCancelDelaySeconds: 0f);

            harvestState.ReleaseTrail();
            harvestState.UpdateState(0.1f, isMoving: false, harvestCancelDelaySeconds: 0f);

            Assert.That(harvestState.Status, Is.EqualTo(PotHarvestStatus.Idle));
            Assert.That(harvestState.HasCapturedTrail, Is.False);
            Assert.That(harvestState.GenerateGold(0.5f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void InterruptedHarvest_ResumesWhenTrailIsStillPresentAndMovementStops()
        {
            PotHarvestState harvestState = new PotHarvestState();
            harvestState.CaptureTrail(1f, 2f);

            harvestState.UpdateState(0.1f, isMoving: true, harvestCancelDelaySeconds: 0f);
            harvestState.UpdateState(0.1f, isMoving: false, harvestCancelDelaySeconds: 0f);

            Assert.That(harvestState.Status, Is.EqualTo(PotHarvestStatus.Harvesting));
            Assert.That(harvestState.HasCapturedTrail, Is.True);
            Assert.That(harvestState.GenerateGold(0.5f, 1f), Is.EqualTo(1f).Within(0.0001f));
        }
    }
}