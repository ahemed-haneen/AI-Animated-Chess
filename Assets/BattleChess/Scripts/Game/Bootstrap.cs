using UnityEngine;
using UnityEngine.Rendering;

namespace BattleChess
{
    /// <summary>
    /// Drop this on one GameObject in an empty scene (the editor menu does it for you) and press Play.
    /// Creates the camera rig, lighting, board, sound, FX, HUD and game controller.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Tooltip("Theme asset. If empty, loads Resources/Themes/PiratesVsBritish or builds it in code.")]
        public ThemeDefinition theme;
        [Tooltip("A URP/Lit material (created by the editor menu). Guarantees the shader is included in builds.")]
        public Material litMaterial;
        [Tooltip("A Sprites/Default material (created by the editor menu).")]
        public Material spriteMaterial;
        public bool cinematicByDefault = true;
        [Tooltip("60 is smooth on modern phones; use 30 to save battery.")]
        public int targetFrameRate = 60;

        // Start (not Awake) so every component in the scene, including the camera rig, has initialised first.
        void Start()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Mats.LitTemplate = litMaterial;
            Mats.SpriteTemplate = spriteMaterial;

            if (theme == null) theme = Resources.Load<ThemeDefinition>("Themes/PiratesVsBritish");
            if (theme == null) theme = ThemeLibrary.PiratesVsBritish();

            // camera
            var cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera");
                cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
                cg.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.background;
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();

            // lighting
            var sun = FindFirstObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                var lg = new GameObject("Sun");
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.color = theme.sunColor;
            sun.intensity = theme.sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(theme.background, Color.white, 0.5f);
            RenderSettings.ambientEquatorColor = Color.Lerp(theme.background, theme.ground, 0.5f) * 0.8f;
            RenderSettings.ambientGroundColor = theme.ground * 0.4f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.background;
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 55f;

            // systems
            var fx = new GameObject("Fx").AddComponent<Fx>();
            var snd = new GameObject("Sound").AddComponent<SoundBank>();
            var board = new GameObject("Board").AddComponent<BoardView>();
            board.Build(theme);
            var hud = new GameObject("HUD").AddComponent<Hud>();
            var director = new GameObject("BattleDirector").AddComponent<BattleDirector>();
            director.cinematic = cinematicByDefault;
            var game = new GameObject("Game").AddComponent<GameController>();
            director.game = game;
            game.Theme = theme; game.Board = board; game.Cam = rig; game.Hud = hud; game.Director = director;
            game.Init();
            if (fx == null || snd == null) Debug.LogWarning("Battle Chess: FX or sound failed to start.");
        }
    }
}
