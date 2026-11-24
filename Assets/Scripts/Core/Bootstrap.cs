using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Driftworld
{
    // Greybox bootstrap for the slice. The whole test world is built in code
    // at load: two islands, one wind lane, the hero with drag movement and
    // glide, a follow camera, and the touch ripple. No scene assets and no
    // art assets are needed to play the loop, which is the point of the
    // greybox: the loop proves itself before art gets a vote.
    public static class Bootstrap
    {
        // MECH-009: the combat sandbox is the same shipped build with
        // ?sandbox in the URL. One flat arena, one training monster, the
        // debug panel open. The slice world builds otherwise.
        public static bool SandboxActive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if (Object.FindAnyObjectByType<PlayerController>() != null) return;
            SandboxActive = Application.absoluteURL != null && Application.absoluteURL.Contains("sandbox");

            try
            {
                Diag.SetStep("world settings");
                RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 45f;
                RenderSettings.fogEndDistance = 170f;
                RenderSettings.fogColor = new Color(0.62f, 0.78f, 0.85f);

                Diag.SetStep("sun");
                MakeSun();
                if (SandboxActive)
                {
                    Diag.SetStep("sandbox arena");
                    MakeSandboxArena();
                    Diag.SetStep("hero");
                    GameObject sandboxHero = MakeHero(new Vector3(0f, 0.3f, 7f), 180f);
                    Diag.SetStep("training monster");
                    MakeTrainingMonster();
                    Diag.SetStep("camera");
                    MakeCamera(sandboxHero);
                    Diag.SetStep("ripple canvas");
                    MakeRippleCanvas(sandboxHero.GetComponent<DragInput>());
                    Diag.SetStep("combat hud");
                    MakeCombatHud(sandboxHero);
                    Diag.SetStep("ready");
                    return;
                }
                Diag.SetStep("island A");
                MakeIslandA();
                Diag.SetStep("island B");
                MakeIslandB();
                Diag.SetStep("wind lane");
                WindLane lane = MakeWindLane();
                Diag.SetStep("hero");
                GameObject hero = MakeHero(new Vector3(8f, 0.3f, 0f), -90f);
                Diag.SetStep("creatures");
                MakeCreatures();
                Diag.SetStep("camera");
                MakeCamera(hero);
                Diag.SetStep("ripple canvas");
                MakeRippleCanvas(hero.GetComponent<DragInput>());
                Diag.SetStep("combat hud");
                MakeCombatHud(hero);
                Diag.SetStep("ready");
            }
            catch (System.Exception ex)
            {
                Diag.Fail(ex);
                Debug.LogException(ex);
            }
        }

        private static void MakeSandboxArena()
        {
            var grass = Mat(new Color(0.42f, 0.62f, 0.35f));
            var stone = Mat(new Color(0.55f, 0.52f, 0.48f));
            Box("SandboxGround", new Vector3(0f, -1f, 0f), new Vector3(44f, 2f, 44f), grass);
            Box("SandboxBlock1", new Vector3(-8f, 1f, -8f), new Vector3(3f, 2f, 3f), stone);
            Box("SandboxBlock2", new Vector3(9f, 0.75f, 6f), new Vector3(2f, 1.5f, 2f), stone);
            MakeTree(new Vector3(-12f, 0f, 10f));
            MakeTree(new Vector3(12f, 0f, -12f));
        }

        private static void MakeTrainingMonster()
        {
            var go = new GameObject("TrainingMossHorn");
            var creature = go.AddComponent<MossHorn>();
            creature.Configure(new Vector3(0f, 0f, -4f), 0f, false, new Vector3(0f, 0f, 0f), new Vector2(20f, 20f), 99);
            creature.SetTraining();
        }

        private static void MakeCreatures()
        {
            // Island A: three commons, the first fight seconds away but
            // never on top of the player. Placement rule: nothing spawns
            // inside aggro range (10 m) of the hero spawn at (8, 0, 0).
            // The first two sat at 7.6 and 6.3 m, so an idle hero was
            // charged, knocked west off the island, and looped through
            // glide and rescue forever (proven by the diag readout on
            // 2026-11-23). These spots sit in notice range or beyond:
            // they lift their heads as the player approaches and only
            // threaten inside 10 m. Island B: one common and the elder.
            MakeCreature(new Vector3(-2f, 0f, 9f), 0f, false, new Vector3(0f, 0f, 0f), new Vector2(15f, 15f), 1);
            MakeCreature(new Vector3(-6f, 0f, -8f), 0f, false, new Vector3(0f, 0f, 0f), new Vector2(15f, 15f), 2);
            MakeCreature(new Vector3(12f, 0f, -10f), 0f, false, new Vector3(0f, 0f, 0f), new Vector2(15f, 15f), 3);
            MakeCreature(new Vector3(53f, -6f, -5f), -6f, false, new Vector3(58f, -6f, 0f), new Vector2(11f, 11f), 4);
            MakeCreature(new Vector3(60f, -6f, 4f), -6f, true, new Vector3(58f, -6f, 0f), new Vector2(11f, 11f), 5);
        }

        private static void MakeCreature(Vector3 pos, float groundY, bool elder, Vector3 islandCenter, Vector2 islandHalf, int index)
        {
            var go = new GameObject(elder ? "ElderMossHorn" : "MossHorn");
            var creature = go.AddComponent<MossHorn>();
            creature.Configure(pos, groundY, elder, islandCenter, islandHalf, index);
        }

        private static void MakeCombatHud(GameObject hero)
        {
            // uGUI buttons receive no touches at all without an EventSystem.
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var combat = hero.GetComponent<PlayerCombat>();
            var harvester = hero.GetComponent<Harvester>();
            CombatHud.DebugPanelStartsOpen = SandboxActive;
            var hudGo = new GameObject("CombatHud");
            var hud = hudGo.AddComponent<CombatHud>();
            hud.Bind(combat, harvester);
            combat.strikeButton = hud.strikeButton;
            combat.guardButton = hud.guardButton;
            combat.skillButton = hud.skillButton;
            combat.dodgeButton = hud.dodgeButton;
            combat.lockButton = hud.lockButton;
            harvester.harvestButton = hud.harvestButton;
            harvester.combat = combat;

            // MECH-009 presentation and observability objects.
            var numbers = new GameObject("DamageNumbers");
            numbers.AddComponent<DamageNumbers>();
            var vfx = new GameObject("HitVfx");
            vfx.AddComponent<HitVfx>();
            var bridge = new GameObject("TestBridge");
            bridge.AddComponent<TestBridge>();
        }

        private static Material Mat(Color color)
        {
            return Diag.SafeMaterial(color);
        }

        private static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        private static void MakeSun()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.86f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        private static void MakeIslandA()
        {
            var grass = Mat(new Color(0.42f, 0.62f, 0.35f));
            var stone = Mat(new Color(0.55f, 0.52f, 0.48f));
            Box("IslandA", new Vector3(0f, -1f, 0f), new Vector3(30f, 2f, 30f), grass);
            Box("RuinBlock1", new Vector3(-6f, 1f, -6f), new Vector3(3f, 2f, 3f), stone);
            Box("RuinBlock2", new Vector3(-9f, 0.75f, 5f), new Vector3(2f, 1.5f, 2f), stone);
            MakeTree(new Vector3(-3f, 0f, 8f));
            MakeTree(new Vector3(5f, 0f, -9f));
            MakeTree(new Vector3(-11f, 0f, -2f));

            var glow = Mat(new Color(1f, 0.8f, 0.3f));
            Box("LaunchLedge", new Vector3(14.8f, 0.06f, 0f), new Vector3(0.5f, 0.12f, 5f), glow);
        }

        private static void MakeIslandB()
        {
            var grass = Mat(new Color(0.38f, 0.58f, 0.4f));
            var stone = Mat(new Color(0.5f, 0.48f, 0.45f));
            Box("IslandB", new Vector3(58f, -7f, 0f), new Vector3(22f, 2f, 22f), grass);
            Box("ArchPillarL", new Vector3(56f, -4.5f, 0f), new Vector3(0.8f, 3f, 0.8f), stone);
            Box("ArchPillarR", new Vector3(60f, -4.5f, 0f), new Vector3(0.8f, 3f, 0.8f), stone);
            Box("ArchLintel", new Vector3(58f, -2.6f, 0f), new Vector3(5.5f, 0.8f, 1f), stone);
            MakeTree(new Vector3(52f, -6f, 6f));
            MakeTree(new Vector3(64f, -6f, -5f));

            var gold = Mat(new Color(0.85f, 0.62f, 0.2f));
            Box("IslandChest", new Vector3(62f, -5.6f, 3f), new Vector3(1.2f, 0.8f, 0.8f), gold);
        }

        private static void MakeTree(Vector3 basePos)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "TreeTrunk";
            trunk.transform.position = basePos + new Vector3(0f, 0.8f, 0f);
            trunk.transform.localScale = new Vector3(0.3f, 0.8f, 0.3f);
            trunk.GetComponent<Renderer>().material = Mat(new Color(0.4f, 0.3f, 0.2f));

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "TreeCanopy";
            canopy.transform.position = basePos + new Vector3(0f, 2.2f, 0f);
            canopy.transform.localScale = new Vector3(1.7f, 1.3f, 1.7f);
            canopy.GetComponent<Renderer>().material = Mat(new Color(0.3f, 0.55f, 0.3f));
            Object.Destroy(canopy.GetComponent<Collider>());
        }

        private static WindLane MakeWindLane()
        {
            var go = new GameObject("WindLane_HubToIslandB");
            go.transform.position = new Vector3(31f, -1f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // forward becomes +X, toward island B
            var lane = go.AddComponent<WindLane>();
            lane.size = new Vector3(14f, 12f, 44f);
            return lane;
        }

        private static GameObject MakeHero(Vector3 spawn, float yawDeg)
        {
            var hero = new GameObject("Hero");
            hero.transform.position = spawn;
            // Face into the island, toward its center and the first Moss
            // Horns. The old east facing put the island edge 7 units ahead
            // of spawn, so a first forward drag ran straight off it.
            hero.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            var body = hero.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.9f, 0f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "HeroVisual";
            visual.transform.SetParent(hero.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
            visual.GetComponent<Renderer>().material = Mat(new Color(0.25f, 0.45f, 0.5f));
            Object.Destroy(visual.GetComponent<Collider>());

            var glider = new GameObject("GliderVisual");
            glider.transform.SetParent(hero.transform, false);
            glider.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "GliderWing";
            wing.transform.SetParent(glider.transform, false);
            wing.transform.localScale = new Vector3(2.8f, 0.07f, 1.15f);
            wing.GetComponent<Renderer>().material = Mat(new Color(0.85f, 0.75f, 0.55f));
            Object.Destroy(wing.GetComponent<Collider>());
            glider.SetActive(false);

            hero.AddComponent<DragInput>();
            var controller = hero.AddComponent<PlayerController>();
            controller.gliderVisual = glider.transform;
            controller.rescueY = -30f;

            var combat = hero.AddComponent<PlayerCombat>();
            combat.respawnPoint = spawn;
            hero.AddComponent<CombatDebugInput>();
            hero.AddComponent<WeaponRangeViz>();
            hero.AddComponent<Harvester>();
            hero.AddComponent<LootBank>();
            return hero;
        }

        private static void MakeCamera(GameObject hero)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.backgroundColor = new Color(0.45f, 0.68f, 0.8f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = hero.transform.position + new Vector3(6.5f, 3.2f, 0f);

            var follow = cam.GetComponent<FollowCamera>();
            if (follow == null) follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.target = hero.transform;
            follow.controller = hero.GetComponent<PlayerController>();
        }

        private static void MakeRippleCanvas(DragInput input)
        {
            var canvasGo = new GameObject("RippleCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            var rippleGo = new GameObject("TouchRipple");
            rippleGo.transform.SetParent(canvasGo.transform, false);
            var rect = rippleGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(130f, 130f);
            rect.position = new Vector3(-200f, -200f, 0f);
            rippleGo.AddComponent<Image>();
            var ripple = rippleGo.AddComponent<TouchRipple>();
            ripple.input = input;
        }
    }
}
