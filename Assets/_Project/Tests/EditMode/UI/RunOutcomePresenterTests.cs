using NUnit.Framework;
using RainbowsEnd.Gameplay.Orchestration;
using RainbowsEnd.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RainbowsEnd.Tests.EditMode.UI
{
    public sealed class RunOutcomePresenterTests
    {
        private GameObject _rootObject;
        private RunOutcomePresenter _presenter;
        private TMP_Text _titleText;
        private TMP_Text _bodyText;
        private TMP_Text _scoreText;
        private TMP_Text _highScoreText;
        private Button _restartButton;

        [SetUp]
        public void SetUp()
        {
            RunOutcomePresenter.ResetHighScore();

            _rootObject = new GameObject("RunOutcomePresenterTests");
            _presenter = _rootObject.AddComponent<RunOutcomePresenter>();

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(_rootObject.transform);
            _titleText = titleObj.AddComponent<TextMeshProUGUI>();

            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(_rootObject.transform);
            _bodyText = bodyObj.AddComponent<TextMeshProUGUI>();

            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(_rootObject.transform);
            _scoreText = scoreObj.AddComponent<TextMeshProUGUI>();

            GameObject highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(_rootObject.transform);
            _highScoreText = highScoreObj.AddComponent<TextMeshProUGUI>();

            GameObject buttonObj = new GameObject("RestartButton");
            buttonObj.transform.SetParent(_rootObject.transform);
            _restartButton = buttonObj.AddComponent<Button>();
        }

        [TearDown]
        public void TearDown()
        {
            RunOutcomePresenter.ResetHighScore();
            if (_rootObject != null)
            {
                Object.DestroyImmediate(_rootObject);
            }
        }

        [Test]
        public void TryUpdateHighScore_SavesWhenScoreIsHigher()
        {
            bool isNewHigh = RunOutcomePresenter.TryUpdateHighScore(150, out int highScore);

            Assert.That(isNewHigh, Is.True);
            Assert.That(highScore, Is.EqualTo(150));
            Assert.That(RunOutcomePresenter.GetSavedHighScore(), Is.EqualTo(150));
        }

        [Test]
        public void TryUpdateHighScore_DoesNotOverwriteWhenScoreIsLower()
        {
            RunOutcomePresenter.TryUpdateHighScore(200, out _);

            bool isNewHigh = RunOutcomePresenter.TryUpdateHighScore(80, out int highScore);

            Assert.That(isNewHigh, Is.False);
            Assert.That(highScore, Is.EqualTo(200));
            Assert.That(RunOutcomePresenter.GetSavedHighScore(), Is.EqualTo(200));
        }

        [Test]
        public void Show_UpdatesDedicatedScoreAndHighScoreLabels_WhenWired()
        {
            _presenter.ConfigureView(_rootObject, _titleText, _bodyText, _restartButton, _scoreText, _highScoreText);

            _presenter.Show(RunOutcome.Win, 5, 5, 300);

            Assert.That(_titleText.text, Is.EqualTo("You Win!"));
            Assert.That(_scoreText.text, Is.EqualTo("Score: 300"));
            Assert.That(_highScoreText.text, Does.Contain("300"));
            Assert.That(_highScoreText.text, Does.Contain("(New Best!)"));
        }

        [Test]
        public void Show_DoesNotShowNewBest_WhenScoreDoesNotBeatHighScore()
        {
            RunOutcomePresenter.TryUpdateHighScore(500, out _);
            _presenter.ConfigureView(_rootObject, _titleText, _bodyText, _restartButton, _scoreText, _highScoreText);

            _presenter.Show(RunOutcome.Lose, 2, 5, 200);

            Assert.That(_titleText.text, Is.EqualTo("You Lose"));
            Assert.That(_scoreText.text, Is.EqualTo("Score: 200"));
            Assert.That(_highScoreText.text, Is.EqualTo("High Score: 500"));
            Assert.That(_highScoreText.text, Does.Not.Contain("(New Best!)"));
        }

        [Test]
        public void Show_FormatsBodyFallback_WhenDedicatedLabelsAreNull()
        {
            _presenter.ConfigureView(_rootObject, _titleText, _bodyText, _restartButton, null, null);

            _presenter.Show(RunOutcome.Win, 5, 5, 120);

            Assert.That(_bodyText.text, Does.Contain("All 5 waves cleared."));
            Assert.That(_bodyText.text, Does.Contain("Score: 120"));
            Assert.That(_bodyText.text, Does.Contain("High Score: 120"));
            Assert.That(_bodyText.text, Does.Contain("(New Best!)"));
        }
    }
}
