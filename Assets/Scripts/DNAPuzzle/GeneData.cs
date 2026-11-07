using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    [CreateAssetMenu(fileName = "NewGene", menuName = "DNA Puzzle/Gene Data")]
    public class GeneData : ScriptableObject
    {
        [Header("Temel Bilgiler")]
        public string geneId;
        public string geneName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public Color customColor = Color.white;

        [Header("Sınıflandırma")]
        public GeneCategory category = GeneCategory.Exoskeleton;
        public GeneRarity rarity = GeneRarity.Common;

        [Header("Bağlantı Uçları (DNA Bazları)")]
        [Tooltip("Sol uçtaki nükleotid bazı")]
        public NucleotideType leftConnector = NucleotideType.A;
        [Tooltip("Sağ uçtaki nükleotid bazı")]
        public NucleotideType rightConnector = NucleotideType.T;

        [Header("Metabolik Maliyet")]
        [Range(1, 5)]
        [Tooltip("Bu genin canlının kalbinden harcadığı metabolizma/enerji")]
        public int energyCost = 2;

        [Header("Kazandırdığı Özellikler")]
        public List<StatModifier> statModifiers = new List<StatModifier>();

        [Header("Genom Bütünlüğü ve Onarım (Hasarlı/Bozuk Durum)")]
        public bool isBroken = false;
        public bool isRepaired = false;
        public int brokenRungIndex = 1;
        public NucleotideType requiredRepairNucleotide = NucleotideType.T;
        public bool hasPlacedNucleotide = false;
        public NucleotideType currentPlacedNucleotide = NucleotideType.T;
        public string defectDescription = "";
        public string repairHint = "";

        [Header("Onarım Hakları ve Deformasyon (Yanma Riski)")]
        public int maxRepairAttempts = 3;
        public int remainingRepairAttempts = 3;
        public bool isDeformed = false;
        public string deformedPhenotypeTitle = "Kaotik Bozulma";

        public void ResetRepairAttempts()
        {
            remainingRepairAttempts = maxRepairAttempts;
            isDeformed = false;
        }

        public void MarkDeformed()
        {
            isDeformed = true;
            remainingRepairAttempts = 0;
            isRepaired = false;
            isBroken = true;
        }

        public int GetStatValue(StatType stat)
        {
            if (statModifiers == null) return 0;
            int total = 0;
            for (int i = 0; i < statModifiers.Count; i++)
            {
                if (statModifiers[i].stat == stat)
                {
                    total += statModifiers[i].value;
                }
            }
            return total;
        }

        public string GetStatsSummary()
        {
            if (statModifiers == null || statModifiers.Count == 0)
                return "Özellik yok";

            var parts = new List<string>();
            foreach (var mod in statModifiers)
            {
                string sign = mod.value >= 0 ? "+" : "";
                string shortName = "";
                switch (mod.stat)
                {
                    case StatType.HeatResistance: shortName = "Isı"; break;
                    case StatType.ColdResistance: shortName = "Soğuk"; break;
                    case StatType.RadiationShield: shortName = "Rad"; break;
                    case StatType.ToxicityFilter: shortName = "Zehir"; break;
                    case StatType.PressureTolerance: shortName = "Basınç"; break;
                    case StatType.OxygenEfficiency: shortName = "Oks"; break;
                    default: shortName = mod.stat.ToString(); break;
                }
                parts.Add($"{sign}{mod.value} {shortName}");
            }
            return string.Join("  ", parts);
        }
    }
}
