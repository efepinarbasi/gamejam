using System;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Nükleotid tipleri (DNA bazları). 
    /// A ile T, C ile G birbirini tamamlar. Universal (Joker) hepsiyle uyumludur.
    /// </summary>
    public enum NucleotideType
    {
        A,          // Adenin (Kırmızı)
        T,          // Timin (Turuncu/Altın - A ile eşleşir)
        C,          // Sitozin (Mavi)
        G,          // Guanin (Mor - C ile eşleşir)
        Universal   // Evrensel / Joker (Neon Zümrüt - Her bazla uyumlu)
    }

    /// <summary>
    /// Genin Biyolojik Kategorisi.
    /// </summary>
    public enum GeneCategory
    {
        All,         // Tümü (Filtreleme için)
        Exoskeleton, // Dış İskelet & Koruyucu Zırh (Isı, Basınç, Deri)
        Metabolic,   // Solunum & Metabolizma (Asit, Toksisite, Oksijen)
        Cellular,    // Hücresel & Sıvı (Kan, Antifriz, Sıcaklık Dengeleme)
        Structural,  // Derin Basınç & İç İskelet (Yerçekimi, Omurga, Kaslar)
        Cosmic,      // Radyasyon & Manyetik Kalkan (Gama, UV, X-Ray)
        Mutagen      // Efsanevi / Kuantum Biyo-Jokerler
    }

    /// <summary>
    /// Genin Nadirlik Seviyesi (Rarity Tier).
    /// </summary>
    public enum GeneRarity
    {
        Common,     // Standart / Yaygın (Yeşil)
        Rare,       // Nadir (Elektrik Mavisi)
        Epic,       // Epik (Neon Mor)
        Legendary   // Efsanevi Kuantum (Altın Sarısı)
    }

    /// <summary>
    /// Canlının gezegenlerde hayatta kalmasını sağlayan direnç ve adaptasyon özellikleri.
    /// </summary>
    public enum StatType
    {
        HeatResistance,      // Sıcaklık Direnci (Lav/Volkanik Gezegenler)
        ColdResistance,      // Soğuk Direnci (Buzul Gezegenler)
        RadiationShield,     // Radyasyon Kalkanı (Güneş Fırtınaları / Radyoaktif Gezegenler)
        ToxicityFilter,      // Toksisite Filtresi (Zehirli Gaz / Asit Gezegenleri)
        PressureTolerance,   // Basınç Dayanımı (Yüksek Yerçekimi / Derin Okyanus Gezegenleri)
        OxygenEfficiency     // Oksijen Verimliliği (Düşük Atmosferli / Seyrek Gezegenler)
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public int value;

        public StatModifier(StatType stat, int value)
        {
            this.stat = stat;
            this.value = value;
        }
    }

    [Serializable]
    public struct StatRequirement
    {
        public StatType stat;
        public int requiredValue;

        public StatRequirement(StatType stat, int requiredValue)
        {
            this.stat = stat;
            this.requiredValue = requiredValue;
        }
    }

    public static class DNAHelper
    {
        /// <summary>
        /// İki nükleotidin birbirini tamamlayıp tamamlamadığını kontrol eder.
        /// A <-> T
        /// C <-> G
        /// Universal <-> Herhangi biri
        /// </summary>
        public static bool IsComplementary(NucleotideType left, NucleotideType right)
        {
            if (left == NucleotideType.Universal || right == NucleotideType.Universal)
                return true;

            if ((left == NucleotideType.A && right == NucleotideType.T) ||
                (left == NucleotideType.T && right == NucleotideType.A))
                return true;

            if ((left == NucleotideType.C && right == NucleotideType.G) ||
                (left == NucleotideType.G && right == NucleotideType.C))
                return true;

            return false;
        }

        public static NucleotideType GetComplement(NucleotideType type)
        {
            switch (type)
            {
                case NucleotideType.A: return NucleotideType.T;
                case NucleotideType.T: return NucleotideType.A;
                case NucleotideType.C: return NucleotideType.G;
                case NucleotideType.G: return NucleotideType.C;
                case NucleotideType.Universal: return NucleotideType.Universal;
                default: return NucleotideType.A;
            }
        }

        public static Color GetNucleotideColor(NucleotideType type)
        {
            switch (type)
            {
                case NucleotideType.A: // ● (Daire / Circle)
                    return new Color(0.15f, 0.85f, 1.0f); // Parlak Camgöbeği
                case NucleotideType.T: // ■ (Kare / Square - ● ile eşleşir)
                    return new Color(0.98f, 0.32f, 0.45f); // Mercan Kırmızısı
                case NucleotideType.C: // ▲ (Üçgen / Triangle)
                    return new Color(0.20f, 0.95f, 0.55f); // Zümrüt Yeşili
                case NucleotideType.G: // ◆ (Elmas / Diamond - ▲ ile eşleşir)
                    return new Color(1.0f, 0.78f, 0.15f); // Kehribar Altın
                case NucleotideType.Universal:
                    return new Color(0.06f, 0.90f, 0.50f); // Neon Zümrüt
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Lacuna Genetics GDD şekil sembolünü döner:
        /// A -> ● (Daire), T -> ■ (Kare), C -> ▲ (Üçgen), G -> ◆ (Elmas)
        /// Eşlenik Kuralı: ● ↔ ■  ve  ▲ ↔ ◆
        /// </summary>
        public static string GetNucleotideSymbol(NucleotideType type)
        {
            switch (type)
            {
                case NucleotideType.A: return "●";
                case NucleotideType.T: return "■";
                case NucleotideType.C: return "▲";
                case NucleotideType.G: return "◆";
                case NucleotideType.Universal: return "★";
                default: return "●";
            }
        }

        public static string GetNucleotideFullName(NucleotideType type)
        {
            switch (type)
            {
                case NucleotideType.A: return "Daire (●)";
                case NucleotideType.T: return "Kare (■)";
                case NucleotideType.C: return "Üçgen (▲)";
                case NucleotideType.G: return "Elmas (◆)";
                case NucleotideType.Universal: return "Joker (★)";
                default: return type.ToString();
            }
        }

        public static NucleotideType ShapeToNucleotide(LacunaShape shape)
        {
            switch (shape)
            {
                case LacunaShape.Circle: return NucleotideType.A;   // ●
                case LacunaShape.Square: return NucleotideType.T;   // ■
                case LacunaShape.Triangle: return NucleotideType.C; // ▲
                case LacunaShape.Diamond: return NucleotideType.G;  // ◆
                default: return NucleotideType.A;
            }
        }

        public static LacunaShape NucleotideToShape(NucleotideType nuc)
        {
            switch (nuc)
            {
                case NucleotideType.A: return LacunaShape.Circle;   // ●
                case NucleotideType.T: return LacunaShape.Square;   // ■
                case NucleotideType.C: return LacunaShape.Triangle; // ▲
                case NucleotideType.G: return LacunaShape.Diamond;  // ◆
                default: return LacunaShape.Circle;
            }
        }

        public static Color GetRarityColor(GeneRarity rarity)
        {
            switch (rarity)
            {
                case GeneRarity.Common: return new Color(0.2f, 0.8f, 0.4f);
                case GeneRarity.Rare: return new Color(0.1f, 0.65f, 1f);
                case GeneRarity.Epic: return new Color(0.75f, 0.3f, 1f);
                case GeneRarity.Legendary: return new Color(1f, 0.8f, 0.15f);
                default: return Color.white;
            }
        }

        public static string GetCategoryDisplayName(GeneCategory cat)
        {
            switch (cat)
            {
                case GeneCategory.All: return "TÜM GENLER";
                case GeneCategory.Exoskeleton: return "🛡️ ZIRH & DERİ";
                case GeneCategory.Metabolic: return "🫁 SOLUNUM & ORGAN";
                case GeneCategory.Cellular: return "🩸 HÜCRE & KAN";
                case GeneCategory.Structural: return "🦾 İSKELET & KAS";
                case GeneCategory.Cosmic: return "☢️ RADYASYON";
                case GeneCategory.Mutagen: return "✨ KUANTUM JOKER";
                default: return cat.ToString();
            }
        }

        public static string GetStatDisplayName(StatType stat)
        {
            switch (stat)
            {
                case StatType.HeatResistance: return "Sıcaklık Direnci";
                case StatType.ColdResistance: return "Soğuk Direnci";
                case StatType.RadiationShield: return "Radyasyon Kalkanı";
                case StatType.ToxicityFilter: return "Toksisite Filtresi";
                case StatType.PressureTolerance: return "Basınç Dayanımı";
                case StatType.OxygenEfficiency: return "Oksijen Verimliliği";
                default: return stat.ToString();
            }
        }
    }
}
