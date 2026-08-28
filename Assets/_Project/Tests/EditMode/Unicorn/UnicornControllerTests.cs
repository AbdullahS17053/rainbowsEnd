using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Unicorn;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Unicorn
{
    public sealed class UnicornControllerTests
    {
        [Test]
        public void PlayerCollision_StunsOnlyWhenNotCarryingPot()
        {
            UnicornConfig unicornConfig = ScriptableObject.CreateInstance<UnicornConfig>();
            PlayerConfig playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            GameObject unicornObject = new GameObject("Unicorn");
            GameObject playerObject = new GameObject("Player");
            GameObject potObject = new GameObject("CarriedPot");

            try
            {
                Rigidbody2D unicornBody = unicornObject.AddComponent<Rigidbody2D>();
                CircleCollider2D unicornCollider = unicornObject.AddComponent<CircleCollider2D>();
                UnicornController unicorn = unicornObject.AddComponent<UnicornController>();
                unicorn.Configure(unicornConfig, unicornBody, unicornCollider, null, null);

                Rigidbody2D playerBody = playerObject.AddComponent<Rigidbody2D>();
                CircleCollider2D playerCollider = playerObject.AddComponent<CircleCollider2D>();
                Health health = playerObject.AddComponent<Health>();
                PlayerRoot player = playerObject.AddComponent<PlayerRoot>();
                PlayerPotCarryController carryController = playerObject.AddComponent<PlayerPotCarryController>();
                SetField(player, "config", playerConfig);
                SetField(player, "body", playerBody);
                SetField(player, "bodyCollider", playerCollider);
                SetField(player, "health", health);
                SetField(player, "potCarryController", carryController);
                Invoke(player, "OnEnable");
                health.Initialize(playerConfig.MaxHealth);

                Invoke(unicorn, "TryKnockbackPlayer", playerCollider);
                Assert.That(player.IsDazed, Is.True);

                Invoke(player, "RecoverFromDazedState");

                PotController carriedPot = potObject.AddComponent<PotController>();
                SetField(carryController, "_carriedPot", carriedPot);

                Invoke(unicorn, "TryKnockbackPlayer", playerCollider);
                Assert.That(player.IsDazed, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(potObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(unicornObject);
                Object.DestroyImmediate(playerConfig);
                Object.DestroyImmediate(unicornConfig);
            }
        }

        [Test]
        public void PotWarningLine_UsesActualPotGeometryEvenWhenColliderDisabled()
        {
            UnicornConfig unicornConfig = ScriptableObject.CreateInstance<UnicornConfig>();
            GameObject unicornObject = new GameObject("Unicorn");
            GameObject potObject = new GameObject("Pot");
            try
            {
                Rigidbody2D body = unicornObject.AddComponent<Rigidbody2D>();
                CircleCollider2D bodyCollider = unicornObject.AddComponent<CircleCollider2D>();
                LineRenderer line = unicornObject.AddComponent<LineRenderer>();
                UnicornController unicorn = unicornObject.AddComponent<UnicornController>();
                unicorn.Configure(unicornConfig, body, bodyCollider, null, null);
                SetField(unicorn, "_targetPoint", new Vector2(0f, 5f));

                CircleCollider2D potCollider = potObject.AddComponent<CircleCollider2D>();
                potCollider.radius = 0.42f;
                potObject.AddComponent<PotController>();

                Invoke(unicorn, "UpdatePotWarning");
                Assert.That(line.enabled, Is.True);

                potCollider.enabled = false;
                Invoke(unicorn, "UpdatePotWarning");
                Assert.That(line.enabled, Is.True);

                potObject.transform.position = new Vector3(2f, 2f, 0f);
                Invoke(unicorn, "UpdatePotWarning");
                Assert.That(line.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(potObject);
                Object.DestroyImmediate(unicornObject);
                Object.DestroyImmediate(unicornConfig);
            }
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
