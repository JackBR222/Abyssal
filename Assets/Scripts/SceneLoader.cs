using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Configurações da Cena")]
    [Tooltip("Nome exato da cena que será carregada")]
    public string sceneName;

    // Carrega a cena configurada no Inspector
    public void LoadConfiguredScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Nenhuma cena foi configurada no Inspector!");
        }
    }

    // Fecha o jogo
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}