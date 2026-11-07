using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Canlının DNA sarmalına takılan genlere göre gövde rengini, boyutunu, 
    /// kabuk, solungaç, parıltı ve uzuvlarını dinamik olarak güncelleyen görselleştirici.
    /// Hem 2D Sprite'lar hem de 3D ilkel objeler/modeller ile çalışacak şekilde esnek tasarlanmıştır.
    /// </summary>
    public class CreatureVisualizer : MonoBehaviour
    {
        [Header("Referanslar")]
        [SerializeField] private DNAPuzzleManager puzzleManager;

        [Header("Görsel Parçalar (İsteğe bağlı atanabilir)")]
        [Tooltip("Canlının ana gövde rendereri (SpriteRenderer veya MeshRenderer)")]
        [SerializeField] private Renderer bodyRenderer;
        [Tooltip("Isı veya zırh genleri takıldığında açılan kabuk/zırh parçası")]
        [SerializeField] private GameObject armorOverlay;
        [Tooltip("Solunum veya zehir genleri takıldığında açılan solungaç/kese")]
        [SerializeField] private GameObject gillsObject;
        [Tooltip("Radyasyon veya enerji genleri takıldığında parlayan biyo-ışık")]
        [SerializeField] private GameObject bioGlowEffect;
        [Tooltip("Basınç veya iskelet genleri takıldığında canlının ölçek katsayısı")]
        [SerializeField] private float sizeScaleFactor = 1.0f;

        private Material dynamicMaterial;

        private void Start()
        {
            if (puzzleManager == null)
            {
                puzzleManager = DNAPuzzleManager.Instance;
                if (puzzleManager == null)
                    puzzleManager = FindAnyObjectByType<DNAPuzzleManager>();
            }

            if (puzzleManager != null)
            {
                puzzleManager.OnStrandUpdated += UpdateCreatureAppearance;
            }

            if (bodyRenderer != null)
            {
                dynamicMaterial = bodyRenderer.material;
            }

            UpdateCreatureAppearance();
        }

        private void OnDestroy()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnStrandUpdated -= UpdateCreatureAppearance;
            }
        }

        /// <summary>
        /// DNA sarmalındaki genlere göre canlının görünümünü baştan aşağı yeniler.
        /// </summary>
        public void UpdateCreatureAppearance()
        {
            if (puzzleManager == null) return;

            var stats = puzzleManager.CalculateTotalStats();
            var strand = puzzleManager.StrandGenes;
            int resonanceCount = puzzleManager.CalculateResonanceCount();

            // 1. Renk Karışımı: Takılan genlerin dominant renklerini harmanla
            Color targetColor = CalculateBlendedColor(strand);

            if (bodyRenderer != null)
            {
                if (dynamicMaterial != null)
                {
                    dynamicMaterial.color = targetColor;
                }
                else
                {
                    bodyRenderer.material.color = targetColor;
                }
            }

            // 2. Basınç Dayanımı arttıkça canlının gövdesi kalınlaşır ve güçlenir
            int pressure = stats.ContainsKey(StatType.PressureTolerance) ? stats[StatType.PressureTolerance] : 0;
            float scaleMultiplier = 1f + (pressure * 0.08f * sizeScaleFactor);
            transform.localScale = Vector3.one * scaleMultiplier;

            // 3. Kabuk / Zırh Katmanı (Sıcaklık veya Basınç yüksekse aktifleşir)
            int heat = stats.ContainsKey(StatType.HeatResistance) ? stats[StatType.HeatResistance] : 0;
            if (armorOverlay != null)
            {
                armorOverlay.SetActive(heat >= 3 || pressure >= 3);
            }

            // 4. Solungaç / Filtre Katmanı (Toksisite veya Oksijen yüksekse aktifleşir)
            int toxicity = stats.ContainsKey(StatType.ToxicityFilter) ? stats[StatType.ToxicityFilter] : 0;
            int oxygen = stats.ContainsKey(StatType.OxygenEfficiency) ? stats[StatType.OxygenEfficiency] : 0;
            if (gillsObject != null)
            {
                gillsObject.SetActive(toxicity >= 2 || oxygen >= 2);
            }

            // 5. Biyo-Parıltı (Rezonans bağı varsa veya Radyasyon kalkanı güçlüyse)
            int radiation = stats.ContainsKey(StatType.RadiationShield) ? stats[StatType.RadiationShield] : 0;
            if (bioGlowEffect != null)
            {
                bioGlowEffect.SetActive(resonanceCount >= 2 || radiation >= 3);
            }
        }

        private Color CalculateBlendedColor(GeneData[] strand)
        {
            if (strand == null || strand.Length == 0)
                return new Color(0.7f, 0.7f, 0.7f); // Standart gri

            Color sum = Color.black;
            int count = 0;

            for (int i = 0; i < strand.Length; i++)
            {
                if (strand[i] != null)
                {
                    sum += strand[i].customColor;
                    count++;
                }
            }

            if (count == 0) return new Color(0.6f, 0.65f, 0.7f); // Nötr organik renk
            return sum / count;
        }
    }
}
