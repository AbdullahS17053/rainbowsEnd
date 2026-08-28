using NUnit.Framework;
using RainbowsEnd.Gameplay.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Player
{
    public sealed class PlayerFeedbackAnimationTests
    {
        private const string ControllerPath = "Assets/_Project/Art/Animators/PlayerFeedback.controller";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player.prefab";
        private const string StunnedClipPath = "Assets/_Project/Art/Animators/Stunned.fbx";
        private const string StandUpClipPath = "Assets/_Project/Art/Animators/Stand Up.fbx";

        [Test]
        public void PlayerFeedbackController_ContainsPlayerOnlyStunRecoveryFlow()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimationClip stunnedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StunnedClipPath);
            AnimationClip standUpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StandUpClipPath);

            Assert.That(controller, Is.Not.Null);
            Assert.That(stunnedClip, Is.Not.Null);
            Assert.That(standUpClip, Is.Not.Null);

            AnimatorControllerParameter stunParameter = FindParameter(controller, "Stun");
            AnimatorControllerParameter standUpParameter = FindParameter(controller, "StandUp");
            Assert.That(stunParameter, Is.Not.Null);
            Assert.That(stunParameter.type, Is.EqualTo(AnimatorControllerParameterType.Trigger));
            Assert.That(standUpParameter, Is.Not.Null);
            Assert.That(standUpParameter.type, Is.EqualTo(AnimatorControllerParameterType.Trigger));

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = FindState(stateMachine, "Idle");
            AnimatorState stunned = FindState(stateMachine, "Stunned");
            AnimatorState standUp = FindState(stateMachine, "StandUp");

            Assert.That(idle, Is.Not.Null);
            Assert.That(stunned, Is.Not.Null);
            Assert.That(stunned.motion, Is.SameAs(stunnedClip));
            Assert.That(stunned.speed, Is.EqualTo(stunnedClip.length).Within(0.01f));
            Assert.That(standUp, Is.Not.Null);
            Assert.That(standUp.motion, Is.SameAs(standUpClip));
            Assert.That(standUp.speed, Is.EqualTo(standUpClip.length / 0.75f).Within(0.01f));

            AnimatorStateTransition anyToStunned = FindTransition(stateMachine.anyStateTransitions, stunned);
            AnimatorStateTransition stunnedToStandUp = FindTransition(stunned.transitions, standUp);
            AnimatorStateTransition standUpToIdle = FindTransition(standUp.transitions, idle);

            Assert.That(anyToStunned, Is.Not.Null);
            Assert.That(HasCondition(anyToStunned, "Stun"), Is.True);
            Assert.That(stunnedToStandUp, Is.Not.Null);
            Assert.That(HasCondition(stunnedToStandUp, "StandUp"), Is.True);
            Assert.That(standUpToIdle, Is.Not.Null);
            Assert.That(standUpToIdle.hasExitTime, Is.True);
        }

        [Test]
        public void PlayerPrefab_UsesFeedbackControllerAndInactiveStars()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Animator animator = root.transform.Find("VisualRoot/Fairy Visual").GetComponent<Animator>();
                GameObject stars = root.transform.Find("StunFeedback").gameObject;
                PlayerRoot playerRoot = root.GetComponent<PlayerRoot>();
                SerializedObject serialized = new SerializedObject(playerRoot);

                Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), Is.EqualTo(ControllerPath));
                Assert.That(stars.activeSelf, Is.False);
                Assert.That(serialized.FindProperty("feedbackAnimator").objectReferenceValue, Is.SameAs(animator));
                Assert.That(serialized.FindProperty("stunnedStars").objectReferenceValue, Is.SameAs(stars));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AnimatorControllerParameter FindParameter(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return parameter;
                }
            }

            return null;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state.name == name)
                {
                    return child.state;
                }
            }

            return null;
        }

        private static AnimatorStateTransition FindTransition(AnimatorStateTransition[] transitions, AnimatorState destination)
        {
            foreach (AnimatorStateTransition transition in transitions)
            {
                if (transition.destinationState == destination)
                {
                    return transition;
                }
            }

            return null;
        }

        private static bool HasCondition(AnimatorStateTransition transition, string parameterName)
        {
            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
