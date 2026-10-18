using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneChange : MonoBehaviour
{
    [SerializeField] string sceneName;
    public  void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}

// Created even when Play Mode starts directly in a gameplay scene.
internal sealed class GlobalTitleShortcut : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (FindFirstObjectByType<GlobalTitleShortcut>() != null) return;
        var shortcut = new GameObject("GlobalTitleShortcut").AddComponent<GlobalTitleShortcut>();
        DontDestroyOnLoad(shortcut.gameObject);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f5Key.wasPressedThisFrame) return;
        GameSession.Reset();
        GameAudioManager.StopAllAudio();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);
    }
}
