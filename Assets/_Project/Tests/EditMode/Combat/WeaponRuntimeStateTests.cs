using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Combat;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Combat
{
    public sealed class WeaponRuntimeStateTests
    {
        [Test]
        public void TryConsumeShot_EnforcesConfiguredCooldown()
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            WeaponRuntimeState runtimeState = new WeaponRuntimeState();
            runtimeState.Configure(config);

            Assert.That(runtimeState.TryConsumeShot(), Is.True);
            Assert.That(runtimeState.TryConsumeShot(), Is.False);

            runtimeState.Tick(config.CooldownSeconds);

            Assert.That(runtimeState.TryConsumeShot(), Is.True);

            Object.DestroyImmediate(config);
        }
    }
}