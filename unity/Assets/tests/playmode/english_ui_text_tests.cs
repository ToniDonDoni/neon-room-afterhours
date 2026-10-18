using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

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
    }
}
