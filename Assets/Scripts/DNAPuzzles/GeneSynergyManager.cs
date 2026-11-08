using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    [System.Serializable]
    public class GeneSynergy
    {
        public string synergyId;
        public string synergyName;
        public string description;
        public string geneIdA;
        public string geneIdB;
        public StatType bonusStat;
        public int bonusValue;
        public int energyReduction;
        public Color auraColor;
    }

    /// <summary>
    /// Genler arasındaki gizli biyolojik kombinasyonları ve sinerjileri yönetir.
    /// Belirli genler aynı sarmalda bir araya geldiğinde ekstra stat bonusları
    /// ve enerji tasarrufu sağlar.
    /// </summary>
    public static class GeneSynergyManager
    {
        private static List<GeneSynergy> allSynergies;

        public static IReadOnlyList<GeneSynergy> AllSynergies
        {
            get
            {
                if (allSynergies == null) InitializeSynergies();
                return allSynergies;
            }
        }

        private static void InitializeSynergies()
        {
            allSynergies = new List<GeneSynergy>
            {
                new GeneSynergy
                {
                    synergyId = "syn_plasma_resonance",
                    synergyName = "🔥 Plazma Rezonansı",
                    description = "Termal Kitin + Plazma Damarı: Lav akışını hızlandırarak +3 Isı Direnci sağlar.",
                    geneIdA = "armor_thermal_chitin",
                    geneIdB = "blood_plasma_vessel",
                    bonusStat = StatType.HeatResistance,
                    bonusValue = 3,
                    energyReduction = 1,
                    auraColor = new Color(1f, 0.45f, 0.1f)
                },
                new GeneSynergy
                {
                    synergyId = "syn_cryo_armor",
                    synergyName = "❄️ Kriyojenik Zırh",
                    description = "Kriyo Kürk + İzolasyon Yağı: Hücre zarlarını dondurarak +3 Soğuk Direnci sağlar.",
                    geneIdA = "armor_cryo_fur",
                    geneIdB = "blood_insulation_lipid",
                    bonusStat = StatType.ColdResistance,
                    bonusValue = 3,
                    energyReduction = 1,
                    auraColor = new Color(0.2f, 0.85f, 1f)
                },
                new GeneSynergy
                {
                    synergyId = "syn_bio_detox",
                    synergyName = "☣️ Biyo-Toksin Filtresi",
                    description = "Asit Solungacı + Metal Karaciğer: Ağır metalleri ayrıştırarak +3 Toksisite Filtresi sağlar.",
                    geneIdA = "resp_acid_gill",
                    geneIdB = "blood_metal_liver",
                    bonusStat = StatType.ToxicityFilter,
                    bonusValue = 3,
                    energyReduction = 1,
                    auraColor = new Color(0.35f, 1f, 0.3f)
                },
                new GeneSynergy
                {
                    synergyId = "syn_cosmic_shield",
                    synergyName = "⚛️ Kozmik Kalkan",
                    description = "Radyotrof Deri + Manyetik Kalkan: Manyetosfer oluşturarak +3 Radyasyon Kalkanı sağlar.",
                    geneIdA = "armor_radiotrophic_skin",
                    geneIdB = "armor_magnetic_shield",
                    bonusStat = StatType.RadiationShield,
                    bonusValue = 3,
                    energyReduction = 1,
                    auraColor = new Color(0.85f, 0.3f, 1f)
                },
                new GeneSynergy
                {
                    synergyId = "syn_titanium_skeleton",
                    synergyName = "🦴 Titanyum İskelet",
                    description = "Hidro-İskelet + Karbon Kemik: Sıvı hidroliği ile kemikleri güçlendirip +3 Basınç Dayanımı sağlar.",
                    geneIdA = "skel_hydro_frame",
                    geneIdB = "skel_carbon_bone",
                    bonusStat = StatType.PressureTolerance,
                    bonusValue = 3,
                    energyReduction = 1,
                    auraColor = new Color(0.25f, 1f, 0.85f)
                },
                new GeneSynergy
                {
                    synergyId = "syn_astral_overload",
                    synergyName = "✨ Astral Aşırı Yükleme",
                    description = "Astral Çekirdek + Kozmik Heliks: Saf biyolojik enerji! Tüm statlara +1 ekler ve -2 Enerji tasarrufu sağlar.",
                    geneIdA = "joker_astral_core",
                    geneIdB = "joker_cosmic_helix",
                    bonusStat = StatType.HeatResistance, // Kod içinde tüm statlara dağıtılır
                    bonusValue = 1,
                    energyReduction = 2,
                    auraColor = new Color(1f, 0.9f, 0.25f)
                }
            };
        }

        public static List<GeneSynergy> FindActiveSynergies(GeneData[] strand)
        {
            var active = new List<GeneSynergy>();
            if (strand == null || strand.Length == 0) return active;

            if (allSynergies == null) InitializeSynergies();

            // Sarmaldaki tüm mevcut gen kimliklerini topla
            var presentGenes = new HashSet<string>();
            foreach (var g in strand)
            {
                if (g != null && !string.IsNullOrEmpty(g.geneId))
                {
                    presentGenes.Add(g.geneId);
                }
            }

            foreach (var syn in allSynergies)
            {
                if (presentGenes.Contains(syn.geneIdA) && presentGenes.Contains(syn.geneIdB))
                {
                    active.Add(syn);
                }
            }

            return active;
        }
    }
}
