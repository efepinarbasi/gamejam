# 🧬 Biyo-Sentez & 3D Canlı Modelleme Entegrasyon Rehberi

Bu rehber, **DNA Genom Yapbozu** ile ekibinizin geliştirdiği **Canlı Modelleme (3D/Blender/Rig), Ses ve Gezegen Oynanış Sistemleri** arasındaki köprüyü ve veri akışını açıklar.

---

## 🚀 1. Hızlı Başlangıç (Tek Satırda Canlı Verisine Erişim)

Arkadaşlarınız kendi sahnelerinde veya canavar oluşturucularında (Spawner) en son sentezlenen canlıya doğrudan static erişim sağlayabilir:

```csharp
using DNAPuzzle;
using UnityEngine;

// Son sentezlenen canlının tüm verisini al:
SynthesizedCreatureData creature = CreatureSynthesisBridge.LastSynthesizedCreature;

if (creature != null && creature.isSurvived)
{
    Debug.Log($"Canlı Hazır: {creature.speciesName} ({creature.speciesTitle})");
    Debug.Log($"Boy: {creature.phenotype.heightMeters:F1}m, Kütle: {creature.phenotype.weightKg:F0}kg");
    Debug.Log($"Kol Uzanımı: {creature.phenotype.armLength:F2}m, Diş Katsayısı: {creature.phenotype.toothSize:F1}x");
    Debug.Log($"Kükreme Tipi: {creature.phenotype.roarType}, Ses Perdesi (Pitch): {creature.phenotype.roarPitch:F2}");
}
```

---

## 📡 2. Event ile Otomatik Tetikleme (Gezegene İniş Tamamlandığında)

Puzzle başarıyla tamamlanıp canlı gezegene fırlatıldığında ve hayatta kalma testi geçildiğinde bu event tetiklenir:

```csharp
using DNAPuzzle;
using UnityEngine;

public class CreatureSpawnerManager : MonoBehaviour
{
    [SerializeField] private AudioSource roarAudioSource;

    private void OnEnable()
    {
        CreatureSynthesisBridge.OnCreatureReadyForGameplay += OnCreatureSpawn;
    }

    private void OnDisable()
    {
        CreatureSynthesisBridge.OnCreatureReadyForGameplay -= OnCreatureSpawn;
    }

    private void OnCreatureSpawn(SynthesizedCreatureData data)
    {
        var p = data.phenotype;
        Debug.Log($"[SPAWN] {data.speciesName} gezegende uyandı!");

        // 1. Vücut tipine uygun ana modeli yükle / spawn et
        GameObject prefab = GetPrefabByBodyType(p.bodyType);
        GameObject spawned = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        // 2. Boyut ve Kütle Ölçeklendirme (Rig / Transform)
        spawned.transform.localScale = Vector3.one * p.scaleMultiplier;
        var rb = spawned.GetComponent<Rigidbody>();
        if (rb != null) rb.mass = p.weightKg;

        // 3. Kol Uzunluğu (Arm Reach / Rig Kemik Uzunluğu veya Scale)
        Transform armLeft = spawned.transform.Find("Rig/Spine/UpperArm_L");
        Transform armRight = spawned.transform.Find("Rig/Spine/UpperArm_R");
        if (armLeft != null) armLeft.localScale = new Vector3(1f, p.armLength / 1.2f, 1f);
        if (armRight != null) armRight.localScale = new Vector3(1f, p.armLength / 1.2f, 1f);

        // 4. Çene & Diş Boyutlandırma (BlendShape veya Kemik)
        var headRenderer = spawned.GetComponentInChildren<SkinnedMeshRenderer>();
        if (headRenderer != null)
        {
            // Örnek blendshape eşleştirmesi:
            int toothIndex = headRenderer.sharedMesh.GetBlendShapeIndex("ToothScale");
            if (toothIndex >= 0) headRenderer.SetBlendShapeWeight(toothIndex, (p.toothSize - 1.0f) * 50f);

            int saberIndex = headRenderer.sharedMesh.GetBlendShapeIndex("SaberFangs");
            if (saberIndex >= 0) headRenderer.SetBlendShapeWeight(saberIndex, p.hasSaberFangs ? 100f : 0f);
        }

        // 5. Renk Paletlerini Materyale Aktarma
        var mainRenderer = spawned.GetComponentInChildren<Renderer>();
        if (mainRenderer != null)
        {
            mainRenderer.material.SetColor("_BaseColor", p.primarySkinColor);
            mainRenderer.material.SetColor("_ArmorColor", p.secondaryColor);
            mainRenderer.material.SetColor("_BellyColor", p.underbellyColor);
            mainRenderer.material.SetColor("_EmissionColor", p.glowColor);
        }

        // 6. Kükreme & Ses Akustiği (AudioSource Pitch / Volume)
        if (roarAudioSource != null)
        {
            roarAudioSource.pitch = p.roarPitch; // 0.4 pes dev gürlemesi ~ 1.8 tiz çığlık
            roarAudioSource.volume = Mathf.Clamp01(p.roarVolumeDb / 130f);
            AudioClip clip = GetRoarClip(p.roarType);
            if (clip != null) roarAudioSource.PlayOneShot(clip);
        }

        // 7. Oynanış Kontrolcüsüne Verileri İletme
        var controller = spawned.GetComponent<CreatureController>();
        if (controller != null)
        {
            controller.maxHealth = p.maxHealth;
            controller.moveSpeed = p.movementSpeed;
            controller.jumpForce = p.jumpForce;
            controller.attackDamage = p.attackDamage;
            controller.biteForce = p.biteForceNewton;
            controller.defenseRating = p.defenseRating;
        }
    }

    private GameObject GetPrefabByBodyType(CreatureBodyType type) { /* ... */ return null; }
    private AudioClip GetRoarClip(RoarType roar) { /* ... */ return null; }
}
```

