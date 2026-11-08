using System;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Oyuncunun gezegenlerde kazandığı yıldız derecelerini (1-3 Yıldız),
    /// toplam ilerlemeyi ve kilit açma sistemini yönetir.
    /// </summary>
    public static class ProgressionManager
    {
        private const string STAR_KEY_PREFIX = "DNA_PlanetStars_";

        public static int GetStars(int planetIndex)
        {
            return PlayerPrefs.GetInt(STAR_KEY_PREFIX + planetIndex, 0);
        }

        public static bool SaveStars(int planetIndex, int newStars)
        {
            int current = GetStars(planetIndex);
            if (newStars > current)
            {
                PlayerPrefs.SetInt(STAR_KEY_PREFIX + planetIndex, newStars);
                PlayerPrefs.Save();
                return true; // Yeni rekor
            }
            return false;
        }

        public static int GetTotalStars()
        {
            int total = 0;
            for (int i = 0; i < 4; i++)
            {
                total += GetStars(i);
            }
            return total;
        }

        public static int CalculateStars(EvaluationResult eval, int activeSynergyCount)
        {
            if (eval == null || !eval.isSuccess) return 0;

            int stars = 1; // 1. Yıldız: Gezegende hayatta kalındı

            // 2. Yıldız: En az 2 rezonans bağı kuruldu
            if (eval.resonanceCount >= 2)
            {
                stars = 2;
            }

            // 3. Yıldız: En az 1 gizli biyo-sinerji aktif VE enerji bütçesinin en fazla %80'i harcandı
            bool energyEfficient = eval.totalEnergyUsed <= Mathf.CeilToInt(eval.maxEnergyBudget * 0.82f);
            if (stars == 2 && activeSynergyCount >= 1 && energyEfficient)
            {
                stars = 3;
            }

            return stars;
        }

        public static string FormatStarString(int starCount)
        {
            switch (starCount)
            {
                case 3: return "★★★";
                case 2: return "★★☆";
                case 1: return "★☆☆";
                default: return "☆☆☆";
            }
        }
    }
}
