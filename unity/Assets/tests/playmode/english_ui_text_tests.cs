using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NeonRoom.Tests
{
    public sealed class EnglishUiTextTests
    {
        [Test]
        public void AllGameAuthoredText_IsEnglish()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "NeonRoomGame.cs"));
            Assert.That(Regex.IsMatch(source, "[\\u0400-\\u04FF]"), Is.False,
                "All player-facing text authored by the Unity game must be in English, including status messages.");
        }

        [UnityTest]
        public IEnumerator AllRenderedGameUiText_IsEnglish()
        {
            yield return SceneManager.LoadSceneAsync("neon_club");
            yield return null;

            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var nonEnglish = texts
                .Where(text => Regex.IsMatch(text.text ?? string.Empty, "[\\u0400-\\u04FF]"))
                .Select(text => text.name + ": " + text.text)
                .ToArray();

            Assert.That(nonEnglish, Is.Empty,
                "Every Unity UI Text label, including currently hidden screens, must be authored in English.");
        }
    }
}
