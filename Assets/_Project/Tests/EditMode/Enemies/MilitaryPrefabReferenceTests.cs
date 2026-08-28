using NUnit.Framework;
using RainbowsEnd.Gameplay.Enemies;
using UnityEditor;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Enemies
{
    public sealed class MilitaryPrefabReferenceTests
    {
        [Test]
        public void WeaponAnchors_AreAssigned()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PF_Military.prefab");
            MilitaryController controller = prefab.GetComponent<MilitaryController>();
            SerializedObject serialized = new SerializedObject(controller);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(serialized.FindProperty("rifleVisual").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("shotgunVisual").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("rifleMuzzle").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("shotgunMuzzle").objectReferenceValue, Is.Not.Null);
        }
    }
}
