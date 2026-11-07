using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    public class BondStatus
    {
        public int leftSlotIndex;
        public int rightSlotIndex;
        public NucleotideType leftConnector;
        public NucleotideType rightConnector;
        public bool isResonant; // True: A-T, C-G veya Universal eşleşmesi
        public bool isMismatch; // True: Uyumsuz bağlantı (Instability üretir)
    }

    public class EvaluationResult
    {
        public bool isSuccess;
        public int starRating; // 1, 2, 3 Yıldız
        public int totalEnergyUsed;
        public int maxEnergyBudget;
        public int instabilityCount;
        public int maxAllowedInstability;
        public int resonanceCount;
        public bool isDeformedCreature;
        public string statusTitle;
        public string detailedMessage;
        public Dictionary<StatType, int> currentStats = new Dictionary<StatType, int>();
        public List<string> failedReasons = new List<string>();
    }

    public class DNAPuzzleManager : MonoBehaviour
    {
        public static DNAPuzzleManager Instance { get; private set; }

        [Header("Aktif Seviye")]
        [SerializeField] private PlanetData currentPlanet;
        [SerializeField] private List<GeneData> genePool = new List<GeneData>();

        [Header("DNA Sarmalı (Slotlar)")]
        private GeneData[] strandGenes;

        // Eventler - UI ve sistem dinleyicileri için
        public event Action OnStrandUpdated;
        public event Action<PlanetData> OnPlanetChanged;
        public event Action<EvaluationResult> OnEvaluationCompleted;

        public PlanetData CurrentPlanet => currentPlanet;
        public IReadOnlyList<GeneData> GenePool => genePool;
        public GeneData[] StrandGenes => strandGenes;
        public int SlotCount => strandGenes != null ? strandGenes.Length : 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (currentPlanet != null)
            {
                InitializePlanet(currentPlanet);
            }
        }

        /// <summary>
        /// Yeni bir gezegen seçildiğinde DNA sarmalını o gezegenin yuva sayısına göre ayarlar.
        /// </summary>
        public void InitializePlanet(PlanetData planet)
        {
            currentPlanet = planet;
            int slots = planet != null ? planet.strandSlotCount : 4;
            strandGenes = new GeneData[slots];

            OnPlanetChanged?.Invoke(currentPlanet);
            OnStrandUpdated?.Invoke();
        }

        /// <summary>
        /// DNA havuzunu ayarlar (keşfedilen/kullanılabilir genler).
        /// </summary>
        public void SetGenePool(List<GeneData> newPool)
        {
            genePool = new List<GeneData>(newPool);
            OnStrandUpdated?.Invoke();
        }

        /// <summary>
        /// Bir geni belirli bir yuvaya yerleştirir.
        /// </summary>
        public bool EquipGene(int slotIndex, GeneData gene)
        {
            if (strandGenes == null || slotIndex < 0 || slotIndex >= strandGenes.Length)
                return false;

            strandGenes[slotIndex] = gene;
            OnStrandUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// İlk boş yuvaya geni otomatik yerleştirir.
        /// </summary>
        public bool EquipGeneToFirstAvailableSlot(GeneData gene)
        {
            if (strandGenes == null) return false;

            for (int i = 0; i < strandGenes.Length; i++)
            {
                if (strandGenes[i] == null)
                {
                    strandGenes[i] = gene;
                    OnStrandUpdated?.Invoke();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Yuvadaki geni kaldırır.
        /// </summary>
        public void RemoveGene(int slotIndex)
        {
            if (strandGenes == null || slotIndex < 0 || slotIndex >= strandGenes.Length)
                return;

            strandGenes[slotIndex] = null;
            OnStrandUpdated?.Invoke();
        }

        /// <summary>
        /// İki yuvanın yerini değiştirir (Puzzle çözerken genlerin sırasını ayarlamak için çok kullanışlı).
        /// </summary>
        public void SwapSlots(int slotA, int slotB)
        {
            if (strandGenes == null ||
                slotA < 0 || slotA >= strandGenes.Length ||
                slotB < 0 || slotB >= strandGenes.Length ||
                slotA == slotB)
                return;

            GeneData temp = strandGenes[slotA];
            strandGenes[slotA] = strandGenes[slotB];
            strandGenes[slotB] = temp;

            OnStrandUpdated?.Invoke();
        }

        /// <summary>
        /// Sarmaldaki tüm genleri temizler.
        /// </summary>
        public void ClearStrand()
        {
            if (strandGenes == null) return;
            for (int i = 0; i < strandGenes.Length; i++)
            {
                strandGenes[i] = null;
            }
            OnStrandUpdated?.Invoke();
        }

        /// <summary>
        /// Yuvalar arasındaki tüm bağların durumunu analiz eder (Rezonans veya Kararsızlık).
        /// </summary>
        public List<BondStatus> GetBondStatuses()
        {
            var bonds = new List<BondStatus>();
            if (strandGenes == null || strandGenes.Length < 2) return bonds;

            for (int i = 0; i < strandGenes.Length - 1; i++)
            {
                var leftGene = strandGenes[i];
                var rightGene = strandGenes[i + 1];

                var bond = new BondStatus
                {
                    leftSlotIndex = i,
                    rightSlotIndex = i + 1,
                    isResonant = false,
                    isMismatch = false
                };

                if (leftGene != null && rightGene != null)
                {
                    bond.leftConnector = leftGene.rightConnector;
                    bond.rightConnector = rightGene.leftConnector;

                    if (DNAHelper.IsComplementary(bond.leftConnector, bond.rightConnector))
                    {
                        bond.isResonant = true;
                    }
                    else
                    {
                        bond.isMismatch = true;
                    }
                }

                bonds.Add(bond);
            }

            return bonds;
        }

        /// <summary>
        /// Mevcut genlerin toplam özelliklerini hesaplar (Rezonans bonusu dahil).
        /// </summary>
        public Dictionary<StatType, int> CalculateTotalStats()
        {
            var totals = new Dictionary<StatType, int>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                totals[stat] = 0;
            }

            if (strandGenes == null) return totals;

            // 1. Temel Gen Özelliklerini Ekle
            for (int i = 0; i < strandGenes.Length; i++)
            {
                if (strandGenes[i] == null) continue;

                foreach (var mod in strandGenes[i].statModifiers)
                {
                    totals[mod.stat] += mod.value;
                }
            }

            // 2. Rezonans Bonusu: Her uyumlu nükleotid bağı için bonus ekle
            var bonds = GetBondStatuses();
            foreach (var bond in bonds)
            {
                if (bond.isResonant)
                {
                    // Rezonans kuran genlerin pozitif özelliklerine +1 bonus katılır
                    var leftGene = strandGenes[bond.leftSlotIndex];
                    var rightGene = strandGenes[bond.rightSlotIndex];

                    ApplyResonanceBonus(totals, leftGene);
                    ApplyResonanceBonus(totals, rightGene);
                }
            }

            // 3. Gizli Biyo-Sinerji Bonusları
            var synergies = GetActiveSynergies();
            foreach (var syn in synergies)
            {
                if (syn.synergyId == "syn_astral_overload")
                {
                    foreach (StatType s in Enum.GetValues(typeof(StatType)))
                    {
                        totals[s] += syn.bonusValue;
                    }
                }
                else
                {
                    totals[syn.bonusStat] += syn.bonusValue;
                }
            }

            return totals;
        }

        /// <summary>
        /// Sarmalda şu anda aktif olan tüm biyolojik gen sinerjilerini döner.
        /// </summary>
        public List<GeneSynergy> GetActiveSynergies()
        {
            return GeneSynergyManager.FindActiveSynergies(strandGenes);
        }

        private void ApplyResonanceBonus(Dictionary<StatType, int> totals, GeneData gene)
        {
            if (gene == null || gene.statModifiers == null) return;
            foreach (var mod in gene.statModifiers)
            {
                if (mod.value > 0)
                {
                    totals[mod.stat] += 1; // Uyumlu her bağ genin etkinliğini artırır!
                }
            }
        }

        /// <summary>
        /// Toplam metabolik enerji tüketimini hesaplar.
        /// </summary>
        public int CalculateTotalEnergy()
        {
            if (strandGenes == null) return 0;
            int total = 0;
            for (int i = 0; i < strandGenes.Length; i++)
            {
                if (strandGenes[i] != null)
                {
                    total += strandGenes[i].energyCost;
                }
            }

            // Sinerjilerden gelen enerji tasarrufunu uygula
            var synergies = GetActiveSynergies();
            foreach (var syn in synergies)
            {
                total -= syn.energyReduction;
            }

            return Mathf.Max(0, total);
        }

        /// <summary>
        /// Hatalı/Uyumsuz nükleotid bağlarından kaynaklanan Kararsızlık (Instability) sayısını hesaplar.
        /// </summary>
        public int CalculateInstability()
        {
            int count = 0;
            var bonds = GetBondStatuses();
            foreach (var bond in bonds)
            {
                if (bond.isMismatch)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Aktif rezonans (mükemmel uyum) sayısını döner.
        /// </summary>
        public int CalculateResonanceCount()
        {
            int count = 0;
            var bonds = GetBondStatuses();
            foreach (var bond in bonds)
            {
                if (bond.isResonant)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Canlının bu gezegende hayatta kalıp kalamayacağını kapsamlı şekilde test eder.
        /// </summary>
        public EvaluationResult EvaluateCreatureSurvival()
        {
            var result = new EvaluationResult
            {
                isSuccess = true,
                currentStats = CalculateTotalStats(),
                totalEnergyUsed = CalculateTotalEnergy(),
                maxEnergyBudget = currentPlanet != null ? currentPlanet.maxEnergyBudget : 10,
                instabilityCount = CalculateInstability(),
                maxAllowedInstability = currentPlanet != null ? currentPlanet.maxAllowedInstability : 1,
                resonanceCount = CalculateResonanceCount()
            };

            // Kriter 0: Deforme / Yanmış Gen Kontrolü (İstenmeyen Kaotik Tür)
            bool hasDeformed = false;
            if (strandGenes != null)
            {
                foreach (var g in strandGenes)
                {
                    if (g != null && g.isDeformed)
                    {
                        hasDeformed = true;
                        break;
                    }
                }
            }

            if (hasDeformed)
            {
                result.isSuccess = false;
                result.isDeformedCreature = true;
                result.starRating = 0;
                result.statusTitle = "☣️ [İSTENMEYEN KAOTİK MUTASYON]";
                result.detailedMessage = "Genom aşırı deneme sonucu yandığı için DNA sarmalı bozuldu! İstenmeyen, kontrolsüz bir Hilkat Garibesi (Aberrant Mutant) ortaya çıktı.";
                result.failedReasons.Add("Sarmaldaki genom biyolojik yanma geçirdi ve zararlı mutajene dönüştü!");
            }

            // Kriter 1: En az bir gen yerleştirilmiş olmalı
            bool hasAnyGene = false;
            bool isLacunaStrand = false;
            int equippedCount = 0;
            if (strandGenes != null)
            {
                for (int i = 0; i < strandGenes.Length; i++)
                {
                    if (strandGenes[i] != null)
                    {
                        hasAnyGene = true;
                        equippedCount++;
                        if (strandGenes[i].isLacunaGene) isLacunaStrand = true;
                    }
                }
            }

            if (!hasAnyGene)
            {
                result.isSuccess = false;
                result.failedReasons.Add("DNA sarmalında hiçbir gen bulunmuyor!");
            }

            if (isLacunaStrand && currentPlanet != null)
            {
                string pId = currentPlanet.planetId.ToLowerInvariant();
                string targetP = pId.Contains("snibblex") || pId.Contains("cryo") ? "SNIBBLEX" :
                                 (pId.Contains("oomgarth") || pId.Contains("toxis") ? "OOMGARTH" : "FABAUP");
                var pDef = LacunaGeneticsDatabase.GetPlanet(targetP);

                if (equippedCount < 4)
                {
                    result.isSuccess = false;
                    result.failedReasons.Add($"Ana DNA sarmalına 4 genomun tamamı yerleştirilmelidir! ({equippedCount}/4 Takılı)");
                }

                if (pDef != null && strandGenes != null)
                {
                    for (int i = 0; i < strandGenes.Length; i++)
                    {
                        var g = strandGenes[i];
                        if (g == null) continue;
                        if (g.isBroken && !g.isRepaired)
                        {
                            result.isSuccess = false;
                            result.failedReasons.Add($"{g.geneName} (Slot {g.lacunaSlot}) henüz onarılmadı!");
                        }
                        else if (!pDef.IsGeneAcceptable(g.lacunaSlot, g.geneName))
                        {
                            result.isSuccess = false;
                            result.failedReasons.Add($"Slot {g.lacunaSlot} ({g.geneName}) {pDef.planetName} gezegen kuralına uymuyor!");
                        }
                    }
                }

                if (result.isSuccess && !hasDeformed)
                {
                    result.starRating = 3;
                    result.statusTitle = $"⭐⭐⭐ {targetP} ADAPTASYONU KUSURSUZ";
                    result.detailedMessage = $"Tebrikler! 4 genom ({strandGenes[0]?.geneName}, {strandGenes[1]?.geneName}, {strandGenes[2]?.geneName}, {strandGenes[3]?.geneName}) {targetP} gezegen kurallarına tam uyum sağladı!";
                    int pIdx = targetP == "SNIBBLEX" ? 1 : (targetP == "OOMGARTH" ? 2 : 0);
                    ProgressionManager.SaveStars(pIdx, 3);
                }
                else if (!hasDeformed)
                {
                    result.starRating = 0;
                    result.statusTitle = $"❌ {targetP} ADAPTASYONU BAŞARISIZ";
                    result.detailedMessage = result.failedReasons.Count > 0
                        ? result.failedReasons[0]
                        : "Sarmaldaki genomlar gezegenin kabul kurallarına uymuyor.";
                }

                OnEvaluationCompleted?.Invoke(result);
                return result;
            }

            // Kriter 2: Enerji Bütçesi
            if (result.totalEnergyUsed > result.maxEnergyBudget)
            {
                result.isSuccess = false;
                result.failedReasons.Add($"Metabolik Aşırı Yüklenme: Harcanan Enerji ({result.totalEnergyUsed}) > Maksimum Kapasite ({result.maxEnergyBudget})!");
            }

            // Kriter 3: Genetik Kararsızlık (Instability)
            if (result.instabilityCount > result.maxAllowedInstability)
            {
                result.isSuccess = false;
                result.failedReasons.Add($"Genetik Çöküş: Uyumsuz Bağ Sayısı ({result.instabilityCount}) > Gezegen Toleransı ({result.maxAllowedInstability})!");
            }

            // Kriter 4: Gezegen Hayatta Kalma Gereksinimleri
            if (currentPlanet != null && currentPlanet.requirements != null)
            {
                foreach (var req in currentPlanet.requirements)
                {
                    int currentVal = result.currentStats.ContainsKey(req.stat) ? result.currentStats[req.stat] : 0;
                    if (currentVal < req.requiredValue)
                    {
                        result.isSuccess = false;
                        string statName = DNAHelper.GetStatDisplayName(req.stat);
                        result.failedReasons.Add($"{statName} yetersiz: ({currentVal} / Gereken: {req.requiredValue})");
                    }
                }
            }

            // Yıldız ve Sonuç Başlığı Belirleme
            if (result.isSuccess)
            {
                if (result.instabilityCount == 0 && result.resonanceCount >= 2)
                {
                    result.starRating = 3;
                    result.statusTitle = "⭐⭐⭐ MÜKEMMEL ADAPTASYON";
                    result.detailedMessage = $"Tebrikler! Canlı, {currentPlanet.planetName} gezegeninin acımasız koşullarına kusursuz uyum sağladı. Sıfır mutasyon ve {result.resonanceCount} güçlü rezonans bağı oluşturuldu!";
                }
                else if (result.instabilityCount <= 1 && result.resonanceCount >= 1)
                {
                    result.starRating = 2;
                    result.statusTitle = "⭐⭐ BAŞARILI ADAPTASYON";
                    result.detailedMessage = $"Canlı, {currentPlanet.planetName} üzerinde hayatta kalmayı başardı ve ekosisteme yerleşti.";
                }
                else
                {
                    result.starRating = 1;
                    result.statusTitle = "⭐ KIRILGAN ADAPTASYON";
                    result.detailedMessage = $"Canlı sınırda da olsa hayatta kalabildi. Daha kararlı bir DNA dizilimi türün geleceğini güçlendirebilir.";
                }

                if (currentPlanet != null)
                {
                    int pIdx = currentPlanet.planetId.Contains("cryo") ? 1 :
                               currentPlanet.planetId.Contains("toxis") ? 2 : 0;
                    ProgressionManager.SaveStars(pIdx, result.starRating);
                }
            }
            else
            {
                result.starRating = 0;
                result.statusTitle = "❌ ADAPTASYON BAŞARISIZ";
                result.detailedMessage = "Oluşturulan organizma gezegenin zorlu şartlarına dayanamadı ve elendi.";
            }

            OnEvaluationCompleted?.Invoke(result);
            return result;
        }
    }
}
