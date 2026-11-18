using UnityEngine;

public class ImplosionTimer : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float implosionTime = 60f;
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private RectTransform progressBarRect;
    
    [Header("Controle")]
    [SerializeField] private bool startOnAwake = true;

    private float currentTime;
    private bool isRunning = false;
    private float initialHeight;

    void Start()
    {
        currentTime = implosionTime;

        if (progressBarRect == null)
        {
            Debug.LogError("O RectTransform da barra não foi atribuído!", this);
        }
        else
        {
            // Salva a altura definida no editor
            initialHeight = progressBarRect.sizeDelta.y;
        }

        if (startOnAwake)
        {
            StartTimer();
        }
    }

    void Update()
    {
        if (!isRunning) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateUI();

            if (currentTime <= 0)
            {
                currentTime = 0;
                TriggerImplosion();
            }
        }
    }

    private void UpdateUI()
    {
        if (progressBarRect != null)
        {
            float progress = currentTime / implosionTime;

            // Altera apenas o scale Y do image da barra de progresso
            Vector2 size = progressBarRect.sizeDelta;
            size.y = initialHeight * progress;
            progressBarRect.sizeDelta = size;
        }
    }

    private void TriggerImplosion()
    {
        isRunning = false;
        Debug.Log("SUBMARINO IMPLODIU! Fim de jogo.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
    }

    public void StartTimer()
    {
        isRunning = true;
        Debug.Log("Timer de implosão iniciado!");
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void AddTime(float timeToAdd)
    {
        currentTime += timeToAdd;

        if (currentTime > implosionTime)
            currentTime = implosionTime;

        Debug.Log($"Tempo adicionado: +{timeToAdd}s");
    }
}