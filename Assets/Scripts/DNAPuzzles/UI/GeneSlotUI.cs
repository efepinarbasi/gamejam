using System;
using UnityEngine;
using UnityEngine.UI;

namespace DNAPuzzle
{
    public class GeneSlotUI : MonoBehaviour
    {
        [Header("Slot Elemanları")]
        [SerializeField] private Text slotIndexText;
        [SerializeField] private GameObject emptyIndicator;
        [SerializeField] private GameObject occupiedContainer;
        [SerializeField] private Text geneNameText;
        [SerializeField] private Text geneStatsText;
        [SerializeField] private Text energyText;
        [SerializeField] private Image leftConnectorBadge;
        [SerializeField] private Text leftConnectorText;
        [SerializeField] private Image rightConnectorBadge;
        [SerializeField] private Text rightConnectorText;
        [SerializeField] private Image slotBackground;

        [Header("Bağlantı Göstergesi (Sonraki Slota Giden Bağ)")]
        [SerializeField] private GameObject bondContainer;
        [SerializeField] private Image bondLine;
        [SerializeField] private Text bondStatusText;

        [Header("Butonlar")]
        [SerializeField] private Button removeButton;
        [SerializeField] private Button moveLeftButton;
        [SerializeField] private Button moveRightButton;
        [SerializeField] private Button selectSlotButton;

        private int slotIndex;
        private GeneData slottedGene;

        public int SlotIndex => slotIndex;
        public GeneData SlottedGene => slottedGene;

        public void Setup(
            int index, 
            GeneData gene, 
            BondStatus bondToNext, 
            bool isLastSlot,
            Action<int> onSelectSlot, 
            Action<int> onRemove, 
            Action<int, int> onSwap)
        {
            slotIndex = index;
            slottedGene = gene;

            if (slotIndexText != null)
                slotIndexText.text = $"Yuva {index + 1}";

            bool hasGene = gene != null;
            if (emptyIndicator != null) emptyIndicator.SetActive(!hasGene);
            if (occupiedContainer != null) occupiedContainer.SetActive(hasGene);

            if (hasGene)
            {
                if (geneNameText != null) geneNameText.text = gene.geneName;
                if (geneStatsText != null) geneStatsText.text = gene.GetStatsSummary();
                if (energyText != null) energyText.text = $"⚡ {gene.energyCost}";

                if (leftConnectorText != null) leftConnectorText.text = gene.leftConnector.ToString();
                if (rightConnectorText != null) rightConnectorText.text = gene.rightConnector.ToString();

                if (leftConnectorBadge != null) leftConnectorBadge.color = DNAHelper.GetNucleotideColor(gene.leftConnector);
                if (rightConnectorBadge != null) rightConnectorBadge.color = DNAHelper.GetNucleotideColor(gene.rightConnector);

                if (slotBackground != null) slotBackground.color = gene.customColor;
            }
            else
            {
                if (slotBackground != null) slotBackground.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            }

            // Buton Olayları
            if (removeButton != null)
            {
                removeButton.gameObject.SetActive(hasGene);
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(() => onRemove?.Invoke(slotIndex));
            }

            if (moveLeftButton != null)
            {
                moveLeftButton.gameObject.SetActive(hasGene && index > 0);
                moveLeftButton.onClick.RemoveAllListeners();
                moveLeftButton.onClick.AddListener(() => onSwap?.Invoke(slotIndex, slotIndex - 1));
            }

            if (moveRightButton != null)
            {
                moveRightButton.gameObject.SetActive(hasGene && !isLastSlot);
                moveRightButton.onClick.RemoveAllListeners();
                moveRightButton.onClick.AddListener(() => onSwap?.Invoke(slotIndex, slotIndex + 1));
            }

            if (selectSlotButton != null)
            {
                selectSlotButton.onClick.RemoveAllListeners();
                selectSlotButton.onClick.AddListener(() => onSelectSlot?.Invoke(slotIndex));
            }

            // Bağlantı (Rezonans / Kararsızlık) Çizgisi
            if (bondContainer != null)
            {
                if (isLastSlot)
                {
                    bondContainer.SetActive(false);
                }
                else
                {
                    bondContainer.SetActive(true);
                    if (bondToNext != null)
                    {
                        if (bondToNext.isResonant)
                        {
                            if (bondLine != null) bondLine.color = Color.green;
                            if (bondStatusText != null) bondStatusText.text = "⚡ UYUMLU";
                        }
                        else if (bondToNext.isMismatch)
                        {
                            if (bondLine != null) bondLine.color = Color.red;
                            if (bondStatusText != null) bondStatusText.text = "✖ UYUMSUZ";
                        }
                        else
                        {
                            if (bondLine != null) bondLine.color = Color.gray;
                            if (bondStatusText != null) bondStatusText.text = "...";
                        }
                    }
                }
            }
        }
    }
}
