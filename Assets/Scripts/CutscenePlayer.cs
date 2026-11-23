
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public class CutscenePlayer : MonoBehaviour
{
    [Header("Vídeos da Cutscene")]
    [Tooltip("Adicione os vídeos na ordem em que devem ser reproduzidos")]
    public List<VideoClip> videoClips = new List<VideoClip>();

    [Header("Resolução")]
    [Tooltip("Largura da Render Texture")]
    public int width = 1920;

    [Tooltip("Altura da Render Texture")]
    public int height = 1080;

    [Header("Eventos")]
    [Tooltip("Evento executado quando todos os vídeos terminarem")]
    public UnityEvent onCutsceneFinished;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private int currentClipIndex = 0;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        RawImage rawImage = GetComponent<RawImage>();

        // Cria a Render Texture automaticamente
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

    // Inicia a reprodução da lista de vídeos
    public void PlayFirstVideo()
    {
        if (videoClips == null || videoClips.Count == 0)
        {
            Debug.LogWarning("Nenhum vídeo foi configurado na cutscene!");
            onCutsceneFinished?.Invoke();
            return;
        }

        currentClipIndex = 0;
        PlayCurrentVideo();
    }

    // Reproduz o vídeo atual
    private void PlayCurrentVideo()
    {
        if (currentClipIndex >= videoClips.Count)
        {
            onCutsceneFinished?.Invoke();
            return;
        }

        VideoClip clip = videoClips[currentClipIndex];

        if (clip == null)
        {
            currentClipIndex++;
            PlayCurrentVideo();
            return;
        }

        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    // Executado quando um vídeo termina
    private void OnVideoFinished(VideoPlayer vp)
    {
        currentClipIndex++;
        PlayCurrentVideo();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
