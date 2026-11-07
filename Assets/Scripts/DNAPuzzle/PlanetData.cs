using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    [CreateAssetMenu(fileName = "NewPlanet", menuName = "DNA Puzzle/Planet Data")]
    public class PlanetData : ScriptableObject
    {
        [Header("Gezegen Bilgileri")]
        public string planetId;
        public string planetName;
        [TextArea(2, 4)]
        public string description;
        public Color themeColor = new Color(0.9f, 0.4f, 0.2f);
        public Sprite planetIcon;

        [Header("DNA Sarmal Kısıtlamaları")]
        [Range(3, 6)]
        [Tooltip("Canlının bu gezegende sahip olabileceği DNA gen yuvası sayısı")]
        public int strandSlotCount = 4;

        [Range(5, 20)]
        [Tooltip("Canlının toplam metabolik enerji kapasitesi (genlerin toplam maliyeti bunu geçemez)")]
        public int maxEnergyBudget = 10;

        [Range(0, 3)]
        [Tooltip("Tolerans gösterilen maksimum uyuşmaz/hatalı bağ sayısı (Instability)")]
        public int maxAllowedInstability = 1;

        [Header("Hayatta Kalma Gereksinimleri")]
        [Tooltip("Canlının bu gezegende yaşayabilmesi için sağlaması gereken minimum özellik değerleri")]
        public List<StatRequirement> requirements = new List<StatRequirement>();

        public int GetRequiredStatValue(StatType stat)
        {
            if (requirements == null) return 0;
            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i].stat == stat)
                    return requirements[i].requiredValue;
            }
            return 0;
        }
    }
}
