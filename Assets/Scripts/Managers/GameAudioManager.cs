using UnityEngine;

public sealed class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager instance;
    private AudioSource musicSource;
    private AudioSource seSource;
    private AudioListener fallbackListener;

    private static GameAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameAudioManager>();
                if (instance == null)
                    instance = new GameObject("GameAudioManager").AddComponent<GameAudioManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        seSource = gameObject.AddComponent<AudioSource>();
        seSource.playOnAwake = false;
        seSource.loop = false;
        seSource.spatialBlend = 0f;
        EnsureListener();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void EnsureListener()
    {
        // Existing scenes have no listener. Use one only when a scene does not supply its own.
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        bool sceneHasListener = false;
        foreach (AudioListener listener in listeners)
        {
            if (listener != fallbackListener && listener.enabled)
            {
                sceneHasListener = true;
                break;
            }
        }
        if (fallbackListener == null && !sceneHasListener)
            fallbackListener = gameObject.AddComponent<AudioListener>();
        if (fallbackListener != null) fallbackListener.enabled = !sceneHasListener;
    }

    private void PlayMusic(AudioClip clip)
    {
        EnsureListener();
        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }
        // Keep both the AudioSource and its playback position between Matching and Chat.
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public static void PlaySe(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        GameAudioManager audio = Instance;
        audio.EnsureListener();
        audio.seSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public static void PlayBgm(AudioClip clip, float volume = 0.65f, bool keepCurrentIfUnassigned = false)
    {
        GameAudioManager audio = Instance;
        if (clip == null && keepCurrentIfUnassigned)
        {
            audio.EnsureListener();
            return;
        }
        audio.musicSource.volume = Mathf.Clamp01(volume);
        audio.PlayMusic(clip);
    }

    public static void StopBgm() => Instance.PlayMusic(null);

    public static void StopAllAudio()
    {
        if (instance == null) return;
        instance.musicSource.Stop();
        instance.musicSource.clip = null;
        instance.seSource.Stop();
    }
}
