using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    public static class DNAPuzzleSampleData
    {
        public static List<PlanetData> CreateSamplePlanets()
        {
            var planets = new List<PlanetData>();

            // 1. Pyros-9 (Volkanik Gezegen)
            var pyros = ScriptableObject.CreateInstance<PlanetData>();
            pyros.planetId = "pyros_9";
            pyros.planetName = "Pyros-9 (Magma Dünyası)";
            pyros.description = "Akkor lav okyanusları ve sülfür püskürmeleri.";
            pyros.themeColor = new Color(0.95f, 0.40f, 0.15f);
            pyros.strandSlotCount = 4;
            pyros.maxEnergyBudget = 11;
            pyros.maxAllowedInstability = 1;
            pyros.requirements = new List<StatRequirement>
            {
                new StatRequirement(StatType.HeatResistance, 5),
                new StatRequirement(StatType.PressureTolerance, 2)
            };
            planets.Add(pyros);

            // 2. Cryo-Kore (Buzul Gezegeni)
            var cryo = ScriptableObject.CreateInstance<PlanetData>();
            cryo.planetId = "cryo_kore";
            cryo.planetName = "Cryo-Kore (Buzul Dünyası)";
            cryo.description = "Mutlak sıfıra yakın dondurucu fırtınalar.";
            cryo.themeColor = new Color(0.15f, 0.75f, 0.95f);
            cryo.strandSlotCount = 4;
            cryo.maxEnergyBudget = 10;
            cryo.maxAllowedInstability = 1;
            cryo.requirements = new List<StatRequirement>
            {
                new StatRequirement(StatType.ColdResistance, 5),
                new StatRequirement(StatType.OxygenEfficiency, 3)
            };
            planets.Add(cryo);

            // 3. Toxis-Prime (Radyoaktif Asit Dünyası)
            var toxis = ScriptableObject.CreateInstance<PlanetData>();
            toxis.planetId = "toxis_prime";
            toxis.planetName = "Toxis-Prime (Asit & Radyasyon)";
            toxis.description = "Ölümcül asit yağmurları ve gama radyasyonu.";
            toxis.themeColor = new Color(0.45f, 0.95f, 0.25f);
            toxis.strandSlotCount = 4;
            toxis.maxEnergyBudget = 12;
            toxis.maxAllowedInstability = 2;
            toxis.requirements = new List<StatRequirement>
            {
                new StatRequirement(StatType.ToxicityFilter, 4),
                new StatRequirement(StatType.RadiationShield, 4),
                new StatRequirement(StatType.PressureTolerance, 1)
            };
            planets.Add(toxis);

            return planets;
        }

        public static List<GeneData> CreateSampleGenes()
        {
            var genes = new List<GeneData>();

            // --- 🛡️ ZIRH & DERİ (Kompakt ve Taşmasız İsimler) ---
            genes.Add(CreateGene("gene_thermal_chitin", "Termal Kitin", "Lav dayanıklı volkanik kabuk.",
                GeneCategory.Exoskeleton, GeneRarity.Common, NucleotideType.A, NucleotideType.G, 3, new Color(1f, 0.42f, 0.12f),
                (StatType.HeatResistance, 3), (StatType.PressureTolerance, 1)));

            genes.Add(CreateGene("gene_magma_carapace", "Magma Kabuğu", "Süper yoğun volkanik kayaç.",
                GeneCategory.Exoskeleton, GeneRarity.Rare, NucleotideType.C, NucleotideType.A, 3, new Color(0.92f, 0.28f, 0.15f),
                (StatType.HeatResistance, 4)));

            genes.Add(CreateGene("gene_cryo_fur", "Kriyo Kürk", "Donma önleyici biyo-lifler.",
                GeneCategory.Exoskeleton, GeneRarity.Common, NucleotideType.G, NucleotideType.T, 2, new Color(0.2f, 0.75f, 1f),
                (StatType.ColdResistance, 3), (StatType.PressureTolerance, 1)));

            genes.Add(CreateGene("gene_lead_skin", "Kurşun Deri", "Yoğun radyasyon kalkanı.",
                GeneCategory.Exoskeleton, GeneRarity.Rare, NucleotideType.C, NucleotideType.G, 3, new Color(0.55f, 0.6f, 0.72f),
                (StatType.RadiationShield, 3), (StatType.PressureTolerance, 1)));

            // --- 🫁 SOLUNUM & ORGAN ---
            genes.Add(CreateGene("gene_magma_sac", "Magma Kesesi", "Isıyı kimyasal güce çevirir.",
                GeneCategory.Metabolic, GeneRarity.Common, NucleotideType.C, NucleotideType.T, 2, new Color(1f, 0.32f, 0.32f),
                (StatType.HeatResistance, 2), (StatType.ToxicityFilter, 1)));

            genes.Add(CreateGene("gene_acid_gills", "Asit Solungacı", "Zehirden oksijen süzer.",
                GeneCategory.Metabolic, GeneRarity.Common, NucleotideType.T, NucleotideType.G, 2, new Color(0.2f, 0.95f, 0.45f),
                (StatType.ToxicityFilter, 3), (StatType.OxygenEfficiency, 1)));

            genes.Add(CreateGene("gene_metal_liver", "Metal Karaciğer", "Asit ve zehirleri nötralize eder.",
                GeneCategory.Metabolic, GeneRarity.Rare, NucleotideType.G, NucleotideType.T, 3, new Color(0.35f, 0.9f, 0.35f),
                (StatType.ToxicityFilter, 4)));

            genes.Add(CreateGene("gene_oxygen_sac", "Oksijen Deposu", "Seyrek gazları depolayan ciğer.",
                GeneCategory.Metabolic, GeneRarity.Common, NucleotideType.T, NucleotideType.T, 2, new Color(0.15f, 0.9f, 0.85f),
                (StatType.OxygenEfficiency, 3)));

            genes.Add(CreateGene("gene_ammonia_neutralizer", "Amonyak Enzimi", "Asitliği dengeleyen reaktör.",
                GeneCategory.Metabolic, GeneRarity.Rare, NucleotideType.A, NucleotideType.T, 3, new Color(0.45f, 0.95f, 0.65f),
                (StatType.ToxicityFilter, 2), (StatType.OxygenEfficiency, 2)));

            // --- 🩸 HÜCRE & KAN ---
            genes.Add(CreateGene("gene_antifreeze_blood", "Antifriz Kanı", "Kanın donmasını engeller.",
                GeneCategory.Cellular, GeneRarity.Rare, NucleotideType.T, NucleotideType.C, 3, new Color(0.1f, 0.65f, 1f),
                (StatType.ColdResistance, 4), (StatType.OxygenEfficiency, 1)));

            genes.Add(CreateGene("gene_thermal_blubber", "İzolasyon Yağı", "Gövde ısısını sabit tutar.",
                GeneCategory.Cellular, GeneRarity.Common, NucleotideType.G, NucleotideType.A, 2, new Color(0.3f, 0.85f, 1f),
                (StatType.ColdResistance, 2), (StatType.PressureTolerance, 2)));

            genes.Add(CreateGene("gene_plasma_capillaries", "Plazma Damarı", "Isı iletimini hızlandırır.",
                GeneCategory.Cellular, GeneRarity.Rare, NucleotideType.A, NucleotideType.A, 2, new Color(1f, 0.5f, 0.2f),
                (StatType.HeatResistance, 3)));

            // --- 🦾 İSKELET & KAS ---
            genes.Add(CreateGene("gene_hydro_skeleton", "Hidro-İskelet", "Ezici basınca karşı sıvı destek.",
                GeneCategory.Structural, GeneRarity.Common, NucleotideType.C, NucleotideType.A, 2, new Color(0.9f, 0.8f, 0.25f),
                (StatType.PressureTolerance, 3), (StatType.HeatResistance, 1)));

            genes.Add(CreateGene("gene_carbon_fibers", "Karbon Kemik", "Ezici yerçekiminde kırılmaz.",
                GeneCategory.Structural, GeneRarity.Epic, NucleotideType.G, NucleotideType.C, 3, new Color(0.95f, 0.85f, 0.15f),
                (StatType.PressureTolerance, 4)));

            genes.Add(CreateGene("gene_barophilic_membrane", "Barofilik Zar", "Basınç altında esnek doku.",
                GeneCategory.Structural, GeneRarity.Rare, NucleotideType.T, NucleotideType.A, 2, new Color(0.8f, 0.9f, 0.35f),
                (StatType.PressureTolerance, 2), (StatType.OxygenEfficiency, 2)));

            // --- ☢️ RADYASYON & KOZMİK ---
            genes.Add(CreateGene("gene_radiation_melanin", "Radyotrof Deri", "Gama ışınlarını enerjiye çevirir.",
                GeneCategory.Cosmic, GeneRarity.Rare, NucleotideType.A, NucleotideType.C, 3, new Color(0.75f, 0.35f, 1f),
                (StatType.RadiationShield, 4), (StatType.ColdResistance, 1)));

            genes.Add(CreateGene("gene_magnetic_gland", "Manyetik Kalkan", "Kozmik rüzgarları saptırır.",
                GeneCategory.Cosmic, GeneRarity.Epic, NucleotideType.C, NucleotideType.T, 3, new Color(0.9f, 0.3f, 1f),
                (StatType.RadiationShield, 3), (StatType.ToxicityFilter, 1)));

            genes.Add(CreateGene("gene_crystal_scales", "Gama Pulu", "Gama parçacıklarını yansıtır.",
                GeneCategory.Cosmic, GeneRarity.Rare, NucleotideType.G, NucleotideType.G, 2, new Color(0.65f, 0.45f, 0.9f),
                (StatType.RadiationShield, 3), (StatType.PressureTolerance, 1)));

            // --- ✨ KUANTUM JOKERLER ---
            genes.Add(CreateGene("gene_quantum_adaptor", "Kuantum Adaptör", "Her uçla bağlanan evrensel gen.",
                GeneCategory.Mutagen, GeneRarity.Legendary, NucleotideType.Universal, NucleotideType.Universal, 4, new Color(1f, 0.85f, 0.2f),
                (StatType.HeatResistance, 1), (StatType.ColdResistance, 1), (StatType.RadiationShield, 1), (StatType.ToxicityFilter, 1)));

            genes.Add(CreateGene("gene_bio_catalyst", "Astral Çekirdek", "Evrensel sol uçlu mutajen.",
                GeneCategory.Mutagen, GeneRarity.Epic, NucleotideType.Universal, NucleotideType.G, 3, new Color(0.2f, 1f, 0.75f),
                (StatType.ToxicityFilter, 3), (StatType.PressureTolerance, 2)));

            genes.Add(CreateGene("gene_resonant_catalyst", "Kozmik Heliks", "Evrensel sağ uçlu hızlandırıcı.",
                GeneCategory.Mutagen, GeneRarity.Epic, NucleotideType.A, NucleotideType.Universal, 3, new Color(1f, 0.3f, 0.65f),
                (StatType.RadiationShield, 3), (StatType.ColdResistance, 2)));

            return genes;
        }

        public static List<GeneData> CreateBrokenGenomesForPlanet(PlanetData planet)
        {
            var list = new List<GeneData>();
            string pId = planet != null ? planet.planetId.ToLower() : "pyros_9";

            if (pId.Contains("cryo"))
            {
                // CRYO-KORE (Buzul Gezegeni) - 4 Bozuk Genom
                // Gereksinimler: Soğuk >= 5, Oksijen >= 3, Bütçe <= 10
                list.Add(CreateBrokenGene("cryo_fur", "Kriyo Kürk", "Dondurucu soğuğa dayanıklı kıl kökleri.",
                    GeneCategory.Exoskeleton, GeneRarity.Common, NucleotideType.A, NucleotideType.T, 2, new Color(0.2f, 0.75f, 1f),
                    rungIndex: 1, requiredNuc: NucleotideType.G, defect: "Termal İzolasyon Hasarı: [C - ?] Bağı Kopuk", hint: "Sitozin (C) karşısına Guanin (G) gelmelidir.",
                    (StatType.ColdResistance, 3), (StatType.PressureTolerance, 1)));

                list.Add(CreateBrokenGene("antifreeze_blood", "Antifriz Kanı", "Hücre içi sıvı donmasını önleyen biyo-tuzlar.",
                    GeneCategory.Cellular, GeneRarity.Rare, NucleotideType.T, NucleotideType.G, 3, new Color(0.1f, 0.65f, 1f),
                    rungIndex: 2, requiredNuc: NucleotideType.A, defect: "Biyo-Antifriz Hatası: [T - ?] Bağı Kopuk", hint: "Timin (T) karşısına Adenin (A) gelmelidir.",
                    (StatType.ColdResistance, 3), (StatType.OxygenEfficiency, 1)));

                list.Add(CreateBrokenGene("oxygen_sac", "Oksijen Deposu", "Buzul fırtınalarında seyrek oksijeni yoğunlaştırır.",
                    GeneCategory.Metabolic, GeneRarity.Common, NucleotideType.G, NucleotideType.C, 2, new Color(0.15f, 0.9f, 0.85f),
                    rungIndex: 0, requiredNuc: NucleotideType.C, defect: "Hava Depolama Zarı Delik: [G - ?] Bağı Kopuk", hint: "Guanin (G) karşısına Sitozin (C) gelmelidir.",
                    (StatType.OxygenEfficiency, 2), (StatType.ColdResistance, 1)));

                list.Add(CreateBrokenGene("subzero_lungs", "Kutup Solungacı", "Kristalleşen atmosfer gazlarını süzen solunum kanadı.",
                    GeneCategory.Metabolic, GeneRarity.Rare, NucleotideType.C, NucleotideType.A, 2, new Color(0.35f, 0.85f, 0.95f),
                    rungIndex: 3, requiredNuc: NucleotideType.T, defect: "Kriyojenik Filtre Tıkalı: [A - ?] Bağı Kopuk", hint: "Adenin (A) karşısına Timin (T) gelmelidir.",
                    (StatType.OxygenEfficiency, 2), (StatType.PressureTolerance, 1)));
            }
            else if (pId.Contains("toxis"))
            {
                // TOXIS-PRIME (Asit & Radyasyon) - 4 Bozuk Genom
                // Gereksinimler: Zehir >= 4, Radyasyon >= 4, Basınç >= 1, Bütçe <= 12
                list.Add(CreateBrokenGene("lead_skin", "Kurşun Deri", "Gama ve asit parçacıklarını emen ağır pul tabakası.",
                    GeneCategory.Exoskeleton, GeneRarity.Rare, NucleotideType.A, NucleotideType.G, 3, new Color(0.55f, 0.6f, 0.72f),
                    rungIndex: 1, requiredNuc: NucleotideType.C, defect: "Gama Kalkanı Delik: [G - ?] Bağı Kopuk", hint: "Guanin (G) karşısına Sitozin (C) gelmelidir.",
                    (StatType.RadiationShield, 3), (StatType.PressureTolerance, 1)));

                list.Add(CreateBrokenGene("metal_liver", "Metal Karaciğer", "Asit ve ağır metalleri nötralize eden biyo-reaktör.",
                    GeneCategory.Metabolic, GeneRarity.Rare, NucleotideType.G, NucleotideType.T, 3, new Color(0.35f, 0.9f, 0.35f),
                    rungIndex: 2, requiredNuc: NucleotideType.T, defect: "Toksin Filtresi Aşınmış: [A - ?] Bağı Kopuk", hint: "Adenin (A) karşısına Timin (T) gelmelidir.",
                    (StatType.ToxicityFilter, 3)));

                list.Add(CreateBrokenGene("radiation_melanin", "Radyotrof Zar", "Radyasyon ışımasını faydalı hücresel enerjiye çevirir.",
                    GeneCategory.Cosmic, GeneRarity.Epic, NucleotideType.T, NucleotideType.A, 3, new Color(0.75f, 0.35f, 1f),
                    rungIndex: 0, requiredNuc: NucleotideType.A, defect: "Melanin Sentezi Hatalı: [T - ?] Bağı Kopuk", hint: "Timin (T) karşısına Adenin (A) gelmelidir.",
                    (StatType.RadiationShield, 3)));

                list.Add(CreateBrokenGene("acid_gills", "Asit Solungacı", "Ölümcül asit dumanlarından saf oksijen üretir.",
                    GeneCategory.Metabolic, GeneRarity.Common, NucleotideType.A, NucleotideType.C, 2, new Color(0.2f, 0.95f, 0.45f),
                    rungIndex: 3, requiredNuc: NucleotideType.G, defect: "Asit Emme Membranı Bozuk: [C - ?] Bağı Kopuk", hint: "Sitozin (C) karşısına Guanin (G) gelmelidir.",
                    (StatType.ToxicityFilter, 3), (StatType.OxygenEfficiency, 1)));
            }
            else
            {
                // PYROS-9 (Magma Dünyası) - 4 Bozuk Genom
                // Gereksinimler: Isı >= 5, Basınç >= 2, Bütçe <= 11
                list.Add(CreateBrokenGene("thermal_chitin", "Termal Kitin", "Lav akıntılarına karşı yüksek ısı dayanımlı volkanik zırh.",
                    GeneCategory.Exoskeleton, GeneRarity.Common, NucleotideType.A, NucleotideType.T, 3, new Color(1f, 0.42f, 0.12f),
                    rungIndex: 1, requiredNuc: NucleotideType.T, defect: "Termal İletkenlik Bozukluğu: [A - ?] Bağı Kopuk", hint: "Adenin (A) karşısına Timin (T) gelmelidir.",
                    (StatType.HeatResistance, 3), (StatType.PressureTolerance, 1)));

                list.Add(CreateBrokenGene("magma_carapace", "Magma Kabuğu", "Süper yoğunlaştırılmış magmatik kayaç dokusu.",
                    GeneCategory.Exoskeleton, GeneRarity.Rare, NucleotideType.T, NucleotideType.C, 3, new Color(0.92f, 0.28f, 0.15f),
                    rungIndex: 2, requiredNuc: NucleotideType.G, defect: "Volkanik Matris Çatlağı: [C - ?] Bağı Kopuk", hint: "Sitozin (C) karşısına Guanin (G) gelmelidir.",
                    (StatType.HeatResistance, 3)));

                list.Add(CreateBrokenGene("hydro_skeleton", "Hidro-İskelet", "Ezici magma okyanusu basıncını dağıtan hidrolik omurga.",
                    GeneCategory.Structural, GeneRarity.Common, NucleotideType.C, NucleotideType.G, 2, new Color(0.9f, 0.8f, 0.25f),
                    rungIndex: 0, requiredNuc: NucleotideType.G, defect: "Hidrolik Omurga Bozulması: [C - ?] Bağı Kopuk", hint: "Sitozin (C) karşısına Guanin (G) gelmelidir.",
                    (StatType.PressureTolerance, 2), (StatType.HeatResistance, 1)));

                list.Add(CreateBrokenGene("magma_sac", "Magma Kesesi", "Isı enerjisini metabolik güce çeviren biyo-çekirdek.",
                    GeneCategory.Metabolic, GeneRarity.Common, NucleotideType.G, NucleotideType.A, 2, new Color(1f, 0.32f, 0.32f),
                    rungIndex: 3, requiredNuc: NucleotideType.C, defect: "Endotermik Valf Arızası: [G - ?] Bağı Kopuk", hint: "Guanin (G) karşısına Sitozin (C) gelmelidir.",
                    (StatType.HeatResistance, 2), (StatType.ToxicityFilter, 1)));
            }

            return list;
        }

        public static List<NucleotideType> CreateRepairCandidatesForPlanet(PlanetData planet, IEnumerable<GeneData> brokenGenomes)
        {
            var candidates = new List<NucleotideType>();
            if (brokenGenomes != null)
            {
                foreach (var g in brokenGenomes)
                {
                    candidates.Add(g.requiredRepairNucleotide);
                }
            }
            candidates.Add(NucleotideType.A);
            candidates.Add(NucleotideType.T);
            candidates.Add(NucleotideType.C);
            candidates.Add(NucleotideType.G);

            // Rastgele Karıştır (Shuffle)
            for (int i = 0; i < candidates.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, candidates.Count);
                var temp = candidates[i];
                candidates[i] = candidates[rnd];
                candidates[rnd] = temp;
            }

            if (candidates.Count > 6)
            {
                candidates.RemoveRange(6, candidates.Count - 6);
            }
            return candidates;
        }

        private static GeneData CreateBrokenGene(string id, string name, string desc, GeneCategory cat, GeneRarity rarity,
            NucleotideType left, NucleotideType right, int cost, Color color, int rungIndex, NucleotideType requiredNuc,
            string defect, string hint, params (StatType stat, int val)[] stats)
        {
            var g = CreateGene(id, name, desc, cat, rarity, left, right, cost, color, stats);
            g.isBroken = true;
            g.isRepaired = false;
            g.brokenRungIndex = rungIndex;
            g.requiredRepairNucleotide = requiredNuc;
            g.defectDescription = defect;
            g.repairHint = hint;
            g.maxRepairAttempts = 3;
            g.remainingRepairAttempts = 3;
            g.isDeformed = false;
            return g;
        }

        private static GeneData CreateGene(string id, string name, string desc, GeneCategory cat, GeneRarity rarity,
            NucleotideType left, NucleotideType right, int cost, Color color, params (StatType stat, int val)[] stats)
        {
            var g = ScriptableObject.CreateInstance<GeneData>();
            g.geneId = id;
            g.geneName = name;
            g.description = desc;
            g.category = cat;
            g.rarity = rarity;
            g.leftConnector = left;
            g.rightConnector = right;
            g.energyCost = cost;
            g.customColor = color;
            g.statModifiers = new List<StatModifier>();
            foreach (var item in stats)
            {
                g.statModifiers.Add(new StatModifier(item.stat, item.val));
            }
            return g;
        }
    }
}
