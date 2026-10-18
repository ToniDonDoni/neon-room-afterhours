// SDDTDD SPEC: ../../../../tasks/spec-006.md
// Gameplay details are preserved from ../tasks/spec-001.md and ../tasks/spec-002.md.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
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

        [UnitySetUp]
        public IEnumerator ResetGameScene()
        {
            yield return SceneManager.LoadSceneAsync("neon_club");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BrowserUi_UsesTheInputSystemUiModuleWithDefaultPointerActions()
        {
            yield return null;

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null, "The WebGL game must create an EventSystem for browser input.");
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            Assert.That(inputModule, Is.Not.Null, "WebGL UI input must use InputSystemUIInputModule.");
            Assert.That(inputModule.leftClick.action, Is.Not.Null, "The browser pointer click action must be configured.");
            Assert.That(inputModule.point.action, Is.Not.Null, "The browser pointer position action must be configured.");
        }

        [Test]
        public void EditorBuild_UsesSpecifiedUnityVersionAndIncludesAPlayableWebScene()
        {
#if UNITY_EDITOR
            Assert.That(Application.unityVersion, Is.EqualTo("6000.3.6f1"), "The project must run in the specified Unity Editor.");
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(BuildTarget.WebGL), "The active Unity Build Profile must target WebGL.");
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(enabledScenes, Is.Not.Empty, "The Web player must include a playable game scene.");
            Assert.That(enabledScenes.All(scene => File.Exists(scene.path)), Is.True, "Every enabled Web player scene must exist in the project.");
#else
            Assert.Fail("This acceptance check must run in the Unity Editor.");
