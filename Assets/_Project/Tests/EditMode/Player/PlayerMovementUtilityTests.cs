using NUnit.Framework;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Player
{
    public sealed class PlayerMovementUtilityTests
    {
        [Test]
        public void MoveTowards_AdvancesVelocityByAccelerationStep()
        {
            Vector2 result = PlayerMovementUtility.MoveTowards(Vector2.zero, new Vector2(4f, 0f), 10f, 0.1f);

            Assert.That(result, Is.EqualTo(new Vector2(1f, 0f)));
        }

        [Test]
        public void MoveTowards_ReturnsCurrentVelocity_WhenStepIsZero()
        {
            Vector2 current = new Vector2(2f, -1f);

            Vector2 result = PlayerMovementUtility.MoveTowards(current, new Vector2(4f, 5f), 0f, 0.1f);

            Assert.That(result, Is.EqualTo(current));
        }
    }
}