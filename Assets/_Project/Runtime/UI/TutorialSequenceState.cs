using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainbowsEnd.UI
{
    public enum TutorialStepId
    {
        PickUpPot = 0,
        PlaceOnTrail = 1,
        PlacePortal = 2,
        OpenUpgrades = 3
    }

    [Serializable]
    public sealed class TutorialStepDefinition
    {
        [SerializeField] private TutorialStepId stepId;
        [SerializeField] [TextArea(2, 4)] private string message;

        public TutorialStepDefinition() { }

        public TutorialStepDefinition(TutorialStepId id, string text)
        {
            stepId = id;
            message = text;
        }

        public TutorialStepId StepId => stepId;

        public string Message => message ?? string.Empty;
    }

    public sealed class TutorialSequenceState
    {
        private static readonly IReadOnlyList<TutorialStepId> OrderedStepList = Array.AsReadOnly(new[]
        {
            TutorialStepId.PickUpPot,
            TutorialStepId.PlaceOnTrail,
            TutorialStepId.PlacePortal,
            TutorialStepId.OpenUpgrades
        });

        private int _currentIndex;

        public static IReadOnlyList<TutorialStepId> OrderedSteps => OrderedStepList;

        public bool IsFinished => _currentIndex >= OrderedStepList.Count;

        public TutorialStepId CurrentStep
        {
            get
            {
                if (IsFinished)
                {
                    throw new InvalidOperationException("The tutorial sequence has finished.");
                }

                return OrderedStepList[_currentIndex];
            }
        }

        public void Reset()
        {
            _currentIndex = 0;
        }

        public bool TryComplete(TutorialStepId step)
        {
            if (IsFinished || CurrentStep != step)
            {
                return false;
            }

            _currentIndex++;
            return true;
        }
    }
}