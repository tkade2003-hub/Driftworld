using UnityEngine;
using UnityEngine.UI;

namespace Driftworld
{
    // The combat HUD, built in code like the rest of the greybox: hero
    // health and stamina, the loot counter (the score the player watches),
    // the engaged creature bar, an objective line that always names the
    // next goal, the combat buttons on the right side (strike, guard,
    // dodge, skill, lock), and the harvest button with its hold progress.
    // MECH-009 adds the weapon label, the lock and dodge buttons, and a
    // developer debug panel (weapon switching, visualization toggles,
    // monster state forcing) that the sandbox opens by default. No
    // joystick is drawn, ever; movement stays on the left side drag.
    public class CombatHud : MonoBehaviour
    {
        public HoldButton strikeButton;
        public HoldButton guardButton;
        public HoldButton skillButton;
        public HoldButton harvestButton;
        public HoldButton dodgeButton;
        public HoldButton lockButton;

        // Set by Bootstrap before the HUD is created in sandbox mode.
        public static bool DebugPanelStartsOpen;

        private PlayerCombat combat;
        private Harvester harvester;
        private Image healthFill;
        private Image staminaFill;
        private Image creatureFill;
        private Text creatureLabel;
        private GameObject creaturePanel;
        private Text lootText;
        private Text objectiveText;
        private Text skillLabel;
        private Text lockLabel;
        private Text weaponLabel;
        private GameObject harvestPanel;
        private Image harvestFill;
        private Text toastText;
        private Text diagText;
        private Font font;

        private GameObject debugPanel;
        private HoldButton dbgToggle;
        private HoldButton[] debugButtons;
        private string[] debugActions;

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

        private HoldButton MakeButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
        {
            var img = MakePanel(parent, aMin, aMax, pos, size, color);
            var btn = img.gameObject.AddComponent<HoldButton>();
            var t = MakeText(img.transform, label, 40, Color.white, TextAnchor.MiddleCenter);
            SetRect(t.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        private HoldButton MakeSmallButton(Transform parent, string label, Vector2 pos, Vector2 size)
        {
            var img = MakePanel(parent, Vector2.zero, Vector2.zero, pos, size, new Color(0.15f, 0.15f, 0.2f, 0.9f));
            var btn = img.gameObject.AddComponent<HoldButton>();
            var t = MakeText(img.transform, label, 26, Color.white, TextAnchor.MiddleCenter);
            SetRect(t.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            gameObject.AddComponent<GraphicRaycaster>();
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

            // Combat buttons, bottom right.
            strikeButton = MakeButton(root, "Strike", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-130f, 150f), new Vector2(200f, 200f), new Color(0.55f, 0.2f, 0.15f, 0.85f));
            guardButton = MakeButton(root, "Guard", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-360f, 130f), new Vector2(160f, 160f), new Color(0.2f, 0.35f, 0.5f, 0.85f));
            dodgeButton = MakeButton(root, "Dodge", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-370f, 360f), new Vector2(160f, 160f), new Color(0.25f, 0.5f, 0.3f, 0.85f));
            skillButton = MakeButton(root, "Wind", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 400f), new Vector2(160f, 160f), new Color(0.15f, 0.5f, 0.6f, 0.85f));
            skillLabel = skillButton.GetComponentInChildren<Text>();
            lockButton = MakeButton(root, "Lock", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-590f, 360f), new Vector2(140f, 140f), new Color(0.4f, 0.35f, 0.15f, 0.85f));
            lockLabel = lockButton.GetComponentInChildren<Text>();

