using System.Reflection;
using NUnit.Framework;
using RainbowsEnd.Core.Config;
using RainbowsEnd.Gameplay.Player;
using RainbowsEnd.Gameplay.Pots;
using RainbowsEnd.Gameplay.Shop;
using RainbowsEnd.UI;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.UI
{
    public sealed class TutorialSequenceTests
    {
        private GameObject _controllerObject;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(TutorialSequenceController.OptOutPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TutorialSequenceController.OptOutPlayerPrefsKey);
            PlayerPrefs.Save();
            if (_controllerObject != null)
            {
                Object.DestroyImmediate(_controllerObject);
            }
        }

        [Test]
        public void Sequence_AdvancesOnlyInApprovedOrder()
        {
            TutorialSequenceState state = new TutorialSequenceState();

            Assert.That(state.CurrentStep, Is.EqualTo(TutorialStepId.PickUpPot));
            Assert.That(state.TryComplete(TutorialStepId.PlaceOnTrail), Is.False);
            Assert.That(state.TryComplete(TutorialStepId.PickUpPot), Is.True);
            Assert.That(state.TryComplete(TutorialStepId.PickUpPot), Is.False);
            Assert.That(state.CurrentStep, Is.EqualTo(TutorialStepId.PlaceOnTrail));
            Assert.That(state.TryComplete(TutorialStepId.PlaceOnTrail), Is.True);
            Assert.That(state.TryComplete(TutorialStepId.PlacePortal), Is.True);
            Assert.That(state.TryComplete(TutorialStepId.OpenUpgrades), Is.True);
            Assert.That(state.IsFinished, Is.True);
        }

        [Test]
        public void Sequence_ResetStartsAtFirstStepWithoutPersistingProgress()
        {
            TutorialSequenceState state = new TutorialSequenceState();
            state.TryComplete(TutorialStepId.PickUpPot);
            state.TryComplete(TutorialStepId.PlaceOnTrail);

            state.Reset();

            Assert.That(state.IsFinished, Is.False);
            Assert.That(state.CurrentStep, Is.EqualTo(TutorialStepId.PickUpPot));
        }

        [Test]
        public void OptOut_IsPersistedAndSuppressesNewControllerRuns()
        {
            _controllerObject = new GameObject("TutorialSequenceController");
            TutorialSequenceController controller = _controllerObject.AddComponent<TutorialSequenceController>();

            Assert.That(controller.IsTutorialSuppressed, Is.False);
            controller.SuppressTutorialPrompts();

            Assert.That(controller.IsTutorialSuppressed, Is.True);
            Assert.That(PlayerPrefs.GetInt(TutorialSequenceController.OptOutPlayerPrefsKey), Is.EqualTo(1));

            Object.DestroyImmediate(_controllerObject);
            _controllerObject = new GameObject("TutorialSequenceController");
            TutorialSequenceController nextController = _controllerObject.AddComponent<TutorialSequenceController>();
            InvokePrivate(nextController, "Awake");

            Assert.That(nextController.IsTutorialSuppressed, Is.True);
        }

        [Test]
        public void ShopController_IsShopOpenTracksOpenCloseAndDisable()
        {
            float previousTimeScale = Time.timeScale;
            GameObject playerObject = new GameObject("Player");
            GameObject registryObject = new GameObject("PotRegistry");
            GameObject presenterObject = new GameObject("ShopPanelPresenter");
            GameObject shopObject = new GameObject("ShopController");
            ShopConfig shopConfig = ScriptableObject.CreateInstance<ShopConfig>();

            try
            {
                playerObject.AddComponent<PlayerInputReader>();
                PlayerRoot playerRoot = playerObject.AddComponent<PlayerRoot>();
                playerObject.AddComponent<PlayerUpgradeRuntime>();
                PotRegistry registry = registryObject.AddComponent<PotRegistry>();
                ShopPanelPresenter presenter = presenterObject.AddComponent<ShopPanelPresenter>();
                ConfigureSingleUpgrade(shopConfig);
                ShopController controller = shopObject.AddComponent<ShopController>();
                controller.Configure(shopConfig, null, null, playerRoot, registry, null, presenter);

                InvokePrivate(controller, "OpenShop");
                Assert.That(controller.IsShopOpen, Is.True);

                InvokePrivate(controller, "CloseShop");
                Assert.That(controller.IsShopOpen, Is.False);

                InvokePrivate(controller, "OpenShop");
                Assert.That(controller.IsShopOpen, Is.True);
                shopObject.SetActive(false);
                InvokePrivate(controller, "OnDisable");
                Assert.That(controller.IsShopOpen, Is.False);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(shopObject);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(registryObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(shopConfig);
            }
        }

        private static void ConfigureSingleUpgrade(ShopConfig shopConfig)
        {
            SerializedObject serializedObject = new SerializedObject(shopConfig);
            SerializedProperty upgradesProperty = serializedObject.FindProperty("upgrades");
            upgradesProperty.arraySize = 1;
            SerializedProperty definitionProperty = upgradesProperty.GetArrayElementAtIndex(0);
            definitionProperty.FindPropertyRelative("displayName").stringValue = "Range Up";
            definitionProperty.FindPropertyRelative("description").stringValue = "Range Up";
            definitionProperty.FindPropertyRelative("effectType").enumValueIndex = (int)ShopUpgradeEffectType.WeaponRange;
            definitionProperty.FindPropertyRelative("cost").intValue = 1;
            definitionProperty.FindPropertyRelative("maxPurchaseCount").intValue = 1;
            definitionProperty.FindPropertyRelative("valuePerPurchase").floatValue = 1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}