using NUnit.Framework;
using RainbowsEnd.Core.SceneManagement;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class SceneNamesTests
    {
        [Test]
        public void BootSceneName_IsStable()
        {
            Assert.That(SceneNames.Boot, Is.EqualTo("Boot"));
        }

        [Test]
        public void MainArenaSceneName_IsStable()
        {
            Assert.That(SceneNames.MainArena, Is.EqualTo("MainArena"));
        }
    }
}