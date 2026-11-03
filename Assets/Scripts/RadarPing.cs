
using UnityEngine;

public class RadarPing : MonoBehaviour
{
    [Header("Ping Settings")]
    [SerializeField] private float fadeDuration = 4f;

    private CanvasGroup canvasGroup;
    private float timer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        timer = 0f;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);

        canvasGroup.alpha = alpha;

        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }

    public void Refresh()
    {
        timer = 0f;
        canvasGroup.alpha = 1f;
    }
}