            // Harvest button, bottom center, with hold progress.
            harvestPanel = MakePanel(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(460f, 120f), new Color(0.1f, 0.1f, 0.1f, 0.7f)).gameObject;
            harvestFill = MakePanel(harvestPanel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.9f, 0.7f, 0.25f, 0.9f));
            harvestButton = harvestPanel.AddComponent<HoldButton>();
            var hLabel = MakeText(harvestPanel.transform, "Hold to Harvest", 40, Color.white, TextAnchor.MiddleCenter);
            SetRect(hLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            harvestPanel.SetActive(false);

            // Harvest result toast, center.
            toastText = MakeText(root, "", 44, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter);
            SetRect(toastText.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 60f));

            BuildDebugPanel(root);

            // Build diagnostics readout, bottom left. Small, and it comes
            // out with the rest of the diagnostics once the loop is proven.
            diagText = MakeText(root, "", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerLeft);
            diagText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetRect(diagText.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(430f, 30f), new Vector2(840f, 40f));
        }

        // The developer panel: two rows of toggles and force buttons, two
        // rows of weapon buttons. Actions are polled through HoldButton
        // press edges in Update, the same input model as combat.
        private void BuildDebugPanel(Transform root)
        {
            dbgToggle = MakeButton(root, "DBG", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 130f), new Vector2(110f, 70f), new Color(0.2f, 0.2f, 0.25f, 0.8f));
            debugPanel = MakePanel(root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(330f, 330f), new Vector2(620f, 330f), new Color(0f, 0f, 0f, 0.55f)).gameObject;

            string[] labels =
            {
                "Hitbox", "Hurtbox", "Numbers", "NoCrit",
                "Reset", "Stagger", "Topple", "Stun", "Enrage",
                "LS", "HAM", "GS", "SNS", "DB",
            };
            debugActions = new[]
            {
                "hitbox", "hurtbox", "numbers", "nocrit",
                "reset", "stagger", "topple", "stun", "enrage",
                "longsword", "hammer", "greatsword", "swordshield", "dualblades",
            };
            debugButtons = new HoldButton[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                // Panel local coordinates run from its bottom left corner.
                // Row 0: indices 0 to 4 (toggles and reset). Row 1: indices
                // 5 to 8 (force buttons). Row 2: indices 9 to 13 (weapons).
                int row = i < 5 ? 0 : i < 9 ? 1 : 2;
                int col = i < 5 ? i : i < 9 ? i - 5 : i - 9;
                float x = 59f + col * 118f;
                float y = 285f - row * 90f;
                debugButtons[i] = MakeSmallButton(debugPanel.transform, labels[i], new Vector2(x, y), new Vector2(110f, 64f));
            }
            debugPanel.SetActive(DebugPanelStartsOpen);
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

        private MossHorn EngagedMonster()
        {
            if (combat == null) return null;
            if (Targeting.Current != null && !Targeting.Current.IsDefeated) return Targeting.Current;
            MossHorn best = null;
            float bestDist = 30f;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn m = MossHorn.All[i];
                if (m == null || m.IsDefeated) continue;
                float d = Vector3.Distance(combat.transform.position, m.transform.position);
                if (d < bestDist) { bestDist = d; best = m; }
            }
            return best;
        }

        private void RunDebugAction(string action)
        {
            switch (action)
            {
                case "hitbox": CombatDebug.ShowHitboxes = !CombatDebug.ShowHitboxes; break;
                case "hurtbox": CombatDebug.ShowHurtboxes = !CombatDebug.ShowHurtboxes; break;
                case "numbers": CombatDebug.ShowDamageNumbers = !CombatDebug.ShowDamageNumbers; break;
                case "nocrit": CombatDebug.ForceNoCrit = !CombatDebug.ForceNoCrit; break;
                case "longsword": case "hammer": case "greatsword": case "swordshield": case "dualblades":
                    if (combat != null) combat.EquipWeapon(action);
                    break;
                default:
                    MossHorn m = EngagedMonster();
                    if (m == null) break;
                    if (action == "reset") m.ResetFight();
                    else if (action == "stagger") m.ForceStagger();
                    else if (action == "topple") m.ForceKnockdown();
                    else if (action == "stun") m.ForceStun();
                    else if (action == "enrage") m.ForceEnrage();
                    break;
            }
        }

        private void Update()
        {
            if (combat == null) return;

            if (dbgToggle != null && dbgToggle.PressedThisFrame)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
            }
            if (debugPanel.activeSelf && debugButtons != null)
            {
                for (int i = 0; i < debugButtons.Length; i++)
                {
                    if (debugButtons[i] != null && debugButtons[i].PressedThisFrame) RunDebugAction(debugActions[i]);
                }
            }

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

            if (skillLabel != null)
            {
                skillLabel.text = combat.SkillCooldownRemaining > 0.05f
                    ? Mathf.CeilToInt(combat.SkillCooldownRemaining).ToString()
                    : (combat.Weapon != null && combat.Weapon.SpecialIsProjectile ? "Wind" : "Skill");
            }
            if (lockLabel != null)
            {
                lockLabel.text = Targeting.Locked ? "LOCKED" : "Lock";
                lockLabel.fontSize = Targeting.Locked ? 30 : 40;
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
                creatureLabel.text = (Targeting.Locked && Targeting.Current == engaged ? "LOCK  " : "")
                    + engaged.DisplayName + state;
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
