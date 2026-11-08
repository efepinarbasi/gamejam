using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DNAPuzzle
{
    public class DNACanvasUI : MonoBehaviour
    {
        [Header("Gezegen Bilgileri")]
        [SerializeField] private Text planetNameText;
        [SerializeField] private Text planetDescriptionText;
        [SerializeField] private Image planetThemeImage;
        [SerializeField] private Text requirementsSummaryText;
        [SerializeField] private Text energyMetersText;
        [SerializeField] private Text instabilityMeterText;
        [SerializeField] private Text resonanceBonusText;

        [Header("DNA Sarmal Yuvaları")]
        [SerializeField] private Transform slotsParent;
        [SerializeField] private GeneSlotUI slotPrefab;

        [Header("Gen Havuzu")]
        [SerializeField] private Transform poolParent;
        [SerializeField] private GeneCardUI cardPrefab;

        [Header("Sonuç Paneli")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultMessageText;
        [SerializeField] private Button closeResultButton;

        [Header("Aksiyon Butonları")]
        [SerializeField] private Button testSurvivalButton;
        [SerializeField] private Button clearStrandButton;

        private DNAPuzzleManager manager;
        private List<GeneSlotUI> slotUIInstances = new List<GeneSlotUI>();
        private List<GeneCardUI> cardUIInstances = new List<GeneCardUI>();
        private int selectedSlot = 0;

        private void Start()
        {
            manager = DNAPuzzleManager.Instance;
            if (manager == null)
            {
                manager = FindAnyObjectByType<DNAPuzzleManager>();
            }

            if (manager != null)
            {
                manager.OnStrandUpdated += RefreshUI;
                manager.OnPlanetChanged += OnPlanetChanged;
                manager.OnEvaluationCompleted += ShowEvaluationResult;

                if (manager.CurrentPlanet != null)
                {
                    OnPlanetChanged(manager.CurrentPlanet);
                }
            }

            if (testSurvivalButton != null)
            {
                testSurvivalButton.onClick.AddListener(() => manager.EvaluateCreatureSurvival());
            }

            if (clearStrandButton != null)
            {
                clearStrandButton.onClick.AddListener(() => manager.ClearStrand());
            }

            if (closeResultButton != null && resultPanel != null)
            {
                closeResultButton.onClick.AddListener(() => resultPanel.SetActive(false));
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnStrandUpdated -= RefreshUI;
                manager.OnPlanetChanged -= OnPlanetChanged;
                manager.OnEvaluationCompleted -= ShowEvaluationResult;
            }
        }

        private void OnPlanetChanged(PlanetData planet)
        {
            if (planet == null) return;

            if (planetNameText != null) planetNameText.text = planet.planetName;
            if (planetDescriptionText != null) planetDescriptionText.text = planet.description;
            if (planetThemeImage != null) planetThemeImage.color = planet.themeColor;

            RebuildSlots(planet.strandSlotCount);
            RebuildPool();
            RefreshUI();
        }

        private void RebuildSlots(int count)
        {
            if (slotsParent == null || slotPrefab == null) return;

            foreach (var s in slotUIInstances)
            {
                if (s != null) Destroy(s.gameObject);
            }
            slotUIInstances.Clear();

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(slotPrefab, slotsParent);
                slotUIInstances.Add(instance);
            }
        }

        private void RebuildPool()
        {
            if (poolParent == null || cardPrefab == null || manager == null) return;

            foreach (var c in cardUIInstances)
            {
                if (c != null) Destroy(c.gameObject);
            }
            cardUIInstances.Clear();

            foreach (var gene in manager.GenePool)
            {
                var instance = Instantiate(cardPrefab, poolParent);
                instance.Bind(gene, (g) =>
                {
                    manager.EquipGene(selectedSlot, g);
                    AutoSelectNextEmptySlot();
                });
                cardUIInstances.Add(instance);
            }
        }

        private void AutoSelectNextEmptySlot()
        {
            if (manager == null || manager.StrandGenes == null) return;
            for (int i = 0; i < manager.SlotCount; i++)
            {
                if (manager.StrandGenes[i] == null)
                {
                    selectedSlot = i;
                    return;
                }
            }
        }

        public void RefreshUI()
        {
            if (manager == null || manager.CurrentPlanet == null) return;

            var planet = manager.CurrentPlanet;
            var currentStats = manager.CalculateTotalStats();
            int currentEnergy = manager.CalculateTotalEnergy();
            int currentInstability = manager.CalculateInstability();
            int currentResonance = manager.CalculateResonanceCount();
            var bonds = manager.GetBondStatuses();

            // Metreler
            if (energyMetersText != null)
            {
                bool ok = currentEnergy <= planet.maxEnergyBudget;
                energyMetersText.text = $"⚡ Enerji: {currentEnergy} / {planet.maxEnergyBudget} {(ok ? "✔" : "✖ AŞILDI!")}";
                energyMetersText.color = ok ? Color.green : Color.red;
            }

            if (instabilityMeterText != null)
            {
                bool ok = currentInstability <= planet.maxAllowedInstability;
                instabilityMeterText.text = $"☣ Kararsızlık: {currentInstability} / Max {planet.maxAllowedInstability} {(ok ? "✔" : "✖ ÇÖKÜŞ!")}";
                instabilityMeterText.color = ok ? Color.green : Color.red;
            }

            if (resonanceBonusText != null)
            {
                resonanceBonusText.text = $"✨ Rezonans Bağı: {currentResonance} adet";
            }

            // Gereksinimler Özeti
            if (requirementsSummaryText != null)
            {
                var lines = new List<string>();
                foreach (var req in planet.requirements)
                {
                    int val = currentStats.ContainsKey(req.stat) ? currentStats[req.stat] : 0;
                    string check = val >= req.requiredValue ? "✔" : "✖";
                    lines.Add($"[{check}] {DNAHelper.GetStatDisplayName(req.stat)}: {val} / {req.requiredValue}");
                }
                requirementsSummaryText.text = string.Join("\n", lines);
            }

            // Slotları Güncelle
            if (slotUIInstances.Count > 0)
            {
                for (int i = 0; i < slotUIInstances.Count; i++)
                {
                    var gene = (manager.StrandGenes != null && i < manager.StrandGenes.Length) ? manager.StrandGenes[i] : null;
                    BondStatus bond = (bonds != null && i < bonds.Count) ? bonds[i] : null;
                    bool isLast = (i == slotUIInstances.Count - 1);

                    slotUIInstances[i].Setup(
                        i,
                        gene,
                        bond,
                        isLast,
                        (slotIdx) => selectedSlot = slotIdx,
                        (slotIdx) => manager.RemoveGene(slotIdx),
                        (slotA, slotB) => manager.SwapSlots(slotA, slotB)
                    );
                }
            }
        }

        private void ShowEvaluationResult(EvaluationResult result)
        {
            if (resultPanel == null) return;

            resultPanel.SetActive(true);
            if (resultTitleText != null)
            {
                resultTitleText.text = result.statusTitle;
                resultTitleText.color = result.isSuccess ? Color.green : Color.red;
            }

            if (resultMessageText != null)
            {
                string msg = result.detailedMessage;
                if (!result.isSuccess && result.failedReasons.Count > 0)
                {
                    msg += "\n\nEksikler / Hatalar:\n• " + string.Join("\n• ", result.failedReasons);
                }
                resultMessageText.text = msg;
            }
        }
    }
}
