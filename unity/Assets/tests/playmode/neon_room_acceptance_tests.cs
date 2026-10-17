// SDDTDD SPEC: ../../../../tasks/spec-006.md
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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonRoom.Tests
{
    public sealed class NeonRoomAcceptanceTests
    {
        [SetUp]
        public void ResetProfile()
        {
            PlayerPrefs.DeleteKey("neon-room.nickname");
            PlayerPrefs.DeleteKey("neon-room.outfit");
            PlayerPrefs.DeleteKey("neon-room.control-mode");
            PlayerPrefs.DeleteKey("neon-room.track");
            PlayerPrefs.DeleteKey("neon-room.volume");
            PlayerPrefs.DeleteKey("neon-room.quality");
            PlayerPrefs.DeleteKey("neon-room.reduced-motion");
        }

        [Test]
        public void UnityApp_ExposesAnEntryPointForEveryPortedAcceptanceArea()
        {
            var requiredObjects = new[]
            {
                "start-button", "nickname-input", "outfit-option-0", "avatar-preview-camera", "location-neon-room",
                "player", "player-camera", "dance-floor", "dj-stage", "bar", "lounge", "action-wheel-button",
                "action-dance-button", "action-drink-button", "action-talk-button", "action-heart-button",
                "action-wave-button", "action-high-five-button", "npc-voice-source", "club-music-source",
                "move-stick", "look-stick", "settings-screen", "volume-slider", "track-option-2",
                "quality-option-low", "reduced-motion-toggle"
            };
            var missing = requiredObjects.Where(name => FindInLoadedScene(name) == null).ToArray();
            Assert.That(missing, Is.Empty,
                "Unity acceptance entry points missing for the ported areas: " + string.Join(", ", missing));
        }

        [Test]
        public void EditorBuild_UsesSpecifiedUnityVersionAndIncludesAPlayableWebScene()
        {
#if UNITY_EDITOR
            Assert.That(Application.unityVersion, Is.EqualTo("6000.3.6f1"), "The project must run in the specified Unity Editor.");
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(BuildTarget.WebGL), "The active Unity Build Profile must target WebGL.");
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(enabledScenes, Is.Not.Empty, "The Web player must include a playable game scene.");
            Assert.That(enabledScenes.Any(scene => File.Exists(scene.path)), Is.True, "Every enabled player scene must exist in the project.");
#else
            Assert.Fail("This acceptance check must run in the Unity Editor.");
#endif
        }

        [Test]
        public void Onboarding_PresentsWorkingSuggestedAndCustomNameChoices()
        {
            var start = FindButton("start-button");
            Assert.That(start, Is.Not.Null, "The onboarding screen must provide a start action.");
            Assert.That(FindButton("suggested-name-luna"), Is.Not.Null, "Onboarding must offer a suggested nickname.");
            var input = FindInLoadedScene("nickname-input");
            Assert.That(input, Is.Not.Null, "Onboarding must allow a custom nickname.");
            Assert.That(input.GetComponent<InputField>(), Is.Not.Null, "The custom nickname must be editable by the player.");
            Assert.That(FindButton("nickname-continue-button"), Is.Not.Null, "The player must be able to confirm a nickname.");
        }

        [Test]
        public void Wardrobe_PresentsTenSelectableLooksAndAWorkingAvatarPreview()
        {
            var outfitButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(button => button.name.StartsWith("outfit-option-") && button.gameObject.scene.IsValid());
            Assert.That(outfitButtons, Is.EqualTo(10), "The wardrobe must expose all ten selectable looks.");
            Assert.That(FindInLoadedScene("avatar-model"), Is.Not.Null, "A selected look must be represented by a live avatar model.");
            var preview = FindInLoadedScene("avatar-preview-camera");
            Assert.That(preview, Is.Not.Null, "The wardrobe must have a camera for inspecting the avatar.");
            Assert.That(preview.GetComponent<Camera>(), Is.Not.Null);
            Assert.That(FindButton("preview-face-button"), Is.Not.Null, "The player must be able to inspect the avatar's face.");
            Assert.That(FindButton("preview-clothes-button"), Is.Not.Null, "The player must be able to inspect the avatar's clothes.");
        }

        [Test]
        public void LocationSelection_OffersTheActiveClubAndKeepsFutureLocationsUnavailable()
        {
            Assert.That(FindButton("location-neon-room"), Is.Not.Null, "The active NEON ROOM location must be selectable.");
            var rooftop = FindButton("location-rooftop");
            Assert.That(rooftop, Is.Not.Null, "The location selector must show the future rooftop option.");
            var enter = FindButton("enter-club-button");
            Assert.That(enter, Is.Not.Null, "The selected location must have an explicit enter action.");
            Assert.That(FindInLoadedScene("selected-location-label"), Is.Not.Null, "The selected location must be visible to the player.");
        }

        [Test]
        public void ClubScene_PresentsPlayableZonesAndIndependentDesktopAndMobileControls()
        {
            foreach (var name in new[] { "player", "player-camera", "dance-floor", "dj-stage", "bar", "lounge" })
                Assert.That(FindInLoadedScene(name), Is.Not.Null, "The playable club must include its " + name + " element.");
            Assert.That(FindButton("action-wheel-button"), Is.Not.Null, "Desktop players must be able to open the action wheel.");
            Assert.That(FindInLoadedScene("move-stick"), Is.Not.Null, "Mobile players need an independent movement stick.");
            Assert.That(FindInLoadedScene("look-stick"), Is.Not.Null, "Mobile players need an independent look stick.");
        }

        [Test]
        public void InteractionScene_PresentsAllGesturesDanceDrinkAndRecordedReplyAudio()
        {
            foreach (var name in new[] { "action-talk-button", "action-heart-button", "action-wave-button", "action-high-five-button", "action-dance-button", "action-drink-button" })
                Assert.That(FindButton(name), Is.Not.Null, "The action wheel must provide " + name + ".");
            var voice = FindInLoadedScene("npc-voice-source");
            Assert.That(voice, Is.Not.Null, "NPC replies must play through a Unity audio source.");
            Assert.That(voice.GetComponent<AudioSource>(), Is.Not.Null);
            Assert.That(FindInLoadedScene("npc-voice-target"), Is.Not.Null, "NPC replies must be range-gated against an NPC target.");
            Assert.That(FindButton("npc-stop-button"), Is.Not.Null, "The player must be able to stop a recorded reply.");
        }

        [Test]
        public void Settings_PresentsPlayableMusicAndPersistentControlAndGraphicsOptions()
        {
            var music = FindInLoadedScene("club-music-source");
            Assert.That(music, Is.Not.Null, "Club music must be played by a Unity audio source.");
            Assert.That(music.GetComponent<AudioSource>(), Is.Not.Null);
            Assert.That(FindInLoadedScene("volume-slider"), Is.Not.Null, "Settings must expose a music-volume control.");
            foreach (var name in new[] { "track-option-2", "quality-option-low", "reduced-motion-toggle", "control-mode-mobile", "control-mode-desktop" })
                Assert.That(FindButton(name), Is.Not.Null, "Settings must provide " + name + ".");
        }

        [UnityTest]
        public IEnumerator NewPlayer_CanEnterACustomNicknameChooseTenLooksAndInspectTheLiveAvatar()
        {
            yield return null;

            Click("start-button");
            AssertVisible("nickname-screen");
            var nickname = GameObject.Find("nickname-input").GetComponent<InputField>();
            nickname.text = "Afterhours Guest";
            Click("nickname-continue-button");
            Assert.That(PlayerPrefs.GetString("neon-room.nickname", ""), Is.EqualTo("Afterhours Guest"));
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

            Click("wardrobe-continue-button");
            AssertVisible("locations-screen");
            Click("location-neon-room");
            Click("enter-club-button");
            AssertVisible("club-hud");
            Click("back-to-wardrobe-button");
            AssertVisible("wardrobe-screen");
            Click("outfit-option-3");
            Assert.That(PlayerPrefs.GetInt("neon-room.outfit", -1), Is.EqualTo(3), "The player can return to the wardrobe and change looks without reloading.");
            Click("back-to-locations-button");
            AssertVisible("locations-screen");
            Click("location-rooftop");
            Assert.That(GameObject.Find("selected-location-label").GetComponent<Text>().text, Does.Contain("ROOFTOP"));
            Assert.That(GameObject.Find("enter-club-button").GetComponent<Button>().interactable, Is.False, "Unimplemented locations remain visibly unavailable.");
            Click("location-neon-room");
            Assert.That(GameObject.Find("enter-club-button").GetComponent<Button>().interactable, Is.True);
            yield return null;
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

            foreach (var zone in new[] { ("map-zone-dancefloor", "DANCE"), ("map-zone-dj", "DJ"), ("map-zone-bar", "BAR"), ("map-zone-lounge", "LOUNGE") })
            {
                Click("map-button");
                AssertVisible("club-map");
                Click(zone.Item1);
                AssertVisible("club-hud");
                Assert.That(GameObject.Find("current-zone-label").GetComponent<Text>().text, Does.Contain(zone.Item2));
            }

            var camera = GameObject.Find("player-camera").transform;
            var rotation = camera.rotation;
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(80f, -20f));
            InputSystem.Update();
            yield return null;
            InputSystem.RemoveDevice(mouse);
            Assert.That(Quaternion.Angle(camera.rotation, rotation), Is.GreaterThan(1f), "Desktop mouse input must control the view.");

            Click("settings-button");
            Click("control-mode-mobile");
            AssertVisible("move-stick");
            AssertVisible("look-stick");
            var startAtControlModeChange = player.position;
            var yaw = camera.rotation;
            DragStick("move-stick", 101, Vector2.zero, new Vector2(0f, 60f));
            DragStick("look-stick", 102, Vector2.zero, new Vector2(60f, 0f));
            yield return new WaitForSeconds(.25f);
            ReleaseStick("move-stick", 101);
            ReleaseStick("look-stick", 102);
            Assert.That(Vector3.Distance(player.position, startAtControlModeChange), Is.GreaterThan(.05f), "Mobile move and look sticks must work simultaneously.");
            Assert.That(Quaternion.Angle(camera.rotation, yaw), Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator Player_CanDanceDrinkAndUseEveryNpcGestureWithRecordedReplies()
        {
            yield return EnterClub();
            Click("map-button");
            Click("map-zone-dancefloor");

            Click("action-wheel-button");
            AssertVisible("action-wheel");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Click("action-wheel-button");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            AssertVisible("action-wheel");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            InputSystem.RemoveDevice(keyboard);
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
                Assert.That(music.time, Is.GreaterThan(0f), "Ducking NPC speech must leave the current music playing without restarting it.");
                Click("npc-stop-button");
                Assert.That(GameObject.Find("npc-speaking-status"), Is.Null);
                Assert.That(music.volume, Is.EqualTo(PlayerPrefs.GetFloat("neon-room.volume", .35f)).Within(.02f));
            }

            var player = GameObject.Find("player").transform;
            var npc = GameObject.Find("npc-voice-target").transform;
            player.position = npc.position + Vector3.forward * 3.4f;
            Click("action-wheel-button");
            Click("action-talk-button");
            var nearVoice = GameObject.Find("npc-voice-source").GetComponent<AudioSource>();
            Assert.That(nearVoice.isPlaying, Is.True, "A deliberate gesture within 3.5 units must trigger the recorded reply.");
            Click("npc-stop-button");

            player.position = npc.position + Vector3.forward * 8f;
            Click("action-wheel-button");
            Click("action-talk-button");
            Assert.That(GameObject.Find("npc-voice-source").GetComponent<AudioSource>().isPlaying, Is.False, "Talk outside 3.5 units must not start the reply.");

            player.position = npc.position + Vector3.forward;
            yield return new WaitForSeconds(.25f);
            Assert.That(GameObject.Find("npc-voice-source").GetComponent<AudioSource>().isPlaying, Is.False, "Approaching an NPC without a gesture must not start a reply.");
            Click("action-wheel-button");
            Click("action-dance-button");
            Assert.That(GameObject.Find("npc-voice-source").GetComponent<AudioSource>().isPlaying, Is.False, "Dancing must not start an NPC reply.");
            Click("action-wheel-button");
            Click("action-drink-button");
            Assert.That(GameObject.Find("npc-voice-source").GetComponent<AudioSource>().isPlaying, Is.False, "Drinking must not start an NPC reply.");

            Click("action-wheel-button");
            Click("action-talk-button");
            var reply = GameObject.Find("npc-voice-source").GetComponent<AudioSource>();
            yield return new WaitForSeconds(.1f);
            var playbackPosition = reply.timeSamples;
            Click("action-wheel-button");
            Click("action-talk-button");
            Assert.That(reply.isPlaying, Is.True);
            Assert.That(reply.timeSamples, Is.GreaterThanOrEqualTo(playbackPosition), "Repeated Talk must not restart or overlap a reply.");
            Click("settings-button");
            Assert.That(reply.isPlaying, Is.False, "Opening a menu must stop the current reply.");
            Click("settings-button");
            Click("action-wheel-button");
            Click("action-talk-button");
            Assert.That(reply.isPlaying, Is.True, "A new gesture after closing settings must start a fresh recorded reply.");
            Click("back-to-wardrobe-button");
            Assert.That(reply.isPlaying, Is.False, "Leaving the club must stop the current reply.");
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
            var music = GameObject.Find("club-music-source").GetComponent<AudioSource>();
            Assert.That(music.isPlaying, Is.True, "Starting music from a user action must start real AudioSource playback.");
            Assert.That(music.clip, Is.Not.Null);
            Assert.That(music.clip.samples, Is.GreaterThan(1000), "A track must contain generated or imported audio samples, not a metadata-only placeholder.");
            Click("track-option-2");
            Assert.That(PlayerPrefs.GetInt("neon-room.track", -1), Is.EqualTo(2));
            Assert.That(GameObject.Find("club-music-source").GetComponent<AudioSource>().clip.name, Does.Contain("Electric pulse"));

            var volume = GameObject.Find("volume-slider").GetComponent<Slider>();
            DragSlider(volume, .65f);
            Assert.That(PlayerPrefs.GetFloat("neon-room.volume", -1f), Is.EqualTo(.65f).Within(.03f));
            Assert.That(music.volume, Is.EqualTo(.65f).Within(.03f), "The volume control must change the audible music source as well as saved settings.");
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
            {
                var path = Path.Combine(directory, filename);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(1024), filename + " must contain a rendered gameplay screenshot.");
                var image = new Texture2D(2, 2, TextureFormat.RGB24, false);
                image.LoadImage(File.ReadAllBytes(path));
                Assert.That(image.width, Is.GreaterThan(320), filename + " must be captured at gameplay resolution.");
                var pixels = image.GetPixels32();
                var brightNeonPixels = pixels.Count(pixel => pixel.r > 150 && (pixel.b > 130 || pixel.g > 130));
                Assert.That(brightNeonPixels, Is.GreaterThan(100), filename + " must render neon-lit scene/UI pixels in the captured frame.");
                Object.DestroyImmediate(image);
            }
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

        private static GameObject FindInLoadedScene(string objectName)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(candidate => candidate.name == objectName && candidate.scene.IsValid());
        }

        private static Button FindButton(string objectName)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == objectName && candidate.gameObject.scene.IsValid());
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

        private static void DragStick(string objectName, int pointerId, Vector2 start, Vector2 end)
        {
            var stick = GameObject.Find(objectName);
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            var pointer = new PointerEventData(eventSystem) { pointerId = pointerId, position = start, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(stick, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = end;
            ExecuteEvents.Execute(stick, pointer, ExecuteEvents.dragHandler);
        }

        private static void ReleaseStick(string objectName, int pointerId)
        {
            var stick = GameObject.Find(objectName);
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>()) { pointerId = pointerId, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(stick, pointer, ExecuteEvents.pointerUpHandler);
        }
    }
}
