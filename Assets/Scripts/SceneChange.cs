using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [SerializeField] string sceneName;
    public  void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
