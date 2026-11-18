
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SubAIReactions : MonoBehaviour
{
    [System.Serializable]
    public class ReactionDialogue
    {
        public AudioClip audio;

        [Header("Visual")]
        public Sprite imageSprite;

        [Header("Animacao")]
        public Sprite[] animationSprites;

        [Range(1f, 30f)]
        public float frameRate = 8f;

        public bool loopAnimation = true;
    }

    [System.Serializable]
    public class LocationComment
    {
        public string locationName;
        public ReactionDialogue dialogue;
        public bool playOnlyOnce = true;
    }

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Visual")]
    [SerializeField] private Image reactionImage;
    [SerializeField] private bool hideImageWhenNoSprite = true;

    [Header("Alerta do Radar")]
    [SerializeField] private ReactionDialogue radarDetectedDialogue;
    [SerializeField] private bool radarAlertOnlyOnce = true;

    [Header("Alerta de Perigo")]
    [SerializeField] private ReactionDialogue playerSpottedDialogue;
    [SerializeField] private bool spottedAlertOnlyOnce = true;

    [Header("Comentarios dos Locais")]
    [SerializeField] private List<LocationComment> locationComments =
        new List<LocationComment>();

    [Header("Configuracoes")]
    [SerializeField] private bool interruptForDanger = true;
    [SerializeField] private float dialogueDelay = 0.5f;

    private Queue<ReactionDialogue> dialogueQueue =
        new Queue<ReactionDialogue>();

    private HashSet<int> visitedLocations =
        new HashSet<int>();

    private Coroutine playbackCoroutine;
    private Coroutine visualCoroutine;

    private bool isPlaying;
    private bool radarAlertPlayed;
    private bool spottedAlertPlayed;

    private void Start()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        if (voiceSource == null)
        {
            Debug.LogError(
                "SubAIReactions: AudioSource nao encontrado!");
        }
    }

    // ALERTA DO RADAR
    public void OnSharkDetectedByRadar()
    {
        if (radarDetectedDialogue == null ||
            radarDetectedDialogue.audio == null)
            return;

        if (radarAlertOnlyOnce && radarAlertPlayed)
            return;

        radarAlertPlayed = true;

        PlayDialogue(radarDetectedDialogue);
    }

    // ALERTA DE PERIGO
    public void OnPlayerSpotted()
    {
        if (playerSpottedDialogue == null ||
            playerSpottedDialogue.audio == null)
            return;

        if (spottedAlertOnlyOnce && spottedAlertPlayed)
            return;

        spottedAlertPlayed = true;

        if (interruptForDanger)
            PlayPriorityDialogue(playerSpottedDialogue);
        else
            PlayDialogue(playerSpottedDialogue);
    }

    // COMENTARIOS DOS LOCAIS
    public void PlayLocationComment(int locationIndex)
    {
        if (locationIndex < 0 ||
            locationIndex >= locationComments.Count)
            return;

        LocationComment location =
            locationComments[locationIndex];

        if (location == null ||
            location.dialogue == null ||
            location.dialogue.audio == null)
            return;

        if (location.playOnlyOnce &&
            visitedLocations.Contains(locationIndex))
            return;

        visitedLocations.Add(locationIndex);

        PlayDialogue(location.dialogue);
    }

    // ADICIONAR FALA
    public void PlayDialogue(ReactionDialogue dialogue)
    {
        if (dialogue == null ||
            dialogue.audio == null ||
            voiceSource == null)
            return;

        dialogueQueue.Enqueue(dialogue);

        StartDialogueQueue();
    }

    // COMPATIBILIDADE COM AUDIOCLIP
    public void PlayDialogue(AudioClip clip)
    {
        if (clip == null)
            return;

        ReactionDialogue dialogue = new ReactionDialogue();
        dialogue.audio = clip;

        PlayDialogue(dialogue);
    }

    // FALA PRIORITARIA
    public void PlayPriorityDialogue(ReactionDialogue dialogue)
    {
        if (dialogue == null ||
            dialogue.audio == null ||
            voiceSource == null)
            return;

        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        dialogueQueue.Clear();

        voiceSource.Stop();
        StopReactionVisual();

        isPlaying = false;

        playbackCoroutine = StartCoroutine(
            PlaySingleDialogue(dialogue)
        );
    }

    public void PlayPriorityDialogue(AudioClip clip)
    {
        if (clip == null)
            return;

        ReactionDialogue dialogue = new ReactionDialogue();
        dialogue.audio = clip;

        PlayPriorityDialogue(dialogue);
    }

    private IEnumerator PlaySingleDialogue(ReactionDialogue dialogue)
    {
        isPlaying = true;

        PlayReactionVisual(dialogue);

        voiceSource.clip = dialogue.audio;
        voiceSource.Play();

        yield return new WaitWhile(
            () => voiceSource != null && voiceSource.isPlaying
        );

        StopReactionVisual();

        isPlaying = false;
        playbackCoroutine = null;

        StartDialogueQueue();
    }

    // FILA DE DIALOGOS
    private void StartDialogueQueue()
    {
        if (isPlaying || voiceSource == null)
            return;

        playbackCoroutine = StartCoroutine(
            ProcessDialogueQueue()
        );
    }

    private IEnumerator ProcessDialogueQueue()
    {
        isPlaying = true;

        while (dialogueQueue.Count > 0)
        {
            ReactionDialogue dialogue = dialogueQueue.Dequeue();

            if (dialogue == null || dialogue.audio == null)
                continue;

            PlayReactionVisual(dialogue);

            voiceSource.clip = dialogue.audio;
            voiceSource.Play();

            yield return new WaitWhile(
                () => voiceSource != null && voiceSource.isPlaying
            );

            StopReactionVisual();

            if (dialogueDelay > 0f)
                yield return new WaitForSeconds(dialogueDelay);
        }

        isPlaying = false;
        playbackCoroutine = null;

        HideReactionImage();
    }

    // VISUAL DAS REACOES
    private void PlayReactionVisual(ReactionDialogue dialogue)
    {
        StopReactionVisual();

        if (reactionImage == null || dialogue == null)
            return;

        if (dialogue.animationSprites != null &&
            dialogue.animationSprites.Length > 0)
        {
            visualCoroutine = StartCoroutine(
                AnimateReactionImage(dialogue)
            );

            return;
        }

        if (dialogue.imageSprite != null)
        {
            reactionImage.sprite = dialogue.imageSprite;
            reactionImage.enabled = true;
        }
        else
        {
            HideReactionImage();
        }
    }

    private IEnumerator AnimateReactionImage(ReactionDialogue dialogue)
    {
        Sprite[] frames = dialogue.animationSprites;

        if (frames == null || frames.Length == 0)
            yield break;

        reactionImage.enabled = true;

        int currentFrame = 0;

        float frameDuration =
            1f / Mathf.Max(1f, dialogue.frameRate);

        while (true)
        {
            if (frames[currentFrame] != null)
                reactionImage.sprite = frames[currentFrame];

            yield return new WaitForSeconds(frameDuration);

            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (dialogue.loopAnimation)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = frames.Length - 1;

                    if (frames[currentFrame] != null)
                        reactionImage.sprite = frames[currentFrame];

                    visualCoroutine = null;
                    yield break;
                }
            }
        }
    }

    private void StopReactionVisual()
    {
        if (visualCoroutine != null)
        {
            StopCoroutine(visualCoroutine);
            visualCoroutine = null;
        }
    }

    private void HideReactionImage()
    {
        if (reactionImage == null)
            return;

        if (hideImageWhenNoSprite)
            reactionImage.enabled = false;
    }

    // PARAR FALAS
    public void StopAllDialogue()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        dialogueQueue.Clear();

        StopReactionVisual();
        HideReactionImage();

        if (voiceSource != null)
            voiceSource.Stop();

        isPlaying = false;
    }

    // CONSULTAR ESTADO
    public bool IsSpeaking()
    {
        return isPlaying;
    }

    // REINICIAR ALERTAS
    public void ResetDetectionAlerts()
    {
        radarAlertPlayed = false;
        spottedAlertPlayed = false;
    }

    // RESETAR LOCAIS VISITADOS
    public void ResetVisitedLocations()
    {
        visitedLocations.Clear();
    }
}
