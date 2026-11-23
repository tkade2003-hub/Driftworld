using System;
using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-005: the loot ledger for the slice. Every harvest grant is one
    // transaction with a stable identity per creature, so a retry or a
    // repeated callback can never grant twice. Drop rolls use a seeded
    // generator so a run is reproducible, per the slice build order.
    public class LootBank : MonoBehaviour
    {
        public static LootBank Instance { get; private set; }

        public int Shards { get; private set; }
        public int Hide { get; private set; }
        public int Meat { get; private set; }
        public int ElderHide { get; private set; }
        public int Horn { get; private set; }
        public int Kills { get; private set; }
        public int Harvests { get; private set; }
        public int ElderHornMisses { get; private set; }

        public event Action OnChanged;

        private readonly HashSet<string> grantedTransactions = new HashSet<string>();
        private System.Random rng;

        private void Awake()
        {
            Instance = this;
            rng = new System.Random(1337);
        }

        public void RegisterKill()
        {
            Kills++;
            OnChanged?.Invoke();
        }

        // Grants the drop table for one defeated creature. Returns false and
        // grants nothing if this creature was already granted.
        public bool GrantHarvest(MossHorn creature, out string summary)
        {
            summary = "";
            if (creature == null) return false;
            if (!grantedTransactions.Add(creature.TransactionId)) return false;

            var parts = new List<string>();
            if (creature.IsElder)
            {
                int elderHide = rng.Next(3, 6);
                ElderHide += elderHide;
                parts.Add(elderHide + " Elder Hide");

                bool horn = ElderHornMisses >= Tuning.Harvest.pity_max
                    || rng.NextDouble() * 100.0 < Tuning.Harvest.horn_rate_pct;
                if (horn)
                {
                    Horn += 1;
                    ElderHornMisses = 0;
                    parts.Add("1 Horn");
                }
                else
                {
                    ElderHornMisses++;
                }

                int shards = rng.Next(40, 61);
                Shards += shards;
                parts.Add(shards + " Shards");
            }
            else
            {
                int hide = rng.Next(2, 4);
                Hide += hide;
                parts.Add(hide + " Hide");

                int meat = rng.Next(1, 3);
                Meat += meat;
                parts.Add(meat + " Meat");

                int shards = rng.Next(8, 15);
                Shards += shards;
                parts.Add(shards + " Shards");
            }

            Harvests++;
            summary = string.Join(", ", parts);
            OnChanged?.Invoke();
            return true;
        }
    }
}
