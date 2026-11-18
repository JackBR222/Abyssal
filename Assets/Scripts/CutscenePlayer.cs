using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public class CutscenePlayer : MonoBehaviour
{
    [Header("Cutscene Clips")]
    public VideoClip firstVideo;
    public VideoClip secondVideo;

    [Header("Resolution")]
    public int width = 1920;
    public int height = 1080;

    [Header("Events")]
    public UnityEvent onCutsceneFinished;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private int currentClipIndex = 0;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        RawImage rawImage = GetComponent<RawImage>();

        // Create Render Texture automatically in memory
        renderTexture = new RenderTexture(width, height, 0);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        rawImage.texture = renderTexture;

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Start()
    {
        PlayFirstVideo();
    }

    public void PlayFirstVideo()
    {
        if (firstVideo == null) return;
        currentClipIndex = 1;
        videoPlayer.clip = firstVideo;
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (currentClipIndex == 1 && secondVideo != null)
        {
            currentClipIndex = 2;
            videoPlayer.clip = secondVideo;
            videoPlayer.Play();
        }
        else
        {
            onCutsceneFinished?.Invoke();
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        if (renderTexture != null)
            renderTexture.Release();
    }
}