---

## 🦎 3. Fenotip ve Vücut Tipleri (`CreatureBodyType`)

| Vücut Tipi (`CreatureBodyType`) | Baskın Koşul | Örnek Model / Referans |
|---|---|---|
| `Reptilian` | Yüksek Isı Direnci (>=3) | Lav kertenkelesi, pullu ejder, zırhlı kertenkele |
| `CryoAvian` | Yüksek Soğuk Direnci (>=3) | Buzul kuşu, tüylü kanatlı yırtıcı |
| `Insectoid` | Yüksek Zehir / Radyasyon (>=3) | Kitin kabuklu böcek, asit akrebi |
| `Leviathan` | Yüksek Basınç Dayanımı (>=3) | Ağır kabuklu biyo-dev, taş zırhlı titan |
| `CosmicApex` | Çoklu Rezonans & Kuantum Gen | Kuantum ışıldayan, boynuzlu ve manyetik helezonlu süper-avcı |
| `Bioluminescent` | Nötr / Standart Hücre | Fosforlu ışıldayan, esnek biyo-organizma |

---

## 📐 4. Boyut, Morfoloji ve Ekstremite Parametreleri

Modelcilerin blend shape, kemik ölçeği (bone scale) veya rig transformlarında kullanacağı sayısal değerler:

| Değişken Adı | Tip | Birim / Aralık | Açıklama |
|---|---|---|---|
| `p.scaleMultiplier` | `float` | `0.8x - 2.5x` | Canlının genel transform ölçeği |
| `p.heightMeters` | `float` | `1.4m - 5.5m` | Canlının dik duruş boyu (metre) |
| `p.weightKg` | `float` | `60kg - 1800kg` | Canlının toplam ağırlığı (Rigidbody mass) |
| `p.armLength` | `float` | `0.8m - 3.2m` | Kolların/pençelerin erişim mesafesi |
| `p.limbCount` | `int` | `2, 4, 6, 8` | Aktif bacak/kol ekstremite sayısı |
| `p.hasClaws` | `bool` | `true/false` | Yırtıcı pençeler aktif mi? |
| `p.clawLength` | `float` | `0.2m - 0.9m` | Pençe tırnak boyutu |
| `p.hasTail` | `bool` | `true/false` | Kuyruk var mı? |
| `p.tailType` | `TailType` | `None, Whip, SpikedClub, Stinger, SteeringFin` | Kuyruğun morfolojik tipi |
| `p.tailLength` | `float` | `0.8m - 4.5m` | Kuyruk omurga uzunluğu |
| `p.hasHorns` | `bool` | `true/false` | Kafa boynuzları/kıskaçları var mı? |
| `p.hornScale` | `float` | `0.5x - 3.0x` | Boynuz büyüklük çarpanı |
| `p.hasArmorPlates`| `bool` | `true/false` | Sırt/göğüs zırh plakaları takılı mı? |
| `p.armorIntensity`| `float` | `0.0 - 1.0` | Plaka kalınlığı / sertliği |

