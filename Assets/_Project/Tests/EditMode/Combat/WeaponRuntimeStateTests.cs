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

        [Test]
        public void AssignConfig_WithoutReset_PreservesCurrentCooldown()
        {
            WeaponConfig bowConfig = ScriptableObject.CreateInstance<WeaponConfig>();
            WeaponConfig temporaryConfig = ScriptableObject.CreateInstance<WeaponConfig>();
            WeaponRuntimeState runtimeState = new WeaponRuntimeState();
            runtimeState.Configure(bowConfig);

            Assert.That(runtimeState.TryConsumeShot(), Is.True);

            runtimeState.Tick(bowConfig.CooldownSeconds * 0.5f);
            runtimeState.AssignConfig(temporaryConfig, false);

            Assert.That(runtimeState.TryConsumeShot(), Is.False);

            runtimeState.Tick(bowConfig.CooldownSeconds * 0.5f);

            Assert.That(runtimeState.TryConsumeShot(), Is.True);

            Object.DestroyImmediate(bowConfig);
            Object.DestroyImmediate(temporaryConfig);
        }

        [Test]
        public void TryConsumeShot_UsesProvidedCooldownOverride()
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            WeaponRuntimeState runtimeState = new WeaponRuntimeState();
            runtimeState.Configure(config);

            float cooldownOverrideSeconds = config.CooldownSeconds * 0.25f;

            Assert.That(runtimeState.TryConsumeShot(cooldownOverrideSeconds), Is.True);

            runtimeState.Tick(cooldownOverrideSeconds * 1.1f);

            Assert.That(runtimeState.TryConsumeShot(), Is.True);

            Object.DestroyImmediate(config);
        }
    }
}