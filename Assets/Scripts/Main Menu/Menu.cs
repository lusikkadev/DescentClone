using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void StartGame()
    {
        StartCoroutine(LoadGameScene());
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator LoadGameScene()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(1);
    }

}
