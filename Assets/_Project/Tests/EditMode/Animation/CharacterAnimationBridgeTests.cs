using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Core.Animation;

namespace RainbowsEnd.Tests.EditMode.Animation
{
    public sealed class CharacterAnimationBridgeTests
    {
        [Test]
        public void LocomotionSampling_RunsInLateUpdate()
        {
            MethodInfo lateUpdate = typeof(CharacterAnimationBridge).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(lateUpdate, Is.Not.Null);
        }
    }
}