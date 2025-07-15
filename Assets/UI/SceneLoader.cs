using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    public string GameScene = "GameScene";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        switch (currentScene)
        {
            case "Homepage":
                SceneManager.LoadScene("Scene2");
                break;

            case "Scene4":
                SceneManager.LoadScene("GameScene");
                break;

            case "PersistentUI":
                SceneManager.LoadScene("Homepage");
                break;

            default:
                Debug.LogWarning($"No scene transition defined for current scene: {currentScene}");
                break;
        }
    }

    public void LoadTargetScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
