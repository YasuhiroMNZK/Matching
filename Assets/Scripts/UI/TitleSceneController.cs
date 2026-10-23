using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class TitleSceneController : MonoBehaviour
{
    [SerializeField] private VideoClip titleVideo;
    [SerializeField] private bool playVideoAudio;
    [SerializeField, Range(0f, 1f)] private float videoVolume = 1f;

    private VideoPlayer player;
    private RawImage videoImage;
    private bool entering;

    private void Start()
    {
        var backdrop = new GameObject("VideoBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(transform, false);
        Stretch(backdrop.GetComponent<RectTransform>());
        backdrop.GetComponent<Image>().color = Color.black;
        backdrop.GetComponent<Image>().raycastTarget = false;

        var surface = new GameObject("TitleVideo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        surface.transform.SetParent(backdrop.transform, false);
        Stretch(surface.GetComponent<RectTransform>());
        videoImage = surface.GetComponent<RawImage>();
        videoImage.color = Color.clear;
        videoImage.raycastTarget = false;

        if (titleVideo == null)
        {
            Debug.LogWarning("Title video is not assigned. Left click can still start the game.", this);
            return;
        }
        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.source = VideoSource.VideoClip;
        player.clip = titleVideo;
        player.isLooping = true;
        player.waitForFirstFrame = true;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.renderMode = VideoRenderMode.APIOnly;
        player.audioOutputMode = playVideoAudio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
        player.prepareCompleted += OnPrepared;
        player.errorReceived += OnVideoError;
        player.Prepare();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (entering) return;
        AspectRatioFitter fitter = videoImage.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = source.height > 0 ? (float)source.width / source.height : 16f / 9f;
        if (playVideoAudio)
            for (ushort track = 0; track < source.audioTrackCount; track++)
                source.SetDirectAudioVolume(track, videoVolume);
        source.Play();
    }

    private void OnVideoError(VideoPlayer source, string message) => Debug.LogWarning("Title video: " + message, this);

    private void Update()
    {
        if (entering) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            entering = true;
            if (player != null) player.Stop();
            GameSession.Reset();
            SceneManager.LoadScene("MatchingScene", LoadSceneMode.Single);
            return;
        }
        if (player != null && player.texture != null)
        {
            videoImage.texture = player.texture;
            videoImage.color = Color.white;
        }
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.prepareCompleted -= OnPrepared;
        player.errorReceived -= OnVideoError;
        player.Stop();
    }
}