---

## 🦷 5. Çene, Dişler ve Isırık Mekaniği

| Değişken Adı | Tip | Birim / Aralık | Açıklama |
|---|---|---|---|
| `p.jawType` | `JawType` | `SerratedFangs, Mandibles, BeakPredator, CrushingPlates` | Canlının kafa ve çene mimarisi |
| `p.toothSize` | `float` | `0.6x - 2.8x` | Diş katsayısı (Mesh BlendShape için) |
| `p.hasSaberFangs`| `bool` | `true/false` | Hançer/kılıç şeklinde uzamış köpek dişleri |
| `p.biteForceNewton` | `float` | `400N - 3200N` | Isırık mekanik kuvveti |

---

## 🔊 6. Kükreme ve Ses Akustiği (Audio Engine)

Ses tasarımcısı arkadaşınız Unity `AudioSource` veya FMOD/Wwise üzerinde şu parametreleri doğrudan bağlayabilir:

| Değişken Adı | Tip | Değer Aralığı | Açıklama |
|---|---|---|---|
| `p.roarType` | `RoarType` | `DraconicRoar, CryoShriek, InfrasonicSubBass, InsectoidChirpClick, PlasmaResonanceHarmonic` | Çalınacak kükreme ses kütüphanesi |
| `p.roarPitch` | `float` | `0.45f - 1.75f` | `AudioSource.pitch` çarpanı (büyük dev canlılar tok/kalın, böcek/buz canlıları tiz) |
| `p.roarVolumeDb` | `int` | `90dB - 130dB` | Kükreme desibel gücü (Kulak çınlatma & sarsıntı efekti) |

---

## 🎨 7. Deri Dokusu, Gözler ve Çok Tonlu Renk Paleti

Shading ve materyal için hazır hesaplanmış renk kodları:

- `p.skinTexture`: `Scales` (Pullu), `ChitinShell` (Kitin kabuk), `CryoFur` (Kürk), `ArmoredPlates` (Ağır plaka), `BioluminescentSlime` (Biyo-jel)
- `p.primarySkinColor` (`Color`): Canlının ana deri/pul rengi
- `p.secondaryColor` (`Color`): Sırt zırhı, kemik çıkıntıları veya çizgiler
- `p.underbellyColor` (`Color`): Karın ve eklem içi yumuşak doku rengi
- `p.glowColor` (`Color`): Gece parıldayan biyolüminesans neon emisyon rengi
- `p.glowPattern`: `None, Veins (Damarlar), Stripes (Çizgiler), PulseSpots (Benekler), CircuitLines (Biyo-hatlar)`
- `p.eyeCount` (`int`): Göz adedi (1, 2, 4 veya 6 petek göz)
- `p.visionType`: `ThermalInfrared, CompoundUltraviolet, PolarizedGlacier, SeismicVibration, QuantumResonance`
- `p.eyeColor` & `p.eyeGlowColor` (`Color`): Göz bebeği ve karanlıkta parlama rengi

---

## 🌍 8. Gezegen Hayatta Kalma Kriterleri

Canlının fırlatıldığı gezegende hayatta kalması için gereken asgari şartlar:
- **Pyros-9 (Magma):** Isı Direnci >= 3, Basınç >= 1
- **Cryo-Kore (Buzul):** Soğuk Direnci >= 3, Oksijen Verimliliği >= 1
- **Toxis-Prime (Asit):** Toksisite Filtresi >= 3, Radyasyon Kalkanı >= 2
- **Tüm Gezegenler:** Metabolik Enerji <= Maksimum Bütçe, Hatalı Bağ <= İzin Verilen Tolerans.
