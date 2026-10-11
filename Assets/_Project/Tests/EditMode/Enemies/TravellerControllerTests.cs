using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Core.Common;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Player;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class TravellerControllerTests
    {
        [Test]
        public void TheftLabel_KeptInactive_NoStaticOverlayAboveThief()
        {
            GameObject traveller = new GameObject("Traveller");
            GameObject labelObject = new GameObject("TheftLabel");
            labelObject.transform.SetParent(traveller.transform);
            labelObject.SetActive(false);

            try
            {
                System.Type tmpType = System.Type.GetType("TMPro.TextMeshPro, Unity.TextMeshPro");
                Assert.That(tmpType, Is.Not.Null);
                Component label = labelObject.AddComponent(tmpType);
                Rigidbody2D body = traveller.AddComponent<Rigidbody2D>();
                CircleCollider2D bodyCollider = traveller.AddComponent<CircleCollider2D>();
                Health health = traveller.AddComponent<Health>();
                PotDrain potDrain = traveller.AddComponent<PotDrain>();
                TravellerController controller = traveller.AddComponent<TravellerController>();
                controller.Configure(null, body, bodyCollider, health, potDrain, null);

                SetField(controller, "theftLabel", label);
                SetField(controller, "_currentState", TravellerState.Draining);
                SetField(potDrain, "_stolenGold", 2.9f);
                InvokeRefresh(controller);

                Assert.That(label.gameObject.activeSelf, Is.False);
                Assert.That(GetLabelText(label), Is.Empty);

                SetField(potDrain, "_stolenGold", 3.1f);
                SetField(controller, "_currentState", TravellerState.Fleeing);
                InvokeRefresh(controller);

                Assert.That(label.gameObject.activeSelf, Is.False);
                Assert.That(GetLabelText(label), Is.Empty);

                controller.OnDespawned();

                Assert.That(label.gameObject.activeSelf, Is.False);
                Assert.That(GetLabelText(label), Is.Empty);

                controller.OnSpawned();

                Assert.That(label.gameObject.activeSelf, Is.False);
                Assert.That(GetLabelText(label), Is.Empty);
                Assert.That(potDrain.StolenGold, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(traveller);
            }
        }

        [Test]
        public void Presentation_UsesCarriedPotRunAndLootCarryBag_AndResetsOnDespawn()
        {
            GameObject traveller = new GameObject("Traveller");
            GameObject bag = new GameObject("CarriedBag");
            GameObject carriedPotObject = new GameObject("CarriedPot");
            GameObject playerObject = new GameObject("Player");

            try
            {
                Rigidbody2D body = traveller.AddComponent<Rigidbody2D>();
                CircleCollider2D bodyCollider = traveller.AddComponent<CircleCollider2D>();
                Health health = traveller.AddComponent<Health>();
                PotDrain potDrain = traveller.AddComponent<PotDrain>();
                TravellerController controller = traveller.AddComponent<TravellerController>();
                controller.Configure(null, body, bodyCollider, health, potDrain, null);

                Assert.That(InvokeBool(controller, "ShouldRunToCarriedPot"), Is.False);

                PotController carriedPot = carriedPotObject.AddComponent<PotController>();
                PlayerRoot player = playerObject.AddComponent<PlayerRoot>();
                SetField(carriedPot, "_carrier", player);
                SetField(controller, "_currentTarget", carriedPot);
                SetField(controller, "_currentState", TravellerState.Seeking);

                Assert.That(InvokeBool(controller, "ShouldRunToCarriedPot"), Is.True);

                SetField(controller, "_currentState", TravellerState.Draining);
                Assert.That(InvokeBool(controller, "ShouldRunToCarriedPot"), Is.False);
                Assert.That(InvokeBool(controller, "ShouldPick"), Is.True);

                SetField(controller, "_currentState", TravellerState.Fleeing);
                Assert.That(InvokeBool(controller, "ShouldPick"), Is.False);
                SetField(controller, "carriedBag", bag);
                InvokePresentation(controller);

                Assert.That(bag.activeSelf, Is.False);


                SetField(controller, "_currentState", TravellerState.Fleeing);
                SetField(potDrain, "_stolenGold", 0.25f);
                InvokePresentation(controller);

                Assert.That(bag.activeSelf, Is.True);

                controller.OnDespawned();

                Assert.That(bag.activeSelf, Is.False);
                Assert.That(InvokeBool(controller, "ShouldCarryLoot"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(traveller);
                Object.DestroyImmediate(bag);
                Object.DestroyImmediate(carriedPotObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        private static void InvokeRefresh(TravellerController controller)
        {
            MethodInfo method = typeof(TravellerController).GetMethod(
                "RefreshTheftLabel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, null);
        }

        private static string GetLabelText(Component label)
        {
            PropertyInfo textProperty = label.GetType().GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(label);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }
        private static void InvokePresentation(TravellerController controller)
        {
            MethodInfo method = typeof(TravellerController).GetMethod(
                "RefreshPresentation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, null);
        }

        private static bool InvokeBool(TravellerController controller, string methodName)
        {
            MethodInfo method = typeof(TravellerController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(controller, null);
        }

        [Test]
        public void StartFleeing_InitializesFleeTargetAndSwitchesState()
        {
            GameObject traveller = new GameObject("Traveller");
            try
            {
                Rigidbody2D body = traveller.AddComponent<Rigidbody2D>();
                CircleCollider2D bodyCollider = traveller.AddComponent<CircleCollider2D>();
                Health health = traveller.AddComponent<Health>();
                PotDrain potDrain = traveller.AddComponent<PotDrain>();
                TravellerController controller = traveller.AddComponent<TravellerController>();
                controller.Configure(null, body, bodyCollider, health, potDrain, null);

                MethodInfo startFleeing = typeof(TravellerController).GetMethod("StartFleeing", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(startFleeing, Is.Not.Null);
                startFleeing.Invoke(controller, null);

                FieldInfo stateField = typeof(TravellerController).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((TravellerState)stateField.GetValue(controller), Is.EqualTo(TravellerState.Fleeing));

                FieldInfo targetField = typeof(TravellerController).GetField("_fleeTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((Vector2)targetField.GetValue(controller), Is.Not.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(traveller);
            }
        }
    }
}
