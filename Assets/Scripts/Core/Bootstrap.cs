using UnityEngine;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if (Object.FindAnyObjectByType<PlayerController>() != null) return;

            RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 170f;
            RenderSettings.fogColor = new Color(0.62f, 0.78f, 0.85f);

            MakeSun();
            MakeIslandA();
            MakeIslandB();
            WindLane lane = MakeWindLane();
            GameObject hero = MakeHero();
            MakeCamera(hero);
            MakeRippleCanvas(hero.GetComponent<DragInput>());
        }

        private static Material Mat(Color color)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = color;
            return m;
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
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.2f) * 1.6f);
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

        private static GameObject MakeHero()
        {
            var hero = new GameObject("Hero");
            hero.transform.position = new Vector3(8f, 0.3f, 0f);
            hero.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

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
            cam.transform.position = hero.transform.position + new Vector3(-6.5f, 3.2f, 0f);

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