#endif
        }

        [UnityTest]
        public IEnumerator NewPlayer_CanEnterACustomNicknameChooseTenLooksAndInspectTheLiveAvatar()
        {
            yield return null;

            Click("start-button");
            yield return null;
            AssertVisible("nickname-screen");
            var nickname = GameObject.Find("nickname-input").GetComponent<InputField>();
            nickname.text = "Afterhours Guest";
            Click("nickname-continue-button");
            yield return null;
            Assert.That(PlayerPrefs.GetString("neon-room.nickname", ""), Is.EqualTo("Afterhours Guest"));
            AssertVisible("wardrobe-screen");

            var looks = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(button => button.name.StartsWith("outfit-option-") && button.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(looks.Length, Is.EqualTo(10), "The wardrobe must expose all ten selectable looks.");
            Click("outfit-option-9");
            yield return null;
            AssertVisible("avatar-preview");
            AssertVisible("avatar-model");

            var angleBeforeDrag = GameObject.Find("avatar-model").transform.rotation;
            Drag("avatar-preview", 202, new Vector2(0f, 0f), new Vector2(120f, 20f));
            yield return null;
            var rotatedAvatar = GameObject.Find("avatar-model");
            Assert.That(rotatedAvatar, Is.Not.Null, "The selected avatar must remain visible after dragging its preview.");
            Assert.That(Quaternion.Angle(rotatedAvatar.transform.rotation, angleBeforeDrag), Is.GreaterThan(1f), "Dragging the visible preview must rotate the selected avatar.");

            var previewCamera = GameObject.Find("avatar-preview-camera").GetComponent<Camera>();
            var fullDistance = previewCamera.transform.localPosition.magnitude;
            Click("preview-face-button");
            Assert.That(previewCamera.transform.localPosition.magnitude, Is.LessThan(fullDistance));
            var faceView = previewCamera.transform.localPosition;
            Click("preview-clothes-button");
            Assert.That(Vector3.Distance(previewCamera.transform.localPosition, faceView), Is.GreaterThan(.05f), "Face and clothing inspection must render different avatar views.");
            Click("preview-zoom-in-button");
            Assert.That(previewCamera.transform.localPosition.magnitude, Is.LessThan(2f));

            Click("wardrobe-continue-button");
            yield return null;
            AssertVisible("locations-screen");
            Click("control-mode-mobile");
            Assert.That(PlayerPrefs.GetString("neon-room.control-mode", ""), Is.EqualTo("mobile"), "The player chooses a control scheme before entering the club.");
            Click("location-neon-room");
            Click("enter-club-button");
            yield return null;
            AssertVisible("club-hud");
            Click("back-to-wardrobe-button");
            yield return null;
            AssertVisible("wardrobe-screen");
            Click("outfit-option-3");
            Assert.That(PlayerPrefs.GetInt("neon-room.outfit", -1), Is.EqualTo(3), "The player can return to the wardrobe and change looks without reloading.");
            Click("back-to-locations-button");
            yield return null;
            AssertVisible("locations-screen");
            Click("location-rooftop");
            Assert.That(GameObject.Find("selected-location-label").GetComponent<Text>().text, Does.Contain("ROOFTOP"));
            Assert.That(GameObject.Find("enter-club-button").GetComponent<Button>().interactable, Is.False, "Unimplemented locations remain visibly unavailable.");
            Click("location-neon-room");
            Assert.That(GameObject.Find("enter-club-button").GetComponent<Button>().interactable, Is.True);
            Click("enter-club-button");
            yield return null;
            AssertVisible("move-stick");
            AssertVisible("look-stick");
            Click("back-to-wardrobe-button");
            yield return null;
            Click("wardrobe-continue-button");
            yield return null;
            Click("location-neon-room");
            PlayerPrefs.Save();
            var gameScene = SceneManager.GetActiveScene().name;
            Assert.That(gameScene, Is.Not.Empty, "The game must run from a reloadable Unity scene.");
            yield return SceneManager.LoadSceneAsync(gameScene);
            yield return null;
            Assert.That(PlayerPrefs.GetString("neon-room.nickname", ""), Is.EqualTo("Afterhours Guest"), "The chosen nickname must survive reloading the game scene.");
            Assert.That(PlayerPrefs.GetInt("neon-room.outfit", -1), Is.EqualTo(3), "The chosen outfit must survive reloading the game scene.");
            Assert.That(PlayerPrefs.GetString("neon-room.control-mode", ""), Is.EqualTo("mobile"), "The chosen controls must survive reloading the game scene.");
            Click("start-button");
            yield return null;
            yield return null;
            Click("suggested-name-luna");
            Click("nickname-continue-button");
            Assert.That(PlayerPrefs.GetString("neon-room.nickname", ""), Is.EqualTo("Luna"), "A suggested nickname must be selectable and saved.");
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

            foreach (var zone in new[]
            {
                ("map-zone-dancefloor", "DANCE FLOOR", new Vector3(0f, player.position.y, 4f)),
                ("map-zone-bar", "BAR", new Vector3(-6f, player.position.y, 1f)),
                ("map-zone-lounge", "LOUNGE", new Vector3(6.2f, player.position.y, 5f)),
                ("map-zone-entrance", "ENTRANCE GALLERY", new Vector3(0f, player.position.y, 10f))
            })
            {
                Click("map-button");
                yield return null;
                AssertVisible("club-map");
                Click(zone.Item1);
                yield return null;
                AssertVisible("club-hud");
                Assert.That(GameObject.Find("current-zone-label").GetComponent<Text>().text, Does.Contain(zone.Item2));
                Assert.That(Vector3.Distance(player.position, zone.Item3), Is.LessThan(.1f), "Map travel must place the player in the selected zone.");
            }

            var DJstageStart = player.position;
            var DJwalk = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(DJwalk, new KeyboardState(Key.W));
            InputSystem.Update();
            yield return new WaitForSeconds(8f);
            InputSystem.QueueStateEvent(DJwalk, new KeyboardState());
            InputSystem.Update();
            InputSystem.RemoveDevice(DJwalk);
            Assert.That(player.position.z, Is.LessThan(DJstageStart.z - 5f), "Walking must reach the DJ stage without map teleportation.");
            Assert.That(GameObject.Find("current-zone-label").GetComponent<Text>().text, Does.Contain("DJ"));

            var camera = GameObject.Find("player-camera").transform;
            var rotation = camera.rotation;
            var mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(80f, -20f));
            yield return null;
            InputSystem.RemoveDevice(mouse);
            Assert.That(Quaternion.Angle(camera.rotation, rotation), Is.GreaterThan(1f), "Desktop mouse input must control the view.");

            Click("settings-button");
            yield return null;
            Click("control-mode-mobile");
            AssertVisible("move-stick");
            AssertVisible("look-stick");
            Click("settings-close-button");
            yield return null;
            var startAtControlModeChange = player.position;
            var yaw = camera.rotation;
            DragStick("move-stick", 101, Vector2.zero, new Vector2(0f, 60f));
            DragStick("look-stick", 102, Vector2.zero, new Vector2(60f, 0f));
            yield return new WaitForSeconds(1f);
            ReleaseStick("move-stick", 101);
            ReleaseStick("look-stick", 102);
            Assert.That(Vector3.Distance(player.position, startAtControlModeChange), Is.GreaterThan(.05f), "Mobile move and look sticks must work simultaneously.");
            Assert.That(Quaternion.Angle(camera.rotation, yaw), Is.GreaterThan(1f), "The mobile look stick must rotate the camera while moving.");
        }

        [UnityTest]
        public IEnumerator Player_CanDanceDrinkAndUseEveryNpcGestureWithRecordedReplies()
        {
            yield return EnterClub();
            Click("settings-button");
            yield return null;
            Click("music-toggle-button");
            Click("control-mode-desktop");
            var clubMusicForContinuity = GameObject.Find("club-music-source").GetComponent<AudioSource>();
            Assert.That(clubMusicForContinuity.isPlaying, Is.True, "The club track must be playing before testing an NPC reply.");
            var musicClipBeforeReply = clubMusicForContinuity.clip;
            Click("settings-close-button");
            yield return null;
            Click("map-button");
            yield return null;
            Click("map-zone-dancefloor");
            yield return null;

            Click("action-wheel-button");
            yield return null;
            AssertVisible("action-wheel");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            Click("action-wheel-button");
            Assert.That(FindAny("action-wheel").activeInHierarchy, Is.False, "The action wheel must be closed before testing its keyboard shortcut.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            Assert.That(Keyboard.current, Is.SameAs(keyboard), "The test keyboard must be the active Input System keyboard.");
            GameObject.Find("NEON ROOM Runtime").SendMessage("Update");
            yield return null;
            AssertVisible("action-wheel");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.RemoveDevice(keyboard);
            Click("action-dance-button");
            AssertVisible("dance-status");
            Click("action-wheel-button");
            yield return null;
            Click("action-dance-button");
            Assert.That(GameObject.Find("dance-status"), Is.Null);

            Click("map-button");
            yield return null;
            Click("map-zone-bar");
            yield return null;
            Click("action-wheel-button");
            yield return null;
            Click("action-drink-button");
            AssertVisible("drink-status");
            Click("action-wheel-button");
            yield return null;
            Click("action-drink-button");
            Assert.That(GameObject.Find("drink-status"), Is.Null);

            Click("map-button");
            yield return null;
            Click("map-zone-dancefloor");
            yield return null;
            foreach (var action in new[] { "talk", "heart", "wave", "high-five" })
            {
                Click("action-wheel-button");
                yield return null;
                var musicPositionBeforeReply = clubMusicForContinuity.timeSamples;
                Click("action-" + action + "-button");
                AssertVisible("npc-speaking-status");
                Assert.That(GameObject.Find("speaking-label").GetComponent<Text>().text, Is.EqualTo("LUNA  ·  I RECOGNIZE THIS MUSIC"), "The recorded NPC reply must be announced with its English line.");
                var voice = GameObject.Find("npc-voice-source").GetComponent<AudioSource>();
                var music = GameObject.Find("club-music-source").GetComponent<AudioSource>();
                Assert.That(voice.isPlaying, Is.True, action + " must play the bundled NPC recording.");
                Assert.That(voice.clip.name, Is.EqualTo("npc-voice"), "Every gesture must use the supplied recorded reply asset.");
                Assert.That(voice.clip.length, Is.EqualTo(6.4f).Within(.15f), "The bundled NPC recording must be the supplied 6.4 second one-shot.");
                Assert.That(music.isPlaying, Is.True, "NPC speech must not stop club music.");
                Assert.That(music.clip, Is.SameAs(musicClipBeforeReply), "An NPC reply must keep the same music track instance playing.");
                Assert.That(music.volume, Is.LessThan(PlayerPrefs.GetFloat("neon-room.volume", .35f)));
                yield return new WaitForSeconds(.12f);
                Assert.That(music.timeSamples, Is.GreaterThan(musicPositionBeforeReply), "Ducking speech must let the existing music playback clock continue forward.");
                musicPositionBeforeReply = music.timeSamples;
                Click("npc-stop-button");
                Assert.That(GameObject.Find("npc-speaking-status"), Is.Null);
                Assert.That(music.volume, Is.EqualTo(PlayerPrefs.GetFloat("neon-room.volume", .35f)).Within(.02f));
            }

            var player = GameObject.Find("player").transform;
            var npc = GameObject.Find("npc-voice-target").transform;
            player.position = npc.position + Vector3.forward * 3.4f;
            Click("action-wheel-button");
            yield return null;
            Click("action-talk-button");
            var nearVoice = GameObject.Find("npc-voice-source").GetComponent<AudioSource>();
            Assert.That(nearVoice.isPlaying, Is.True, "A deliberate gesture within 3.5 units must trigger the recorded reply.");
            yield return null;
            Click("npc-stop-button");

            player.position = npc.position + Vector3.forward * 8f;
            Click("action-wheel-button");
            yield return null;
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
        }

        [UnityTest]
        public IEnumerator Settings_ChangeAndPersistControlsMusicVolumeTrackAndGraphics()
        {
            yield return EnterClub();
            Click("settings-button");
            yield return null;
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
            var samples = new float[Mathf.Min(music.clip.samples, music.clip.channels * 2048)];
            Assert.That(music.clip.GetData(samples, 0), Is.True, "The browser track must provide audible sample data.");
            Assert.That(samples.Any(sample => Mathf.Abs(sample) > .001f), Is.True, "The procedural track must not be silent.");
            Click("track-option-2");
            Assert.That(PlayerPrefs.GetInt("neon-room.track", -1), Is.EqualTo(2));
            Assert.That(GameObject.Find("club-music-source").GetComponent<AudioSource>().clip.name, Does.Contain("Electric pulse"));

            var volume = GameObject.Find("volume-slider").GetComponent<Slider>();
            DragSlider(volume, .65f);
            Assert.That(PlayerPrefs.GetFloat("neon-room.volume", -1f), Is.EqualTo(.65f).Within(.03f));
            Assert.That(music.volume, Is.EqualTo(.65f).Within(.03f), "The volume control must change the audible music source as well as saved settings.");
            Click("settings-close-button");
            yield return null;
            Click("action-wheel-button");
            yield return null;
            var player = GameObject.Find("player").transform;
            var nearestNpc = GameObject.Find("npc-voice-target").transform;
            player.position = nearestNpc.position + Vector3.forward;
            Click("action-talk-button");
            var voice = GameObject.Find("npc-voice-source").GetComponent<AudioSource>();
            Assert.That(voice.isPlaying, Is.True);
            Assert.That(voice.volume, Is.EqualTo(.65f).Within(.03f), "Recorded speech must respect the selected volume.");
            Click("settings-button");
            yield return null;
            Click("quality-option-low");
            Assert.That(PlayerPrefs.GetString("neon-room.quality", ""), Is.EqualTo("low"));
            Click("reduced-motion-toggle");
            Assert.That(PlayerPrefs.GetInt("neon-room.reduced-motion", 0), Is.EqualTo(1));

            Click("control-mode-desktop");
            Assert.That(PlayerPrefs.GetString("neon-room.control-mode", ""), Is.EqualTo("desktop"));

            PlayerPrefs.Save();
            var sceneName = SceneManager.GetActiveScene().name;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;
            yield return EnterClub();
            Click("settings-button");
            Assert.That(GameObject.Find("volume-slider").GetComponent<Slider>().value, Is.EqualTo(.65f).Within(.03f), "The selected volume must be restored in the UI after reloading the game.");
            Assert.That(PlayerPrefs.GetInt("neon-room.track", -1), Is.EqualTo(2), "The selected track must survive reloading the game.");
            Assert.That(PlayerPrefs.GetString("neon-room.quality", ""), Is.EqualTo("low"), "The selected graphics quality must survive reloading the game.");
            Assert.That(PlayerPrefs.GetInt("neon-room.reduced-motion", 0), Is.EqualTo(1), "The reduced-motion setting must survive reloading the game.");
            Assert.That(PlayerPrefs.GetString("neon-room.control-mode", ""), Is.EqualTo("desktop"), "The control choice must survive reloading the game.");
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

        [Test]
        public void WebBuild_IncludesTheExactSuppliedNpcRecording()
        {
            var recordingPath = Path.Combine(Application.dataPath, "resources", "npc-voice.mp3");
            Assert.That(File.Exists(recordingPath), Is.True, "The supplied NPC recording must be bundled with the Web player.");
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(recordingPath))
            {
                var hash = System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                Assert.That(hash, Is.EqualTo("a981353c785357ca4d7dde008eea630a57f2ce9c6318067d54624d750feb7b9f"), "Bundle the exact supplied recording, not a substitute or a mixed track.");
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

        private static GameObject FindAny(string objectName)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(transform => transform.gameObject)
                .FirstOrDefault(gameObject => gameObject.name == objectName);
        }

        private static void Click(string objectName)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == objectName && candidate.gameObject.activeInHierarchy);
            Assert.That(button, Is.Not.Null, "Expected visible interactive button '" + objectName + "'.");
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null, "The visible game controls must be wired through Unity's EventSystem.");
            Canvas.ForceUpdateCanvases();
            var canvas = button.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "The game control must belong to a rendered Canvas.");
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var buttonRect = button.GetComponent<RectTransform>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, buttonRect.TransformPoint(buttonRect.rect.center));
            var hit = new PointerEventData(eventSystem) { position = screenPoint, button = PointerEventData.InputButton.Left };
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(hit, raycastResults);
            Assert.That(raycastResults.FirstOrDefault().gameObject, Is.Not.Null, "The control '" + objectName + "' must be reachable by a real UI raycast.");
            Assert.That(raycastResults.First().gameObject.GetComponentInParent<Button>(), Is.SameAs(button), "Visible control '" + objectName + "' must receive input at its rendered position. Top raycast hit: " + raycastResults.First().gameObject.name);
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
            var canvas = slider.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "The slider must belong to a rendered Canvas.");
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var center = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                pointerPress = slider.gameObject,
                eligibleForClick = true,
                position = center
            };
            AssertTopUiHit(eventSystem, slider.gameObject, center, "The volume slider must be reachable at its visible position.");
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, center, eventCamera, out var local);
            var edge = rect.rect.xMin + rect.rect.width * normalizedValue;
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(edge, local.y, 0f));
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        }

        private static void DragStick(string objectName, int pointerId, Vector2 start, Vector2 end)
        {
            var stick = GameObject.Find(objectName);
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            var canvas = stick.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "The mobile stick must belong to a rendered Canvas.");
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect = stick.GetComponent<RectTransform>();
            var center = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
            AssertTopUiHit(eventSystem, stick, center, "The mobile stick must be reachable at its visible position.");
            var pointer = new PointerEventData(eventSystem) { pointerId = pointerId, position = center, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(stick, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = center + (end - start);
            ExecuteEvents.Execute(stick, pointer, ExecuteEvents.dragHandler);
        }

        private static void Drag(string objectName, int pointerId, Vector2 start, Vector2 end)
        {
            var target = GameObject.Find(objectName);
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            var canvas = target.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "The drag target must belong to a rendered Canvas.");
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect = target.GetComponent<RectTransform>();
            var center = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(eventSystem) { pointerId = pointerId, position = center, button = PointerEventData.InputButton.Left };
            AssertTopUiHit(eventSystem, target, center, "The drag target must be reachable at its visible position.");
            pointer.position = center;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            pointer.position = center + (end - start);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
        }

        private static void AssertTopUiHit(EventSystem eventSystem, GameObject target, Vector2 screenPoint, string message)
        {
            var pointer = new PointerEventData(eventSystem) { position = screenPoint, button = PointerEventData.InputButton.Left };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            var hit = results.FirstOrDefault().gameObject;
            Assert.That(hit, Is.Not.Null, message);
            Assert.That(hit == target || hit.transform.IsChildOf(target.transform), Is.True, message + " A different overlay must not intercept the input. Top hit: " + hit.name);
        }

        private static void ReleaseStick(string objectName, int pointerId)
        {
            var stick = GameObject.Find(objectName);
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>()) { pointerId = pointerId, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(stick, pointer, ExecuteEvents.pointerUpHandler);
        }
    }
}
