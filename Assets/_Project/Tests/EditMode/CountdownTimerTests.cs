using NUnit.Framework;
using RainbowsEnd.Core.Utilities;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class CountdownTimerTests
    {
        [Test]
        public void Start_SetsDurationAndBeginsRunning()
        {
            CountdownTimer timer = new CountdownTimer();

            timer.Start(3f);

            Assert.That(timer.DurationSeconds, Is.EqualTo(3f));
            Assert.That(timer.RemainingSeconds, Is.EqualTo(3f));
            Assert.That(timer.IsRunning, Is.True);
            Assert.That(timer.IsComplete, Is.False);
        }

        [Test]
        public void Tick_CompletesTimer_WhenDurationExpires()
        {
            CountdownTimer timer = new CountdownTimer();
            timer.Start(1.5f);

            timer.Tick(0.5f);
            timer.Tick(1f);

            Assert.That(timer.RemainingSeconds, Is.EqualTo(0f));
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.IsComplete, Is.True);
            Assert.That(timer.ElapsedNormalized, Is.EqualTo(1f));
        }
    }
}