using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    public static class DNAPuzzleSampleData
    {
        public static int ActiveCreatureIndex = 0;

        public static List<PlanetData> CreateSamplePlanets()
        {
            var planets = new List<PlanetData>();

            // 1. FABAUP (Kavrulmuş Kumullar)
            var fabaup = ScriptableObject.CreateInstance<PlanetData>();
            fabaup.planetId = "fabaup";
            fabaup.planetName = "FABAUP (Kavrulmuş Kumullar)";
            fabaup.description = "Deri: KRELL/KLOOT • Boy: En Az ZORP • Göz: En Fazla VESHI • Uzuv: En Az PLORT";
            fabaup.themeColor = new Color(0.95f, 0.45f, 0.15f);
            fabaup.strandSlotCount = 4;
            fabaup.maxEnergyBudget = 12;
            fabaup.maxAllowedInstability = 2;
            fabaup.requirements = new List<StatRequirement>
            {
                new StatRequirement(StatType.HeatResistance, 5),
                new StatRequirement(StatType.PressureTolerance, 2)
            };
            planets.Add(fabaup);

            // 2. SNIBBLEX (Donmuş ve Karanlık)
            var snibblex = ScriptableObject.CreateInstance<PlanetData>();
            snibblex.planetId = "snibblex";
            snibblex.planetName = "SNIBBLEX (Donmuş ve Karanlık)";
            snibblex.description = "Deri: KOSH/KIMBO • Boy: En Az ZEMBO • Göz: En Az VARQ • Uzuv: En Fazla PUNKO";
            snibblex.themeColor = new Color(0.20f, 0.78f, 0.98f);
            snibblex.strandSlotCount = 4;
            snibblex.maxEnergyBudget = 12;
            snibblex.maxAllowedInstability = 2;
            snibblex.requirements = new List<StatRequirement>
            {
                new StatRequirement(StatType.ColdResistance, 5),
                new StatRequirement(StatType.OxygenEfficiency, 3)
            };
            planets.Add(snibblex);

            // 3. OOMGARTH (Ezici Yerçekimli Bataklık)
            var oomgarth = ScriptableObject.CreateInstance<PlanetData>();
            oomgarth.planetId = "oomgarth";
            oomgarth.planetName = "OOMGARTH (Ağır Çekim Bataklığı)";
            oomgarth.description = "Deri: KIMBO/KUPPA • Boy: En Fazla ZEMBO • Göz: En Az VESHI • Uzuv: En Az PONDO";
            oomgarth.themeColor = new Color(0.35f, 0.92f, 0.45f);
            oomgarth.strandSlotCount = 4;
            oomgarth.maxEnergyBudget = 12;
            oomgarth.maxAllowedInstability = 2;
            oomgarth.requirements = new List<StatRequirement>
            {
                new StatRequirement(StatType.ToxicityFilter, 4),
                new StatRequirement(StatType.PressureTolerance, 4)
            };
            planets.Add(oomgarth);

            return planets;
        }

        public static List<GeneData> CreateSampleGenes()
        {
            var genes = new List<GeneData>();
            var allLacunaGenes = LacunaGeneticsDatabase.GetAllGenes();

            foreach (var def in allLacunaGenes)
            {
                var g = CreateGeneFromLacunaDef(def, isBroken: false, rungIndex: 0);
                genes.Add(g);
            }

            return genes;
        }

        public static List<GeneData> CreateBrokenGenomesForCreature(int creatureIdx)
        {
            var list = new List<GeneData>();
            var creatures = LacunaGeneticsDatabase.GetCreatures();
            int idx = Mathf.Clamp(creatureIdx, 0, creatures.Count - 1);
            ActiveCreatureIndex = idx;

            var c = creatures[idx];
            string[] startNames = { c.startGeneZ, c.startGeneV, c.startGeneK, c.startGeneP };

            for (int s = 0; s < 4; s++)
            {
                var def = LacunaGeneticsDatabase.GetGene(startNames[s]);
                if (def == null) continue;

                int brokenRung = (idx + s) % 4;
                var brokenGene = CreateGeneFromLacunaDef(def, isBroken: true, rungIndex: brokenRung);
                list.Add(brokenGene);
            }

            return list;
        }

        public static List<GeneData> CreateBrokenGenomesForPlanet(PlanetData planet)
        {
            var creatures = LacunaGeneticsDatabase.GetCreatures();
            int idx = Mathf.Clamp(ActiveCreatureIndex, 0, creatures.Count - 1);

            if (planet != null)
            {
                string pId = planet.planetId.ToLowerInvariant();
                string targetP = "FABAUP";
                if (pId.Contains("snibblex") || pId.Contains("cryo")) targetP = "SNIBBLEX";
                else if (pId.Contains("oomgarth") || pId.Contains("toxis")) targetP = "OOMGARTH";

                if (creatures[idx].targetPlanet != targetP)
                {
                    for (int i = 0; i < creatures.Count; i++)
                    {
                        if (creatures[i].targetPlanet == targetP)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
            }

            return CreateBrokenGenomesForCreature(idx);
        }

        public static List<NucleotideType> CreateRepairCandidatesForPlanet(PlanetData planet, IEnumerable<GeneData> brokenGenomes)
        {
            var candidates = new List<NucleotideType>();

            // Her zaman 4 ana nükleotid şeklini (●, ■, ▲, ◆) tepside bulundur
            candidates.Add(NucleotideType.A); // ● Daire
            candidates.Add(NucleotideType.T); // ■ Kare
            candidates.Add(NucleotideType.C); // ▲ Üçgen
            candidates.Add(NucleotideType.G); // ◆ Elmas

            if (brokenGenomes != null)
            {
                foreach (var g in brokenGenomes)
                {
                    if (candidates.Count < 6)
                    {
                        candidates.Add(g.requiredRepairNucleotide);
                    }
                }
            }

            while (candidates.Count < 6)
            {
                candidates.Add(NucleotideType.A);
            }

            return candidates;
        }

        public static GeneData CreateGeneFromLacunaDef(LacunaGeneDefinition def, bool isBroken, int rungIndex)
        {
            var g = ScriptableObject.CreateInstance<GeneData>();

            GeneCategory cat = GeneCategory.Structural;
            StatType primaryStat = StatType.PressureTolerance;
            switch (def.slot)
            {
                case LacunaSlotType.Z:
                    cat = GeneCategory.Structural;
                    primaryStat = StatType.PressureTolerance;
                    break;
                case LacunaSlotType.V:
                    cat = GeneCategory.Cosmic;
                    primaryStat = StatType.OxygenEfficiency;
                    break;
                case LacunaSlotType.K:
                    cat = GeneCategory.Exoskeleton;
                    primaryStat = StatType.HeatResistance;
                    break;
                case LacunaSlotType.P:
                    cat = GeneCategory.Metabolic;
                    primaryStat = StatType.ColdResistance;
                    break;
            }

            g.geneId = "lacuna_" + def.geneName.ToLowerInvariant();
            g.geneName = def.geneName;
            g.description = $"[SLOT {def.slot}] {def.traitDescriptionTR} ({def.pairCount} Çift)";
            g.category = cat;
            g.rarity = GeneRarity.Rare;
            g.energyCost = 2;
            g.customColor = def.displayColor;
            g.statModifiers = new List<StatModifier>
            {
                new StatModifier(primaryStat, 3),
                new StatModifier(StatType.ToxicityFilter, 2)
            };

            g.ApplyLacunaGeneDefinition(def, keepBrokenState: false);

            int clampedRung = Mathf.Clamp(rungIndex, 0, 3);
            NucleotideType topNuc = DNAHelper.ShapeToNucleotide(g.lacunaTopStrand[clampedRung]);
            NucleotideType reqBotNuc = DNAHelper.ShapeToNucleotide(g.lacunaBottomStrand[clampedRung]);

            string topSym = DNAHelper.GetNucleotideSymbol(topNuc);
            string botSym = DNAHelper.GetNucleotideSymbol(reqBotNuc);
            string topFull = DNAHelper.GetNucleotideFullName(topNuc);
            string botFull = DNAHelper.GetNucleotideFullName(reqBotNuc);

            g.isBroken = isBroken;
            g.isRepaired = !isBroken;
            g.brokenRungIndex = clampedRung;
            g.requiredRepairNucleotide = reqBotNuc;
            g.defectDescription = $"SLOT {def.slot} ({def.geneName}) Hasarı: [{topSym} - ?] Bağı Kopuk";
            g.repairHint = $"Eşlenik Kuralı: {topFull} karşısına her zaman {botFull} gelmelidir.";
            g.maxRepairAttempts = 3;
            g.remainingRepairAttempts = 3;
            g.isDeformed = false;

            return g;
        }
    }
}
