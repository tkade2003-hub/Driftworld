using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-009: monster body parts. Each part is a sphere volume carried as
    // data on the monster (local offset and radius, scaled with the
    // creature), tested geometrically against weapon arcs during active
    // frames. This is the project's first hitbox system, and it follows the
    // codebase idiom set by WindLane.Contains and the Wind Spear distance
    // checks: deterministic shape tests in code, no physics callbacks, no
    // colliders on creatures, and therefore no new stripping surface (see
    // the Stage 1 audit, Physics dependencies).
    public enum PartId { Head, Neck, Torso, ForelegL, ForelegR, HindlegL, HindlegR, Tail }

    public class HurtboxPart
    {
        public PartId Id;
        public Vector3 LocalOffset;   // unscaled, monster local space, forward is +Z
        public float Radius;          // unscaled
        public float PhysMult;
        public float ElemMult;
        public float BreakThreshold;  // part damage pool for a break, 0 means unbreakable
        public float DamagePool;
        public float SeverPool;       // Cut damage only, tail only
        public bool Broken;
        public bool Severed;
        public MossHorn Owner;

        public bool Inert => Severed;
        public float EffectivePhysMult => PhysMult * (Broken ? 1f + Tuning.Parts.broken_bonus : 1f);

        public Vector3 WorldCenter
        {
            get
            {
                Vector3 scaled = LocalOffset * Owner.PartScale;
                return Owner.transform.position + Owner.PartCenterOffset
                    + Owner.transform.rotation * scaled;
            }
        }

        public float WorldRadius => Radius * Owner.PartScale;
    }

    public class HurtboxSet : MonoBehaviour
    {
        public readonly List<HurtboxPart> Parts = new List<HurtboxPart>();
        private readonly List<GameObject> vizSpheres = new List<GameObject>();
        private MossHorn owner;
        private bool vizBuilt;

        public void Build(MossHorn monster)
        {
            owner = monster;
            Parts.Clear();
            Add(PartId.Head, new Vector3(0f, 1.35f, 1.55f), 0.55f, Tuning.Parts.head_phys, Tuning.Parts.head_elem, Tuning.Parts.head_break);
            Add(PartId.Neck, new Vector3(0f, 1.20f, 1.00f), 0.50f, Tuning.Parts.neck_phys, Tuning.Parts.neck_elem, Tuning.Parts.neck_break);
            Add(PartId.Torso, new Vector3(0f, 1.10f, 0f), 0.95f, Tuning.Parts.torso_phys, Tuning.Parts.torso_elem, Tuning.Parts.torso_break);
            Add(PartId.ForelegL, new Vector3(-0.45f, 0.45f, 0.75f), 0.42f, Tuning.Parts.foreleg_phys, Tuning.Parts.foreleg_elem, Tuning.Parts.foreleg_break);
            Add(PartId.ForelegR, new Vector3(0.45f, 0.45f, 0.75f), 0.42f, Tuning.Parts.foreleg_phys, Tuning.Parts.foreleg_elem, Tuning.Parts.foreleg_break);
            Add(PartId.HindlegL, new Vector3(-0.45f, 0.45f, -0.75f), 0.42f, Tuning.Parts.hindleg_phys, Tuning.Parts.hindleg_elem, Tuning.Parts.hindleg_break);
            Add(PartId.HindlegR, new Vector3(0.45f, 0.45f, -0.75f), 0.42f, Tuning.Parts.hindleg_phys, Tuning.Parts.hindleg_elem, Tuning.Parts.hindleg_break);
            var tail = Add(PartId.Tail, new Vector3(0f, 1.15f, -1.50f), 0.45f, Tuning.Parts.tail_phys, Tuning.Parts.tail_elem, 0f);
            tail.SeverPool = 0f;
        }

        private HurtboxPart Add(PartId id, Vector3 offset, float radius, float phys, float elem, float breakThreshold)
        {
            var p = new HurtboxPart
            {
                Id = id,
                LocalOffset = offset,
                Radius = radius,
                PhysMult = phys,
                ElemMult = elem,
                BreakThreshold = breakThreshold,
                Owner = owner,
            };
            Parts.Add(p);
            return p;
        }

        public HurtboxPart Get(PartId id)
        {
            for (int i = 0; i < Parts.Count; i++) if (Parts[i].Id == id) return Parts[i];
            return null;
        }

        public void ResetParts()
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                Parts[i].DamagePool = 0f;
                Parts[i].SeverPool = 0f;
                Parts[i].Broken = false;
                Parts[i].Severed = false;
            }
        }

        // Records damage against a part. Returns 1 when this hit breaks the
        // part, 2 when it severs the tail, 0 otherwise.
        public int RecordHit(HurtboxPart part, DamageResult r, DamageKind kind)
        {
            part.DamagePool += r.Total;
            if (part.Id == PartId.Tail && kind == DamageKind.Cut && !part.Severed)
            {
                part.SeverPool += r.Total;
                if (part.SeverPool >= Tuning.Parts.tail_sever)
                {
                    part.Severed = true;
                    return 2;
                }
            }
            if (!part.Broken && part.BreakThreshold > 0f && part.DamagePool >= part.BreakThreshold)
            {
                part.Broken = true;
                return 1;
            }
            return 0;
        }

        // The weapon arc test. Origin is the hero chest; a part is hit when
        // its sphere reaches inside step range and, unless the hero is
        // standing inside the part, inside the facing cone.
        public static bool TestPart(Vector3 origin, Vector3 facing, AttackStep step, HurtboxPart part, out float dist)
        {
            Vector3 center = part.WorldCenter;
            Vector3 to = center - origin;
            dist = to.magnitude;
            if (dist > step.Range + part.WorldRadius) return false;
            Vector3 flat = to; flat.y = 0f;
            if (flat.magnitude > 1.0f)
            {
                float angle = Vector3.Angle(facing, flat.normalized);
                if (angle > step.ArcDeg * 0.5f) return false;
            }
            return true;
        }

        // Debug visualization: one translucent sphere per part, following
        // the live world centers. Built lazily, shown only while the
        // hurtbox toggle is on.
        private void Update()
        {
            bool show = CombatDebug.ShowHurtboxes && owner != null && !owner.IsDefeated;
            if (show && !vizBuilt) BuildViz();
            for (int i = 0; i < vizSpheres.Count; i++)
            {
                var go = vizSpheres[i];
                go.SetActive(show);
                if (!show) continue;
                var part = Parts[i];
                go.transform.position = part.WorldCenter;
                float d = part.WorldRadius * 2f;
                go.transform.localScale = new Vector3(d, d, d);
                var rend = go.GetComponent<Renderer>();
                if (part.Severed) rend.material.color = new Color(0.3f, 0.3f, 0.3f, 0.25f);
                else if (part.Broken) rend.material.color = new Color(1f, 0.55f, 0.15f, 0.4f);
                else rend.material.color = new Color(0.2f, 0.9f, 0.8f, 0.3f);
            }
        }

        private void BuildViz()
        {
            vizBuilt = true;
            for (int i = 0; i < Parts.Count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "HurtboxViz_" + Parts[i].Id;
                Object.Destroy(go.GetComponent<Collider>());
                var mat = Diag.SafeMaterial(new Color(0.2f, 0.9f, 0.8f, 0.3f));
                // Standard shader transparent mode, so the spheres read as
                // overlays rather than solid balls hiding the monster.
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                go.GetComponent<Renderer>().material = mat;
                go.SetActive(false);
                vizSpheres.Add(go);
            }
        }
    }
}
