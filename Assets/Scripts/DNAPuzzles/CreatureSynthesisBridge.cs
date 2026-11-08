using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    /// <summary>
    /// Canlının temel vücut tipi morfolojisi.
    /// Arkadaşlarınız 3D model seçerken veya prosedürel oluştururken bu tipi referans alabilir.
    /// </summary>
    public enum CreatureBodyType
    {
        Reptilian,      // Sürüngen / Kertenkele / Lav Ejderhası tipi
        Insectoid,      // Eklembacaklı / Asit Böceği / Kitin Kabuklu
        CryoAvian,      // Buzul Kuşu / Kriyojenik Kanatlı Gezgin
        Leviathan,      // Ağır Zırhlı Biyomekanik Dev
        Bioluminescent, // Işıldayan Derin Biyo-Organizma
        CosmicApex,     // Kuantum / Enerji Tabanlı Apex Canlı
        AberrantMutant  // İstenmeyen Kaotik Hilkat Garibesi / Deforme Yanmış Biyo-Form
    }

    public enum JawType
    {
        SerratedFangs,       // Tırtıklı Lav/Avcı Dişleri (Ejder/Magma)
        Mandibles,           // Eklembacaklı Kıskaçları (Böcek/Asit)
        BeakPredator,        // Kemik Kıran Kriyojenik Gaga (Buz Kuşu)
        CrushingPlates       // Ezici Zırhlı Çene Plakaları (Titan/Leviathan)
    }

    public enum RoarType
    {
        DraconicRoar,            // Akkor Magma Kükremesi (Derin gürleme)
        CryoShriek,              // Kriyojenik Sonik Çığlık (Dondurucu tiz ses)
        InfrasonicSubBass,       // Sismik İnfrasonik Bas (Toprağı sarsan bas)
        InsectoidChirpClick,     // Asidik Tıslama & Kitin Çatırtısı
        PlasmaResonanceHarmonic, // Kuantum Plazma Uğultusu
        AberrantGlitchRoar       // Deforme Kaotik Çığlık (Bozuk cızırtılı kükreme)
    }

    public enum TailType
    {
        None,
        Whip,            // Çevik kamçı kuyruk
        SpikedClub,      // Dikenli zırhlı gülle kuyruk
        Stinger,         // Asit/zehir iğneli akrep kuyruğu
        SteeringFin      // Havada/suda süzülen dümen yüzgeç kuyruk
    }

    public enum VisionType
    {
        ThermalInfrared,     // Termal Kızılötesi Görüş (Isı avcısı)
        CompoundUltraviolet, // Morötesi Petek Göz Görüşü (Böcek)
        PolarizedGlacier,    // Polarize Buzul ve Fırtına Görüşü
        SeismicVibration,    // Sismik Titreşim Algısı (Kör ama duyarlı)
        QuantumResonance     // Kuantum / Enerji Görüşü
    }

    public enum SkinTextureType
    {
        Scales,              // Akkor lav pulları / zırhlı kertenkele derisi
        ChitinShell,         // Eklembacaklı kitin kabuk
        CryoFur,             // Kalın kriyojenik yalıtım kürkü
        ArmoredPlates,       // Titanyum-kemik ağır plakalar
        BioluminescentSlime  // Işıldayan nemli biyo-jel zarı
    }

    public enum GlowPattern
    {
        None,
        Veins,          // Damarlar boyunca akan plazma
        Stripes,        // Yırtıcı kamuflaj çizgileri
        PulseSpots,     // Nabız gibi parlayan biyolüminesans benekler
        CircuitLines    // Biyomekanik hatlar
    }

    /// <summary>
    /// Arkadaşlarınızın 3D canlı modellemede kullanacağı detaylı fenotipik ve görsel organ özellikleri.
    /// Kollar, bacaklar, boyut, dişler, kükreme ve renk paletlerinin tamamını içerir!
    /// </summary>
    [System.Serializable]
    public class CreaturePhenotype
    {
        [Header("Temel Morfoloji ve Boyut")]
        public CreatureBodyType bodyType;
        public float scaleMultiplier = 1.0f; // Boyut katsayısı (0.8x - 2.5x)
        public float heightMeters = 2.0f;    // Canlının boyu/yüksekliği (metre)
        public float weightKg = 150f;        // Canlının tahmini kütlesi (kg)

        [Header("Kollar, Bacaklar ve Ekstremiteler")]
        public float armLength = 1.2f;       // Kol / pençe uzanma mesafesi (metre)
        public int limbCount = 4;            // Kol/bacak sayısı (2, 4, 6, 8)
        public bool hasClaws;                // Yırtıcı pençeler var mı?
        public float clawLength = 0.5f;      // Pençe boyutu

        [Header("Çene, Dişler ve Isırık")]
        public JawType jawType;              // Çene tipi
        public float toothSize = 1.0f;       // Diş büyüklüğü katsayısı (0.6x - 2.8x)
        public bool hasSaberFangs;           // Kılıç dişler / uzatılmış hançer dişler var mı?
        public float biteForceNewton = 800f; // Isırık kuvveti (Newton)

        [Header("Kükreme ve Akustik")]
        public RoarType roarType;            // Kükreme / ses türü
        public float roarPitch = 1.0f;       // Ses perdesi (0.4f pes/kalın - 1.8f tiz/çığlık)
        public int roarVolumeDb = 110;       // Kükreme şiddeti (Desibel)

        [Header("Kuyruk ve Aksesuarlar")]
        public bool hasTail = true;          // Kuyruk var mı?
        public TailType tailType;            // Kuyruk türü (Topuz, kamçı, iğne vb.)
        public float tailLength = 1.5f;      // Kuyruk uzunluğu (metre)

        [Header("Gözler ve Algı")]
        public int eyeCount = 2;             // Göz sayısı (1, 2, 4, 6)
        public VisionType visionType;        // Görme / algılama modu
        public Color eyeColor;               // Göz iris rengi
        public Color eyeGlowColor;           // Gözün karanlıkta parıldama rengi

        [Header("Deri, Doku ve Çok Tonlu Renk Paleti")]
        public SkinTextureType skinTexture;  // Deri dokusu (Pullu, kürklü, kitin, plakalı)
        public Color primarySkinColor;       // Ana gövde/deri rengi
        public Color secondaryColor;        // Sırt zırhı / pul rengi
        public Color underbellyColor;       // Karın / göğüs altı yumuşak doku rengi
        public Color glowColor;             // Biyolüminesans neon rengi
        public GlowPattern glowPattern;     // Işıldama deseni

        [Header("Organ ve Ekstremite Bayrakları")]
        public bool hasArmorPlates;         // Sırt/göğüs zırh plakaları takılı mı?
        public float armorIntensity;        // Zırh yoğunluğu (0.0f - 1.0f)
        public bool hasGillsOrFins;         // Solungaç / biyo-kanatçıklar var mı?
        public float gillScale;             // Solungaç büyüklüğü
        public bool hasSpinalRidge;         // Omurga segmentleri / sırt dikeni var mı?
        public bool hasHorns;               // Isı/savunma boynuzları var mı?
        public float hornScale = 1.0f;      // Boynuz boyutu
        public bool hasBioluminescence;     // Gece/karanlıkta parıldama
        public bool hasCosmicRings;         // Etrafında manyetik helezon halkaları dönüyor mu?

        [Header("Oyun İçi Oynanış İstatistikleri (Canlı Kontrol Edildiğinde)")]
        public float maxHealth = 100f;      // Can puanı
        public float movementSpeed = 6.0f;  // Yürüme/koşma hızı
        public float jumpForce = 7.5f;      // Zıplama yüksekliği
        public float attackDamage = 15f;    // Saldırı gücü
        public float defenseRating = 10f;   // Zırh/hasar azaltma oranı
        public float metabolicEfficiency = 1.0f; // Enerji tasarrufu
    }

    /// <summary>
    /// Puzzle tamamlandığında veya test edildiğinde sentezlenen canlının TÜM verisini içeren paket.
    /// Takım arkadaşlarınız oyunun diğer sahnelerinde veya sistemlerinde tek satırla bu veriyi alır!
    /// </summary>
    [System.Serializable]
    public class SynthesizedCreatureData
    {
        public bool isValid;                     // Sentez verisi geçerli mi?
        public string speciesName;               // Örn: "Pyros-Saurian Alpha", "Cryo-Glider Prime"
        public string speciesTitle;              // Örn: "Akkor Zırhlı Apex Avcı"
        public List<GeneData> equippedGenes = new List<GeneData>();     // Takılan genlerin tam listesi
        [System.NonSerialized]
        public Dictionary<StatType, int> stats = new Dictionary<StatType, int>();  // Final hesaplanmış statlar
        public List<GeneSynergy> activeSynergies = new List<GeneSynergy>();// Aktif biyolojik sinerjiler
        
        public int totalEnergyUsed;              // Tüketilen metabolik enerji
        public int maxEnergyBudget;              // Gezegenin enerji bütçesi
        public int resonanceCount;               // Kusursuz uyumlu bağ sayısı
        public int instabilityCount;             // Uyumsuzluk/mutasyon sayısı
        
        public PlanetData targetPlanet;          // Gönderilen gezegen
        public bool isSurvived;                  // Hayatta kaldı mı?
        public int starRating;                   // 1 - 3 Yıldız
        public string survivalSummary;           // Rapor özeti
        
        public CreaturePhenotype phenotype = new CreaturePhenotype();      // Arkadaşlarınızın 3D modelleyeceği görsel veriler
    }

    /// <summary>
    /// DNA Yapbozu ile oyunun diğer bölümleri (Canlı Modelleme, Gezegen Sahnesi, vb.) 
    /// arasındaki ana iletişim köprüsü.
    /// Arkadaşlarınız `CreatureSynthesisBridge.LastSynthesizedCreature` ile canlının tüm verisine ulaşabilir!
    /// </summary>
    public static class CreatureSynthesisBridge
    {
        /// <summary>
        /// En son sentezlenen ve gezegene gönderilen canlının tüm verisi.
        /// </summary>
        public static SynthesizedCreatureData LastSynthesizedCreature { get; private set; }

        /// <summary>
        /// Canlı başarıyla sentezlenip gezegende hayatta kaldığında tetiklenen evrensel event.
        /// Arkadaşlarınız bu evente abone olarak kendi canlı sahnelerini başlatabilir!
        /// </summary>
        public static event Action<SynthesizedCreatureData> OnCreatureReadyForGameplay;

        /// <summary>
        /// Canlı testi tamamlandığında (başarılı veya başarısız) tetiklenen event.
        /// </summary>
        public static event Action<SynthesizedCreatureData> OnCreatureEvaluated;

        /// <summary>
        /// Yapboz ekranında canlı önizlemesi (3D Holo-Organizma) için event tetiklemeden fenotip hesaplar.
        /// </summary>
        public static SynthesizedCreatureData PreviewFromPuzzle(
            GeneData[] strand,
            Dictionary<StatType, int> stats,
            List<GeneSynergy> synergies,
            PlanetData planet)
        {
            return CreateCreatureDataInternal(strand, stats, synergies, planet, null);
        }

        /// <summary>
        /// Yapbozdan gelen verilerle canlının fenotipini, adını ve oyun istatistiklerini sentezler ve eventleri tetikler.
        /// </summary>
        public static SynthesizedCreatureData BuildFromPuzzle(
            GeneData[] strand, 
            Dictionary<StatType, int> stats, 
            List<GeneSynergy> synergies, 
            PlanetData planet, 
            EvaluationResult eval)
        {
            var data = CreateCreatureDataInternal(strand, stats, synergies, planet, eval);

            LastSynthesizedCreature = data;
            OnCreatureEvaluated?.Invoke(data);

            if (data.isSurvived)
            {
                OnCreatureReadyForGameplay?.Invoke(data);
            }

            return data;
        }

        private static SynthesizedCreatureData CreateCreatureDataInternal(
            GeneData[] strand,
            Dictionary<StatType, int> stats,
            List<GeneSynergy> synergies,
            PlanetData planet,
            EvaluationResult eval)
        {
            var data = new SynthesizedCreatureData();
            data.isValid = true;
            data.equippedGenes = new List<GeneData>();
            if (strand != null)
            {
                foreach (var g in strand)
                {
                    if (g != null) data.equippedGenes.Add(g);
                }
            }

            data.stats = stats != null ? new Dictionary<StatType, int>(stats) : new Dictionary<StatType, int>();
            data.activeSynergies = synergies != null ? new List<GeneSynergy>(synergies) : new List<GeneSynergy>();
            data.targetPlanet = planet;

            if (eval != null)
            {
                data.isSurvived = eval.isSuccess;
                data.starRating = eval.starRating;
                data.totalEnergyUsed = eval.totalEnergyUsed;
                data.maxEnergyBudget = eval.maxEnergyBudget;
                data.resonanceCount = eval.resonanceCount;
                data.instabilityCount = eval.instabilityCount;
                data.survivalSummary = eval.detailedMessage;
            }

            // Canlının Görsel Fenotipini (Arkadaşlarınızın Modellerini) Hesapla
            data.phenotype = GeneratePhenotype(data.equippedGenes, data.stats, planet, eval);

            // Canlının Dinamik Tür Adını ve Başlığını Belirle
            GenerateSpeciesIdentity(data, eval == null);

            return data;
        }

        private static CreaturePhenotype GeneratePhenotype(
            List<GeneData> genes, 
            Dictionary<StatType, int> stats, 
            PlanetData planet, 
            EvaluationResult eval)
        {
            var p = new CreaturePhenotype();

            int heat = stats.ContainsKey(StatType.HeatResistance) ? stats[StatType.HeatResistance] : 0;
            int cold = stats.ContainsKey(StatType.ColdResistance) ? stats[StatType.ColdResistance] : 0;
            int tox = stats.ContainsKey(StatType.ToxicityFilter) ? stats[StatType.ToxicityFilter] : 0;
            int rad = stats.ContainsKey(StatType.RadiationShield) ? stats[StatType.RadiationShield] : 0;
            int press = stats.ContainsKey(StatType.PressureTolerance) ? stats[StatType.PressureTolerance] : 0;
            int oxy = stats.ContainsKey(StatType.OxygenEfficiency) ? stats[StatType.OxygenEfficiency] : 0;

            // 1. Vücut Tipi (Body Type) Belirleme
            bool hasDeformedGene = genes != null && genes.Exists(g => g != null && g.isDeformed);
            if (hasDeformedGene)
            {
                p.bodyType = CreatureBodyType.AberrantMutant; // İstenmeyen deforme kaotik mutant
            }
            else if (heat >= 3 && heat > cold) p.bodyType = CreatureBodyType.Reptilian; // Lav Ejderi/Kertenkele
            else if (cold >= 3 && cold > heat) p.bodyType = CreatureBodyType.CryoAvian; // Kriyojenik Kanatlı
            else if (tox >= 3 || rad >= 3) p.bodyType = CreatureBodyType.Insectoid; // Asit Eklembacaklı
            else if (press >= 3) p.bodyType = CreatureBodyType.Leviathan; // Zırhlı Dev
            else if (eval != null && eval.resonanceCount >= 3) p.bodyType = CreatureBodyType.CosmicApex; // Kozmik Apex
            else p.bodyType = CreatureBodyType.Bioluminescent; // Nötr Işıldayan

            // 2. Renk Paleti (Canlının 3D Materyal Renkleri)
            if (p.bodyType == CreatureBodyType.AberrantMutant)
            {
                p.primarySkinColor = new Color(0.24f, 0.06f, 0.28f); // Koyu Yanık Karbon & Mor
                p.secondaryColor = new Color(0.42f, 0.05f, 0.08f);   // Çürük Kan Kırmızısı
                p.underbellyColor = new Color(0.10f, 0.22f, 0.08f);  // Zehirli Küf Yeşili
                p.glowColor = new Color(0.95f, 0.15f, 0.85f);        // Kararsız Plazma Işığı
            }
            else if (p.bodyType == CreatureBodyType.Reptilian)
            {
                p.primarySkinColor = new Color(0.95f, 0.35f, 0.10f); // Magma Turuncusu
                p.secondaryColor = new Color(0.18f, 0.08f, 0.05f);   // Volkanik Bazalt
                p.glowColor = new Color(1f, 0.85f, 0.20f);           // Akkor Kor
            }
            else if (p.bodyType == CreatureBodyType.CryoAvian)
            {
                p.primarySkinColor = new Color(0.25f, 0.82f, 1f);    // Buzul Camgöbeği
                p.secondaryColor = new Color(0.85f, 0.95f, 1f);      // Kar Beyazı
                p.glowColor = new Color(0.40f, 0.95f, 1f);           // Kriyojenik Parıltı
            }
            else if (p.bodyType == CreatureBodyType.Insectoid)
            {
                p.primarySkinColor = new Color(0.25f, 0.85f, 0.25f); // Asit Yeşili
                p.secondaryColor = new Color(0.65f, 0.15f, 0.85f);   // Radyasyon Menekşesi
                p.glowColor = new Color(0.45f, 1f, 0.30f);           // Biyo-Işık
            }
            else if (p.bodyType == CreatureBodyType.Leviathan)
            {
                p.primarySkinColor = new Color(0.20f, 0.35f, 0.55f); // Ağır Titanyum Mavisi
                p.secondaryColor = new Color(0.10f, 0.15f, 0.25f);   // Derin Zırh
                p.glowColor = new Color(0.30f, 0.85f, 1f);
            }
            else if (p.bodyType == CreatureBodyType.CosmicApex)
            {
                p.primarySkinColor = new Color(0.85f, 0.25f, 1f);    // Kuantum Plazma Moru
                p.secondaryColor = new Color(1f, 0.88f, 0.20f);      // Altın Enerji
                p.glowColor = new Color(0.40f, 1f, 0.90f);
            }
            else
            {
                p.primarySkinColor = new Color(0.20f, 0.75f, 0.95f); // Standart Neon Turkuaz
                p.secondaryColor = new Color(0.12f, 0.22f, 0.35f);
                p.glowColor = new Color(0.35f, 0.90f, 1f);
            }

            // 3. Organ & Ekstremite Kontrolleri
            p.hasArmorPlates = (press >= 2 || heat >= 2 || genes.Exists(g => g.category == GeneCategory.Exoskeleton));
            p.armorIntensity = Mathf.Clamp01((press + heat) / 6.0f);

            p.hasGillsOrFins = (oxy >= 2 || genes.Exists(g => g.category == GeneCategory.Metabolic));
            p.gillScale = Mathf.Clamp(0.5f + (oxy * 0.25f), 0.5f, 1.4f);

            p.hasSpinalRidge = genes.Exists(g => g.category == GeneCategory.Structural);
            p.hasHorns = (heat >= 3 || genes.Exists(g => g.category == GeneCategory.Exoskeleton));
            p.hornScale = Mathf.Clamp(0.6f + (heat * 0.35f), 0.5f, 2.5f);

            p.hasBioluminescence = (rad >= 2 || tox >= 2 || genes.Exists(g => g.category == GeneCategory.Cellular));
            p.hasCosmicRings = (genes.Exists(g => g.category == GeneCategory.Cosmic) || (eval != null && eval.resonanceCount >= 2));

            // 4. Kollar, Bacaklar ve Ekstremite Uzunlukları
            p.limbCount = (p.bodyType == CreatureBodyType.Insectoid) ? 6 : 4;
            p.armLength = Mathf.Clamp(0.9f + (oxy * 0.22f) + (press * 0.16f), 0.8f, 2.5f);
            p.hasClaws = (heat >= 2 || tox >= 2 || genes.Exists(g => g.category == GeneCategory.Exoskeleton));
            p.clawLength = Mathf.Clamp(0.4f + (heat * 0.20f) + (tox * 0.22f), 0.3f, 2.0f);

            // 5. Diş Boyutu, Çene Tipi ve Isırık Kuvveti
            p.biteForceNewton = 600f + (press * 480f) + (heat * 320f);
            if (heat >= 3)
            {
                p.jawType = JawType.SerratedFangs;
                p.toothSize = Mathf.Clamp(1.2f + (heat * 0.30f), 1.0f, 2.8f);
                p.hasSaberFangs = heat >= 4;
            }
            else if (tox >= 3 || p.bodyType == CreatureBodyType.Insectoid)
            {
                p.jawType = JawType.Mandibles;
                p.toothSize = 1.6f;
                p.hasSaberFangs = false;
            }
            else if (cold >= 3 || p.bodyType == CreatureBodyType.CryoAvian)
            {
                p.jawType = JawType.BeakPredator;
                p.toothSize = 1.3f;
                p.hasSaberFangs = false;
            }
            else if (press >= 3 || p.bodyType == CreatureBodyType.Leviathan)
            {
                p.jawType = JawType.CrushingPlates;
                p.toothSize = 2.1f;
                p.hasSaberFangs = true;
            }
            else
            {
                p.jawType = JawType.SerratedFangs;
                p.toothSize = 1.0f;
                p.hasSaberFangs = false;
            }

            // 6. Kükreme Türü, Perdesi ve Desibeli
            if (hasDeformedGene)
            {
                p.roarType = RoarType.AberrantGlitchRoar;
                p.roarPitch = 0.62f;
                p.roarVolumeDb = 138;
            }
            else if (heat >= 3)
            {
                p.roarType = RoarType.DraconicRoar;
                p.roarPitch = Mathf.Clamp(0.85f - (press * 0.08f), 0.45f, 1.1f);
                p.roarVolumeDb = Mathf.RoundToInt(115 + (heat * 4));
            }
            else if (cold >= 3)
            {
                p.roarType = RoarType.CryoShriek;
                p.roarPitch = Mathf.Clamp(1.35f + (oxy * 0.10f), 1.1f, 1.85f);
                p.roarVolumeDb = Mathf.RoundToInt(110 + (cold * 3));
            }
            else if (press >= 3)
            {
                p.roarType = RoarType.InfrasonicSubBass;
                p.roarPitch = 0.40f;
                p.roarVolumeDb = Mathf.RoundToInt(125 + (press * 3));
            }
            else if (tox >= 3 || rad >= 3)
            {
                p.roarType = RoarType.InsectoidChirpClick;
                p.roarPitch = 1.25f;
                p.roarVolumeDb = 105;
            }
            else if (eval != null && eval.resonanceCount >= 3)
            {
                p.roarType = RoarType.PlasmaResonanceHarmonic;
                p.roarPitch = 1.0f;
                p.roarVolumeDb = 130;
            }
            else
            {
                p.roarType = RoarType.DraconicRoar;
                p.roarPitch = 1.0f;
                p.roarVolumeDb = 95;
            }

            // 7. Boyut, Yükseklik ve Ağırlık Hesaplama
            p.scaleMultiplier = Mathf.Clamp(0.85f + (press * 0.18f) + (heat * 0.08f), 0.8f, 2.5f);
            p.heightMeters = Mathf.Round((1.65f * p.scaleMultiplier) * 10f) / 10f;
            p.weightKg = Mathf.Round(95f * Mathf.Pow(p.scaleMultiplier, 2.7f) + (press * 160f));

            // 8. Kuyruk Morfolojisi
            p.hasTail = true;
            if (p.bodyType == CreatureBodyType.Reptilian)
            {
                p.tailType = TailType.Whip;
                p.tailLength = 2.2f * p.scaleMultiplier;
            }
            else if (p.bodyType == CreatureBodyType.Leviathan)
            {
                p.tailType = TailType.SpikedClub;
                p.tailLength = 1.8f * p.scaleMultiplier;
            }
            else if (p.bodyType == CreatureBodyType.Insectoid)
            {
                p.tailType = TailType.Stinger;
                p.tailLength = 1.9f * p.scaleMultiplier;
            }
            else if (p.bodyType == CreatureBodyType.CryoAvian)
            {
                p.tailType = TailType.SteeringFin;
                p.tailLength = 1.5f * p.scaleMultiplier;
            }
            else
            {
                p.tailType = TailType.Whip;
                p.tailLength = 1.4f;
            }

            // 9. Gözler ve Algılama Modu
            if (p.bodyType == CreatureBodyType.Insectoid)
            {
                p.eyeCount = 6;
                p.visionType = VisionType.CompoundUltraviolet;
                p.eyeColor = new Color(0.85f, 0.1f, 0.95f);
                p.eyeGlowColor = new Color(0.4f, 1f, 0.2f);
            }
            else if (p.bodyType == CreatureBodyType.CryoAvian)
            {
                p.eyeCount = 4;
                p.visionType = VisionType.PolarizedGlacier;
                p.eyeColor = new Color(0.1f, 0.85f, 1f);
                p.eyeGlowColor = new Color(0.5f, 0.95f, 1f);
            }
            else if (p.bodyType == CreatureBodyType.Leviathan)
            {
                p.eyeCount = 2;
                p.visionType = VisionType.SeismicVibration;
                p.eyeColor = new Color(0.3f, 0.6f, 0.9f);
                p.eyeGlowColor = new Color(0.2f, 0.7f, 1f);
            }
            else if (p.bodyType == CreatureBodyType.Reptilian)
            {
                p.eyeCount = 2;
                p.visionType = VisionType.ThermalInfrared;
                p.eyeColor = new Color(1f, 0.75f, 0.1f);
                p.eyeGlowColor = new Color(1f, 0.25f, 0.05f);
            }
            else
            {
                p.eyeCount = 2;
                p.visionType = VisionType.ThermalInfrared;
                p.eyeColor = Color.cyan;
                p.eyeGlowColor = Color.white;
            }

            // 10. Deri Dokusu, Çok Tonlu Renk Paleti ve Işıma Deseni
            if (p.bodyType == CreatureBodyType.Reptilian)
            {
                p.skinTexture = SkinTextureType.Scales;
                p.underbellyColor = new Color(1f, 0.68f, 0.25f);
                p.glowPattern = GlowPattern.Veins;
            }
            else if (p.bodyType == CreatureBodyType.CryoAvian)
            {
                p.skinTexture = SkinTextureType.CryoFur;
                p.underbellyColor = new Color(0.92f, 0.98f, 1f);
                p.glowPattern = GlowPattern.Stripes;
            }
            else if (p.bodyType == CreatureBodyType.Insectoid)
            {
                p.skinTexture = SkinTextureType.ChitinShell;
                p.underbellyColor = new Color(0.60f, 0.95f, 0.35f);
                p.glowPattern = GlowPattern.PulseSpots;
            }
            else if (p.bodyType == CreatureBodyType.Leviathan)
            {
                p.skinTexture = SkinTextureType.ArmoredPlates;
                p.underbellyColor = new Color(0.18f, 0.26f, 0.40f);
                p.glowPattern = GlowPattern.CircuitLines;
            }
            else if (p.bodyType == CreatureBodyType.CosmicApex)
            {
                p.skinTexture = SkinTextureType.BioluminescentSlime;
                p.underbellyColor = new Color(1f, 0.92f, 0.45f);
                p.glowPattern = GlowPattern.Veins;
            }
            else
            {
                p.skinTexture = SkinTextureType.Scales;
                p.underbellyColor = new Color(0.4f, 0.85f, 0.95f);
                p.glowPattern = GlowPattern.PulseSpots;
            }

            // 11. Oyun İçi Oynanış İstatistikleri (Canlı Oyunda Kontrol Edilirse)
            p.maxHealth = 80f + (press * 28f) + (heat * 16f);
            p.movementSpeed = Mathf.Clamp(5.5f + (oxy * 1.3f) - (p.armorIntensity * 1.6f), 3.5f, 10.5f);
            p.jumpForce = Mathf.Clamp(6.5f + (oxy * 0.9f) - (press * 0.4f), 4.5f, 11f);
            p.attackDamage = 12f + (heat * 6f) + (tox * 8f);
            p.defenseRating = 5f + (press * 8f) + (cold * 5f);

            return p;
        }

        private static void GenerateSpeciesIdentity(SynthesizedCreatureData data, bool isPreview = false)
        {
            var p = data.phenotype;
            string prefix = "Neo";
            string root = "Organizma";
            string suffix = "Prime";

            int heat = data.stats.ContainsKey(StatType.HeatResistance) ? data.stats[StatType.HeatResistance] : 0;
            int cold = data.stats.ContainsKey(StatType.ColdResistance) ? data.stats[StatType.ColdResistance] : 0;
            int tox = data.stats.ContainsKey(StatType.ToxicityFilter) ? data.stats[StatType.ToxicityFilter] : 0;
            int rad = data.stats.ContainsKey(StatType.RadiationShield) ? data.stats[StatType.RadiationShield] : 0;
            int press = data.stats.ContainsKey(StatType.PressureTolerance) ? data.stats[StatType.PressureTolerance] : 0;

            if (heat >= 3) prefix = "Pyro";
            else if (cold >= 3) prefix = "Cryo";
            else if (tox >= 3) prefix = "Toxi";
            else if (rad >= 3) prefix = "Radia";
            else if (press >= 3) prefix = "Titan";

            switch (p.bodyType)
            {
                case CreatureBodyType.Reptilian: root = "Saurian"; break;
                case CreatureBodyType.Insectoid: root = "Chitin"; break;
                case CreatureBodyType.CryoAvian: root = "Glider"; break;
                case CreatureBodyType.Leviathan: root = "Goliath"; break;
                case CreatureBodyType.CosmicApex: root = "Archon"; break;
                default: root = "Bioform"; break;
            }

            if (data.equippedGenes.Count == 0)
            {
                data.speciesName = "HOLO-ORGANİZMA";
                data.speciesTitle = "Biyo-Sentez Hazır";
                return;
            }

            bool hasDeformedGene = data.equippedGenes.Exists(g => g != null && g.isDeformed);
            if (hasDeformedGene)
            {
                data.speciesName = "Kaotik Hilkat Garibesi";
                data.speciesTitle = "☣️ [İSTENMEYEN MUTASYON: Kararsız Aberrant]";
                return;
            }

            if (isPreview)
            {
                suffix = "Alpha";
                data.speciesTitle = $"Canlı Model: {p.bodyType}";
            }
            else if (data.isSurvived)
            {
                suffix = data.starRating == 3 ? "Apex" : (data.starRating == 2 ? "Dominus" : "Survivor");
                data.speciesTitle = $"{data.targetPlanet?.planetName ?? "Bilinmeyen"} Kolonisti";
            }
            else
            {
                suffix = "Mutant-X";
                data.speciesTitle = "Kararsız Prototip (Uyumsuz)";
            }

            data.speciesName = $"{prefix}-{root} {suffix}";
        }
    }
}
