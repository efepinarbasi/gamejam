using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Lacuna Genetics GDD: 4 Nükleotid Şekli.
    /// Eşleşme Kuralı:
    /// ● (Circle) her zaman ■ (Square) ile eşleşir.
    /// ▲ (Triangle) her zaman ◆ (Diamond) ile eşleşir.
    /// </summary>
    public enum LacunaShape
    {
        None = 0,       // Eksik baz (Boşluk / Gap)
        Circle = 1,     // ●
        Triangle = 2,   // ▲
        Square = 3,     // ■
        Diamond = 4     // ◆
    }

    /// <summary>
    /// 4 Gen Slotu:
    /// Z (4 Çift - Boy), V (6 Çift - Göz), K (8 Çift - Deri Rengi), P (10 Çift - Uzuv)
    /// </summary>
    public enum LacunaSlotType
    {
        Z = 0,
        V = 1,
        K = 2,
        P = 3
    }

    public enum LacunaQteSpeed
    {
        Slow = 0,
        Medium = 1,
        Fast = 2,
        Fastest = 3
    }

    [Serializable]
    public class LacunaGeneDefinition
    {
        public LacunaSlotType slot;
        public int pairCount;
        public string geneName;
        public LacunaShape[] topStrand;
        public LacunaShape[] bottomStrand;
        public string bodyTraitEN;
        public string bodyTraitTR;
        public int rank; // K slotunda rank yoktur (-1)

        public string traitDescriptionTR => bodyTraitTR;

        public Color displayColor
        {
            get
            {
                switch (geneName)
                {
                    case "KRELL": return new Color(1.0f, 0.50f, 0.15f);
                    case "KUPPA": return new Color(1.0f, 0.88f, 0.20f);
                    case "KOSH": return new Color(0.92f, 0.96f, 1.0f);
                    case "KIMBO": return new Color(0.15f, 0.90f, 0.85f);
                    case "KLOOT": return new Color(0.68f, 0.32f, 1.0f);
                }
                switch (slot)
                {
                    case LacunaSlotType.Z: return new Color(0.25f, 0.82f, 1.0f);
                    case LacunaSlotType.V: return new Color(0.30f, 0.95f, 0.55f);
                    case LacunaSlotType.K: return new Color(1.0f, 0.58f, 0.22f);
                    case LacunaSlotType.P: return new Color(0.78f, 0.42f, 1.0f);
                    default: return Color.cyan;
                }
            }
        }

        public string FormatTopSequence()
        {
            if (topStrand == null) return "";
            var parts = new string[topStrand.Length];
            for (int i = 0; i < topStrand.Length; i++) parts[i] = LacunaGeneticsDatabase.GetSymbol(topStrand[i]);
            return string.Join(" ", parts);
        }

        public string FormatBottomSequence()
        {
            if (bottomStrand == null) return "";
            var parts = new string[bottomStrand.Length];
            for (int i = 0; i < bottomStrand.Length; i++) parts[i] = LacunaGeneticsDatabase.GetSymbol(bottomStrand[i]);
            return string.Join(" ", parts);
        }
    }

    [Serializable]
    public class LacunaPlanetDefinition
    {
        public string planetName;
        public string subtitleTR;
        public string rulebookReasonTR;
        public string skinRuleTR;
        public string heightRuleTR;
        public string eyesRuleTR;
        public string limbsRuleTR;
        public string[] acceptableK;
        public string[] acceptableZ;
        public string[] acceptableV;
        public string[] acceptableP;
        public Color themeColor;

        public bool IsGeneAcceptable(LacunaSlotType slot, string geneName)
        {
            if (string.IsNullOrEmpty(geneName)) return false;
            string[] list = null;
            switch (slot)
            {
                case LacunaSlotType.Z: list = acceptableZ; break;
                case LacunaSlotType.V: list = acceptableV; break;
                case LacunaSlotType.K: list = acceptableK; break;
                case LacunaSlotType.P: list = acceptableP; break;
            }
            if (list == null) return false;
            for (int i = 0; i < list.Length; i++)
            {
                if (string.Equals(list[i], geneName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public string GetRuleForSlot(LacunaSlotType slot)
        {
            switch (slot)
            {
                case LacunaSlotType.Z: return heightRuleTR;
                case LacunaSlotType.V: return eyesRuleTR;
                case LacunaSlotType.K: return skinRuleTR;
                case LacunaSlotType.P: return limbsRuleTR;
                default: return "";
            }
        }
    }

    [Serializable]
    public struct LacunaGap
    {
        public LacunaSlotType slot;
        public int pairIndex;
        public bool missingTop;
        public bool missingBottom;

        public LacunaGap(LacunaSlotType slot, int pairIndex, bool missingTop, bool missingBottom)
        {
            this.slot = slot;
            this.pairIndex = pairIndex;
            this.missingTop = missingTop;
            this.missingBottom = missingBottom;
        }
    }

    [Serializable]
    public class LacunaCreatureRosterEntry
    {
        public int number; // 1..10
        public string creatureName;
        public string planetName;
        public string geneZ;
        public string geneV;
        public string geneK;
        public string geneP;
        public LacunaSlotType wrongSlot;
        public string[] acceptableFixes;
        public string fixDescriptionTR;
        public string trapNoteTR;
        public LacunaQteSpeed qteSpeed;
        public List<LacunaGap> gaps = new List<LacunaGap>();
        public string landlordIntroTR;
        public string landlordClueTR;

        public int creatureNumber => number;
        public string targetPlanet => planetName;
        public string startGeneZ => geneZ;
        public string startGeneV => geneV;
        public string startGeneK => geneK;
        public string startGeneP => geneP;

        public string GetInitialGeneName(LacunaSlotType slot)
        {
            switch (slot)
            {
                case LacunaSlotType.Z: return geneZ;
                case LacunaSlotType.V: return geneV;
                case LacunaSlotType.K: return geneK;
                case LacunaSlotType.P: return geneP;
                default: return geneZ;
            }
        }
    }

    /// <summary>
    /// Aktif yaratığın tek bir slotundaki (Z, V, K, P) düzenlenebilir çift zincir durumu.
    /// </summary>
    public class EditableGeneStrand
    {
        public LacunaSlotType slot;
        public int pairCount;
        public string originalGeneName;
        public LacunaShape[] top;
        public LacunaShape[] bottom;
        public bool[] wasTopMissing;
        public bool[] wasBottomMissing;

        public EditableGeneStrand(LacunaGeneDefinition def, List<LacunaGap> creatureGaps)
        {
            slot = def.slot;
            pairCount = def.pairCount;
            originalGeneName = def.geneName;
            top = new LacunaShape[pairCount];
            bottom = new LacunaShape[pairCount];
            wasTopMissing = new bool[pairCount];
            wasBottomMissing = new bool[pairCount];

            for (int i = 0; i < pairCount; i++)
            {
                top[i] = def.topStrand[i];
                bottom[i] = def.bottomStrand[i];
            }

            if (creatureGaps != null)
            {
                foreach (var g in creatureGaps)
                {
                    if (g.slot == slot && g.pairIndex >= 0 && g.pairIndex < pairCount)
                    {
                        if (g.missingTop)
                        {
                            top[g.pairIndex] = LacunaShape.None;
                            wasTopMissing[g.pairIndex] = true;
                        }
                        if (g.missingBottom)
                        {
                            bottom[g.pairIndex] = LacunaShape.None;
                            wasBottomMissing[g.pairIndex] = true;
                        }
                    }
                }
            }
        }

        public bool HasAnyMissing()
        {
            for (int i = 0; i < pairCount; i++)
            {
                if (top[i] == LacunaShape.None || bottom[i] == LacunaShape.None)
                    return true;
            }
            return false;
        }

        public int GetMissingCount()
        {
            int c = 0;
            for (int i = 0; i < pairCount; i++)
            {
                if (top[i] == LacunaShape.None) c++;
                if (bottom[i] == LacunaShape.None) c++;
            }
            return c;
        }

        public bool AreAllPairsValidComplements()
        {
            for (int i = 0; i < pairCount; i++)
            {
                if (!LacunaGeneticsDatabase.ArePartners(top[i], bottom[i]))
                    return false;
            }
            return true;
        }

        public int GetInvalidPairCount()
        {
            int c = 0;
            for (int i = 0; i < pairCount; i++)
            {
                if (top[i] != LacunaShape.None && bottom[i] != LacunaShape.None)
                {
                    if (!LacunaGeneticsDatabase.ArePartners(top[i], bottom[i]))
                        c++;
                }
            }
            return c;
        }
    }

    public static class LacunaGeneticsDatabase
    {
        private static List<LacunaGeneDefinition> allGenes;
        private static List<LacunaPlanetDefinition> allPlanets;
        private static List<LacunaCreatureRosterEntry> allCreatures;

        private static Dictionary<LacunaShape, Texture2D> shapeIcons;

        public static LacunaShape GetPartner(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return LacunaShape.Square;
                case LacunaShape.Square: return LacunaShape.Circle;
                case LacunaShape.Triangle: return LacunaShape.Diamond;
                case LacunaShape.Diamond: return LacunaShape.Triangle;
                default: return LacunaShape.None;
            }
        }

        public static bool ArePartners(LacunaShape a, LacunaShape b)
        {
            if (a == LacunaShape.None || b == LacunaShape.None) return false;
            return GetPartner(a) == b;
        }

        public static string GetSymbol(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return "●";
                case LacunaShape.Triangle: return "▲";
                case LacunaShape.Square: return "■";
                case LacunaShape.Diamond: return "◆";
                default: return "?";
            }
        }

        public static string GetShapeNameTR(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return "● Daire";
                case LacunaShape.Triangle: return "▲ Üçgen";
                case LacunaShape.Square: return "■ Kare";
                case LacunaShape.Diamond: return "◆ Elmas";
                default: return "? Eksik";
            }
        }

        public static Color GetShapeColor(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return new Color(0.15f, 0.85f, 1.0f);   // Parlak Camgöbeği (●)
                case LacunaShape.Square: return new Color(1.0f, 0.32f, 0.45f);   // Mercan Kırmızısı (■)
                case LacunaShape.Triangle: return new Color(0.20f, 0.95f, 0.55f); // Zümrüt Yeşili (▲)
                case LacunaShape.Diamond: return new Color(1.0f, 0.78f, 0.15f);  // Kehribar Altın (◆)
                default: return new Color(0.55f, 0.62f, 0.72f);
            }
        }

        public static int GetSlotPairCount(LacunaSlotType slot)
        {
            switch (slot)
            {
                case LacunaSlotType.Z: return 4;
                case LacunaSlotType.V: return 6;
                case LacunaSlotType.K: return 8;
                case LacunaSlotType.P: return 10;
                default: return 4;
            }
        }

        public static string GetSlotHeaderTR(LacunaSlotType slot)
        {
            switch (slot)
            {
                case LacunaSlotType.Z: return "SLOT Z (4 Çift)";
                case LacunaSlotType.V: return "SLOT V (6 Çift)";
                case LacunaSlotType.K: return "SLOT K (8 Çift)";
                case LacunaSlotType.P: return "SLOT P (10 Çift)";
                default: return slot.ToString();
            }
        }

        public static Texture2D GetShapeIcon(LacunaShape shape)
        {
            if (shapeIcons == null) BuildProceduralShapeIcons();
            if (shapeIcons.TryGetValue(shape, out var tex) && tex != null)
                return tex;
            BuildProceduralShapeIcons();
            return shapeIcons.ContainsKey(shape) ? shapeIcons[shape] : null;
        }

        private static void BuildProceduralShapeIcons()
        {
            shapeIcons = new Dictionary<LacunaShape, Texture2D>();
            int size = 64;

            foreach (LacunaShape s in Enum.GetValues(typeof(LacunaShape)))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (x + 0.5f) / size * 2f - 1f; // -1..1
                        float ny = (y + 0.5f) / size * 2f - 1f; // -1..1
                        float alpha = 0f;

                        switch (s)
                        {
                            case LacunaShape.Circle:
                            {
                                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                                alpha = Mathf.Clamp01((0.78f - dist) * 18f);
                                break;
                            }
                            case LacunaShape.Square:
                            {
                                float maxAxis = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                                alpha = Mathf.Clamp01((0.70f - maxAxis) * 20f);
                                break;
                            }
                            case LacunaShape.Triangle:
                            {
                                // Yukarı bakan eşkenar üçgen
                                float ty = ny; // -0.70 .. +0.75
                                if (ty >= -0.68f && ty <= 0.76f)
                                {
                                    float progress = (0.76f - ty) / 1.44f; // tepede 0, tabanda 1
                                    float halfWidth = progress * 0.78f;
                                    float dx = halfWidth - Mathf.Abs(nx);
                                    alpha = Mathf.Clamp01(dx * 18f);
                                }
                                break;
                            }
                            case LacunaShape.Diamond:
                            {
                                // 4 köşeli keskin yıldız-elmas (■ ile asla karışmaz!)
                                float manhattan = Mathf.Abs(nx) + Mathf.Abs(ny);
                                float starPinch = Mathf.Abs(nx) * Mathf.Abs(ny) * 0.55f;
                                alpha = Mathf.Clamp01((0.86f - (manhattan + starPinch)) * 16f);
                                break;
                            }
                            case LacunaShape.None:
                            {
                                // Kesikli çember
                                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                                if (dist > 0.52f && dist < 0.72f)
                                {
                                    float angle = Mathf.Atan2(ny, nx);
                                    if (Mathf.Sin(angle * 6f) > 0f) alpha = 0.65f;
                                }
                                break;
                            }
                        }

                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                shapeIcons[s] = tex;
            }
        }

        public static List<LacunaGeneDefinition> GetAllGenes()
        {
            if (allGenes == null) InitializeDatabase();
            return allGenes;
        }

        public static List<LacunaGeneDefinition> GetGenesBySlot(LacunaSlotType slot)
        {
            if (allGenes == null) InitializeDatabase();
            return allGenes.FindAll(g => g.slot == slot);
        }

        public static LacunaGeneDefinition GetGeneByName(string name)
        {
            if (allGenes == null) InitializeDatabase();
            return allGenes.Find(g => string.Equals(g.geneName, name, StringComparison.OrdinalIgnoreCase));
        }

        public static LacunaGeneDefinition GetGene(string name) => GetGeneByName(name);
        public static LacunaPlanetDefinition GetPlanet(string name) => GetPlanetByName(name);
        public static List<LacunaPlanetDefinition> GetPlanets() => GetAllPlanets();
        public static List<LacunaCreatureRosterEntry> GetCreatures() => GetCreatureRoster();

        /// <summary>
        /// Verilen üst ve alt dizilim, slottaki 5 genden biriyle (ve tüm eşlenik kurallarıyla) tam eşleşiyorsa o geni döner.
        /// </summary>
        public static LacunaGeneDefinition MatchStrandToGene(LacunaSlotType slot, LacunaShape[] top, LacunaShape[] bottom)
        {
            if (top == null || bottom == null) return null;
            var slotGenes = GetGenesBySlot(slot);

            // Önce tüm çiftlerin dolu ve birbirinin doğru eşleniği olduğundan emin ol
            for (int i = 0; i < top.Length; i++)
            {
                if (!ArePartners(top[i], bottom[i]))
                    return null;
            }

            foreach (var g in slotGenes)
            {
                if (g.pairCount != top.Length) continue;
                bool match = true;
                for (int i = 0; i < g.pairCount; i++)
                {
                    if (g.topStrand[i] != top[i] || g.bottomStrand[i] != bottom[i])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return g;
            }
            return null;
        }

        /// <summary>
        /// Hasarlı (eksik bazlı) bir dizilimin eldeki mevcut bazlarına göre slottaki hangi genlerle uyuşabileceğini bulur.
        /// </summary>
        public static List<LacunaGeneDefinition> FindCompatibleGenesForDamagedStrand(LacunaSlotType slot, LacunaShape[] top, LacunaShape[] bottom)
        {
            var result = new List<LacunaGeneDefinition>();
            var slotGenes = GetGenesBySlot(slot);
            foreach (var g in slotGenes)
            {
                if (g.pairCount != top.Length) continue;
                bool ok = true;
                for (int i = 0; i < g.pairCount; i++)
                {
                    if (top[i] != LacunaShape.None && top[i] != g.topStrand[i]) { ok = false; break; }
                    if (bottom[i] != LacunaShape.None && bottom[i] != g.bottomStrand[i]) { ok = false; break; }
                }
                if (ok) result.Add(g);
            }
            return result;
        }

        /// <summary>
        /// İki gen arasındaki kaç çiftin (pozisyonun) farklı olduğunu hesaplar ("1 pair away", "2 pairs away").
        /// </summary>
        public static int CountPairDifferences(LacunaGeneDefinition a, LacunaGeneDefinition b)
        {
            if (a == null || b == null || a.pairCount != b.pairCount) return 99;
            int diff = 0;
            for (int i = 0; i < a.pairCount; i++)
            {
                if (a.topStrand[i] != b.topStrand[i]) diff++;
            }
            return diff;
        }

        public static List<LacunaPlanetDefinition> GetAllPlanets()
        {
            if (allPlanets == null) InitializeDatabase();
            return allPlanets;
        }

        public static LacunaPlanetDefinition GetPlanetByName(string name)
        {
            if (allPlanets == null) InitializeDatabase();
            return allPlanets.Find(p => string.Equals(p.planetName, name, StringComparison.OrdinalIgnoreCase));
        }

        public static List<LacunaCreatureRosterEntry> GetCreatureRoster()
        {
            if (allCreatures == null) InitializeDatabase();
            return allCreatures;
        }

        private static LacunaShape[] ParseStrand(string pattern)
        {
            // Semboller: C/● = Circle, T/▲ = Triangle, S/■ = Square, D/◆ = Diamond
            var list = new List<LacunaShape>();
            foreach (char ch in pattern)
            {
                switch (ch)
                {
                    case '●':
                    case 'C': list.Add(LacunaShape.Circle); break;
                    case '▲':
                    case 'T': list.Add(LacunaShape.Triangle); break;
                    case '■':
                    case 'S': list.Add(LacunaShape.Square); break;
                    case '◆':
                    case 'D': list.Add(LacunaShape.Diamond); break;
                }
            }
            return list.ToArray();
        }

        private static void AddGene(LacunaSlotType slot, string name, string topPattern, string traitEN, string traitTR, int rank)
        {
            var top = ParseStrand(topPattern);
            var bot = new LacunaShape[top.Length];
            for (int i = 0; i < top.Length; i++)
            {
                bot[i] = GetPartner(top[i]);
            }

            allGenes.Add(new LacunaGeneDefinition
            {
                slot = slot,
                pairCount = top.Length,
                geneName = name,
                topStrand = top,
                bottomStrand = bot,
                bodyTraitEN = traitEN,
                bodyTraitTR = traitTR,
                rank = rank
            });
        }

        private static void InitializeDatabase()
        {
            allGenes = new List<LacunaGeneDefinition>();

            // =========================================================================
            // 1. SLOT Z (4 Çift — Boy / Height)
            // Sıralama: ZUUN (1) < ZIMMOK (2) < ZEMBO (3) < ZORP (4) < ZAKKO (5)
            // =========================================================================
            AddGene(LacunaSlotType.Z, "ZORP",   "● ▲ ■ ◆", "Tall",   "Uzun Boy",  4);
            AddGene(LacunaSlotType.Z, "ZIMMOK", "● ▲ ■ ●", "Short",  "Kısa Boy",  2);
            AddGene(LacunaSlotType.Z, "ZUUN",   "● ▲ ◆ ●", "Tiny",   "Minik Boy", 1);
            AddGene(LacunaSlotType.Z, "ZAKKO",  "◆ ▲ ■ ◆", "Giant",  "Dev Boy",   5);
            AddGene(LacunaSlotType.Z, "ZEMBO",  "● ■ ■ ◆", "Medium", "Orta Boy",  3);

            // =========================================================================
            // 2. SLOT V (6 Çift — Gözler / Eyes)
            // Sıralama: VIXX (0) < VOLM (1) < VESHI (2) < VARQ (3) < VUNDO (4)
            // =========================================================================
            AddGene(LacunaSlotType.V, "VOLM",  "▲ ● ◆ ■ ▲ ◆", "One eye",    "Tek Göz",      1);
            AddGene(LacunaSlotType.V, "VESHI", "▲ ● ◆ ■ ▲ ●", "Two eyes",   "İki Göz",      2);
            AddGene(LacunaSlotType.V, "VARQ",  "▲ ● ◆ ■ ■ ◆", "Three eyes", "Üç Göz",       3);
            AddGene(LacunaSlotType.V, "VUNDO", "▲ ● ◆ ◆ ▲ ◆", "Stalk eyes", "Saplı Gözler", 4);
            AddGene(LacunaSlotType.V, "VIXX",  "■ ● ◆ ■ ▲ ◆", "Eyeless",    "Gözsüz",       0);

            // =========================================================================
            // 3. SLOT K (8 Çift — Deri Rengi / Skin — Rank: Yok)
            // =========================================================================
            AddGene(LacunaSlotType.K, "KRELL", "■ ● ◆ ▲ ■ ▲ ● ◆", "Orange skin", "Turuncu Deri", -1);
            AddGene(LacunaSlotType.K, "KUPPA", "■ ● ◆ ▲ ■ ▲ ● ▲", "Yellow skin", "Sarı Deri",    -1);
            AddGene(LacunaSlotType.K, "KOSH",  "◆ ● ◆ ▲ ■ ▲ ● ◆", "White skin",  "Beyaz Deri",   -1);
            AddGene(LacunaSlotType.K, "KIMBO", "■ ● ◆ ■ ■ ▲ ● ◆", "Teal skin",   "Turkuaz Deri", -1);
            AddGene(LacunaSlotType.K, "KLOOT", "■ ● ◆ ▲ ■ ● ● ◆", "Violet skin", "Mor Deri",     -1);

            // =========================================================================
            // 4. SLOT P (10 Çift — Uzuvlar / Limbs)
            // Sıralama: PHELM (0) < PONDO (1) < PLORT (2) < PUNKO (3) < PRIZZ (4)
            // =========================================================================
            AddGene(LacunaSlotType.P, "PHELM", "● ● ▲ ▲ ■ ■ ◆ ◆ ● ▲", "Armless",      "Kolsuz",        0);
            AddGene(LacunaSlotType.P, "PLORT", "● ● ▲ ▲ ■ ■ ◆ ◆ ● ■", "Two arms",     "İki Kol",       2);
            AddGene(LacunaSlotType.P, "PRIZZ", "● ● ▲ ▲ ■ ■ ◆ ◆ ▲ ▲", "Four arms",    "Dört Kol",      4);
            AddGene(LacunaSlotType.P, "PUNKO", "● ● ■ ▲ ■ ■ ◆ ◆ ● ▲", "Tentacles",    "Dokunaçlar",    3);
            AddGene(LacunaSlotType.P, "PONDO", "● ● ▲ ▲ ■ ■ ◆ ● ● ▲", "Stubby limbs", "Kütük Uzuvlar", 1);

            // =========================================================================
            // GEZEGENLER (FABAUP, SNIBBLEX, OOMGARTH)
            // =========================================================================
            allPlanets = new List<LacunaPlanetDefinition>
            {
                new LacunaPlanetDefinition
                {
                    planetName = "FABAUP",
                    subtitleTR = "Kavrulmuş Kumullar (Scorched Dunes)",
                    rulebookReasonTR = "Kavurucu güneş parıltısında fazla gözler yanıp kül olur; kum fırtınalarında sadece uzun ve güçlü uzuvlular ayakta kalır.",
                    skinRuleTR = "KRELL veya KLOOT",
                    heightRuleTR = "En az ZORP (ZORP ve üstü)",
                    eyesRuleTR = "En fazla VESHI (VESHI ve altı)",
                    limbsRuleTR = "En az PLORT (PLORT ve üstü)",
                    acceptableK = new[] { "KRELL", "KLOOT" },
                    acceptableZ = new[] { "ZORP", "ZAKKO" },
                    acceptableV = new[] { "VIXX", "VOLM", "VESHI" },
                    acceptableP = new[] { "PLORT", "PUNKO", "PRIZZ" },
                    themeColor = new Color(1.0f, 0.45f, 0.15f)
                },
                new LacunaPlanetDefinition
                {
                    planetName = "SNIBBLEX",
                    subtitleTR = "Donmuş ve Karanlık (Frozen & Lightless)",
                    rulebookReasonTR = "Dondurucu soğukta fazladan uzuvlar vücut ısısını hızla tüketir; zifiri karanlıkta ise gelişmiş gözler şarttır.",
                    skinRuleTR = "KOSH veya KIMBO",
                    heightRuleTR = "En az ZEMBO (ZEMBO ve üstü)",
                    eyesRuleTR = "En az VARQ (VARQ ve üstü)",
                    limbsRuleTR = "En fazla PUNKO (PUNKO ve altı)",
                    acceptableK = new[] { "KOSH", "KIMBO" },
                    acceptableZ = new[] { "ZEMBO", "ZORP", "ZAKKO" },
                    acceptableV = new[] { "VARQ", "VUNDO" },
                    acceptableP = new[] { "PHELM", "PONDO", "PLORT", "PUNKO" },
                    themeColor = new Color(0.20f, 0.82f, 1.0f)
                },
                new LacunaPlanetDefinition
                {
                    planetName = "OOMGARTH",
                    subtitleTR = "Ezici Yerçekimli Bataklık (Crushing-Gravity Swamp)",
                    rulebookReasonTR = "Aşırı uzun her canlı devasa yerçekimi altında kendi ağırlığıyla kırılır; bataklıkta tutunacak uzuvlar zorunludur.",
                    skinRuleTR = "KIMBO veya KUPPA",
                    heightRuleTR = "En fazla ZEMBO (ZEMBO ve altı)",
                    eyesRuleTR = "En az VESHI (VESHI ve üstü)",
                    limbsRuleTR = "En az PONDO (PONDO ve üstü)",
                    acceptableK = new[] { "KIMBO", "KUPPA" },
                    acceptableZ = new[] { "ZUUN", "ZIMMOK", "ZEMBO" },
                    acceptableV = new[] { "VESHI", "VARQ", "VUNDO" },
                    acceptableP = new[] { "PONDO", "PLORT", "PUNKO", "PRIZZ" },
                    themeColor = new Color(0.35f, 0.92f, 0.45f)
                }
            };

            // =========================================================================
            // YARATIK LİSTESİ (CREATURE ROSTER 1..10 — GDD SAYFA 6 & 7)
            // =========================================================================
            allCreatures = new List<LacunaCreatureRosterEntry>
            {
                // #1 WIBBLE — FABAUP — ZIMMOK* VOLM KRELL PLORT
                new LacunaCreatureRosterEntry
                {
                    number = 1,
                    creatureName = "WIBBLE",
                    planetName = "FABAUP",
                    geneZ = "ZIMMOK", // YANLIŞ (ZIMMOK -> ZORP 1 çift fark)
                    geneV = "VOLM",
                    geneK = "KRELL",
                    geneP = "PLORT",
                    wrongSlot = LacunaSlotType.Z,
                    acceptableFixes = new[] { "ZORP", "ZAKKO" },
                    fixDescriptionTR = "Slot Z'deki ZIMMOK genini ZORP'a dönüştür (4. çifti ●/■ yerine ◆/▲ yap).",
                    trapNoteTR = "TUZAK: ZEMBO da ZIMMOK'a 1 çift uzaklıktadır ama FABAUP 'En az ZORP' istediği için ZEMBO yetersiz kalır!",
                    qteSpeed = LacunaQteSpeed.Slow,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 1, true, false),  // Partneri ◆ -> ▲
                        new LacunaGap(LacunaSlotType.V, 2, false, true),  // Partneri ◆ -> ▲
                        new LacunaGap(LacunaSlotType.K, 4, true, true)    // Her iki zincir eksik -> Kural kitabından KRELL (5. çift ■/●)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"İlk numune FABAUP kumullarından: WIBBLE. Ailen yörünge malikanemde çok rahat... Programın gerisinde kalmadığın sürece öyle kalacaklar.\"",
                    landlordClueTR = "İPUCU #1/10: Kapsül mühürlendiğinde Ev Sahibi eldivenli eliyle camı okşadı. FABAUP gezegeninin yüzyıllar önce tamamen kuruduğunu fark ettin."
                },

                // #2 SNORFLE — SNIBBLEX — ZORP VOLM* KOSH PLORT
                new LacunaCreatureRosterEntry
                {
                    number = 2,
                    creatureName = "SNORFLE",
                    planetName = "SNIBBLEX",
                    geneZ = "ZORP",
                    geneV = "VOLM", // YANLIŞ (VOLM -> VARQ veya VUNDO 1 çift fark)
                    geneK = "KOSH",
                    geneP = "PLORT",
                    wrongSlot = LacunaSlotType.V,
                    acceptableFixes = new[] { "VARQ", "VUNDO" },
                    fixDescriptionTR = "Slot V'deki VOLM genini VARQ (5. çifti ■/●) veya VUNDO (4. çifti ◆/▲) yap.",
                    trapNoteTR = "SNIBBLEX zifiri karanlıktır; 'En az VARQ' gerekir, VOLM ve VESHI yetersizdir.",
                    qteSpeed = LacunaQteSpeed.Slow,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 0, false, true),
                        new LacunaGap(LacunaSlotType.V, 1, true, true),   // Kural kitabından V slotu 2. çift (●/■)
                        new LacunaGap(LacunaSlotType.P, 7, true, false)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"SNORFLE, SNIBBLEX buzullarından. Karanlıkta göremeyen bir canlı orada bir dakika bile yaşayamaz.\"",
                    landlordClueTR = "İPUCU #2/10: Ev Sahibi numunenin gözlerine uzun uzun baktı. 'Eskiden bu buzulların altında şehirler parlardı' diye fısıldadı."
                },

                // #3 BAABLEBO — FABAUP — ZORP VESHI KUPPA* PUNKO
                new LacunaCreatureRosterEntry
                {
                    number = 3,
                    creatureName = "BAABLEBO",
                    planetName = "FABAUP",
                    geneZ = "ZORP",
                    geneV = "VESHI",
                    geneK = "KUPPA", // YANLIŞ (KUPPA -> KRELL 1 çift fark)
                    geneP = "PUNKO",
                    wrongSlot = LacunaSlotType.K,
                    acceptableFixes = new[] { "KRELL", "KLOOT" },
                    fixDescriptionTR = "Slot K'deki KUPPA genini KRELL'e dönüştür (8. çifti ▲/◆ yerine ◆/▲ yap).",
                    trapNoteTR = "FABAUP sadece KRELL veya KLOOT derisini kabul eder; KUPPA kavurucu güneşte yanar.",
                    qteSpeed = LacunaQteSpeed.Slow,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.V, 4, false, true),
                        new LacunaGap(LacunaSlotType.K, 2, true, true),   // Kural kitabından K slotu 3. çift (◆/▲)
                        new LacunaGap(LacunaSlotType.P, 2, true, false)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"BAABLEBO tekrar FABAUP'a dönecek. Derisi bu radyasyona dayanamıyor. Saat işliyor mühendis.\"",
                    landlordClueTR = "İPUCU #3/10: Masada unuttuğu manifestoda tüm gezegenlerin 'Ölü Biyosfer' olarak sınıflandırıldığını gördün. Sadece tek bir dünya canlı kalmış."
                },

                // #4 GLUMP — OOMGARTH — ZIMMOK VOLM* KIMBO PLORT
                new LacunaCreatureRosterEntry
                {
                    number = 4,
                    creatureName = "GLUMP",
                    planetName = "OOMGARTH",
                    geneZ = "ZIMMOK",
                    geneV = "VOLM", // YANLIŞ (VOLM -> VESHI, VARQ veya VUNDO 1 çift fark)
                    geneK = "KIMBO",
                    geneP = "PLORT",
                    wrongSlot = LacunaSlotType.V,
                    acceptableFixes = new[] { "VESHI", "VARQ", "VUNDO" },
                    fixDescriptionTR = "Slot V'deki VOLM genini VESHI (6. çifti ●/■), VARQ veya VUNDO yap.",
                    trapNoteTR = "OOMGARTH bataklığında ZIMMOK boyu uygundur ama VOLM gözü yetersizdir (En az VESHI gerekir).",
                    qteSpeed = LacunaQteSpeed.Medium,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 2, true, false),
                        new LacunaGap(LacunaSlotType.V, 2, true, true),
                        new LacunaGap(LacunaSlotType.K, 3, false, true),
                        new LacunaGap(LacunaSlotType.P, 5, true, false)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"Dördüncü gece: GLUMP. OOMGARTH'ın ezici yerçekimi bataklığından. Hata payın daralıyor.\"",
                    landlordClueTR = "İPUCU #4/10: Ev Sahibi bir fotoğraf bıraktı; ailenin arkasındaki pencerede hiç yıldız yoktu, sadece devasa bir kriyo-arşiv kubbesi vardı."
                },

                // #5 TWIZZIT — SNIBBLEX — ZEMBO VARQ KLOOT* PUNKO
                new LacunaCreatureRosterEntry
                {
                    number = 5,
                    creatureName = "TWIZZIT",
                    planetName = "SNIBBLEX",
                    geneZ = "ZEMBO",
                    geneV = "VARQ",
                    geneK = "KLOOT", // YANLIŞ (KLOOT -> KOSH veya KIMBO 2 çift fark!)
                    geneP = "PUNKO",
                    wrongSlot = LacunaSlotType.K,
                    acceptableFixes = new[] { "KOSH", "KIMBO" },
                    fixDescriptionTR = "Slot K'deki KLOOT genini KOSH (1. çift ◆/▲, 6. çift ▲/◆) veya KIMBO (4. çift ■/●, 6. çift ▲/◆) yap (2 çift değişir!).",
                    trapNoteTR = "DİKKAT: KLOOT'tan KRELL'e 1 çiftle geçilir ama SNIBBLEX KRELL kabul etmez! 2 çift değiştirerek KOSH veya KIMBO yapmalısın.",
                    qteSpeed = LacunaQteSpeed.Medium,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 1, false, true),
                        new LacunaGap(LacunaSlotType.V, 4, true, false),
                        new LacunaGap(LacunaSlotType.K, 1, true, true),
                        new LacunaGap(LacunaSlotType.P, 3, true, true)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"TWIZZIT. Bu kez tek bir çifti değiştirmek yetmeyecek. Kural kitabını dikkatli oku.\"",
                    landlordClueTR = "İPUCU #5/10: Onardığın her yaratığın hücresel imzası, aynı antik genetik kökenden geliyor. Ev Sahibi rastgele yaratıklar toplamıyor."
                },

                // #6 MOOPSY — OOMGARTH — ZEMBO VESHI KUPPA PHELM*
                new LacunaCreatureRosterEntry
                {
                    number = 6,
                    creatureName = "MOOPSY",
                    planetName = "OOMGARTH",
                    geneZ = "ZEMBO",
                    geneV = "VESHI",
                    geneK = "KUPPA",
                    geneP = "PHELM", // YANLIŞ (PHELM -> PONDO 1 çift fark)
                    wrongSlot = LacunaSlotType.P,
                    acceptableFixes = new[] { "PONDO", "PLORT", "PUNKO", "PRIZZ" },
                    fixDescriptionTR = "Slot P'deki PHELM genini PONDO (8. çift ●/■), PLORT (10. çift ■/●) veya PUNKO (3. çift ■/●) yap.",
                    trapNoteTR = "TUZAK: OOMGARTH'ta ZEMBO boyu tam sınırdadır (En fazla ZEMBO). Sakın ZEMBO'yu ZORP yapma, yerçekiminde ezilir! Sorun P slotundaki PHELM'dedir.",
                    qteSpeed = LacunaQteSpeed.Medium,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 3, true, false),
                        new LacunaGap(LacunaSlotType.V, 1, true, true),
                        new LacunaGap(LacunaSlotType.K, 6, true, false),
                        new LacunaGap(LacunaSlotType.P, 4, true, true)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"Altıncı numune: MOOPSY. Bataklıkta uzuvsuz kalan her şey dibe çöker. Gözünü dört aç.\"",
                    landlordClueTR = "İPUCU #6/10: Ev Sahibi'nin eldiveni bir an sıyrıldı; bileğinde organik deri değil, senin dizilediğin baz çiftlerine benzeyen sentetik dikişler vardı."
                },

                // #7 KRAKKO — SNIBBLEX — ZAKKO VUNDO KIMBO PRIZZ*
                new LacunaCreatureRosterEntry
                {
                    number = 7,
                    creatureName = "KRAKKO",
                    planetName = "SNIBBLEX",
                    geneZ = "ZAKKO",
                    geneV = "VUNDO",
                    geneK = "KIMBO",
                    geneP = "PRIZZ", // YANLIŞ (PRIZZ -> PHELM 1 çift, veya PUNKO/PLORT/PONDO 2 çift)
                    wrongSlot = LacunaSlotType.P,
                    acceptableFixes = new[] { "PHELM", "PONDO", "PLORT", "PUNKO" },
                    fixDescriptionTR = "Slot P'deki PRIZZ genini PHELM (9. çift ●/■ - 1 çift fark) veya PLORT/PUNKO/PONDO (2 çift fark) yap.",
                    trapNoteTR = "TASARIM TUZAĞI: PRIZZ SNIBBLEX için fazla uzuvludur (En fazla PUNKO). En yakın düzeltme 1 çift farkla PHELM'dir; SNIBBLEX'te alt sınır olmadığı için PHELM tamamen geçerlidir!",
                    qteSpeed = LacunaQteSpeed.Fast,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 1, true, true),   // ZAKKO (◆ ? ■ ◆) tek eşleşir
                        new LacunaGap(LacunaSlotType.V, 2, true, true),
                        new LacunaGap(LacunaSlotType.K, 4, true, false),
                        new LacunaGap(LacunaSlotType.P, 1, true, true),
                        new LacunaGap(LacunaSlotType.P, 8, false, true)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"Yedinci gece: KRAKKO. SNIBBLEX soğuğunda PRIZZ geni ölüm demektir. Kapsül mühürleme hızı artıyor.\"",
                    landlordClueTR = "İPUCU #7/10: Onardığın yaratıklar birer canavar değil; yok olmuş bir ekosistemin birbirini tamamlayan organları ve koruyucuları."
                },

                // #8 ZEEBO-ZEEBO — OOMGARTH — ZEMBO VARQ KOSH* PRIZZ
                new LacunaCreatureRosterEntry
                {
                    number = 8,
                    creatureName = "ZEEBO-ZEEBO",
                    planetName = "OOMGARTH",
                    geneZ = "ZEMBO",
                    geneV = "VARQ",
                    geneK = "KOSH", // YANLIŞ (KOSH -> KIMBO veya KUPPA 2 çift fark)
                    geneP = "PRIZZ",
                    wrongSlot = LacunaSlotType.K,
                    acceptableFixes = new[] { "KIMBO", "KUPPA" },
                    fixDescriptionTR = "Slot K'deki KOSH genini KIMBO (1. çift ■/● ve 4. çift ■/●) veya KUPPA (1. çift ■/● ve 8. çift ▲/◆) yap (2 çift fark).",
                    trapNoteTR = "TUZAK: KOSH'tan KRELL'e 1 çiftle geçilir ama OOMGARTH sadece KIMBO veya KUPPA kabul eder (2 çift değiştirmelisin!).",
                    qteSpeed = LacunaQteSpeed.Fast,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 2, false, true),
                        new LacunaGap(LacunaSlotType.V, 1, true, true),
                        new LacunaGap(LacunaSlotType.K, 2, true, true),
                        new LacunaGap(LacunaSlotType.K, 6, true, false),
                        new LacunaGap(LacunaSlotType.P, 5, true, true)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"ZEEBO-ZEEBO. Sekizinci numune. Neredeyse bitti. Kolay görünen yola aldanma.\"",
                    landlordClueTR = "İPUCU #8/10: Ev Sahibi ilk kez sesini yumuşattı: 'Yalnızlık, bozuk bir genomdan çok daha zor onarılır mühendis.'"
                },

                // #9 PHLORB — FABAUP — ZUUN* VESHI KLOOT PRIZZ
                new LacunaCreatureRosterEntry
                {
                    number = 9,
                    creatureName = "PHLORB",
                    planetName = "FABAUP",
                    geneZ = "ZUUN", // YANLIŞ (ZUUN -> ZORP 2 çift fark!)
                    geneV = "VESHI",
                    geneK = "KLOOT",
                    geneP = "PRIZZ",
                    wrongSlot = LacunaSlotType.Z,
                    acceptableFixes = new[] { "ZORP", "ZAKKO" },
                    fixDescriptionTR = "Slot Z'deki ZUUN genini ZORP'a dönüştür (3. çifti ■/● ve 4. çifti ◆/▲ yap — 4 çiftlik genin yarısı değişir!).",
                    trapNoteTR = "TUZAK: ZUUN'dan 1 çift değiştirerek ZIMMOK elde edilir ama FABAUP 'En az ZORP' ister! 4 çiftlik Z geninin tam 2 çiftini değiştirmelisin.",
                    qteSpeed = LacunaQteSpeed.Fast,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 1, true, true),   // ZUUN (● ? ◆ ●) tek eşleşir
                        new LacunaGap(LacunaSlotType.V, 2, true, true),
                        new LacunaGap(LacunaSlotType.K, 1, true, true),
                        new LacunaGap(LacunaSlotType.P, 0, true, false),
                        new LacunaGap(LacunaSlotType.P, 6, false, true)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"Dokuzuncu numune: PHLORB. Kısa ve acımasız bir dizilim. Bundan sonra sadece tek bir numune kaldı.\"",
                    landlordClueTR = "İPUCU #9/10: Dokuzuncu kapsül mühürlenirken Ev Sahibi malikanenin anahtarlarını masanın kenarına bıraktı. Ailen güvendeydi."
                },

                // #10 GROMBLE (THE FINALE) — FABAUP — ZAKKO VOLM KRELL PONDO*
                new LacunaCreatureRosterEntry
                {
                    number = 10,
                    creatureName = "GROMBLE",
                    planetName = "FABAUP",
                    geneZ = "ZAKKO",
                    geneV = "VOLM",
                    geneK = "KRELL",
                    geneP = "PONDO", // YANLIŞ (PONDO -> PLORT, PUNKO veya PRIZZ 2 çift fark!)
                    wrongSlot = LacunaSlotType.P,
                    acceptableFixes = new[] { "PLORT", "PUNKO", "PRIZZ" },
                    fixDescriptionTR = "FİNAL: Slot P'deki 10 çiftlik PONDO genini PLORT (8. çift ◆/▲ ve 10. çift ■/●), PUNKO (3. çift ■/● ve 8. çift ◆/▲) veya PRIZZ (8. çift ◆/▲ ve 9. çift ▲/◆) yap (2 çift fark!).",
                    trapNoteTR = "FİNAL TUZAĞI: PONDO'ya 1 çift uzaklıktaki tek gen PHELM'dir ama FABAUP 'En az PLORT' istediği için PHELM yaratığı öldürür! Doğru çözüm (PLORT, PUNKO veya PRIZZ) tam 2 çift uzaklıktadır.",
                    qteSpeed = LacunaQteSpeed.Fastest,
                    gaps = new List<LacunaGap>
                    {
                        new LacunaGap(LacunaSlotType.Z, 2, true, false),
                        new LacunaGap(LacunaSlotType.V, 1, true, true),
                        new LacunaGap(LacunaSlotType.K, 2, true, true),
                        new LacunaGap(LacunaSlotType.K, 5, false, true),
                        new LacunaGap(LacunaSlotType.P, 0, true, true),
                        new LacunaGap(LacunaSlotType.P, 4, true, false),
                        new LacunaGap(LacunaSlotType.P, 6, false, true)
                    },
                    landlordIntroTR = "EV SAHİBİ: \"Son numune: GROMBLE. Dev, turuncu, tek gözlü... Ama FABAUP için uzuvları yetersiz. En uzun gen, en dar zamanlama. Bunu başarırsan özgürsün.\"",
                    landlordClueTR = "FİNAL (10/10): Son kapsül mühürlendiğinde Ev Sahibi aileni serbest bıraktı, onardığın 10 yaratığın hepsini sana armağan etti ve sessizce sordu: \"Ya planımda eksik olan son parça hiçbir zaman bir gen değilse?\""
                }
            };
        }
    }
}
