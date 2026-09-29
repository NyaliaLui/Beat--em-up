using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Builds the ENTIRE game in code at runtime - camera, lighting, arena, props, player,
    /// systems and UI - and wires everything together. Self-starts via RuntimeInitializeOnLoadMethod
    /// so it runs in ANY scene with zero manual wiring: just have an (even empty) scene in Build
    /// Settings and press Play. No prefabs, no .unity/.prefab YAML, no .meta GUIDs to author.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (FindAnyObjectByType<Bootstrap>() != null) return;
            var go = new GameObject("IronholdBootstrap");
            go.AddComponent<Bootstrap>();
        }

        private async void Start()
        {
            ApplyAppSettings();
            ClearDefaultSceneObjects();

            // ---- Load shared sprites / textures (all pipeline-agnostic) ----
            Sprite buttonSprite = RenderUtil.LoadSprite(GameConfig.TexButton);
            Sprite panelSprite = RenderUtil.LoadSprite(GameConfig.TexPanel);
            Sprite titleSprite = RenderUtil.LoadSprite(GameConfig.TexTitle);
            Texture2D floorTex = RenderUtil.LoadTexture(GameConfig.TexFloor);
            Texture2D wallTex = RenderUtil.LoadTexture(GameConfig.TexWall);
            Texture2D skyTex = RenderUtil.LoadTexture(GameConfig.TexSkybox);

            BuildEnvironment(floorTex, wallTex, skyTex);
            CameraFollow camera = BuildCamera();

            // ---- Systems ----
            var systemsGo = new GameObject("Systems");
            var gm = systemsGo.AddComponent<GameManager>();         // Awake sets GameManager.Instance
            var score = systemsGo.AddComponent<ScoreManager>();
            var waves = systemsGo.AddComponent<WaveManager>();
            var announcer = new GameObject("Announcer").AddComponent<AnnouncerVO>(); // own AudioSource
            var sfx = new GameObject("Sfx").AddComponent<SfxManager>();              // own AudioSource
            announcer.transform.SetParent(systemsGo.transform);
            sfx.transform.SetParent(systemsGo.transform);
            systemsGo.AddComponent<Impact>();                                        // hit-stop kernel
            systemsGo.AddComponent<MusicManager>();                                  // adaptive 2-layer music
            new GameObject("HitSparks").AddComponent<HitSparks>();                   // one shared burst system
            new GameObject("SlashArc").AddComponent<SlashArc>();                     // pooled sword arc flash

            gm.Score = score;
            gm.Waves = waves;
            gm.Announcer = announcer;
            gm.Sfx = sfx;
            gm.Camera = camera;

            // ---- Models + actors ----
            await GlbLoader.PreloadAll(new[]
            {
                GameConfig.ModelHero, GameConfig.ModelGrunt, GameConfig.ModelSkeleton,
                GameConfig.ModelBrute, GameConfig.ModelCrate, GameConfig.ModelBarrel, GameConfig.ModelBrazier,
                GameConfig.ModelChest, GameConfig.ModelChestOpen
            });

            PlayerController player = await BuildPlayer();
            gm.Player = player;
            gm.PlayerHealth = player.GetComponent<PlayerHealth>();
            gm.PlayerStamina = player.GetComponent<PlayerStamina>();

            waves.Configure(player.transform);
            camera.Configure(player.transform);

            await BuildProps();

            // Easter eggs: hidden chest in the background + the manager watching all three triggers.
            SecretChest chest = await SecretChest.Build();
            gm.EasterEggs = systemsGo.AddComponent<EasterEggManager>();
            gm.EasterEggs.Configure(waves, chest);

            // Warm the skeletal clip library while the menu is up. Fire-and-forget by design:
            // the game is fully playable procedurally until each ActorAnimator flips Ready.
            WarmAnimLibrary();

            // ---- UI ----
            Transform canvas = BuildCanvas();
            var dmgNumbers = new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
            dmgNumbers.Build(canvas);
            if (Debug.isDebugBuild) new GameObject("AnimDebugCycler").AddComponent<AnimDebugCycler>();

            var uiGo = new GameObject("UI");
            gm.Hud = uiGo.AddComponent<HUDController>();
            gm.Menu = uiGo.AddComponent<MenuController>();
            gm.Pause = uiGo.AddComponent<PauseController>();
            gm.GameOverUI = uiGo.AddComponent<GameOverController>();
            gm.PromoUI = uiGo.AddComponent<PromoPopupController>();

            gm.Hud.Build(canvas, player, panelSprite, buttonSprite);
            gm.Menu.Build(canvas, titleSprite, panelSprite);
            gm.Pause.Build(canvas, panelSprite);
            gm.GameOverUI.Build(canvas, panelSprite);
            gm.PromoUI.Build(canvas); // built last so it draws above everything

            gm.GoToMenu();
        }

        private static async void WarmAnimLibrary()
        {
            // Hero clips load inside PlayerController.BindSkeletal; warm the enemy sets here
            // so wave-1 spawns bind instantly. Sequential loads keep the memory peak at one GLB.
            await AnimLibrary.PreloadSet(GameConfig.GruntClips);
            await AnimLibrary.PreloadSet(GameConfig.SkeletonClips);
            await AnimLibrary.PreloadSet(GameConfig.BruteClips);
        }

        private static void ApplyAppSettings()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        /// <summary>Remove the default scene's camera/light/listener so we own the scene cleanly.</summary>
        private static void ClearDefaultSceneObjects()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(c.gameObject);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) Destroy(l.gameObject);
            foreach (var a in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) Destroy(a);
        }

        private void BuildEnvironment(Texture2D floorTex, Texture2D wallTex, Texture2D skyTex)
        {
            // Lighting: cool moonlight key + warm ambient.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.38f);
            var sunGo = new GameObject("Moonlight");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.7f, 0.78f, 1f);
            sun.intensity = 0.9f;
            sunGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            // Ground (top surface at y = 0). Cube primitive brings a BoxCollider for the CCs.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(60f, 1f, 8f);
            ground.transform.position = new Vector3(0f, -0.5f, GameConfig.PlayZ);
            ApplyTiled(ground, floorTex, GameConfig.StoneGrey, new Vector2(30f, 4f), lit: true);

            // Back wall (behind the action plane; no collision in the lane).
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "BackWall";
            wall.transform.localScale = new Vector3(60f, 16f, 1f);
            wall.transform.position = new Vector3(0f, 7f, 3.2f);
            Destroy(wall.GetComponent<Collider>());
            ApplyTiled(wall, wallTex, GameConfig.StoneGrey, new Vector2(12f, 4f), lit: true);

            // Night-sky backdrop (unlit, far behind).
            var sky = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sky.name = "SkyBackdrop";
            sky.transform.localScale = new Vector3(140f, 64f, 0.5f);
            sky.transform.position = new Vector3(0f, 16f, 30f);
            Destroy(sky.GetComponent<Collider>());
            var skyRend = sky.GetComponent<Renderer>();
            if (skyTex != null) skyTex.wrapMode = TextureWrapMode.Clamp;
            skyRend.sharedMaterial = RenderUtil.CreateUnlit(skyTex != null ? Color.white : GameConfig.NightBlue, skyTex);
        }

        private static void ApplyTiled(GameObject go, Texture2D tex, Color fallback, Vector2 tiling, bool lit)
        {
            if (tex != null) tex.wrapMode = TextureWrapMode.Repeat;
            Material m = lit ? RenderUtil.CreateLit(tex != null ? Color.white : fallback, tex)
                             : RenderUtil.CreateUnlit(tex != null ? Color.white : fallback, tex);
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling);
            m.mainTextureScale = tiling;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        private CameraFollow BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameConfig.NightBlue;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            go.AddComponent<AudioListener>();
            return go.AddComponent<CameraFollow>();
        }

        private async Task<PlayerController> BuildPlayer()
        {
            var go = new GameObject("Player");
            var cc = go.AddComponent<CharacterController>();
            cc.height = GameConfig.CharacterHeight;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, GameConfig.CharacterHeight * 0.5f, 0f);
            cc.slopeLimit = 60f;
            cc.stepOffset = 0.3f;

            go.AddComponent<PlayerStamina>();
            go.AddComponent<PlayerHealth>();
            go.AddComponent<CombatSystem>();
            var controller = go.AddComponent<PlayerController>();

            go.transform.position = new Vector3(0f, 0.1f, GameConfig.PlayZ);
            Transform visual = await GlbLoader.AttachVisual(GameConfig.ModelHero, go.transform, 1.85f, GameConfig.Iron);
            controller.SetVisual(visual);
            return controller;
        }

        private async Task BuildProps()
        {
            await MakeProp(GameConfig.ModelBrazier, new Vector3(-6f, 0f, 1.7f), 1.5f, GameConfig.Iron, withFireLight: true);
            await MakeProp(GameConfig.ModelBrazier, new Vector3(6f, 0f, 1.7f), 1.5f, GameConfig.Iron, withFireLight: true);
            await MakeProp(GameConfig.ModelCrate, new Vector3(-10.5f, 0f, 1.8f), 1.0f, GameConfig.Leather, withFireLight: false);
            await MakeProp(GameConfig.ModelBarrel, new Vector3(9.5f, 0f, 1.8f), 1.1f, GameConfig.Leather, withFireLight: false);
            await MakeProp(GameConfig.ModelCrate, new Vector3(11.5f, 0f, 1.9f), 1.0f, GameConfig.Leather, withFireLight: false);
        }

        private async Task MakeProp(string model, Vector3 pos, float height, Color fallback, bool withFireLight)
        {
            var go = new GameObject("Prop_" + model);
            go.transform.position = pos;
            await GlbLoader.AttachVisual(model, go.transform, height, fallback);
            if (withFireLight)
            {
                var lightGo = new GameObject("BrazierLight");
                lightGo.transform.SetParent(go.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                var lp = lightGo.AddComponent<Light>();
                lp.type = LightType.Point;
                lp.color = GameConfig.EmberOrange;
                lp.range = 12f;
                lp.intensity = 2.2f;
                var flick = lightGo.AddComponent<BrazierFlicker>();
                flick.Target = lp;
            }
        }

        private Transform BuildCanvas()
        {
            var canvasGo = new GameObject("UICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return canvasGo.transform;
        }
    }
}
