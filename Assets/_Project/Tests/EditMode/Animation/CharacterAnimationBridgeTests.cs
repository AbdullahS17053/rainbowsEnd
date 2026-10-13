using System.Reflection;
using NUnit.Framework;
using UnityEngine;
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

        [Test]
        public void SetActiveAnimator_DisablesRootMotionAndUpdatesActiveAnimator()
        {
            GameObject root = new GameObject("TestRoot");
            CharacterAnimationBridge bridge = root.AddComponent<CharacterAnimationBridge>();

            GameObject animObj = new GameObject("AnimObj");
            animObj.transform.SetParent(root.transform);
            Animator anim = animObj.AddComponent<Animator>();
            anim.applyRootMotion = true;

            bridge.SetActiveAnimator(anim);

            Assert.That(bridge.ActiveAnimator, Is.EqualTo(anim));
            Assert.That(anim.applyRootMotion, Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void GetActiveAnimator_FallsBackToActiveChildWhenAssignedAnimatorInactive()
        {
            GameObject root = new GameObject("TestRoot");
            CharacterAnimationBridge bridge = root.AddComponent<CharacterAnimationBridge>();

            GameObject maleObj = new GameObject("Male");
            maleObj.transform.SetParent(root.transform);
            Animator maleAnim = maleObj.AddComponent<Animator>();

            GameObject femaleObj = new GameObject("Female");
            femaleObj.transform.SetParent(root.transform);
            Animator femaleAnim = femaleObj.AddComponent<Animator>();

            bridge.SetActiveAnimator(maleAnim);
            maleObj.SetActive(false);
            femaleObj.SetActive(true);

            Assert.That(bridge.GetActiveAnimator(), Is.EqualTo(femaleAnim));

            Object.DestroyImmediate(root);
        }
    }
}