using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;
using RainbowsEnd.Gameplay.Pots;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class MilitaryCarryCoordinationTests
    {
        [Test]
        public void Militaries_JoinWaitingPot_AssembleAroundPot_AndAbandonWithoutPartner()
        {
            GameObject registryObject = new GameObject("PotRegistry");
            GameObject nearPotObject = new GameObject("NearPot");
            GameObject waitingPotObject = new GameObject("WaitingPot");
            GameObject firstObject = new GameObject("FirstMilitary");
            GameObject secondObject = new GameObject("SecondMilitary");

            try
            {
                PotRegistry registry = registryObject.AddComponent<PotRegistry>();
                PotController nearPot = nearPotObject.AddComponent<PotController>();
                PotController waitingPot = waitingPotObject.AddComponent<PotController>();
                nearPot.TryStoreGold(1f);
                waitingPot.TryStoreGold(1f);
                registry.RegisterPot(nearPot);
                registry.RegisterPot(waitingPot);

                nearPot.transform.position = Vector3.zero;
                waitingPot.transform.position = new Vector3(10f, 0f);
                firstObject.transform.position = new Vector3(8.6f, 0f);
                secondObject.transform.position = Vector3.zero;
                firstObject.AddComponent<CircleCollider2D>();
                secondObject.AddComponent<CircleCollider2D>();
                MilitaryController first = firstObject.AddComponent<MilitaryController>();
                MilitaryController second = secondObject.AddComponent<MilitaryController>();
                SetField(first, "_currentState", MilitaryState.Stealing);
                SetField(first, "_stealTargetPot", waitingPot);
                SetField(second, "_potRegistry", registry);

                Assert.That(Invoke<PotController>(second, "ResolveNearestStealablePot"), Is.SameAs(waitingPot));

                firstObject.transform.position = new Vector3(8.6f, 0f);
                secondObject.transform.position = new Vector3(11.4f, 0f);
                SetField(second, "_currentState", MilitaryState.Stealing);
                SetField(second, "_stealTargetPot", waitingPot);

                Assert.That(
                    Invoke<MilitaryController>(first, "FindCarryPartner", waitingPot, true),
                    Is.SameAs(second),
                    "Soldiers on opposite sides of the pot should still assemble.");

                secondObject.SetActive(false);
                Invoke<object>(first, "TickStealing");

                Assert.That(GetField<MilitaryState>(first, "_currentState"), Is.EqualTo(MilitaryState.Combat));
                Assert.That(GetField<PotController>(first, "_stealTargetPot"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(nearPotObject);
                Object.DestroyImmediate(waitingPotObject);
                Object.DestroyImmediate(registryObject);
            }
        }

        private static T Invoke<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
            return (T)method.Invoke(target, arguments);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            return (T)field.GetValue(target);
        }
        [Test]
        public void StartEscaping_InitializesFleeTargetAndPath()
        {
            GameObject militaryObj = new GameObject("Military");
            try
            {
                militaryObj.AddComponent<CircleCollider2D>();
                MilitaryController military = militaryObj.AddComponent<MilitaryController>();
                MethodInfo startEscaping = typeof(MilitaryController).GetMethod("StartEscaping", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(startEscaping, Is.Not.Null);
                startEscaping.Invoke(military, null);

                FieldInfo targetField = typeof(MilitaryController).GetField("_fleeTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((Vector2)targetField.GetValue(military), Is.Not.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(militaryObj);
            }
        }
    }
}
