using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kamerayı Base konumundan ekran istasyonlarına (sol / masa / sağ) animasyonla taşır.
/// Free look yok: sadece raycast ile tıkla -> geç, Esc / sağ tık -> Base'e dön.
/// Ana kameraya (veya kamerayı taşıyan boş bir objeye) ekle.
/// </summary>
public class CameraStationController : MonoBehaviour
{
    public enum State { AtBase, Moving, AtStation }

    [Header("Referanslar")]
    [Tooltip("Hareket ettirilecek kamera. Boşsa Camera.main kullanılır.")]
    public Camera cam;
    [Tooltip("Base (başlangıç) bakış noktası. Boşsa oyun başındaki kamera pozu kaydedilir.")]
    public Transform baseAnchor;

    [Header("Raycast")]
    public LayerMask stationMask = ~0;
    public float maxRayDistance = 50f;

    [Header("Lacuna oyun döngüsü")]
    [Tooltip("Sırayla: 0 = sol ekran (sequencer), 1 = orta ekran (gen editörü), 2 = sağ ekran (kapsül). Boş bırakırsan tüm istasyonlar hep açık olur.")]
    public ScreenStation[] loopStations = new ScreenStation[3];

    [Header("Geçiş")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Base'e dönüş süresi")]
    public float returnDuration = 0.7f;

    public State CurrentState { get; private set; } = State.AtBase;
    public ScreenStation ActiveStation { get; private set; }
    public float MoveProgress { get; private set; } = 0f;

    public static CameraStationController Instance { get; private set; }

    /// <summary>Kamera şu an hangi oyun ekranının başında? 0 = sol, 1 = orta, 2 = sağ, -1 = hiçbiri.</summary>
    public int CurrentLoopIndex
    {
        get
        {
            if (CurrentState != State.AtStation || ActiveStation == null || loopStations == null) return -1;
            return System.Array.IndexOf(loopStations, ActiveStation);
        }
    }

    /// <summary>Loop Stations listesinde en az bir istasyon atanmış mı?</summary>
    public bool HasLoopStations
    {
        get
        {
            if (loopStations == null) return false;
            foreach (var s in loopStations)
                if (s != null) return true;
            return false;
        }
    }
    public Vector3 BasePosition => _basePos;
    public Quaternion BaseRotation => _baseRot;
    public float BaseFov => _baseFov;

    /// <summary>
    /// Terminal içi alt menü veya modal açıkken ESC/Sağ tıkın Base'e dönmesini engellemek için geri çağırım.
    /// </summary>
    public static System.Func<bool> IsInputIntercepted;

    ScreenStation _hovered;
    Vector3 _basePos;
    Quaternion _baseRot;
    float _baseFov;
    Coroutine _moveRoutine;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = GetComponent<Camera>() != null ? GetComponent<Camera>() : Camera.main;
        if (cam == null) return;

        if (baseAnchor != null && baseAnchor != cam.transform)
        {
            _basePos = baseAnchor.position;
            _baseRot = baseAnchor.rotation;
            cam.transform.SetPositionAndRotation(_basePos, _baseRot);
        }
        else
        {
            _basePos = cam.transform.position;
            _baseRot = cam.transform.rotation;
        }
        _baseFov = cam.fieldOfView;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (cam != null && CurrentState == State.AtBase)
        {
            cam.transform.SetPositionAndRotation(_basePos, _baseRot);
            cam.fieldOfView = _baseFov;
        }
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.AtBase:
                UpdateHover();
                if (_hovered != null && ClickDown())
                    GoToStation(_hovered);
                break;

            case State.AtStation:
                // Oyun bu ekranı kilitlediyse (süre doldu, yaratık gitti veya öldü) Base'e dön
                if (!IsStationAvailable(ActiveStation))
                {
                    ReturnToBase();
                    break;
                }
                if (BackPressed())
                {
                    if (IsInputIntercepted != null && IsInputIntercepted())
                    {
                        // Terminal veya alt arayüz tarafından yakalandı, Base'e dönme
                    }
                    else
                    {
                        ReturnToBase();
                    }
                }
                break;
        }
    }

    // ---------- Public API ----------

    public void GoToStation(ScreenStation station)
    {
        if (station == null || station.cameraAnchor == null) return;
        if (!IsStationAvailable(station)) return;
        if (CurrentState == State.Moving) return;
        var leaving = ActiveStation;
        SetHovered(null);
        ActiveStation = station;
        StartMove(station.cameraAnchor.position, station.cameraAnchor.rotation,
                  station.OverrideFov ? station.fieldOfView : _baseFov,
                  station.moveDuration, State.AtStation, station, leaving);
        station.SetInteractable(false); // istasyondayken collider tıklamayı engellemesin
    }

    public void ReturnToBase()
    {
        if (CurrentState != State.AtStation) return;
        var leaving = ActiveStation;
        StartMove(_basePos, _baseRot, _baseFov, returnDuration, State.AtBase, null, leaving);
    }

    // ---------- Hareket ----------

    void StartMove(Vector3 pos, Quaternion rot, float fov, float duration,
                   State endState, ScreenStation arrived, ScreenStation left = null)
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveRoutine(pos, rot, fov, duration, endState, arrived, left));
    }

    IEnumerator MoveRoutine(Vector3 toPos, Quaternion toRot, float toFov, float duration,
                            State endState, ScreenStation arrived, ScreenStation left)
    {
        CurrentState = State.Moving;
        if (left != null) left.NotifyExit();

        Transform t = cam.transform;
        Vector3 fromPos = t.position;
        Quaternion fromRot = t.rotation;
        float fromFov = cam.fieldOfView;

        float time = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (time < duration)
        {
            time += Time.deltaTime;
            float rawT = Mathf.Clamp01(time / duration);
            // Sinematik pürüzsüz quintic eğri (smootherstep: 6t^5 - 15t^4 + 10t^3)
            float smoothT = rawT * rawT * rawT * (rawT * (rawT * 6f - 15f) + 10f);
            float k = (ease != null && ease.length > 1) ? ease.Evaluate(rawT) : smoothT;

            // Masaya yaklaşırken doğal kamera kavisi ve hafif odaklanma baş eğimi
            float arc = Mathf.Sin(rawT * Mathf.PI) * 0.09f;
            Quaternion tilt = Quaternion.Euler(Mathf.Sin(rawT * Mathf.PI) * 1.4f, 0f, 0f);
            MoveProgress = (endState == State.AtStation) ? k : (1f - k);
            t.position = Vector3.LerpUnclamped(fromPos, toPos, k) + Vector3.up * arc;
            t.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, k) * tilt;
            cam.fieldOfView = Mathf.LerpUnclamped(fromFov, toFov, k);
            yield return null;
        }

        t.SetPositionAndRotation(toPos, toRot);
        cam.fieldOfView = toFov;
        CurrentState = endState;
        MoveProgress = (endState == State.AtStation) ? 1f : 0f;

        if (arrived != null)
        {
            arrived.NotifyEnter();
            if (left != null && left != arrived) left.SetInteractable(true);
        }
        else
        {
            if (left != null) left.SetInteractable(true);
            ActiveStation = null;
        }
        _moveRoutine = null;
    }

    // ---------- Lacuna: ekran kilidi ----------

    /// <summary>
    /// Bu istasyona şu an girilebilir mi? Sahnede GameManager yoksa veya istasyon
    /// loopStations listesinde değilse her zaman true döner.
    /// </summary>
    bool IsStationAvailable(ScreenStation station)
    {
        if (station == null) return false;

        var gm = Lacuna.GameManager.Instance;
        if (gm == null || loopStations == null) return true;

        int index = System.Array.IndexOf(loopStations, station);
        if (index < 0 || index > 2) return true;
        return gm.CanVisitScreen(index);
    }

    // ---------- Hover ----------

    void UpdateHover()
    {
        if (cam == null) return;
        Vector2 pointerPos = PointerPosition();
        if (!cam.pixelRect.Contains(pointerPos))
        {
            SetHovered(null);
            return;
        }
        Ray ray = cam.ScreenPointToRay(pointerPos);
        ScreenStation hit = null;
        if (Physics.Raycast(ray, out RaycastHit info, maxRayDistance, stationMask, QueryTriggerInteraction.Collide))
            hit = info.collider.GetComponentInParent<ScreenStation>();
        if (hit != null && !IsStationAvailable(hit)) hit = null; // kilitli ekran vurgulanmaz, tıklanmaz
        SetHovered(hit);
    }

    void SetHovered(ScreenStation s)
    {
        if (s == _hovered) return;
        if (_hovered != null) _hovered.SetHover(false);
        _hovered = s;
        if (_hovered != null) _hovered.SetHover(true);
    }

    // ---------- Input (eski + yeni Input System ile uyumlu) ----------

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    static Vector2 PointerPosition() => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    static bool ClickDown() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    static bool BackPressed() =>
        (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
#else
    static Vector2 PointerPosition() => Input.mousePosition;
    static bool ClickDown() => Input.GetMouseButtonDown(0);
    static bool BackPressed() => Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#endif
}