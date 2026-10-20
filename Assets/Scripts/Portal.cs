using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Configurações da Cena")]
    [SerializeField] private string sceneNameEnd;

    [Header("Configurações do Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1.0f;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica o Player e garante que a transição ocorra apenas uma vez
        if (other.CompareTag("Player") && !isTransitioning)
        {
            StartCoroutine(TransitionSequence());
        }
    }

    private IEnumerator TransitionSequence()
    {
        isTransitioning = true;

        // Executa o Fade Out se a CanvasGroup tiver sido atribuída
        if (fadeCanvasGroup != null)
        {
            float counter = 0f;

            while (counter < fadeDuration)
            {
                counter += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, counter / fadeDuration);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneNameEnd);
    }
}