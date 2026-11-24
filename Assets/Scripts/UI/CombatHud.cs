using UnityEngine;
using UnityEngine.UI;

namespace Driftworld
{
    // The combat HUD, built in code like the rest of the greybox: hero
    // health and stamina, the loot counter (the score the player watches),
    // the engaged creature bar, an objective line that always names the
    // next goal, the weapon label, and the harvest progress panel.
    // Controls overhaul 2026-11-24: the HUD is display only. Every on
    // screen action button (strike, guard, dodge, skill, lock, harvest)
    // and the developer debug panel are gone; combat is gestures now
    // (drag moves, tap strikes, flick dodges, still hold harvests). The
    // canvas carries no GraphicRaycaster, so the HUD layer cannot
    // intercept a touch anywhere on screen.
    public class CombatHud : MonoBehaviour
    {
        private PlayerCombat combat;
        private Harvester harvester;
        private Image healthFill;
        private Image staminaFill;
        private Image creatureFill;
        private Text creatureLabel;
        private GameObject creaturePanel;
        private Text lootText;
        private Text objectiveText;
        private Text weaponLabel;
        private GameObject harvestPanel;
        private Image harvestFill;
        private Text toastText;
        private Text diagText;
        private Font font;

        public void Bind(PlayerCombat playerCombat, Harvester playerHarvester)
        {
            combat = playerCombat;
            harvester = playerHarvester;
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        private Text MakeText(Transform parent, string content, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void SetRect(RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            r.pivot = new Vector2(0.5f, 0.5f);
        }

        private Image MakePanel(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            SetRect(go.GetComponent<RectTransform>(), aMin, aMax, pos, size);
            return img;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // No GraphicRaycaster on purpose: nothing on this canvas is
            // interactive, and without a raycaster no HUD element can
            // claim a touch before the gesture stream sees it.
            Transform root = transform;

            // Hero health and stamina, top left.
            var hpBg = MakePanel(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -60f), new Vector2(440f, 34f), new Color(0f, 0f, 0f, 0.5f));
            healthFill = MakePanel(hpBg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.8f, 0.3f, 0.25f));
            var stBg = MakePanel(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -102f), new Vector2(440f, 20f), new Color(0f, 0f, 0f, 0.5f));
            staminaFill = MakePanel(stBg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.95f, 0.8f, 0.3f));

