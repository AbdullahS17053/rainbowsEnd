using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class TravellerTargetSelectionUtilityTests
    {
        [Test]
        public void SelectBestCandidateIndex_PrefersHarvestingPotOverCloserIdlePot()
        {
            TravellerTargetCandidate[] candidates =
            {
                new TravellerTargetCandidate(new Vector2(1f, 0f), isHarvesting: false, hasGold: false),
                new TravellerTargetCandidate(new Vector2(3f, 0f), isHarvesting: true, hasGold: false)
            };

            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, candidates, 10f);

            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void SelectBestCandidateIndex_FallsBackToNearestPotWhenNoPotIsHarvesting()
        {
            TravellerTargetCandidate[] candidates =
            {
                new TravellerTargetCandidate(new Vector2(4f, 0f), isHarvesting: false, hasGold: false),
                new TravellerTargetCandidate(new Vector2(2f, 0f), isHarvesting: false, hasGold: false)
            };

            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, candidates, 10f);

            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void SelectBestCandidateIndex_ReturnsMinusOneWhenAllTargetsAreOutOfRange()
        {
            TravellerTargetCandidate[] candidates =
            {
                new TravellerTargetCandidate(new Vector2(12f, 0f), isHarvesting: true, hasGold: false),
                new TravellerTargetCandidate(new Vector2(15f, 0f), isHarvesting: false, hasGold: false)
            };

            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, candidates, 5f);

            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void SelectBestCandidateIndex_ReturnsMinusOneWhenThereAreNoCandidates()
        {
            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, new TravellerTargetCandidate[0], 5f);

            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void SelectBestCandidateIndex_PrefersGoldPotOverEmptyHarvestingPot()
        {
            TravellerTargetCandidate[] candidates =
            {
                new TravellerTargetCandidate(new Vector2(1f, 0f), isHarvesting: true, hasGold: false),
                new TravellerTargetCandidate(new Vector2(3f, 0f), isHarvesting: false, hasGold: true)
            };

            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, candidates, 10f);

            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void SelectBestCandidateIndex_ExcludesPotsWithNoGoldAndNotHarvesting()
        {
            TravellerTargetCandidate[] candidates =
            {
                new TravellerTargetCandidate(new Vector2(1f, 0f), isHarvesting: false, hasGold: false),
                new TravellerTargetCandidate(new Vector2(5f, 0f), isHarvesting: false, hasGold: false)
            };

            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, candidates, 10f);

            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void SelectBestCandidateIndex_PrefersHarvestingAndGoldOverHarvestingOnly()
        {
            TravellerTargetCandidate[] candidates =
            {
                new TravellerTargetCandidate(new Vector2(1f, 0f), isHarvesting: true, hasGold: true),
                new TravellerTargetCandidate(new Vector2(2f, 0f), isHarvesting: true, hasGold: false)
            };

            int index = TravellerTargetSelectionUtility.SelectBestCandidateIndex(Vector2.zero, candidates, 10f);

            Assert.That(index, Is.EqualTo(0));
        }
    }
}
