using System;
using UnityEngine;
using UnityEngine.UI;

namespace DNAPuzzle
{
    public class GeneCardUI : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text leftConnectorText;
        [SerializeField] private Text rightConnectorText;
        [SerializeField] private Image leftConnectorBadge;
        [SerializeField] private Image rightConnectorBadge;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button actionButton;

        private GeneData boundGene;
        private Action<GeneData> onClickCallback;

        public GeneData BoundGene => boundGene;

        public void Bind(GeneData gene, Action<GeneData> onClick)
        {
            boundGene = gene;
            onClickCallback = onClick;

            if (gene == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (nameText != null) nameText.text = gene.geneName;
            if (descriptionText != null) descriptionText.text = gene.description;
            if (energyText != null) energyText.text = $"⚡ {gene.energyCost}";
            if (statsText != null) statsText.text = gene.GetStatsSummary();

            if (leftConnectorText != null) leftConnectorText.text = gene.leftConnector.ToString();
            if (rightConnectorText != null) rightConnectorText.text = gene.rightConnector.ToString();

            if (leftConnectorBadge != null) leftConnectorBadge.color = DNAHelper.GetNucleotideColor(gene.leftConnector);
            if (rightConnectorBadge != null) rightConnectorBadge.color = DNAHelper.GetNucleotideColor(gene.rightConnector);

            if (backgroundImage != null) backgroundImage.color = gene.customColor;

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => onClickCallback?.Invoke(boundGene));
            }
        }
    }
}