            // Weapon label under the bars.
            weaponLabel = MakeText(root, "", 30, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperLeft);
            SetRect(weaponLabel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -140f), new Vector2(440f, 40f));

            // Loot counter, top right.
            lootText = MakeText(root, "", 38, Color.white, TextAnchor.UpperRight);
            SetRect(lootText.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-330f, -70f), new Vector2(620f, 120f));

            // Engaged creature bar, top center.
            creaturePanel = MakePanel(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(520f, 30f), new Color(0f, 0f, 0f, 0.5f)).gameObject;
            creatureFill = MakePanel(creaturePanel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.5f, 0.75f, 0.35f));
            creatureLabel = MakeText(root, "", 34, Color.white, TextAnchor.MiddleCenter);
            SetRect(creatureLabel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(700f, 44f));

            // Objective line, under the creature bar.
            objectiveText = MakeText(root, "", 34, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
            SetRect(objectiveText.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(980f, 90f));
            objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Harvest progress, bottom center. Display only: the hold
            // itself is a still touch anywhere beside the corpse.
            harvestPanel = MakePanel(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(460f, 120f), new Color(0.1f, 0.1f, 0.1f, 0.7f)).gameObject;
            harvestFill = MakePanel(harvestPanel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.9f, 0.7f, 0.25f, 0.9f));
            var hLabel = MakeText(harvestPanel.transform, "Hold to Harvest", 40, Color.white, TextAnchor.MiddleCenter);
            SetRect(hLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            harvestPanel.SetActive(false);

            // Harvest result toast, center.
            toastText = MakeText(root, "", 44, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter);
            SetRect(toastText.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 60f));

            // Build diagnostics readout, bottom left. Small, and it comes
            // out with the rest of the diagnostics once the loop is proven.
            diagText = MakeText(root, "", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerLeft);
            diagText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetRect(diagText.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(430f, 30f), new Vector2(840f, 40f));
        }

        private static void SetFill(Image fill, float frac)
        {
            if (fill == null) return;
            var r = fill.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (combat == null) return;

            SetFill(healthFill, combat.Health / Tuning.Combat.hero_health_max);
            SetFill(staminaFill, combat.Stamina / Tuning.Combat.stamina_max);
            if (staminaFill != null)
            {
                staminaFill.color = Time.time - combat.LastStaminaDeniedAt < 0.3f
                    ? new Color(0.95f, 0.3f, 0.25f)
                    : new Color(0.95f, 0.8f, 0.3f);
            }

            if (weaponLabel != null && combat.Weapon != null)
            {
                weaponLabel.text = combat.Weapon.DisplayName
                    + (combat.CurrentStepName.Length > 0 ? "  " + combat.CurrentStepName : "");
            }

            if (LootBank.Instance != null)
            {
                var b = LootBank.Instance;
                lootText.text = "Shards " + b.Shards + "   Hide " + b.Hide + "   Meat " + b.Meat
                    + (b.ElderHide > 0 ? "   Elder Hide " + b.ElderHide : "")
                    + (b.Horn > 0 ? "   Horn " + b.Horn : "");
            }

            // The engaged creature, from the one shared targeting source.
            MossHorn engaged = Targeting.DisplayTarget(combat.transform.position);
            bool elderDown = false;
            bool elderHarvested = false;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c == null) continue;
                if (c.IsElder && c.IsDefeated) elderDown = true;
                if (c.IsElder && c.IsHarvested) elderHarvested = true;
            }
            if (engaged != null)
            {
                creaturePanel.SetActive(true);
                SetFill(creatureFill, engaged.Hp / engaged.MaxHp);
                string state = "";
                switch (engaged.CurrentState)
                {
                    case MossHorn.State.Staggered: state = "  STAGGERED"; break;
                    case MossHorn.State.Stunned: state = "  STUNNED"; break;
                    case MossHorn.State.Knockdown: state = "  DOWN"; break;
                    case MossHorn.State.Exhausted: state = "  EXHAUSTED"; break;
                    case MossHorn.State.Flinch: state = "  FLINCH"; break;
                }
                if (engaged.IsEnraged) state += "  ENRAGED";
                creatureLabel.text = engaged.DisplayName + state;
            }
            else
            {
                creaturePanel.SetActive(false);
                creatureLabel.text = "";
            }

            var bank = LootBank.Instance;
            if (bank != null)
            {
                if (elderHarvested) objectiveText.text = "Slice loop complete. The crafting bench arrives with the next build step.";
                else if (elderDown) objectiveText.text = "The Elder is down. Hold Harvest beside it.";
                else if (bank.Harvests > 0) objectiveText.text = "Glide east to the far island. An Elder Moss Horn waits there.";
                else if (bank.Kills > 0) objectiveText.text = "It is down. Stand close and hold Harvest.";
                else objectiveText.text = "Hunt a Moss Horn. It always warns you before it charges. Slip the charge, then answer.";
            }

            bool showHarvest = harvester != null && harvester.Nearby != null;
            harvestPanel.SetActive(showHarvest);
            if (showHarvest) SetFill(harvestFill, harvester.Progress);

            if (harvester != null && harvester.LastGrantAt > 0f && Time.time - harvester.LastGrantAt < 3.5f)
            {
                toastText.text = "Harvested: " + harvester.LastGrantSummary;
            }
            else
            {
                toastText.text = "";
            }

            if (diagText != null)
            {
                diagText.text = "DIAG step: " + Diag.Step
                    + (Diag.Note.Length > 0 ? "  |  " + Diag.Note : "")
                    + (Diag.Error.Length > 0 ? "  |  ERROR: " + Diag.Error : "")
                    + "  |  " + Diag.Status + " " + Diag.CamStatus + "  |  " + Diag.Combat;
            }
        }
    }
}
