using System.Collections;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeonRoom
{
    [DefaultExecutionOrder(-1000)]
    public sealed class NeonRoomGame : MonoBehaviour
    {
        private static readonly string[] OutfitNames =
        {
            "Glass Heart", "Afterglow", "Acid Bloom", "Velvet Static", "Neon Nomad",
            "Midnight Icon", "Silver Current", "Violet Signal", "Chrome Siren", "Last Light"
        };

        private static readonly Color[] OutfitColors =
        {
            new Color(.12f, .9f, 1f), new Color(1f, .18f, .62f), new Color(.68f, .34f, 1f),
            new Color(.98f, .34f, .62f), new Color(.2f, .92f, .72f), new Color(.9f, .84f, 1f),
            new Color(.48f, .78f, 1f), new Color(.75f, .35f, 1f), new Color(1f, .39f, .78f), new Color(.85f, 1f, .35f)
        };

        private static readonly int[] OutfitAppearanceIndices = { 8, 1, 2, 3, 4, 5, 6, 7, 0, 9 };

        private const string NicknameKey = "neon-room.nickname";
        private const string OutfitKey = "neon-room.outfit";
        private const string OutfitOrderVersionKey = "neon-room.outfit-order-version";
        private const string ControlsKey = "neon-room.control-mode";
        private const string TrackKey = "neon-room.track";
        private const string VolumeKey = "neon-room.volume";
        private const string QualityKey = "neon-room.quality";
        private const string MotionKey = "neon-room.reduced-motion";
        private const int PreviewAvatarLayer = 8;
        private const int ClubCharacterLayer = 9;

        private Canvas canvas;
        private Transform uiRoot;
        private Camera playerCamera;
        private Transform player;
        private Transform firstPersonHands;
        private Material firstPersonOutfitMaterial;
        private static Sprite actionCircleSprite;
        private Transform previewAvatar;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private AudioSource musicSource;
        private AudioSource voiceSource;
        private AudioClip[] musicTracks;
        private GameObject npcCrowd;
        private Transform[] crowdAvatars;
        private Vector3[] crowdRestPositions;
        private Quaternion[] crowdRestRotations;
        private GameObject actionWheel;
        private GameObject settingsScreen;
        private GameObject clubHud;
        private GameObject clubMap;
        private GameObject npcSpeakingStatus;
        private Text npcSpeakingLabel;
        private GameObject danceStatus;
        private GameObject drinkStatus;
        private Text currentZoneLabel;
        private Slider volumeSlider;
        private string controlMode;
        private int selectedOutfit;
        private int selectedTrack;
        private float selectedVolume;
        private float yaw;
        private float pitch;
        private bool portraitLayout;
        private float reducedMotion;
        private bool inClub;
        private bool musicInitialized;
        private bool dancing;
        private bool drinking;
        private bool mobileMoveHeld;
        private bool mobileLookHeld;
        private Vector2 mobileMove;
        private Vector2 mobileLook;
        private Coroutine replyRoutine;
        private Vector2 dragStart;
        private bool wasNearNpc;
        private string lastReportedZoneLabel;
        private float previewYaw;
        private float nextMobileStateReportTime;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NeonRoomReportState(string key, string value);
#endif

        public AudioClip suppliedNpcVoice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<NeonRoomGame>() == null)
                new GameObject("NEON ROOM Runtime").AddComponent<NeonRoomGame>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            MigrateOutfitOrder();
            selectedOutfit = Mathf.Clamp(PlayerPrefs.GetInt(OutfitKey, 0), 0, OutfitNames.Length - 1);
            selectedTrack = Mathf.Clamp(PlayerPrefs.GetInt(TrackKey, 0), 0, 2);
            selectedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .35f));
            controlMode = PlayerPrefs.GetString(ControlsKey, "desktop");
            if (controlMode != "mobile" && controlMode != "desktop") controlMode = "desktop";
            reducedMotion = PlayerPrefs.GetInt(MotionKey, 0);
            CreateEnvironment();
            CreateAudio();
            CreateUi();
            ShowScreen("start-screen");
            ReportWebState("outfit", selectedOutfit.ToString());
        }

        private static void MigrateOutfitOrder()
        {
            if (PlayerPrefs.GetInt(OutfitOrderVersionKey, 0) >= 1) return;
            if (PlayerPrefs.HasKey(OutfitKey))
            {
                var savedOutfit = PlayerPrefs.GetInt(OutfitKey, 0);
                if (savedOutfit == 0) PlayerPrefs.SetInt(OutfitKey, 8);
                else if (savedOutfit == 8) PlayerPrefs.SetInt(OutfitKey, 0);
            }
            PlayerPrefs.SetInt(OutfitOrderVersionKey, 1);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            ApplyResponsiveLayout();
            if (previewAvatar != null)
                previewAvatar.localRotation = Quaternion.Euler(0f, previewYaw + (reducedMotion < .5f ? Mathf.Sin(Time.unscaledTime * .35f) * 8f : 0f), 0f);
            if (!inClub) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && controlMode == "desktop")
            {
                if (keyboard.eKey.wasPressedThisFrame) ToggleActionWheel();
                var forward = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                var side = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                MovePlayer(forward, side, Time.deltaTime);
            }

            if (Mouse.current != null && controlMode == "desktop")
            {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * .12f;
                pitch = Mathf.Clamp(pitch - delta.y * .1f, -45f, 45f);
            }

            if (controlMode == "mobile")
            {
                MovePlayer(mobileMove.y, mobileMove.x, Time.deltaTime);
                if (mobileLookHeld)
                {
                    yaw += mobileLook.x * Time.deltaTime * 2.1f;
                    pitch = Mathf.Clamp(pitch - mobileLook.y * Time.deltaTime * 1.5f, -45f, 45f);
                }
            }

            if (controlMode == "mobile" && (mobileMoveHeld || mobileLookHeld) && Time.unscaledTime >= nextMobileStateReportTime)
            {
                nextMobileStateReportTime = Time.unscaledTime + .2f;
                ReportWebState("player-position", string.Format(CultureInfo.InvariantCulture, "{0:0.00},{1:0.00},{2:0.00}", player.position.x, player.position.y, player.position.z));
                ReportWebState("player-yaw", yaw.ToString("0.00", CultureInfo.InvariantCulture));
                ReportWebState("mobile-move", string.Format(CultureInfo.InvariantCulture, "{0:0.00},{1:0.00}", mobileMove.x, mobileMove.y));
                ReportWebState("mobile-look", string.Format(CultureInfo.InvariantCulture, "{0:0.00},{1:0.00}", mobileLook.x, mobileLook.y));
            }

            if (playerCamera != null) playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            UpdateCrowd();
            UpdateZoneLabel();
            UpdateNpcProximity();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) StopReply();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) StopReply();
        }

        private void CreateEnvironment()
        {
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                existingCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                existingCamera.fieldOfView = 68f;
            }
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            player.name = "player";
            player.position = new Vector3(0f, 0f, 10f);
            player.GetComponent<Renderer>().enabled = false;
            player.GetComponent<CapsuleCollider>().enabled = false;
            playerCamera = existingCamera;
            playerCamera.name = "player-camera";
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = new Color(.025f, .012f, .045f, 1f);
            playerCamera.transform.SetParent(player, false);
            playerCamera.transform.localPosition = new Vector3(0f, 1.66f, 0f);
            playerCamera.transform.localRotation = Quaternion.identity;
            CreateFirstPersonHands();
            yaw = 0f;
            pitch = 0f;

            var dark = CreateMaterial(new Color(.035f, .025f, .07f), .2f, .5f);
            var floor = CreateMaterial(new Color(.045f, .035f, .09f), .82f, .78f);
            var chrome = CreateMaterial(new Color(.32f, .31f, .43f), .85f, .88f);
            var cyan = CreateMaterial(new Color(.08f, .6f, .95f), .2f, .2f, new Color(.05f, .45f, 1f));
            var magenta = CreateMaterial(new Color(.8f, .06f, .42f), .15f, .25f, new Color(1f, .02f, .35f));
            var violet = CreateMaterial(new Color(.38f, .1f, .75f), .25f, .3f, new Color(.55f, .05f, 1f));
            var lime = CreateMaterial(new Color(.65f, .9f, .12f), .1f, .3f, new Color(.55f, .85f, .03f));
            MakeCube("club-floor", new Vector3(0f, -.12f, 0f), new Vector3(26f, .2f, 28f), floor);
            MakeCube("club-ceiling", new Vector3(0f, 7f, 0f), new Vector3(26f, .2f, 28f), dark);
            MakeCube("club-back-wall", new Vector3(0f, 3.5f, -14f), new Vector3(26f, 7f, .25f), dark);
            MakeCube("club-left-wall", new Vector3(-13f, 3.5f, 0f), new Vector3(.25f, 7f, 28f), dark);
            MakeCube("club-right-wall", new Vector3(13f, 3.5f, 0f), new Vector3(.25f, 7f, 28f), dark);
            MakeCube("dance-floor", new Vector3(0f, .01f, -2f), new Vector3(11f, .05f, 10f), chrome);
            MakeCube("dj-stage", new Vector3(0f, .35f, -11f), new Vector3(8f, .7f, 3f), dark);
            MakeCube("bar", new Vector3(-10f, .55f, 1f), new Vector3(3.2f, 1.1f, 8f), chrome);
            MakeCube("lounge", new Vector3(9.5f, .4f, 4f), new Vector3(3.2f, .8f, 7f), violet);

            MakeCube("neon-floor-line", new Vector3(-4.6f, .005f, 0f), new Vector3(.025f, .012f, 26f), violet);
            MakeCube("neon-floor-line", new Vector3(4.6f, .005f, 0f), new Vector3(.025f, .012f, 26f), cyan);
            for (var x = -12; x <= 12; x += 6)
                MakeCube("neon-ceiling-line", new Vector3(x, 6.85f, 0f), new Vector3(.035f, .03f, 26f), x % 2 == 0 ? magenta : cyan);
            for (var z = -12; z <= 12; z += 6)
                MakeCube("neon-ceiling-crossline", new Vector3(0f, 6.85f, z), new Vector3(26f, .03f, .035f), violet);
            // Crossed overhead trusses, edge light columns, and a layered DJ backdrop
            // break up the box silhouette and frame the dance floor like the reference.
            for (var i = 0; i < 8; i++)
            {
                var angle = i * 45f;
                var beam = MakeCube("ceiling-light-truss", new Vector3(0f, 6.72f, -4f), new Vector3(.045f, .035f, 18f), i % 2 == 0 ? cyan : magenta);
                beam.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            for (var ring = 0; ring < 3; ring++)
            {
                var radius = 2.2f + ring * 1.8f;
                var segmentLength = 2f * Mathf.PI * radius / 16f * 1.08f;
                for (var segment = 0; segment < 16; segment++)
                {
                    var angle = segment * Mathf.PI * 2f / 16f;
                    var position = new Vector3(Mathf.Sin(angle) * radius, 6.22f - ring * .22f, -2.8f + Mathf.Cos(angle) * radius);
                    var halo = MakeCube("ceiling-halo-segment", position, new Vector3(.065f, .045f, segmentLength), ring == 1 ? magenta : cyan);
                    halo.transform.rotation = Quaternion.Euler(0f, segment * 22.5f + 90f, 0f);
                }
            }
            for (var i = 0; i < 6; i++)
            {
                var x = (i % 3 - 1) * 3.3f;
                var z = i < 3 ? 2f : -6f;
                var spotlight = new GameObject("dance-floor-spotlight");
                spotlight.transform.position = new Vector3(x, 6.1f, z);
                spotlight.transform.LookAt(new Vector3(x * .55f, .1f, -2f));
                var light = spotlight.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = i % 3 == 0 ? new Color(.12f, .72f, 1f) : i % 3 == 1 ? new Color(1f, .1f, .48f) : new Color(.62f, .22f, 1f);
                light.intensity = 20f;
                light.range = 15f;
                light.spotAngle = 52f;
            }
            for (var i = -2; i <= 2; i++)
            {
                MakeCube("dj-backdrop-panel", new Vector3(i * 2.25f, 3.55f, -13.72f), new Vector3(2.02f, 4.3f, .12f), i % 2 == 0 ? violet : dark);
                var bar = MakeCube("dj-backdrop-neon", new Vector3(i * 2.25f, 1.45f, -13.62f), new Vector3(.035f, 2.35f, .06f), i % 2 == 0 ? cyan : magenta);
            }
            var marqueeObject = new GameObject("after-hours-marquee");
            marqueeObject.transform.position = new Vector3(0f, 5.05f, -13.48f);
            marqueeObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var marquee = marqueeObject.AddComponent<TextMesh>();
            marquee.text = "AFTER HOURS";
            marquee.anchor = TextAnchor.MiddleCenter;
            marquee.alignment = TextAlignment.Center;
            marquee.fontSize = 84;
            marquee.characterSize = .34f;
            marquee.color = new Color(.5f, .82f, 1f);
            marquee.fontStyle = FontStyle.Bold;
            var consoleBase = MakeCube("dj-console-base", new Vector3(0f, 1.18f, -9.45f), new Vector3(3.6f, .28f, .92f), dark);
            for (var i = 0; i < 2; i++)
            {
                var x = i == 0 ? -1.05f : 1.05f;
                MakeCube("dj-console-deck", new Vector3(x, 1.35f, -9.45f), new Vector3(.86f, .08f, .7f), chrome);
                MakeAvatarPart(null, "dj-console-vinyl", PrimitiveType.Cylinder,
                    new Vector3(x, 1.405f, -9.45f), new Vector3(.29f, .035f, .29f), violet, 0);
            }
            MakeCube("dj-console-mixer", new Vector3(0f, 1.38f, -9.45f), new Vector3(.68f, .12f, .76f), magenta);
            for (var side = -1; side <= 1; side += 2)
            for (var i = 0; i < 4; i++)
            {
                var columnZ = -10f + i * 6f;
                MakeCube("club-light-column", new Vector3(side * 12.72f, 3.25f, columnZ), new Vector3(.1f, 5.8f, .42f), dark);
                MakeCube("club-column-neon", new Vector3(side * 12.64f, 3.25f, columnZ), new Vector3(.06f, 4.9f, .07f), (i + side) % 2 == 0 ? cyan : magenta);
            }
            for (var y = 0; y < 3; y++)
            {
                var speaker = MakeCube("dj-speaker", new Vector3(y == 0 ? -4.8f : y == 1 ? 4.8f : 0f, 1.1f, -12.2f), new Vector3(1f, 2.4f, .6f), chrome);
                var light = speaker.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = y % 2 == 0 ? Color.cyan : Color.magenta;
                light.intensity = .7f;
                light.range = 7f;
            }
            var djAvatar = CreateAvatar("dj-avatar", 2, ClubCharacterLayer);
            djAvatar.position = new Vector3(0f, 0f, -11.4f);
            var pointLight = new GameObject("club-light").AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(.65f, .12f, 1f);
            pointLight.intensity = 55f;
            pointLight.range = 20f;
            pointLight.transform.position = new Vector3(0f, 5f, -3f);
            var secondLight = new GameObject("dance-light").AddComponent<Light>();
            secondLight.type = LightType.Point;
            secondLight.color = new Color(1f, .08f, .42f);
            secondLight.intensity = 38f;
            secondLight.range = 16f;
            secondLight.transform.position = new Vector3(7f, 4.5f, 4f);

            npcCrowd = new GameObject("npc-crowd");
            var npcPositions = new[]
            {
                new Vector3(0f, 0f, 2.5f), new Vector3(-3f, 0f, -3f), new Vector3(2.6f, 0f, -3.2f),
                new Vector3(3.4f, 0f, 1f), new Vector3(-2f, 0f, 1f), new Vector3(7f, 0f, 8f),
                new Vector3(-6f, 0f, 6f), new Vector3(5f, 0f, -6f),
                new Vector3(-6f, 0f, 3f), new Vector3(-5f, 0f, -1f), new Vector3(-6f, 0f, -5f),
                new Vector3(6f, 0f, 4f), new Vector3(7f, 0f, 0f), new Vector3(6f, 0f, -4f),
                new Vector3(-3f, 0f, 5f), new Vector3(3f, 0f, 5f), new Vector3(-1f, 0f, -8f),
                new Vector3(2f, 0f, -8f), new Vector3(-8f, 0f, -2f), new Vector3(8f, 0f, -2f)
            };
            for (var i = 0; i < npcPositions.Length; i++)
            {
                var avatar = CreateAvatar("npc-" + i, i, ClubCharacterLayer);
                avatar.SetParent(npcCrowd.transform, true);
                avatar.position = npcPositions[i];
                avatar.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f);
                avatar.localScale = Vector3.one * (i == 0 ? 1f : .72f + (i % 4) * .08f);
                if (i == 0)
                {
                    avatar.name = "npc-voice-target";
                }
            }
            crowdAvatars = npcCrowd.GetComponentsInChildren<Transform>().Where(item => item.parent == npcCrowd.transform).ToArray();
            crowdRestPositions = crowdAvatars.Select(item => item.localPosition).ToArray();
            crowdRestRotations = crowdAvatars.Select(item => item.localRotation).ToArray();
            RenderSettings.ambientLight = new Color(.14f, .08f, .24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.035f, .016f, .08f);
            RenderSettings.fogDensity = .008f;
            var key = new GameObject("club-key-light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(.58f, .48f, .9f);
            key.intensity = .55f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .35f;
            key.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        }

        private void CreateAudio()
        {
            var musicObject = new GameObject("club-music-source");
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = selectedVolume;
            musicTracks = new[] { CreateMusic("Acid afterglow", 124), CreateMusic("Violet hours", 118), CreateMusic("Electric pulse", 132) };
            musicSource.clip = musicTracks[selectedTrack];

            var voiceObject = new GameObject("npc-voice-source");
            voiceObject.transform.SetParent(player, false);
            voiceSource = voiceObject.AddComponent<AudioSource>();
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.dopplerLevel = 0f;
            voiceSource.volume = selectedVolume;
            voiceSource.clip = suppliedNpcVoice;
        }

        private void CreateUi()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventObject.GetComponent<EventSystem>();
            }
            var legacyInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInputModule != null) legacyInputModule.enabled = false;
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            if (inputModule.actionsAsset == null) inputModule.AssignDefaultActions();
            var canvasObject = new GameObject("NEON ROOM UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = .5f;
            uiRoot = CreateRect(canvas.transform, "screens", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var start = CreateScreen("start-screen", "AFTER DARK\nFIND YOUR FREQUENCY", "A neon night, your own rules.");
            AddButton(start.transform, "start-button", "ENTER THE NIGHT", new Vector2(0f, -170f), new Vector2(400f, 66f), OpenNickname, AccentLime);
            AddText(start.transform, "start-tagline", "NEON ROOM  ·  LOCAL CLUB  ·  ONE NIGHT AT A TIME", new Vector2(0f, -225f), new Vector2(700f, 30f), 17, Color.white);

            CreateNicknameScreen();
            CreateWardrobeScreen();
            CreateLocationsScreen();
            CreateControlSelectionScreen();
            clubHud = CreateRect(uiRoot, "club-hud", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            BuildClubHud(clubHud.transform);
            BuildMobileControls(uiRoot);
            settingsScreen = CreateRect(uiRoot, "settings-screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            BuildSettings(settingsScreen.transform);
            clubMap = CreateRect(uiRoot, "club-map", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            BuildMap(clubMap.transform);
            npcSpeakingStatus = AddPanel(clubHud.transform, "npc-speaking-status", new Vector2(0f, -330f), new Vector2(390f, 68f), new Color(.08f, .04f, .14f, .96f));
            npcSpeakingLabel = AddText(npcSpeakingStatus.transform, "speaking-label", "LUNA  ·  I RECOGNIZE THIS MUSIC", new Vector2(-50f, 0f), new Vector2(280f, 50f), 16, Color.white);
            AddButton(npcSpeakingStatus.transform, "npc-stop-button", "STOP", new Vector2(135f, 0f), new Vector2(92f, 42f), StopReply, AccentPink);
            npcSpeakingStatus.SetActive(false);
            danceStatus = AddTextObject(clubHud.transform, "dance-status", "DANCE MODE  ·  MOVE TO STOP", new Vector2(0f, -74f), new Vector2(390f, 44f), 18, AccentLime);
            danceStatus.SetActive(false);
            drinkStatus = AddTextObject(clubHud.transform, "drink-status", "NEON TONIC  ·  CHEERS", new Vector2(0f, -125f), new Vector2(390f, 44f), 18, AccentCyan);
            drinkStatus.SetActive(false);
            SetControlSelectionAppearance("control-selection-screen");
            SetControlSelectionAppearance("settings-screen");
        }

        private void CreateNicknameScreen()
        {
            var screen = CreateScreen("nickname-screen", "WHAT SHOULD WE CALL YOU?", "Choose a name for tonight or enter your own.");
            var fieldObject = AddPanel(screen.transform, "nickname-input", new Vector2(0f, -30f), new Vector2(480f, 60f), new Color(.08f, .065f, .14f, .98f));
            var input = fieldObject.AddComponent<InputField>();
            input.textComponent = AddText(fieldObject.transform, "nickname-value", "", Vector2.zero, new Vector2(450f, 50f), 22, Color.white);
            input.placeholder = AddText(fieldObject.transform, "nickname-placeholder", "Your after-dark name", Vector2.zero, new Vector2(450f, 50f), 20, new Color(.65f, .6f, .75f));
            input.targetGraphic = fieldObject.GetComponent<Image>();
            input.text = PlayerPrefs.GetString(NicknameKey, "");
            var names = new[] { "Luna", "Void", "Neon Kid", "Kira", "Acid Soul" };
            for (var i = 0; i < names.Length; i++)
            {
                var captured = names[i];
                AddButton(screen.transform, "suggested-name-" + captured.ToLowerInvariant().Replace(" ", "-"), captured, new Vector2(-250f + i * 125f, -112f), new Vector2(112f, 46f), () => input.text = captured, AccentViolet);
            }
            AddButton(screen.transform, "nickname-continue-button", "CHOOSE YOUR LOOK  →", new Vector2(0f, -205f), new Vector2(420f, 62f), () =>
            {
                var nickname = input.text.Trim();
                if (string.IsNullOrEmpty(nickname)) { SetStatus("CHOOSE A NAME FIRST"); return; }
                PlayerPrefs.SetString(NicknameKey, nickname);
                PlayerPrefs.Save();
                ReportWebState("player-name", nickname);
                ShowWardrobe();
            }, AccentLime);
        }

        private void CreateWardrobeScreen()
        {
            var screen = CreateRect(uiRoot, "wardrobe-screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            AddText(screen.transform, "wardrobe-heading", "02  /  DRESS FOR YOURSELF\nFind your reflection.", new Vector2(0f, 385f), new Vector2(1060f, 100f), 34, Color.white);
            var preview = AddPanel(screen.transform, "avatar-preview", new Vector2(-360f, -35f), new Vector2(610f, 570f), new Color(.06f, .045f, .13f, .95f));
            var raw = new GameObject("avatar-preview-image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(preview.transform, false);
            Stretch(raw.rectTransform, 14f);
            raw.color = Color.white;
            CreatePreviewCamera(raw);
            AddText(preview.transform, "avatar-caption", "DRAG TO ROTATE  ↔", new Vector2(0f, -250f), new Vector2(420f, 36f), 17, AccentCyan);
            preview.AddComponent<AvatarDragHandler>().game = this;
            AddButton(preview.transform, "preview-face-button", "FACE", new Vector2(-160f, 245f), new Vector2(120f, 42f), () => SetPreviewView("face"), AccentViolet);
            AddButton(preview.transform, "preview-clothes-button", "CLOTHES", new Vector2(0f, 245f), new Vector2(140f, 42f), () => SetPreviewView("clothes"), AccentPink);
            AddButton(preview.transform, "preview-full-button", "FULL", new Vector2(160f, 245f), new Vector2(120f, 42f), () => SetPreviewView("full"), AccentViolet);
            AddButton(preview.transform, "preview-zoom-in-button", "+", new Vector2(250f, 195f), new Vector2(48f, 42f), () => ZoomPreview(-1f), AccentCyan);
            AddButton(preview.transform, "preview-zoom-out-button", "−", new Vector2(250f, 145f), new Vector2(48f, 42f), () => ZoomPreview(.35f), AccentCyan);

            AddText(screen.transform, "outfit-label", "10 AFTER-DARK LOOKS", new Vector2(435f, 260f), new Vector2(400f, 40f), 20, AccentLime);
            for (var i = 0; i < OutfitNames.Length; i++)
            {
                var index = i;
                var color = OutfitColors[OutfitAppearanceIndices[i]];
                var column = i % 2;
                var row = i / 2;
                var button = AddButton(screen.transform, "outfit-option-" + i, OutfitNames[i].ToUpperInvariant(), new Vector2(315f + column * 245f, 195f - row * 82f), new Vector2(230f, 68f), () => SelectOutfit(index), color);
                button.GetComponent<Image>().color = new Color(.09f, .07f, .16f, .96f);
                var swatch = AddPanel(button.transform, "look-swatch", new Vector2(-88f, 0f), new Vector2(11f, 42f), color);
            }
            AddText(screen.transform, "outfit-info", "ASYMMETRY  ·  DRAPE  ·  CHROME", new Vector2(435f, -155f), new Vector2(470f, 32f), 15, new Color(.76f, .73f, .84f));
            AddButton(screen.transform, "wardrobe-continue-button", "FIND YOUR PARTY  →", new Vector2(435f, -225f), new Vector2(480f, 62f), ShowLocations, AccentLime);
            AddButton(screen.transform, "back-to-locations-button", "BACK TO LOCATIONS", new Vector2(435f, 340f), new Vector2(280f, 42f), ShowLocations, AccentCyan);
        }

        private void CreateLocationsScreen()
        {
            var screen = CreateScreen("locations-screen", "03  /  FOLLOW THE FREQUENCY\nWhere are we going?", "One door is open. A whole night ahead.");
            var card = AddPanel(screen.transform, "location-card", new Vector2(0f, 32f), new Vector2(790f, 160f), new Color(.1f, .065f, .17f, .98f));
            AddText(card.transform, "club-title", "NEON ROOM\nDANCE FLOOR  ·  DJ  ·  BAR  ·  LOUNGE", Vector2.zero, new Vector2(750f, 120f), 30, AccentCyan);
            var locations = new[] { ("neon-room", "NEON ROOM"), ("rooftop", "ROOFTOP  ·  SOON"), ("forest-rave", "FOREST RAVE  ·  SOON"), ("space-club", "SPACE CLUB  ·  SOON") };
            for (var i = 0; i < locations.Length; i++)
            {
                var item = locations[i];
                AddButton(screen.transform, "location-" + item.Item1, item.Item2, new Vector2(-300f + i * 200f, -100f), new Vector2(190f, 52f), () => SelectLocation(item.Item1), i == 0 ? AccentLime : AccentViolet);
            }
            AddText(screen.transform, "selected-location-label", "NEON ROOM  ·  OPEN", new Vector2(0f, -163f), new Vector2(700f, 36f), 20, AccentLime);
            AddButton(screen.transform, "enter-club-button", "LET'S GO  ↗", new Vector2(0f, -250f), new Vector2(400f, 60f), OpenControlSelection, AccentLime);
        }

        private void CreateControlSelectionScreen()
        {
            var screen = CreateScreen("control-selection-screen", "HOW WILL YOU PLAY?", "Choose controls for this device. You can change this in Settings.");
            AddButton(screen.transform, "control-mode-mobile", "PHONE\nTWO TOUCH STICKS", new Vector2(-180f, -70f), new Vector2(340f, 150f), () => SetControlMode("mobile"), AccentCyan);
            AddButton(screen.transform, "control-mode-desktop", "KEYBOARD & MOUSE\nWASD  ·  MOUSE  ·  E", new Vector2(180f, -70f), new Vector2(340f, 150f), () => SetControlMode("desktop"), AccentViolet);
            AddButton(screen.transform, "control-confirm-button", "ENTER THE CLUB  ↗", new Vector2(0f, -205f), new Vector2(420f, 62f), ConfirmControlSelection, AccentLime);
            AddButton(screen.transform, "control-back-button", "BACK TO LOCATIONS", new Vector2(0f, -285f), new Vector2(300f, 46f), ShowLocations, AccentCyan);
        }

        private void BuildClubHud(Transform root)
        {
            AddPanel(root, "hud-top-bar", new Vector2(0f, 405f), new Vector2(1600f, 88f), new Color(.025f, .02f, .055f, .8f));
            AddText(root, "club-title-label", "NEON ROOM   /   AFTER HOURS", new Vector2(-490f, 405f), new Vector2(500f, 60f), 22, AccentCyan);
            currentZoneLabel = AddText(root, "current-zone-label", "DANCE FLOOR", new Vector2(0f, 405f), new Vector2(360f, 54f), 20, AccentLime);
            var mapButton = AddButton(root, "map-button", "MAP", Vector2.zero, new Vector2(105f, 52f), OpenMap, AccentCyan);
            var settingsButton = AddButton(root, "settings-button", "SETTINGS", Vector2.zero, new Vector2(140f, 52f), OpenSettings, AccentViolet);
            var leaveButton = AddButton(root, "back-to-wardrobe-button", "LEAVE CLUB", Vector2.zero, new Vector2(145f, 52f), LeaveClub, AccentPink);
            AnchorHudButton(mapButton, -333f);
            AnchorHudButton(settingsButton, -181f);
            AnchorHudButton(leaveButton, -24f);
            AddText(root, "controls-help", "WASD MOVE   ·   MOUSE LOOK   ·   E ACTIONS", new Vector2(0f, -405f), new Vector2(700f, 48f), 16, Color.white);
            AddButton(root, "action-wheel-button", "ACTIONS  ·  E", new Vector2(0f, -345f), new Vector2(260f, 62f), ToggleActionWheel, AccentLime);
            actionWheel = AddPanel(root, "action-wheel", new Vector2(0f, -125f), new Vector2(440f, 440f), new Color(.025f, .015f, .07f, .82f));
            var wheelImage = actionWheel.GetComponent<Image>();
            wheelImage.sprite = GetActionCircleSprite();
            wheelImage.raycastTarget = false;
            AddRadialActionButton(actionWheel.transform, "action-talk-button", "TALK", new Vector2(0f, 135f), () => UseNpcAction("talk"), AccentCyan);
            AddRadialActionButton(actionWheel.transform, "action-heart-button", "HEART  ♡", new Vector2(117f, 68f), () => UseNpcAction("heart"), AccentPink);
            AddRadialActionButton(actionWheel.transform, "action-wave-button", "WAVE", new Vector2(117f, -68f), () => UseNpcAction("wave"), AccentViolet);
            AddRadialActionButton(actionWheel.transform, "action-high-five-button", "HIGH FIVE", new Vector2(0f, -135f), () => UseNpcAction("high-five"), AccentPink);
            AddRadialActionButton(actionWheel.transform, "action-dance-button", "DANCE", new Vector2(-117f, -68f), ToggleDance, AccentLime);
            AddRadialActionButton(actionWheel.transform, "action-drink-button", "DRINK", new Vector2(-117f, 68f), ToggleDrink, AccentCyan);
            actionWheel.SetActive(false);
        }

        private void BuildMobileControls(Transform root)
        {
            var sticks = AddPanel(root, "mobile-controls", new Vector2(0f, -300f), new Vector2(760f, 180f), new Color(.02f, .02f, .05f, .2f));
            sticks.GetComponent<Image>().raycastTarget = false;
            var move = AddPanel(sticks.transform, "move-stick", new Vector2(-285f, 0f), new Vector2(190f, 190f), new Color(.12f, .8f, 1f, .3f));
            var look = AddPanel(sticks.transform, "look-stick", new Vector2(285f, 0f), new Vector2(190f, 190f), new Color(1f, .2f, .6f, .3f));
            move.AddComponent<MobileStick>().Setup(this, false);
            look.AddComponent<MobileStick>().Setup(this, true);
            sticks.SetActive(false);
        }

        private void BuildSettings(Transform root)
        {
            AddPanel(root, "settings-card", Vector2.zero, new Vector2(740f, 690f), new Color(.025f, .02f, .065f, .98f));
            AddText(root, "settings-title", "YOUR FREQUENCY\nClub settings", new Vector2(0f, 280f), new Vector2(640f, 90f), 30, Color.white);
            AddText(root, "settings-volume-label", "MUSIC + VOICE VOLUME", new Vector2(0f, 195f), new Vector2(500f, 36f), 17, AccentCyan);
            var sliderObj = AddPanel(root, "volume-slider", new Vector2(0f, 150f), new Vector2(520f, 32f), new Color(.25f, .18f, .35f, 1f));
            volumeSlider = sliderObj.AddComponent<Slider>();
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = selectedVolume;
            volumeSlider.fillRect = AddPanel(sliderObj.transform, "fill", new Vector2(-130f, 0f), new Vector2(260f, 12f), AccentCyan).GetComponent<RectTransform>();
            volumeSlider.handleRect = AddPanel(sliderObj.transform, "handle", new Vector2(0f, 0f), new Vector2(28f, 38f), Color.white).GetComponent<RectTransform>();
            volumeSlider.onValueChanged.AddListener(value => { selectedVolume = value; PlayerPrefs.SetFloat(VolumeKey, value); PlayerPrefs.Save(); ApplyMusicVolume(); if (voiceSource != null) voiceSource.volume = value; });
            AddButton(root, "track-option-0", "ACID AFTERGLOW", new Vector2(-190f, 80f), new Vector2(230f, 52f), () => SelectTrack(0), AccentViolet);
            AddButton(root, "track-option-1", "VIOLET HOURS", new Vector2(0f, 80f), new Vector2(210f, 52f), () => SelectTrack(1), AccentPink);
            AddButton(root, "track-option-2", "ELECTRIC PULSE", new Vector2(195f, 80f), new Vector2(230f, 52f), () => SelectTrack(2), AccentCyan);
            AddButton(root, "music-toggle-button", "START MUSIC", new Vector2(0f, 15f), new Vector2(330f, 48f), ToggleMusic, AccentLime);
            AddButton(root, "settings-control-mode-mobile", "PHONE CONTROLS", new Vector2(-160f, -65f), new Vector2(240f, 48f), () => SetControlMode("mobile"), AccentCyan);
            AddButton(root, "settings-control-mode-desktop", "DESKTOP CONTROLS", new Vector2(160f, -65f), new Vector2(240f, 48f), () => SetControlMode("desktop"), AccentViolet);
            AddButton(root, "quality-option-low", "LOW GRAPHICS", new Vector2(-145f, -135f), new Vector2(220f, 48f), () => SetQuality("low"), AccentViolet);
            AddButton(root, "quality-option-high", "HIGH GRAPHICS", new Vector2(145f, -135f), new Vector2(220f, 48f), () => SetQuality("high"), AccentViolet);
            AddButton(root, "reduced-motion-toggle", "REDUCED MOTION  ·  TOGGLE", new Vector2(0f, -202f), new Vector2(400f, 48f), ToggleReducedMotion, AccentPink);
            AddButton(root, "settings-close-button", "BACK TO THE CLUB", new Vector2(0f, -278f), new Vector2(360f, 52f), CloseSettings, AccentLime);
            root.gameObject.SetActive(false);
        }

        private void BuildMap(Transform root)
        {
            AddPanel(root, "map-card", Vector2.zero, new Vector2(700f, 520f), new Color(.025f, .02f, .065f, .98f));
            AddText(root, "map-title", "FIND YOUR SPACE", new Vector2(0f, 190f), new Vector2(600f, 60f), 30, Color.white);
            AddButton(root, "map-zone-dancefloor", "◉  DANCE FLOOR", new Vector2(-150f, 90f), new Vector2(250f, 70f), () => GoToZone(0), AccentPink);
            AddButton(root, "map-zone-bar", "♧  BAR", new Vector2(150f, 90f), new Vector2(250f, 70f), () => GoToZone(1), AccentCyan);
            AddButton(root, "map-zone-lounge", "☾  LOUNGE", new Vector2(-150f, 0f), new Vector2(250f, 70f), () => GoToZone(2), AccentViolet);
            AddButton(root, "map-zone-entrance", "↗  ENTRANCE GALLERY", new Vector2(150f, 0f), new Vector2(250f, 70f), () => GoToZone(3), AccentLime);
            AddButton(root, "map-close-button", "CLOSE MAP", new Vector2(0f, -140f), new Vector2(250f, 54f), CloseMap, AccentCyan);
            root.gameObject.SetActive(false);
        }

        private GameObject CreateScreen(string name, string heading, string subtitle)
        {
            var screen = CreateRect(uiRoot, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var background = screen.AddComponent<Image>();
            background.color = new Color(.025f, .015f, .065f, .96f);
            AddText(screen.transform, name + "-heading", heading, new Vector2(0f, 180f), new Vector2(1100f, 190f), 38, Color.white);
            AddText(screen.transform, name + "-subtitle", subtitle, new Vector2(0f, 55f), new Vector2(900f, 56f), 21, new Color(.75f, .72f, .83f));
            return screen;
        }

        private void OpenNickname() => ShowScreen("nickname-screen");
        private void ShowWardrobe()
        {
            EnsurePreviewAvatar();
            ShowScreen("wardrobe-screen");
        }
        private void ShowLocations() => ShowScreen("locations-screen");

        private void OpenControlSelection() => ShowScreen("control-selection-screen");

        private void ConfirmControlSelection()
        {
            EnterClub();
        }

        private void ApplyResponsiveLayout()
        {
            if (canvas == null || Screen.height == 0) return;
            var isPortrait = (float)Screen.width / Screen.height < .8f;
            if (isPortrait == portraitLayout) return;
            portraitLayout = isPortrait;
            if (isPortrait)
            {
                SetRect("wardrobe-heading", new Vector2(0f, 790f), new Vector2(760f, 100f));
                SetRect("avatar-preview", new Vector2(0f, 340f), new Vector2(590f, 420f));
                SetRect("avatar-caption", new Vector2(0f, -185f), new Vector2(400f, 34f));
                SetRect("preview-face-button", new Vector2(-145f, 170f), new Vector2(105f, 40f));
                SetRect("preview-clothes-button", new Vector2(0f, 170f), new Vector2(125f, 40f));
                SetRect("preview-full-button", new Vector2(145f, 170f), new Vector2(105f, 40f));
                SetRect("preview-zoom-in-button", new Vector2(225f, 115f), new Vector2(42f, 38f));
                SetRect("preview-zoom-out-button", new Vector2(225f, 68f), new Vector2(42f, 38f));
                SetRect("outfit-label", new Vector2(0f, -105f), new Vector2(700f, 36f));
                SetRect("outfit-info", new Vector2(0f, -520f), new Vector2(700f, 32f));
                SetRect("wardrobe-continue-button", new Vector2(0f, -610f), new Vector2(620f, 66f));
                SetRect("back-to-locations-button", new Vector2(0f, 640f), new Vector2(330f, 44f));
                for (var i = 0; i < OutfitNames.Length; i++)
                {
                    var column = i % 2;
                    var row = i / 2;
                    SetRect("outfit-option-" + i, new Vector2(column == 0 ? -190f : 190f, -175f - row * 72f), new Vector2(350f, 62f));
                }
                SetRect("location-card", new Vector2(0f, 495f), new Vector2(760f, 150f));
                SetRect("location-neon-room", new Vector2(-190f, -80f), new Vector2(350f, 58f));
                SetRect("location-rooftop", new Vector2(190f, -80f), new Vector2(350f, 58f));
                SetRect("location-forest-rave", new Vector2(-190f, -150f), new Vector2(350f, 58f));
                SetRect("location-space-club", new Vector2(190f, -150f), new Vector2(350f, 58f));
                SetRect("selected-location-label", new Vector2(0f, -230f), new Vector2(700f, 38f));
                SetRect("enter-club-button", new Vector2(0f, -310f), new Vector2(580f, 64f));
                SetRect("control-mode-mobile", new Vector2(-180f, -70f), new Vector2(340f, 150f));
                SetRect("control-mode-desktop", new Vector2(180f, -70f), new Vector2(340f, 150f));
                SetRect("control-confirm-button", new Vector2(0f, -205f), new Vector2(620f, 66f));
                SetRect("controls-help", new Vector2(0f, -830f), new Vector2(700f, 48f));
                SetRect("action-wheel-button", new Vector2(0f, 700f), new Vector2(320f, 70f));
                SetRect("action-wheel", new Vector2(0f, 330f), new Vector2(420f, 420f));
                SetRect("mobile-controls", new Vector2(0f, -675f), new Vector2(760f, 210f));
                SetRect("move-stick", new Vector2(-285f, 0f), new Vector2(210f, 210f));
                SetRect("look-stick", new Vector2(285f, 0f), new Vector2(210f, 210f));
                SetRect("club-title-label", new Vector2(0f, 805f), new Vector2(760f, 60f));
                SetRect("current-zone-label", new Vector2(0f, 750f), new Vector2(600f, 48f));
                SetRect("map-button", new Vector2(-300f, -379f), new Vector2(90f, 48f));
                SetRect("settings-button", new Vector2(-185f, -379f), new Vector2(115f, 48f));
                SetRect("back-to-wardrobe-button", new Vector2(-24f, -379f), new Vector2(120f, 48f));
                SetRect("settings-card", Vector2.zero, new Vector2(740f, 690f));
                SetRect("settings-title", new Vector2(0f, 280f), new Vector2(640f, 90f));
                SetRect("track-option-0", new Vector2(-180f, 80f), new Vector2(240f, 50f));
                SetRect("track-option-1", new Vector2(0f, 80f), new Vector2(220f, 50f));
                SetRect("track-option-2", new Vector2(180f, 80f), new Vector2(240f, 50f));
                SetRect("settings-control-mode-mobile", new Vector2(-170f, -65f), new Vector2(320f, 55f));
                SetRect("settings-control-mode-desktop", new Vector2(170f, -65f), new Vector2(320f, 55f));
                SetRect("quality-option-low", new Vector2(-145f, -135f), new Vector2(300f, 52f));
                SetRect("quality-option-high", new Vector2(145f, -135f), new Vector2(300f, 52f));
                SetRect("settings-close-button", new Vector2(0f, -278f), new Vector2(520f, 58f));
            }
            else
            {
                SetRect("wardrobe-heading", new Vector2(0f, 385f), new Vector2(1060f, 100f));
                SetRect("avatar-preview", new Vector2(-360f, -35f), new Vector2(610f, 570f));
                SetRect("avatar-caption", new Vector2(0f, -250f), new Vector2(420f, 36f));
                SetRect("preview-face-button", new Vector2(-160f, 245f), new Vector2(120f, 42f));
                SetRect("preview-clothes-button", new Vector2(0f, 245f), new Vector2(140f, 42f));
                SetRect("preview-full-button", new Vector2(160f, 245f), new Vector2(120f, 42f));
                SetRect("preview-zoom-in-button", new Vector2(250f, 195f), new Vector2(48f, 42f));
                SetRect("preview-zoom-out-button", new Vector2(250f, 145f), new Vector2(48f, 42f));
                SetRect("outfit-label", new Vector2(435f, 260f), new Vector2(400f, 40f));
                SetRect("outfit-info", new Vector2(435f, -155f), new Vector2(470f, 32f));
                SetRect("wardrobe-continue-button", new Vector2(435f, -225f), new Vector2(480f, 62f));
                SetRect("back-to-locations-button", new Vector2(435f, 340f), new Vector2(280f, 42f));
                for (var i = 0; i < OutfitNames.Length; i++)
                {
                    var column = i % 2;
                    var row = i / 2;
                    SetRect("outfit-option-" + i, new Vector2(315f + column * 245f, 195f - row * 82f), new Vector2(230f, 68f));
                }
                SetRect("location-card", new Vector2(0f, 32f), new Vector2(790f, 160f));
                SetRect("location-neon-room", new Vector2(-300f, -100f), new Vector2(190f, 52f));
                SetRect("location-rooftop", new Vector2(-100f, -100f), new Vector2(190f, 52f));
                SetRect("location-forest-rave", new Vector2(100f, -100f), new Vector2(190f, 52f));
                SetRect("location-space-club", new Vector2(300f, -100f), new Vector2(190f, 52f));
                SetRect("selected-location-label", new Vector2(0f, -163f), new Vector2(700f, 36f));
                SetRect("enter-club-button", new Vector2(0f, -250f), new Vector2(400f, 60f));
                SetRect("control-mode-mobile", new Vector2(-180f, -70f), new Vector2(340f, 150f));
                SetRect("control-mode-desktop", new Vector2(180f, -70f), new Vector2(340f, 150f));
                SetRect("control-confirm-button", new Vector2(0f, -205f), new Vector2(420f, 62f));
                SetRect("controls-help", new Vector2(0f, -405f), new Vector2(700f, 48f));
                SetRect("action-wheel-button", new Vector2(0f, -345f), new Vector2(260f, 62f));
                SetRect("action-wheel", new Vector2(0f, -125f), new Vector2(440f, 440f));
                SetRect("mobile-controls", new Vector2(0f, -300f), new Vector2(760f, 180f));
                SetRect("move-stick", new Vector2(-285f, 0f), new Vector2(190f, 190f));
                SetRect("look-stick", new Vector2(285f, 0f), new Vector2(190f, 190f));
                SetRect("club-title-label", new Vector2(-490f, 405f), new Vector2(500f, 60f));
                SetRect("current-zone-label", new Vector2(0f, 405f), new Vector2(360f, 54f));
                SetRect("map-button", new Vector2(-333f, -379f), new Vector2(105f, 52f));
                SetRect("settings-button", new Vector2(-181f, -379f), new Vector2(140f, 52f));
                SetRect("back-to-wardrobe-button", new Vector2(-24f, -379f), new Vector2(145f, 52f));
                SetRect("settings-card", Vector2.zero, new Vector2(740f, 690f));
                SetRect("settings-title", new Vector2(0f, 280f), new Vector2(640f, 90f));
                SetRect("track-option-0", new Vector2(-190f, 80f), new Vector2(230f, 52f));
                SetRect("track-option-1", new Vector2(0f, 80f), new Vector2(210f, 52f));
                SetRect("track-option-2", new Vector2(195f, 80f), new Vector2(230f, 52f));
                SetRect("settings-control-mode-mobile", new Vector2(-160f, -65f), new Vector2(240f, 48f));
                SetRect("settings-control-mode-desktop", new Vector2(160f, -65f), new Vector2(240f, 48f));
                SetRect("quality-option-low", new Vector2(-145f, -135f), new Vector2(220f, 48f));
                SetRect("quality-option-high", new Vector2(145f, -135f), new Vector2(220f, 48f));
                SetRect("settings-close-button", new Vector2(0f, -278f), new Vector2(360f, 52f));
            }
        }

        private static void SetRect(string name, Vector2 position, Vector2 size)
        {
            var gameObject = FindAny(name);
            if (gameObject == null) return;
            var rect = gameObject.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void ShowScreen(string screenName)
        {
            StopReply();
            foreach (Transform child in uiRoot)
            {
                var isMobileControls = child.name == "mobile-controls" && controlMode == "mobile" && (screenName == "club-hud" || screenName == "settings-screen");
                child.gameObject.SetActive(child.name == screenName || isMobileControls);
            }
            inClub = screenName == "club-hud";
            if (firstPersonHands != null) firstPersonHands.gameObject.SetActive(inClub);
            if (!inClub) Cursor.lockState = CursorLockMode.None;
            ReportWebState("screen", screenName);
            PublishVisibleUiText();
        }

        private void SelectOutfit(int index)
        {
            selectedOutfit = index;
            PlayerPrefs.SetInt(OutfitKey, index);
            PlayerPrefs.Save();
            ApplyFirstPersonOutfit();
            if (previewAvatar != null) Destroy(previewAvatar.gameObject);
            previewAvatar = null;
            EnsurePreviewAvatar();
            ReportWebState("outfit", selectedOutfit.ToString());
            PublishVisibleUiText();
        }

        private void EnsurePreviewAvatar()
        {
            if (previewAvatar != null) return;
            previewAvatar = CreateAvatar("avatar-model", selectedOutfit, PreviewAvatarLayer);
            previewAvatar.position = Vector3.zero;
            previewYaw = 0f;
            previewAvatar.gameObject.layer = PreviewAvatarLayer;
            foreach (var child in previewAvatar.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewAvatarLayer;
            if (previewCamera != null) previewCamera.transform.LookAt(previewAvatar.position + Vector3.up * 1.05f);
        }

        private void CreatePreviewCamera(RawImage image)
        {
            var cameraObject = new GameObject("avatar-preview-camera");
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.035f, .02f, .1f, 1f);
            previewCamera.cullingMask = 1 << PreviewAvatarLayer;
            previewCamera.fieldOfView = 28f;
            previewCamera.transform.position = new Vector3(0f, 1.08f, 5.5f);
            previewCamera.transform.LookAt(new Vector3(0f, 1.05f, 0f));
            previewTexture = new RenderTexture(700, 800, 16, RenderTextureFormat.ARGB32);
            previewCamera.targetTexture = previewTexture;
            image.texture = previewTexture;
            var key = new GameObject("avatar-preview-light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.2f;
            key.color = new Color(.62f, .74f, 1f);
            key.transform.rotation = Quaternion.Euler(35f, -22f, 0f);
            key.gameObject.layer = PreviewAvatarLayer;
            EnsurePreviewAvatar();
        }

        private void SetPreviewView(string view)
        {
            if (previewCamera == null) return;
            if (view == "face") previewCamera.transform.localPosition = new Vector3(0f, 1.85f, 2.25f);
            else if (view == "clothes") previewCamera.transform.localPosition = new Vector3(0f, 1.1f, 2.55f);
            else previewCamera.transform.localPosition = new Vector3(0f, 1.08f, 5.5f);
            previewCamera.transform.LookAt(view == "face" ? new Vector3(0f, 1.72f, 0f) : new Vector3(0f, 1.05f, 0f));
        }

        private void ZoomPreview(float amount)
        {
            if (previewCamera == null) return;
            var direction = (previewCamera.transform.position - Vector3.zero).normalized;
            var next = previewCamera.transform.position + direction * amount;
            if (next.magnitude < .9f) next = direction * .9f;
            previewCamera.transform.position = next;
        }

        private void EnterClub()
        {
            if (previewAvatar != null) { Destroy(previewAvatar.gameObject); previewAvatar = null; }
            if (previewCamera != null) previewCamera.gameObject.SetActive(false);
            if (player == null) CreateEnvironment();
            player.position = new Vector3(0f, 0f, 10f);
            yaw = 180f;
            pitch = 0f;
            ReportWebState("player-position", string.Format(CultureInfo.InvariantCulture, "{0:0.00},{1:0.00},{2:0.00}", player.position.x, player.position.y, player.position.z));
            ReportWebState("player-yaw", yaw.ToString("0.00", CultureInfo.InvariantCulture));
            inClub = true;
            if (!musicInitialized)
            {
                musicInitialized = true;
                musicSource.Play();
            }
            UpdateMusicButtonLabel();
            if (firstPersonHands != null) firstPersonHands.gameObject.SetActive(true);
            ApplyControls();
            ShowScreen("club-hud");
            ApplyControls();
            SetStatus("WELCOME, " + PlayerPrefs.GetString(NicknameKey, "GUEST").ToUpperInvariant() + "  ·  YOUR NIGHT");
        }

        private void LeaveClub()
        {
            StopReply();
            inClub = false;
            drinking = false;
            dancing = false;
            if (drinkStatus != null) drinkStatus.SetActive(false);
            if (danceStatus != null) danceStatus.SetActive(false);
            if (actionWheel != null) actionWheel.SetActive(false);
            if (previewCamera != null) previewCamera.gameObject.SetActive(true);
            ShowWardrobe();
        }

        private void OpenSettings()
        {
            StopReply();
            actionWheel.SetActive(false);
            settingsScreen.SetActive(true);
            clubHud.SetActive(false);
            settingsScreen.transform.Find("volume-slider").GetComponent<Slider>().SetValueWithoutNotify(selectedVolume);
        }

        private void CloseSettings()
        {
            settingsScreen.SetActive(false);
            clubHud.SetActive(true);
            ApplyControls();
        }

        private void OpenMap()
        {
            StopReply();
            actionWheel.SetActive(false);
            clubHud.SetActive(false);
            clubMap.SetActive(true);
        }

        private void CloseMap()
        {
            clubMap.SetActive(false);
            clubHud.SetActive(true);
        }

        private void GoToZone(int zone)
        {
            var positions = new[] { new Vector3(0f, 0f, 4f), new Vector3(-6f, 0f, 1f), new Vector3(6.2f, 0f, 5f), new Vector3(0f, 0f, 10f) };
            player.position = positions[zone];
            yaw = zone == 1 ? 90f : zone == 2 ? -90f : 180f;
            UpdateZoneLabel();
            CloseMap();
        }

        private void ToggleActionWheel()
        {
            if (!inClub) return;
            actionWheel.SetActive(!actionWheel.activeSelf);
            PublishVisibleUiText();
        }

        private void ToggleDance()
        {
            dancing = !dancing;
            drinking = false;
            danceStatus.SetActive(dancing);
            drinkStatus.SetActive(false);
            actionWheel.SetActive(false);
        }

        private void ToggleDrink()
        {
            if (!drinking && player.position.x > -5.25f)
            {
                SetStatus("WALK TO THE BAR FIRST");
                actionWheel.SetActive(false);
                return;
            }
            drinking = !drinking;
            dancing = false;
            danceStatus.SetActive(false);
            drinkStatus.SetActive(drinking);
            actionWheel.SetActive(false);
        }

        private void UseNpcAction(string action)
        {
            actionWheel.SetActive(false);
            if (dancing || drinking) return;
            var nearestNpc = FindNearestNpc();
            if (npcCrowd == null || !npcCrowd.activeInHierarchy || nearestNpc == null) return;
            if (Vector3.Distance(player.position, nearestNpc.position) > 3.5f)
            {
                SetStatus("MOVE CLOSER TO A CLUB GUEST");
                return;
            }
            if (voiceSource.isPlaying) return;
            if (voiceSource.clip == null) { ApplyMusicVolume(); return; }
            voiceSource.volume = selectedVolume;
            voiceSource.Play();
            ApplyMusicVolume(true);
            npcSpeakingLabel.text = "LUNA  ·  I RECOGNIZE THIS MUSIC";
            npcSpeakingStatus.SetActive(true);
            ReportWebState("reply", npcSpeakingLabel.text);
            ReportWebState("voice-clip", voiceSource.clip.name);
            ReportWebState("reply-playing", voiceSource.isPlaying ? "true" : "false");
            ReportWebState("voice-volume", voiceSource.volume.ToString("0.00", CultureInfo.InvariantCulture));
            ReportWebState("voice-load-state", voiceSource.clip.loadState.ToString());
            PublishVisibleUiText();
            replyRoutine = StartCoroutine(WaitForReply());
        }

        private IEnumerator WaitForReply()
        {
            while (voiceSource != null && voiceSource.isPlaying) yield return null;
            StopReply();
        }

        private void StopReply()
        {
            if (replyRoutine != null) { StopCoroutine(replyRoutine); replyRoutine = null; }
            if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();
            if (npcSpeakingStatus != null) npcSpeakingStatus.SetActive(false);
            ReportWebState("reply-playing", "false");
            ApplyMusicVolume();
        }

        private void ToggleMusic()
        {
            musicInitialized = true;
            if (musicSource.isPlaying) musicSource.Pause();
            else musicSource.Play();
            ApplyMusicVolume();
            UpdateMusicButtonLabel();
        }

        private void UpdateMusicButtonLabel()
        {
            var button = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(candidate => candidate.name == "music-toggle-button");
            if (button != null) button.GetComponentInChildren<Text>().text = musicSource.isPlaying ? "STOP MUSIC" : "START MUSIC";
        }

        private void SelectTrack(int index)
        {
            selectedTrack = index;
            PlayerPrefs.SetInt(TrackKey, selectedTrack);
            PlayerPrefs.Save();
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
                musicSource.clip = musicTracks[selectedTrack];
                musicSource.Play();
            }
            else musicSource.clip = musicTracks[selectedTrack];
        }

        private void SetControlMode(string mode)
        {
            if (mode != "mobile" && mode != "desktop") return;
            controlMode = mode;
            PlayerPrefs.SetString(ControlsKey, mode);
            PlayerPrefs.Save();
            ReportWebState("control-mode", mode);
            SetControlSelectionAppearance("control-selection-screen");
            SetControlSelectionAppearance("settings-screen");
            var help = FindAny("controls-help");
            if (help != null)
            {
                var label = help.GetComponent<Text>();
                if (label != null) label.text = mode == "mobile" ? "LEFT STICK MOVE   ·   RIGHT STICK LOOK   ·   ACTIONS" : "WASD MOVE   ·   MOUSE LOOK   ·   E ACTIONS";
            }
            ApplyControls();
        }

        private void SetControlSelectionAppearance(string screenName)
        {
            var screen = FindAny(screenName);
            if (screen == null) return;
            var phone = screen.transform.Find(screenName == "settings-screen" ? "settings-control-mode-mobile" : "control-mode-mobile");
            var desktop = screen.transform.Find(screenName == "settings-screen" ? "settings-control-mode-desktop" : "control-mode-desktop");
            SetSelectedControlColor(phone, controlMode == "mobile", AccentCyan);
            SetSelectedControlColor(desktop, controlMode == "desktop", AccentViolet);
        }

        private static void SetSelectedControlColor(Transform button, bool selected, Color accent)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = selected ? new Color(accent.r, accent.g, accent.b, .55f) : new Color(.09f, .07f, .16f, .96f);
        }

        private void ApplyControls()
        {
            var sticks = FindAny("mobile-controls");
            var visible = (inClub || (settingsScreen != null && settingsScreen.activeSelf)) && controlMode == "mobile";
            if (sticks != null) sticks.SetActive(visible);
            ReportWebState("mobile-controls-visible", visible ? "true" : "false");
        }

        private static GameObject FindAny(string objectName)
        {
            return FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(transform => transform.gameObject)
                .FirstOrDefault(gameObject => gameObject.name == objectName);
        }

        private void SetQuality(string quality)
        {
            PlayerPrefs.SetString(QualityKey, quality);
            PlayerPrefs.Save();
            QualitySettings.SetQualityLevel(quality == "low" ? 0 : QualitySettings.names.Length - 1, true);
        }

        private void ToggleReducedMotion()
        {
            reducedMotion = reducedMotion < .5f ? 1f : 0f;
            PlayerPrefs.SetInt(MotionKey, reducedMotion > .5f ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SelectLocation(string location)
        {
            var open = location == "neon-room";
            var label = GameObject.Find("selected-location-label");
            if (label != null) label.GetComponent<Text>().text = open ? "NEON ROOM  ·  OPEN" : location.ToUpperInvariant() + "  ·  COMING SOON";
            ReportWebState("location", open ? "neon-room" : location);
            PublishVisibleUiText();
            var enter = GameObject.Find("enter-club-button");
            if (enter != null) enter.GetComponent<Button>().interactable = open;
        }

        private void MovePlayer(float forward, float side, float deltaTime)
        {
            if ((dancing && (Mathf.Abs(forward) > .01f || Mathf.Abs(side) > .01f)))
            {
                dancing = false;
                danceStatus.SetActive(false);
            }
            var scale = deltaTime * 3f / Mathf.Max(1f, new Vector2(forward, side).magnitude);
            var rad = yaw * Mathf.Deg2Rad;
            var dx = Mathf.Sin(rad) * forward + Mathf.Cos(rad) * side;
            var dz = Mathf.Cos(rad) * forward - Mathf.Sin(rad) * side;
            var next = player.position + new Vector3(dx, 0f, dz) * scale;
            next.x = Mathf.Clamp(next.x, -12f, 12f);
            next.z = Mathf.Clamp(next.z, -13f, 13f);
            if (!(next.x < -7.9f && next.x > -12f && next.z > -3.5f && next.z < 5.5f)) player.position = next;
        }

        private void UpdateZoneLabel()
        {
            if (currentZoneLabel == null || player == null) return;
            var p = player.position;
            var zone = p.z > 7f ? "ENTRANCE GALLERY" : p.x < -5.5f ? "BAR / RECHARGE" : p.x > 5.5f ? "LOUNGE / SLOW DOWN" : p.z < -6f ? "DJ STAGE" : "DANCE FLOOR";
            currentZoneLabel.text = zone;
            if (zone != lastReportedZoneLabel)
            {
                lastReportedZoneLabel = zone;
                PublishVisibleUiText();
            }
        }

        private void UpdateNpcProximity()
        {
            if (!inClub || player == null) return;
            var nearestNpc = FindNearestNpc();
            var nearNpc = nearestNpc != null && Vector3.Distance(player.position, nearestNpc.position) <= 3.5f;
            if (nearNpc == wasNearNpc) return;
            wasNearNpc = nearNpc;
            ReportWebState("near-npc", nearNpc ? "true" : "false");
        }

        private Transform FindNearestNpc()
        {
            if (player == null || crowdAvatars == null) return null;
            Transform nearestNpc = null;
            var nearestDistance = float.PositiveInfinity;
            foreach (var npc in crowdAvatars)
            {
                if (npc == null || !npc.gameObject.activeInHierarchy) continue;
                var distance = Vector3.Distance(player.position, npc.position);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestNpc = npc;
            }
            return nearestNpc;
        }

        private void UpdateCrowd()
        {
            if (!inClub || reducedMotion > .5f || crowdAvatars == null) return;
            for (var i = 0; i < crowdAvatars.Length; i++)
            {
                var beat = Time.time * 1.8f + i * .73f;
                crowdAvatars[i].localPosition = crowdRestPositions[i] + Vector3.up * (Mathf.Sin(beat) * .045f);
                crowdAvatars[i].localRotation = crowdRestRotations[i] * Quaternion.Euler(0f, Mathf.Sin(beat * .5f) * 4f, Mathf.Sin(beat) * 3f);
            }
        }

        private static void PublishVisibleUiText()
        {
            var visibleText = FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.isActiveAndEnabled && text.gameObject.activeInHierarchy)
                .Select(text => text.text)
                .Where(text => !string.IsNullOrWhiteSpace(text));
            ReportWebState("ui-copy", string.Join("\n", visibleText.ToArray()));
        }

        private static void ReportWebState(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NeonRoomReportState(key, value ?? string.Empty);
#endif
        }

        private void ApplyMusicVolume(bool ducked = false)
        {
            if (musicSource != null) musicSource.volume = selectedVolume * (ducked ? .35f : 1f);
        }

        private void SetStatus(string message)
        {
            var target = GameObject.Find("current-zone-label");
            if (target != null && currentZoneLabel != null && inClub) currentZoneLabel.text = message;
            PublishVisibleUiText();
        }

        private static AudioClip CreateMusic(string name, int bpm)
        {
            const int sampleRate = 22050;
            const int bars = 4;
            var duration = bars * 4f * 60f / bpm;
            var count = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[count];
            var beatLength = 60f / bpm;
            for (var i = 0; i < count; i++)
            {
                var time = (float)i / sampleRate;
                var beat = time % beatLength;
                var barBeat = Mathf.FloorToInt(time / beatLength) % 4;
                var kickEnv = Mathf.Exp(-beat * 18f);
                var kick = barBeat == 0 ? Mathf.Sin(2f * Mathf.PI * (52f + 80f * Mathf.Exp(-beat * 20f)) * time) * kickEnv * .35f : 0f;
                var hat = Mathf.Sin(2f * Mathf.PI * 7300f * time) * Mathf.Exp(-beat * 55f) * (barBeat % 2 == 1 ? .06f : .025f);
                var bass = Mathf.Sin(2f * Mathf.PI * 55f * time) * (.11f + .05f * Mathf.Sin(time * 1.7f));
                var synth = Mathf.Sin(2f * Mathf.PI * 220f * time) * .04f + Mathf.Sin(2f * Mathf.PI * 330f * time) * .035f;
                samples[i] = Mathf.Clamp(kick + hat + bass + synth, -.85f, .85f);
            }
            var clip = AudioClip.Create(name + " · " + bpm + " BPM", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static Transform CreateAvatar(string name, int outfit, int layer)
        {
            var appearance = OutfitAppearanceIndices[Mathf.Clamp(outfit, 0, OutfitAppearanceIndices.Length - 1)];
            if (appearance == 8)
            {
                var glassHeart = Resources.Load<GameObject>("avatars/glass-heart-ai");
                if (glassHeart != null)
                {
                    var avatar = new GameObject(name).transform;
                    var model = Instantiate(glassHeart, avatar).transform;
                    var renderers = model.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        var scale = 2.08f / Mathf.Max(bounds.size.y, .001f);
                        model.localScale *= scale;
                        model.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
                    }
                    foreach (var part in avatar.GetComponentsInChildren<Transform>(true))
                        part.gameObject.layer = layer;
                    return avatar;
                }
                Debug.LogError("Glass Heart model missing from Resources/avatars.");
            }
            if (appearance == 3)
            {
                var velvetStatic = Resources.Load<GameObject>("avatars/velvet-static");
                if (velvetStatic != null)
                {
                    var avatar = Instantiate(velvetStatic);
                    avatar.name = name;
                    foreach (var part in avatar.GetComponentsInChildren<Transform>(true))
                        part.gameObject.layer = layer;
                    return avatar.transform;
                }
                Debug.LogError("Velvet Static prefab missing from Resources/avatars.");
            }
            if (appearance == 2)
            {
                var acidBloom = Instantiate(Resources.Load<GameObject>("avatars/acid-bloom"));
                acidBloom.name = name;
                foreach (var part in acidBloom.GetComponentsInChildren<Transform>(true))
                    part.gameObject.layer = layer;
                return acidBloom.transform;
            }
            var root = new GameObject(name).transform;
            root.gameObject.layer = layer;
            // Keep the original ten outfit choices while giving them visibly distinct
            // silhouettes matching the fashion styles used by the browser prototype.
            var style = new[] { 0, 1, 2, 3, 4, 5, 0, 2, 3, 1 }[appearance];
            var fitted = style == 0 || style == 4;
            var wide = style == 1 || style == 3 || style == 5;
            var skin = CreateMaterial(new Color(.72f, .55f, .6f), .05f, .65f);
            var suit = CreateMaterial(OutfitColors[appearance], .65f, .25f, OutfitColors[appearance] * .45f);
            var dark = CreateMaterial(new Color(.035f, .025f, .06f), .25f, .38f);
            var metal = CreateMaterial(new Color(.58f, .72f, .82f), .82f, .78f, OutfitColors[appearance] * .4f);
            var torsoWidth = style == 5 ? .4f : .36f;
            MakeProfiledPart(root, "body", new Vector3(0f, 1.23f, 0f), new[]
            {
                new Vector3(-.36f, torsoWidth * .53f, .17f), new Vector3(-.30f, torsoWidth * .59f, .205f),
                new Vector3(-.16f, torsoWidth * .67f, .22f), new Vector3(.04f, torsoWidth * .88f, .245f),
                new Vector3(.22f, torsoWidth, .235f), new Vector3(.34f, torsoWidth * .78f, .19f),
                new Vector3(.36f, torsoWidth * .54f, .16f)
            }, suit, layer);
            MakeAvatarPart(root, "neck", PrimitiveType.Cylinder, new Vector3(0f, 1.62f, 0f), new Vector3(.095f, .12f, .095f), skin, layer);
            MakeAvatarPart(root, "head", PrimitiveType.Sphere, new Vector3(0f, 1.88f, 0f), new Vector3(.25f, .32f, .22f), skin, layer);
            MakeAvatarPart(root, "hair", PrimitiveType.Sphere, new Vector3(0f, 1.995f, -.025f), new Vector3(.265f, style == 2 || style == 5 ? .18f : .13f, .245f), style == 2 ? metal : dark, layer);
            if (style == 0 || style == 3 || style == 4)
            {
                MakeAvatarPart(root, "hair-left-lock", PrimitiveType.Capsule, new Vector3(-.19f, 1.78f, -.005f), new Vector3(.055f, .25f, .055f), dark, layer).localRotation = Quaternion.Euler(0f, 0f, -6f);
                MakeAvatarPart(root, "hair-right-lock", PrimitiveType.Capsule, new Vector3(.19f, 1.78f, -.005f), new Vector3(.055f, .25f, .055f), dark, layer).localRotation = Quaternion.Euler(0f, 0f, 6f);
            }
            MakeAvatarPart(root, "avatar-left-eye", PrimitiveType.Sphere, new Vector3(-.068f, 1.89f, .225f), new Vector3(.045f, .035f, .025f), metal, layer);
            MakeAvatarPart(root, "avatar-right-eye", PrimitiveType.Sphere, new Vector3(.068f, 1.89f, .225f), new Vector3(.045f, .035f, .025f), metal, layer);
            MakeAvatarPart(root, "avatar-nose", PrimitiveType.Sphere, new Vector3(0f, 1.845f, .237f), new Vector3(.027f, .04f, .025f), skin, layer);
            MakeAvatarPart(root, "left-arm", PrimitiveType.Capsule, new Vector3(-.31f, 1.12f, 0f), new Vector3(.105f, .37f, .105f), skin, layer).localRotation = Quaternion.Euler(0f, 0f, 8f);
            MakeAvatarPart(root, "right-arm", PrimitiveType.Capsule, new Vector3(.31f, 1.12f, 0f), new Vector3(.105f, .37f, .105f), skin, layer).localRotation = Quaternion.Euler(0f, 0f, -8f);
            MakeAvatarPart(root, "left-wrist-cuff", PrimitiveType.Cylinder, new Vector3(-.32f, .78f, .012f), new Vector3(.11f, .018f, .11f), metal, layer).localRotation = Quaternion.Euler(90f, 0f, 0f);
            MakeAvatarPart(root, "right-wrist-cuff", PrimitiveType.Cylinder, new Vector3(.32f, .78f, .012f), new Vector3(.11f, .018f, .11f), metal, layer).localRotation = Quaternion.Euler(90f, 0f, 0f);
            var legWidth = wide ? .31f : .21f;
            var legRadius = wide ? .095f : .065f;
            MakeProfiledPart(root, "left-leg", new Vector3(-.105f, .46f, 0f), new[]
            {
                new Vector3(-.4f, legRadius * .76f, .105f), new Vector3(-.34f, legRadius * .88f, .12f),
                new Vector3(-.14f, legRadius, .13f), new Vector3(.12f, legRadius * .91f, .125f),
                new Vector3(.34f, legRadius * .75f, .105f), new Vector3(.4f, legRadius * .82f, .12f)
            }, wide ? suit : dark, layer);
            MakeProfiledPart(root, "right-leg", new Vector3(.105f, .46f, 0f), new[]
            {
                new Vector3(-.4f, legRadius * .76f, .105f), new Vector3(-.34f, legRadius * .88f, .12f),
                new Vector3(-.14f, legRadius, .13f), new Vector3(.12f, legRadius * .91f, .125f),
                new Vector3(.34f, legRadius * .75f, .105f), new Vector3(.4f, legRadius * .82f, .12f)
            }, wide ? suit : dark, layer);
            MakeAvatarPart(root, "left-boot", PrimitiveType.Cube, new Vector3(-.105f, .075f, .075f), new Vector3(.18f, .12f, .30f), dark, layer);
            MakeAvatarPart(root, "right-boot", PrimitiveType.Cube, new Vector3(.105f, .075f, .075f), new Vector3(.18f, .12f, .30f), dark, layer);
            MakeAvatarPart(root, "belt", PrimitiveType.Cube, new Vector3(0f, .91f, .04f), new Vector3(.42f, .065f, .28f), metal, layer);
            MakeAvatarPart(root, "visor", PrimitiveType.Cube, new Vector3(0f, 1.925f, .258f), new Vector3(.29f, .035f, .025f), suit, layer);
            if (fitted)
            {
                MakeTaperedPart(root, "fashion-wrap-skirt", .31f, .19f, .78f, new Vector3(0f, .56f, 0f), suit, layer);
                MakeAvatarPart(root, "skirt-wrap-panel", PrimitiveType.Cube, new Vector3(.045f, .55f, .22f), new Vector3(.22f, .62f, .028f), dark, layer).localRotation = Quaternion.Euler(0f, 0f, -8f);
            }
            if (wide)
            {
                var trousers = new GameObject("fashion-wide-trousers").transform;
                trousers.SetParent(root, false);
                trousers.localPosition = Vector3.zero;
                trousers.gameObject.layer = layer;
                MakeAvatarPart(trousers, "left-wide-trouser", PrimitiveType.Cylinder, new Vector3(-.12f, .44f, 0f), new Vector3(.17f, .42f, .16f), suit, layer);
                MakeAvatarPart(trousers, "right-wide-trouser", PrimitiveType.Cylinder, new Vector3(.12f, .44f, 0f), new Vector3(.17f, .42f, .16f), suit, layer);
                if (style == 3)
                {
                    MakeAvatarPart(trousers, "cargo-pocket-left", PrimitiveType.Cube, new Vector3(-.285f, .47f, .13f), new Vector3(.075f, .17f, .035f), metal, layer);
                    MakeAvatarPart(trousers, "cargo-pocket-right", PrimitiveType.Cube, new Vector3(.285f, .47f, .13f), new Vector3(.075f, .17f, .035f), metal, layer);
                }
            }
            if (style == 2 || style == 5)
            {
                var coat = new GameObject("long-coat").transform;
                coat.SetParent(root, false);
                coat.gameObject.layer = layer;
                MakeAvatarPart(coat, "coat-back-panel", PrimitiveType.Cube, new Vector3(0f, 1.04f, -.27f), new Vector3(.43f, .84f, .075f), style == 2 ? metal : suit, layer);
                MakeAvatarPart(coat, "coat-left-lapel", PrimitiveType.Cube, new Vector3(-.135f, 1.28f, .27f), new Vector3(.12f, .42f, .045f), suit, layer).localRotation = Quaternion.Euler(0f, 0f, -13f);
                MakeAvatarPart(coat, "coat-right-lapel", PrimitiveType.Cube, new Vector3(.135f, 1.28f, .27f), new Vector3(.12f, .42f, .045f), suit, layer).localRotation = Quaternion.Euler(0f, 0f, 13f);
                MakeAvatarPart(coat, "coat-neon-trim", PrimitiveType.Cube, new Vector3(0f, 1.02f, -.31f), new Vector3(.018f, .77f, .012f), suit, layer);
            }
            if (style == 0)
            {
                var leatherTop = new GameObject("fashion-leather-top").transform;
                leatherTop.SetParent(root, false);
                leatherTop.gameObject.layer = layer;
                MakeAvatarPart(leatherTop, "leather-bodice", PrimitiveType.Sphere, new Vector3(0f, 1.27f, 0f), new Vector3(.41f, .42f, .29f), dark, layer);
                MakeAvatarPart(leatherTop, "leather-left-lapel", PrimitiveType.Cube, new Vector3(-.115f, 1.40f, .305f), new Vector3(.10f, .29f, .035f), metal, layer).localRotation = Quaternion.Euler(0f, 0f, -17f);
                MakeAvatarPart(leatherTop, "leather-right-lapel", PrimitiveType.Cube, new Vector3(.115f, 1.40f, .305f), new Vector3(.10f, .29f, .035f), metal, layer).localRotation = Quaternion.Euler(0f, 0f, 17f);
                MakeAvatarPart(leatherTop, "leather-center-zip", PrimitiveType.Cube, new Vector3(0f, 1.22f, .32f), new Vector3(.014f, .31f, .014f), suit, layer);
            }
            if (style == 1)
            {
                var jacket = new GameObject("fashion-jacket").transform;
                jacket.SetParent(root, false);
                jacket.gameObject.layer = layer;
                MakeAvatarPart(jacket, "jacket-shell", PrimitiveType.Sphere, new Vector3(0f, 1.27f, 0f), new Vector3(.45f, .47f, .31f), suit, layer);
                MakeAvatarPart(jacket, "dark-shirt", PrimitiveType.Cube, new Vector3(0f, 1.23f, .325f), new Vector3(.18f, .35f, .035f), dark, layer);
                MakeAvatarPart(jacket, "jacket-left-lapel", PrimitiveType.Cube, new Vector3(-.125f, 1.40f, .345f), new Vector3(.10f, .30f, .035f), metal, layer).localRotation = Quaternion.Euler(0f, 0f, -15f);
                MakeAvatarPart(jacket, "jacket-right-lapel", PrimitiveType.Cube, new Vector3(.125f, 1.40f, .345f), new Vector3(.10f, .30f, .035f), metal, layer).localRotation = Quaternion.Euler(0f, 0f, 15f);
                MakeAvatarPart(jacket, "jacket-zip", PrimitiveType.Cube, new Vector3(0f, 1.20f, .36f), new Vector3(.012f, .31f, .012f), metal, layer);
            }
            if (style == 3)
            {
                var bomber = new GameObject("fashion-bomber").transform;
                bomber.SetParent(root, false);
                bomber.gameObject.layer = layer;
                MakeAvatarPart(bomber, "bomber-shell", PrimitiveType.Sphere, new Vector3(0f, 1.29f, 0f), new Vector3(.48f, .40f, .32f), suit, layer);
                MakeAvatarPart(bomber, "bomber-front-panel", PrimitiveType.Cube, new Vector3(0f, 1.27f, .345f), new Vector3(.24f, .27f, .018f), dark, layer);
                MakeAvatarPart(bomber, "bomber-zip", PrimitiveType.Cube, new Vector3(0f, 1.28f, .37f), new Vector3(.012f, .30f, .012f), metal, layer);
                MakeAvatarPart(bomber, "bomber-collar", PrimitiveType.Cylinder, new Vector3(0f, 1.51f, 0f), new Vector3(.19f, .035f, .16f), dark, layer);
            }
            MakeAvatarPart(root, "necklace", PrimitiveType.Cylinder, new Vector3(0f, 1.53f, .07f), new Vector3(.12f, .015f, .11f), metal, layer).localRotation = Quaternion.Euler(90f, 0f, 0f);
            return root;
        }

        private static Transform MakeTaperedPart(Transform parent, string name, float bottomRadius, float topRadius,
            float height, Vector3 position, Material material, int layer)
        {
            const int sides = 24;
            var vertices = new Vector3[(sides + 1) * 2];
            var triangles = new int[sides * 6];
            for (var i = 0; i <= sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var x = Mathf.Sin(angle);
                var z = Mathf.Cos(angle);
                vertices[i] = new Vector3(x * bottomRadius, -height * .5f, z * bottomRadius);
                vertices[sides + 1 + i] = new Vector3(x * topRadius, height * .5f, z * topRadius);
                if (i == sides) continue;
                var b = i;
                var t = sides + 1 + i;
                var nextB = i + 1;
                var nextT = sides + 2 + i;
                var offset = i * 6;
                triangles[offset] = b; triangles[offset + 1] = nextB; triangles[offset + 2] = t;
                triangles[offset + 3] = nextB; triangles[offset + 4] = nextT; triangles[offset + 5] = t;
            }
            var mesh = new Mesh { name = name + " mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            part.layer = layer;
            return part.transform;
        }

        private static Transform MakeProfiledPart(Transform parent, string name, Vector3 position, Vector3[] profile, Material material, int layer)
        {
            const int sides = 16;
            var vertices = new Vector3[profile.Length * sides + 2];
            var triangles = new int[(profile.Length - 1) * sides * 6 + sides * 6];
            for (var ring = 0; ring < profile.Length; ring++)
            {
                for (var side = 0; side < sides; side++)
                {
                    var angle = side * Mathf.PI * 2f / sides;
                    vertices[ring * sides + side] = new Vector3(Mathf.Cos(angle) * profile[ring].y, profile[ring].x, Mathf.Sin(angle) * profile[ring].z);
                }
            }
            var bottomCenter = profile.Length * sides;
            var topCenter = bottomCenter + 1;
            vertices[bottomCenter] = new Vector3(0f, profile[0].x, 0f);
            vertices[topCenter] = new Vector3(0f, profile[profile.Length - 1].x, 0f);
            var triangle = 0;
            for (var ring = 0; ring < profile.Length - 1; ring++)
            for (var side = 0; side < sides; side++)
            {
                var next = (side + 1) % sides;
                var lower = ring * sides + side;
                var lowerNext = ring * sides + next;
                var upper = (ring + 1) * sides + side;
                var upperNext = (ring + 1) * sides + next;
                triangles[triangle++] = lower; triangles[triangle++] = upper; triangles[triangle++] = lowerNext;
                triangles[triangle++] = lowerNext; triangles[triangle++] = upper; triangles[triangle++] = upperNext;
            }
            for (var side = 0; side < sides; side++)
            {
                var next = (side + 1) % sides;
                triangles[triangle++] = bottomCenter; triangles[triangle++] = next; triangles[triangle++] = side;
                var top = (profile.Length - 1) * sides;
                triangles[triangle++] = topCenter; triangles[triangle++] = top + side; triangles[triangle++] = top + next;
            }
            var mesh = new Mesh { name = name == "body" ? "neon-tailored-torso" : "neon-sculpted-" + name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            part.layer = layer;
            return part.transform;
        }

        private void CreateFirstPersonHands()
        {
            firstPersonHands = new GameObject("first-person-hands").transform;
            firstPersonHands.SetParent(playerCamera.transform, false);
            firstPersonOutfitMaterial = CreateMaterial(Color.white, .7f, .72f);
            var skin = CreateMaterial(new Color(.72f, .55f, .6f), .05f, .65f);
            var chrome = CreateMaterial(new Color(.38f, .78f, .94f), .82f, .78f, new Color(.12f, .48f, .8f));
            var leftSleeve = MakeAvatarPart(firstPersonHands, "first-person-left-sleeve", PrimitiveType.Capsule,
                new Vector3(-.34f, -.66f, .74f), new Vector3(.12f, .31f, .12f), firstPersonOutfitMaterial, 0);
            leftSleeve.localRotation = Quaternion.Euler(0f, 0f, -33f);
            var rightSleeve = MakeAvatarPart(firstPersonHands, "first-person-right-sleeve", PrimitiveType.Capsule,
                new Vector3(.34f, -.66f, .74f), new Vector3(.12f, .31f, .12f), firstPersonOutfitMaterial, 0);
            rightSleeve.localRotation = Quaternion.Euler(0f, 0f, 33f);
            MakeAvatarPart(firstPersonHands, "first-person-left-hand", PrimitiveType.Sphere,
                new Vector3(-.22f, -.49f, .98f), new Vector3(.105f, .075f, .075f), skin, 0);
            MakeAvatarPart(firstPersonHands, "first-person-right-hand", PrimitiveType.Sphere,
                new Vector3(.22f, -.49f, .98f), new Vector3(.105f, .075f, .075f), skin, 0);
            MakeAvatarPart(firstPersonHands, "first-person-left-cuff", PrimitiveType.Cylinder,
                new Vector3(-.29f, -.56f, .88f), new Vector3(.072f, .024f, .072f), chrome, 0).localRotation = Quaternion.Euler(0f, 0f, -33f);
            MakeAvatarPart(firstPersonHands, "first-person-right-cuff", PrimitiveType.Cylinder,
                new Vector3(.29f, -.56f, .88f), new Vector3(.072f, .024f, .072f), chrome, 0).localRotation = Quaternion.Euler(0f, 0f, 33f);
            firstPersonHands.gameObject.SetActive(inClub);
            ApplyFirstPersonOutfit();
        }

        private void ApplyFirstPersonOutfit()
        {
            if (firstPersonOutfitMaterial == null) return;
            var color = selectedOutfit == 2 ? new Color(.025f, .018f, .033f)
                : OutfitColors[OutfitAppearanceIndices[Mathf.Clamp(selectedOutfit, 0, OutfitAppearanceIndices.Length - 1)]];
            firstPersonOutfitMaterial.color = color;
            if (firstPersonOutfitMaterial.HasProperty("_EmissionColor"))
                firstPersonOutfitMaterial.SetColor("_EmissionColor", color * .2f);
        }

        private static void AddRadialActionButton(Transform parent, string name, string label, Vector2 position,
            UnityEngine.Events.UnityAction action, Color accent)
        {
            var button = AddButton(parent, name, label, position, new Vector2(96f, 96f), action, accent);
            var image = button.GetComponent<Image>();
            image.sprite = GetActionCircleSprite();
            image.alphaHitTestMinimumThreshold = .08f;
            image.color = new Color(accent.r, accent.g, accent.b, .82f);
            var colors = button.colors;
            colors.normalColor = new Color(accent.r, accent.g, accent.b, .82f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = accent * .75f;
            button.colors = colors;
            var text = button.GetComponentInChildren<Text>();
            text.rectTransform.sizeDelta = new Vector2(78f, 78f);
            text.fontSize = 16;
        }

        private static Sprite GetActionCircleSprite()
        {
            if (actionCircleSprite != null) return actionCircleSprite;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "action-circle-sprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var center = (size - 1f) * .5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01((center - distance) * .9f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            actionCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size);
            actionCircleSprite.name = "action-circle-sprite";
            return actionCircleSprite;
        }

        private static Transform MakeAvatarPart(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, int layer)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = GetBuiltInCollider(part, shape);
            if (collider != null) Destroy(collider);
            part.layer = layer;
            return part.transform;
        }

        private static void AnchorHudButton(Button button, float rightOffset)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(rightOffset, -379f);
        }

        private static GameObject MakeCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            _ = cube.GetComponent<BoxCollider>();
            return cube;
        }

        private static Collider GetBuiltInCollider(GameObject primitive, PrimitiveType shape)
        {
            switch (shape)
            {
                case PrimitiveType.Sphere: return primitive.GetComponent<SphereCollider>();
                case PrimitiveType.Capsule: return primitive.GetComponent<CapsuleCollider>();
                case PrimitiveType.Cylinder: return primitive.GetComponent<CapsuleCollider>();
                case PrimitiveType.Cube: return primitive.GetComponent<BoxCollider>();
                default: return primitive.GetComponent<Collider>();
            }
        }

        private static Material CreateMaterial(Color color, float metalness, float smoothness, Color? emission = null)
        {
            var template = Resources.Load<Material>("neon-room-lit");
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("A supported rendering shader must be included in the WebGL player.");
            var material = template != null ? new Material(template) : new Material(shader);
            material.color = color;
            material.enableInstancing = true;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metalness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static GameObject AddPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = CreateRect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static Text AddText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var rect = CreateRect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject AddTextObject(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            return AddText(parent, name, value, position, size, fontSize, color).gameObject;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color accent)
        {
            var rect = CreateRect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.08f, .065f, .15f, .98f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(.12f, .085f, .21f, .98f);
            colors.highlightedColor = accent;
            colors.pressedColor = accent * .75f;
            colors.selectedColor = accent;
            colors.disabledColor = new Color(.12f, .1f, .15f, .5f);
            button.colors = colors;
            button.onClick.AddListener(action);
            var text = AddText(rect, name + "-label", label, Vector2.zero, size - new Vector2(16f, 8f), Mathf.Clamp(Mathf.RoundToInt(size.y * .34f), 15, 24), Color.white);
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private void OnDestroy()
        {
            if (previewTexture != null) previewTexture.Release();
        }

        private static readonly Color AccentLime = new Color(.68f, .93f, .17f);
        private static readonly Color AccentCyan = new Color(.22f, .83f, 1f);
        private static readonly Color AccentPink = new Color(1f, .23f, .62f);
        private static readonly Color AccentViolet = new Color(.68f, .4f, 1f);

        private sealed class AvatarDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            public NeonRoomGame game;
            public void OnBeginDrag(PointerEventData eventData) => game.dragStart = eventData.position;
            public void OnDrag(PointerEventData eventData)
            {
                if (game.previewAvatar == null) return;
                var delta = eventData.position.x - game.dragStart.x;
                game.previewYaw -= delta * .7f;
                game.previewAvatar.localRotation = Quaternion.Euler(0f, game.previewYaw, 0f);
                game.dragStart = eventData.position;
            }
        }

        private sealed class MobileStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            private NeonRoomGame game;
            private bool lookStick;
            private Vector2 origin;
            public void Setup(NeonRoomGame owner, bool isLook)
            {
                game = owner;
                lookStick = isLook;
            }
            public void OnPointerDown(PointerEventData eventData)
            {
                origin = eventData.position;
                SetHeld(true);
            }
            public void OnDrag(PointerEventData eventData)
            {
                var value = Vector2.ClampMagnitude((eventData.position - origin) / 70f, 1f);
                if (lookStick) game.mobileLook = value;
                else game.mobileMove = value;
            }
            public void OnPointerUp(PointerEventData eventData)
            {
                if (lookStick) game.mobileLook = Vector2.zero;
                else game.mobileMove = Vector2.zero;
                SetHeld(false);
            }
            private void SetHeld(bool held)
            {
                if (lookStick) game.mobileLookHeld = held;
                else game.mobileMoveHeld = held;
            }
        }
    }
}
