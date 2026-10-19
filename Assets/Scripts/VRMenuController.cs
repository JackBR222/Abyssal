using UnityEngine;
using UnityEngine.SceneManagement;

public class VRMenuController : MonoBehaviour
{
    [SerializeField] private string SceneNameGame;
    [SerializeField] private string SceneNameMenu;

    public void StartGame()
    {
        SceneManager.LoadScene(SceneNameGame);
    }
    
    public void MenuGame()
    {
        SceneManager.LoadScene(SceneNameMenu);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}