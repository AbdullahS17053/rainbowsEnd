using NUnit.Framework;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace RainbowsEnd.Tests.EditMode.Player
{
    public sealed class PlayerAimResolverTests
    {
        [Test]
        public void ResolveAimDirection_UsesControllerAim_WhenNotPointerInput()
        {
            Vector2 direction = PlayerAimResolver.ResolveAimDirection(new Vector2(0.25f, 0.5f), false, null, Vector3.zero, Vector2.up);

            Assert.That(direction, Is.EqualTo(new Vector2(0.4472136f, 0.8944272f)).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void ResolveAimDirection_ReturnsFallback_WhenControllerAimIsInsideDeadzone()
        {
            Vector2 direction = PlayerAimResolver.ResolveAimDirection(new Vector2(0.05f, 0.05f), false, null, Vector3.zero, Vector2.left, 0.2f);

            Assert.That(direction, Is.EqualTo(Vector2.left));
        }
    }
}