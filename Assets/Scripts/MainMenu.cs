using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour { 
    public void GoToScene(String sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
