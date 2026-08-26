using NUnit.Framework;
using RainbowsEnd.Gameplay.Orchestration;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Orchestration
{
    public sealed class RunDirectorTests
    {
        [Test]
        public void Configure_ResetsRunStatistics()
        {
            GameObject directorObject = new GameObject("RunDirector");
            RunDirector director = directorObject.AddComponent<RunDirector>();

            try
            {
                director.Configure(null, null, null, null, null, null);
                director.RecordKill();
                director.RecordSuccessfulTheftEscape();

                Assert.That(director.KillCount, Is.EqualTo(1));
                Assert.That(director.SuccessfulTheftEscapeCount, Is.EqualTo(1));

                director.Configure(null, null, null, null, null, null);

                Assert.That(director.ElapsedRunSeconds, Is.Zero);
                Assert.That(director.KillCount, Is.Zero);
                Assert.That(director.SuccessfulTheftEscapeCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
            }
        }

        [Test]
        public void RecordStatistics_AccumulateOneAtATime()
        {
            GameObject directorObject = new GameObject("RunDirector");
            RunDirector director = directorObject.AddComponent<RunDirector>();

            try
            {
                director.Configure(null, null, null, null, null, null);

                director.RecordKill();
                director.RecordKill();
                director.RecordSuccessfulTheftEscape();
                director.RecordSuccessfulTheftEscape();
                director.RecordSuccessfulTheftEscape();

                Assert.That(director.KillCount, Is.EqualTo(2));
                Assert.That(director.SuccessfulTheftEscapeCount, Is.EqualTo(3));
                Assert.That(director.ElapsedRunSeconds, Is.GreaterThanOrEqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
            }
        }
    }
}
