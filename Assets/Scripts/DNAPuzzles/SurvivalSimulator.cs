using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNAPuzzle
{
    public enum SimulationPhase
    {
        Idle,
        AtmosphericEntry,   // Termal ve Soğuk Testi
        ChemicalAtmosphere, // Toksisite ve Radyasyon Testi
        GravityPressure,    // Basınç ve İskelet Testi
        GeneticIntegrity,   // Genetik Kararlılık ve Kalp/Enerji Testi
        Completed
    }

    public class SurvivalSimulator : MonoBehaviour
    {
        public static SurvivalSimulator Instance { get; private set; }

        [Header("Zamanlama")]
        [SerializeField] private float stepDuration = 1.0f;

        private Coroutine simulationRoutine;

        public event Action<SimulationPhase, string, bool> OnPhaseUpdate; // Phase, Message, isSuccessSoFar
        public event Action<EvaluationResult> OnSimulationFinished;

        public bool IsSimulating => simulationRoutine != null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartSimulation(DNAPuzzleManager manager)
        {
            if (manager == null) return;
            if (simulationRoutine != null)
            {
                StopCoroutine(simulationRoutine);
            }

            simulationRoutine = StartCoroutine(RunSimulationCoroutine(manager));
        }

        private IEnumerator RunSimulationCoroutine(DNAPuzzleManager manager)
        {
            var planet = manager.CurrentPlanet;
            var evaluation = manager.EvaluateCreatureSurvival();
            var stats = manager.CalculateTotalStats();

            bool isLacuna = false;
            if (manager.StrandGenes != null)
            {
                for (int i = 0; i < manager.StrandGenes.Length; i++)
                {
                    if (manager.StrandGenes[i] != null && manager.StrandGenes[i].isLacunaGene)
                    {
                        isLacuna = true;
                        break;
                    }
                }
            }

            if (isLacuna)
            {
                OnPhaseUpdate?.Invoke(
                    SimulationPhase.AtmosphericEntry,
                    $"🔥 FAZ 1: {planet.planetName} atmosferine giriş ve Deri (Slot K) kontrolü...",
                    true
                );
                yield return new WaitForSeconds(stepDuration * 0.75f);

                OnPhaseUpdate?.Invoke(
                    SimulationPhase.GravityPressure,
                    $"🌍 FAZ 2: Yerçekimi & Boy (Slot Z) + Uzuv (Slot P) adaptasyon testi...",
                    true
                );
                yield return new WaitForSeconds(stepDuration * 0.75f);

                OnPhaseUpdate?.Invoke(
                    SimulationPhase.ChemicalAtmosphere,
                    $"👁️ FAZ 3: Görüş (Slot V) ve ●↔■ / ▲↔◆ baz eşlenik kararlılığı taranıyor...",
                    true
                );
                yield return new WaitForSeconds(stepDuration * 0.75f);

                if (!evaluation.isSuccess)
                {
                    string failReason = evaluation.failedReasons.Count > 0 ? evaluation.failedReasons[0] : "Genom gezegen kurallarına uymadı!";
                    OnPhaseUpdate?.Invoke(SimulationPhase.Completed, $"❌ KRİTİK: {failReason}", false);
                    yield return new WaitForSeconds(stepDuration);
                    FinishSimulation(evaluation);
                    yield break;
                }

                OnPhaseUpdate?.Invoke(
                    SimulationPhase.Completed,
                    $"✨ CANLI {planet.planetName.ToUpper()} GEZEGENİNDE HAYATTA KALDI!",
                    true
                );
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            // FAZ 1: Atmosfere Giriş (Termal & Soğuk)
            OnPhaseUpdate?.Invoke(
                SimulationPhase.AtmosphericEntry, 
                $"🔥 FAZ 1: {planet.planetName} atmosferine giriş yapılıyor...", 
                true
            );
            yield return new WaitForSeconds(stepDuration);

            int reqHeat = planet.GetRequiredStatValue(StatType.HeatResistance);
            int curHeat = stats.ContainsKey(StatType.HeatResistance) ? stats[StatType.HeatResistance] : 0;
            int reqCold = planet.GetRequiredStatValue(StatType.ColdResistance);
            int curCold = stats.ContainsKey(StatType.ColdResistance) ? stats[StatType.ColdResistance] : 0;

            if (curHeat < reqHeat)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.AtmosphericEntry, $"❌ KRİTİK: Aşırı ısı kalkanı aştı! ({curHeat}/{reqHeat} Sıcaklık)", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }
            if (curCold < reqCold)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.AtmosphericEntry, $"❌ KRİTİK: Organizma kriyojenik şoka girdi! ({curCold}/{reqCold} Soğuk)", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            OnPhaseUpdate?.Invoke(SimulationPhase.AtmosphericEntry, "✔ Atmosferik termal bariyer başarıyla aşıldı!", true);
            yield return new WaitForSeconds(stepDuration * 0.7f);

            // FAZ 2: Kimyasal ve Radyasyon Filtresi
            OnPhaseUpdate?.Invoke(SimulationPhase.ChemicalAtmosphere, "🧪 FAZ 2: Yüzey kimyası ve radyasyon dalgaları taranıyor...", true);
            yield return new WaitForSeconds(stepDuration);

            int reqTox = planet.GetRequiredStatValue(StatType.ToxicityFilter);
            int curTox = stats.ContainsKey(StatType.ToxicityFilter) ? stats[StatType.ToxicityFilter] : 0;
            int reqRad = planet.GetRequiredStatValue(StatType.RadiationShield);
            int curRad = stats.ContainsKey(StatType.RadiationShield) ? stats[StatType.RadiationShield] : 0;
            int reqOxy = planet.GetRequiredStatValue(StatType.OxygenEfficiency);
            int curOxy = stats.ContainsKey(StatType.OxygenEfficiency) ? stats[StatType.OxygenEfficiency] : 0;

            if (curTox < reqTox)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.ChemicalAtmosphere, $"❌ KRİTİK: Asit ve zehirli gazlar hücreleri eritti! ({curTox}/{reqTox})", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }
            if (curRad < reqRad)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.ChemicalAtmosphere, $"❌ KRİTİK: Radyasyon DNA sarmalını parçaladı! ({curRad}/{reqRad})", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }
            if (curOxy < reqOxy)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.ChemicalAtmosphere, $"❌ KRİTİK: Seyrek atmosferde solunum çöktü! ({curOxy}/{reqOxy} Oksijen)", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            OnPhaseUpdate?.Invoke(SimulationPhase.ChemicalAtmosphere, "✔ Biyo-filtreler ve radyasyon kalkanı stabil!", true);
            yield return new WaitForSeconds(stepDuration * 0.7f);

            // FAZ 3: Basınç ve Yerçekimi
            OnPhaseUpdate?.Invoke(SimulationPhase.GravityPressure, "🌍 FAZ 3: Yüzey yerçekimi ve atmosferik basınç testi...", true);
            yield return new WaitForSeconds(stepDuration);

            int reqPress = planet.GetRequiredStatValue(StatType.PressureTolerance);
            int curPress = stats.ContainsKey(StatType.PressureTolerance) ? stats[StatType.PressureTolerance] : 0;

            if (curPress < reqPress)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.GravityPressure, $"❌ KRİTİK: Canlının iskeleti yüksek basınca dayanamadı! ({curPress}/{reqPress})", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            OnPhaseUpdate?.Invoke(SimulationPhase.GravityPressure, "✔ İskelet ve kas sistemi basınca uyum sağladı!", true);
            yield return new WaitForSeconds(stepDuration * 0.7f);

            // FAZ 4: Genetik Kararlılık ve Kalp / Metabolizma
            OnPhaseUpdate?.Invoke(SimulationPhase.GeneticIntegrity, "🧬 FAZ 4: Kalp atışı ve DNA sarmal kararlılığı kontrol ediliyor...", true);
            yield return new WaitForSeconds(stepDuration);

            if (evaluation.instabilityCount > evaluation.maxAllowedInstability)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.GeneticIntegrity, $"❌ KRİTİK: Aşırı uyuşmaz bağlar ölümcül mutasyona yol açtı!", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            if (evaluation.totalEnergyUsed > evaluation.maxEnergyBudget)
            {
                OnPhaseUpdate?.Invoke(SimulationPhase.GeneticIntegrity, $"❌ KRİTİK: Metabolik yüklenme! Kalp yetmezliği gerçekleşti.", false);
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            if (!evaluation.isSuccess)
            {
                string failReason = evaluation.failedReasons.Count > 0 ? evaluation.failedReasons[0] : "Adaptasyon sınırları aşılamadı!";
                OnPhaseUpdate?.Invoke(
                    SimulationPhase.Completed, 
                    $"❌ KRİTİK: {failReason}", 
                    false
                );
                yield return new WaitForSeconds(stepDuration);
                FinishSimulation(evaluation);
                yield break;
            }

            // NİHAİ BAŞARI
            OnPhaseUpdate?.Invoke(
                SimulationPhase.Completed, 
                $"✨ CANLI {planet.planetName.ToUpper()} GEZEGENİNDE HAYATTA KALDI VE YENİ TÜRE DÖNÜŞTÜ!", 
                true
            );
            yield return new WaitForSeconds(stepDuration);

            FinishSimulation(evaluation);
        }

        private void FinishSimulation(EvaluationResult result)
        {
            simulationRoutine = null;
            OnSimulationFinished?.Invoke(result);
        }
    }
}
