using NUnit.Framework;
using RainbowsEnd.Audio;
using UnityEngine;

namespace RainbowsEnd.Tests.EditMode.Audio
{
    public sealed class AudioHearingRadiusServiceTests
    {
        private GameObject _serviceObj;
        private AudioHearingRadiusService _service;

        [SetUp]
        public void SetUp()
        {
            _serviceObj = new GameObject("AudioHearingRadiusServiceTests");
            _service = _serviceObj.AddComponent<AudioHearingRadiusService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceObj != null)
            {
                Object.DestroyImmediate(_serviceObj);
            }
        }

        [Test]
        public void IsWithinEarshot_ReturnsTrueAndAudibleVolume_WhenInsideRadius()
        {
            Vector2 origin = _service.ListenerPosition;
            // 1.0 unit is well within the 7 player-widths maximum radius (~5.6m)
            bool inEarshot = _service.IsWithinEarshot(origin + new Vector2(1f, 0f), out float volumeScale);

            Assert.That(inEarshot, Is.True);
            Assert.That(volumeScale, Is.GreaterThanOrEqualTo(0.8f));
        }

        [Test]
        public void IsWithinEarshot_GatesOutAndReturnsFalse_WhenBeyondHearingRadiusAndMargin()
        {
            Vector2 origin = _service.ListenerPosition;
            // 20.0 units is far beyond the earshot radius and falloff margin
            bool inEarshot = _service.IsWithinEarshot(origin + new Vector2(20f, 0f), out float volumeScale);

            Assert.That(inEarshot, Is.False);
            Assert.That(volumeScale, Is.EqualTo(0f));
        }
    }
}
