// SDDTDD SPEC: ../tasks/spec-005.md
// Gameplay details are preserved from ../tasks/spec-001.md and ../tasks/spec-002.md.
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NeonRoom.Tests
{
    public sealed class NeonRoomAcceptanceTests
    {
        [UnityTest]
        public IEnumerator NewPlayer_CanChooseNicknameAndTenLooks_AndInspectTheLiveAvatar()
        {
            yield return null;

            Click("start-button");
            AssertVisible("nickname-screen");
            Click("suggested-name-luna");
            Click("nickname-continue-button");
            AssertVisible("wardrobe-screen");

            var looks = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("outfit-option-") && button.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(looks.Length, Is.EqualTo(10), "The wardrobe must expose all ten selectable looks.");
            Click("outfit-option-9");
            AssertVisible("avatar-preview");
            AssertVisible("avatar-model");

            var previewCamera = GameObject.Find("avatar-preview-camera").GetComponent<Camera>();
            var fullDistance = previewCamera.transform.localPosition.magnitude;
            Click("preview-face-button");
            Assert.That(previewCamera.transform.localPosition.magnitude, Is.LessThan(fullDistance));
            Click("preview-clothes-button");
            Click("preview-zoom-in-button");
            Assert.That(previewCamera.transform.localPosition.magnitude, Is.LessThan(2f));

            returnToLocations();
            yield return null;
            Assert.That(PlayerPrefs.GetInt("neon-room.outfit", -1), Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator Player_CanEnterTheClubMoveAndReachItsNamedZones()
        {
            yield return EnterClub();
            AssertVisible("club-hud");
            AssertVisible("player");
            AssertVisible("dance-floor");
            AssertVisible("dj-stage");
            AssertVisible("bar");
            AssertVisible("lounge");

            var player = GameObject.Find("player").transform;
            var start = player.position;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.Update();
            yield return new WaitForSeconds(.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            InputSystem.RemoveDevice(keyboard);
            Assert.That(Vector3.Distance(player.position, start), Is.GreaterThan(.1f), "WASD must move the player in the rendered club.");

            Click("map-button");
            AssertVisible("club-map");
            Click("map-zone-bar");
            AssertVisible("club-hud");
            Assert.That(GameObject.Find("current-zone-label").GetComponent<Text>().text, Does.Contain("BAR"));

            Click("back-to-wardrobe-button");
            AssertVisible("wardrobe-screen");
            Click("outfit-option-3");
            Assert.That(PlayerPrefs.GetInt("neon-room.outfit", -1), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Player_CanDanceDrinkAndUseEveryNpcGestureWithRecordedReplies()
        {
            yield return EnterClub();
            Click("map-button");
            Click("map-zone-dancefloor");

            Click("action-wheel-button");
            AssertVisible("action-wheel");
            Click("action-dance-button");
            AssertVisible("dance-status");
            Click("action-wheel-button");
            Click("action-dance-button");
            Assert.That(GameObject.Find("dance-status"), Is.Null);

            Click("map-button");
            Click("map-zone-bar");
            Click("action-wheel-button");
            Click("action-drink-button");
            AssertVisible("drink-status");
            Click("action-wheel-button");
            Click("action-drink-button");
            Assert.That(GameObject.Find("drink-status"), Is.Null);

            Click("map-button");
            Click("map-zone-dancefloor");
            foreach (var action in new[] { "talk", "heart", "wave", "high-five" })
            {
                Click("action-wheel-button");
                Click("action-" + action + "-button");
                AssertVisible("npc-speaking-status");
                var voice = GameObject.Find("npc-voice-source").GetComponent<AudioSource>();
                var music = GameObject.Find("club-music-source").GetComponent<AudioSource>();
                Assert.That(voice.isPlaying, Is.True, action + " must play the bundled NPC recording.");
                Assert.That(music.isPlaying, Is.True, "NPC speech must not stop club music.");
                Assert.That(music.volume, Is.LessThan(PlayerPrefs.GetFloat("neon-room.volume", .35f)));
                Click("npc-stop-button");
                Assert.That(GameObject.Find("npc-speaking-status"), Is.Null);
                Assert.That(music.volume, Is.EqualTo(PlayerPrefs.GetFloat("neon-room.volume", .35f)).Within(.02f));
            }
        }

        [UnityTest]
        public IEnumerator Settings_ChangeAndPersistControlsMusicVolumeTrackAndGraphics()
        {
            yield return EnterClub();
            Click("settings-button");
            AssertVisible("settings-screen");
            Click("control-mode-mobile");
            Assert.That(PlayerPrefs.GetString("neon-room.control-mode", ""), Is.EqualTo("mobile"));
            AssertVisible("move-stick");
            AssertVisible("look-stick");

            Click("music-toggle-button");
            Assert.That(GameObject.Find("club-music-source").GetComponent<AudioSource>().isPlaying, Is.True);
            Click("track-option-2");
            Assert.That(PlayerPrefs.GetInt("neon-room.track", -1), Is.EqualTo(2));

            var volume = GameObject.Find("volume-slider").GetComponent<Slider>();
            DragSlider(volume, .65f);
            Assert.That(PlayerPrefs.GetFloat("neon-room.volume", -1f), Is.EqualTo(.65f).Within(.03f));
            Click("quality-option-low");
            Assert.That(PlayerPrefs.GetString("neon-room.quality", ""), Is.EqualTo("low"));
            Click("reduced-motion-toggle");
            Assert.That(PlayerPrefs.GetInt("neon-room.reduced-motion", 0), Is.EqualTo(1));

            Click("control-mode-desktop");
            Assert.That(PlayerPrefs.GetString("neon-room.control-mode", ""), Is.EqualTo("desktop"));
        }

        [UnityTest]
        public IEnumerator ReferenceScreens_RenderWardrobeClubAndActionMenuCaptures()
        {
            yield return EnterWardrobe();
            var directory = Path.Combine(Application.temporaryCachePath, "neon-room-unity-acceptance");
            Directory.CreateDirectory(directory);

            yield return Capture(Path.Combine(directory, "wardrobe.png"));
            returnToLocations();
            yield return null;
            Click("enter-club-button");
            yield return null;
            yield return Capture(Path.Combine(directory, "club.png"));
            Click("action-wheel-button");
            yield return null;
            yield return Capture(Path.Combine(directory, "action-menu.png"));

            foreach (var filename in new[] { "wardrobe.png", "club.png", "action-menu.png" })
                Assert.That(new FileInfo(Path.Combine(directory, filename)).Length, Is.GreaterThan(1024), filename + " must contain a rendered gameplay screenshot.");
        }

        private static IEnumerator EnterWardrobe()
        {
            Click("start-button");
            yield return null;
            Click("suggested-name-luna");
            Click("nickname-continue-button");
            yield return null;
            AssertVisible("wardrobe-screen");
        }

        private static IEnumerator EnterClub()
        {
            yield return EnterWardrobe();
            returnToLocations();
            yield return null;
            Click("location-neon-room");
            Click("enter-club-button");
            yield return null;
            AssertVisible("club-hud");
        }

        private static void returnToLocations()
        {
            Click("wardrobe-continue-button");
            AssertVisible("locations-screen");
        }

        private static IEnumerator Capture(string path)
        {
            ScreenCapture.CaptureScreenshot(path, 1);
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        private static void AssertVisible(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            Assert.That(gameObject, Is.Not.Null, "Expected visible game object '" + objectName + "'.");
            Assert.That(gameObject.activeInHierarchy, Is.True, "Expected '" + objectName + "' to be visible and active.");
        }

        private static void Click(string objectName)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == objectName && candidate.gameObject.activeInHierarchy);
            Assert.That(button, Is.Not.Null, "Expected visible interactive button '" + objectName + "'.");
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null, "The visible game controls must be wired through Unity's EventSystem.");
            var data = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                pointerPress = button.gameObject,
                eligibleForClick = true
            };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        private static void DragSlider(Slider slider, float normalizedValue)
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            var rect = slider.GetComponent<RectTransform>();
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                pointerPress = slider.gameObject,
                eligibleForClick = true
            };
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, rect.position, null, out var local);
            var edge = rect.rect.xMin + rect.rect.width * normalizedValue;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local.x, local.y, 0f));
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(edge, local.y, 0f));
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        }
    }
}
