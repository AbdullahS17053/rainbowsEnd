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

        [Test]
        public void ResolveFireOrigin_UsesAuthoredTransformWhenPresent()
        {
            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.position = new Vector3(7f, 9f, 0f);

            Vector2 origin = RangedAttackController.ResolveFireOrigin(
                new Vector2(1f, 2f),
                Vector2.right,
                muzzleObject.transform,
                0.5f);

            Assert.That(origin, Is.EqualTo(new Vector2(7f, 9f)));
            Object.DestroyImmediate(muzzleObject);
        }

        [Test]
        public void ResolveFireOrigin_UsesOffsetFallbackWhenMissing()
        {
            Vector2 origin = RangedAttackController.ResolveFireOrigin(
                new Vector2(1f, 2f),
                Vector2.up,
                null,
                0.5f);

            Assert.That(origin, Is.EqualTo(new Vector2(1f, 2.5f)));
        }
    }
}