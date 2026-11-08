using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Her ekran objesine (Collider'ı olan) eklenir: Sol Ekran, Masa Ekranı, Sağ Ekran.
/// cameraAnchor: kameranın bu ekrana bakarken duracağı boş obje (pozisyon + rotasyon).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ScreenStation : MonoBehaviour
{
    [Header("Kamera")]
    [Tooltip("Kameranın duracağı nokta. Scene'de boş bir obje oluşturup ekrana bakacak şekilde konumlandır.")]
    public Transform cameraAnchor;
    [System.NonSerialized] public Transform displaySurface;
    public float moveDuration = 0.8f;
    public bool OverrideFov = false;
    public float fieldOfView = 40f;

    [Header("Hover (opsiyonel)")]
    [Tooltip("Üzerine gelince açılacak outline/parlama objesi. Boş bırakılabilir.")]
    public GameObject hoverHighlight;

    [Header("Olaylar")]
    [Tooltip("Kamera ekrana vardığında (2D UI'ı burada aç).")]
    public UnityEvent onEnter = new UnityEvent();
    [Tooltip("Base'e dönüş başladığında (2D UI'ı burada kapat).")]
    public UnityEvent onExit = new UnityEvent();

    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (hoverHighlight != null) hoverHighlight.SetActive(false);
    }

    public void SetHover(bool on)
    {
        if (hoverHighlight != null) hoverHighlight.SetActive(on);
    }

    public void SetInteractable(bool on)
    {
        if (_col != null) _col.enabled = on;
        if (!on) SetHover(false);
    }

    public void NotifyEnter() => onEnter?.Invoke();
    public void NotifyExit() => onExit?.Invoke();

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (cameraAnchor == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(cameraAnchor.position, 0.1f);
        Gizmos.DrawRay(cameraAnchor.position, cameraAnchor.forward * 0.6f);
        Gizmos.DrawLine(cameraAnchor.position, transform.position);
    }
#endif
}
