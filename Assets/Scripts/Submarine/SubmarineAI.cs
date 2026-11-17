
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SubmarineAI : MonoBehaviour
{
    [System.Serializable]
    public class Dialogue
    {
        public string dialogueName;
        public AudioClip audio;
    }

    [System.Serializable]
    public class LocationComment
    {
        public string locationName;
        public AudioClip audio;
        public bool playOnlyOnce = true;
    }

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Inicio do Tutorial")]
    [SerializeField] private bool playTutorialOnStart = true;
    [SerializeField] private float tutorialStartDelay = 3f;

    [Header("Tutorial")]
    [SerializeField] private List<Dialogue> tutorialAudios =
        new List<Dialogue>();

    [Header("Alertas do Radar")]
    [SerializeField] private AudioClip radarDetectedAudio;
    [SerializeField] private bool radarAlertOnlyOnce = true;

    [Header("Alerta de Perigo")]
    [SerializeField] private AudioClip playerSpottedAudio;
    [SerializeField] private bool spottedAlertOnlyOnce = true;

    [Header("Comentarios dos Locais")]
    [SerializeField] private List<LocationComment> locationComments =
        new List<LocationComment>();

    [Header("Configuracoes")]
    [SerializeField] private bool interruptForDanger = true;
    [SerializeField] private float dialogueDelay = 0.5f;

    private Queue<AudioClip> dialogueQueue =
        new Queue<AudioClip>();

    private HashSet<int> visitedLocations =
        new HashSet<int>();

    private Coroutine playbackCoroutine;
    private Coroutine tutorialCoroutine;

    private bool isPlaying;
    private bool tutorialFinished;
    private bool tutorialStarted;

    private bool radarAlertPlayed;
    private bool spottedAlertPlayed;

    private void Start()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        if (voiceSource == null)
        {
            Debug.LogError(
                "SubmarineAI: AudioSource nao encontrado!");
            return;
        }

        if (playTutorialOnStart)
        {
            StartTutorial();
        }
    }

    // =========================================
    // INICIAR TUTORIAL
    // =========================================

    public void StartTutorial()
    {
        if (tutorialStarted || tutorialFinished)
            return;

        tutorialStarted = true;

        if (tutorialCoroutine != null)
            StopCoroutine(tutorialCoroutine);

        tutorialCoroutine = StartCoroutine(
            TutorialStartRoutine()
        );
    }

    private IEnumerator TutorialStartRoutine()
    {
        if (tutorialStartDelay > 0f)
            yield return new WaitForSeconds(tutorialStartDelay);

        foreach (Dialogue dialogue in tutorialAudios)
        {
            if (dialogue != null && dialogue.audio != null)
            {
                dialogueQueue.Enqueue(dialogue.audio);
            }
        }

        tutorialFinished = true;
        tutorialCoroutine = null;

        StartDialogueQueue();
    }

    // =========================================
    // CONTROLE DO TUTORIAL
    // =========================================

    public void ResetTutorial()
    {
        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }

        tutorialStarted = false;
        tutorialFinished = false;
    }

    public bool IsTutorialStarted()
    {
        return tutorialStarted;
    }

    public bool IsTutorialFinished()
    {
        return tutorialFinished;
    }

    // =========================================
    // FALAS INDIVIDUAIS
    // =========================================

    public void PlayDialogue(AudioClip clip)
    {
        if (clip == null)
            return;

        dialogueQueue.Enqueue(clip);

        StartDialogueQueue();
    }

    // =========================================
    // RADAR DETECTOU O TUBARAO
    // =========================================

    public void OnSharkDetectedByRadar()
    {
        if (radarDetectedAudio == null)
            return;

        if (radarAlertOnlyOnce && radarAlertPlayed)
            return;

        radarAlertPlayed = true;

        PlayDialogue(radarDetectedAudio);
    }

    // =========================================
    // TUBARAO AVISTOU O JOGADOR
    // =========================================

    public void OnPlayerSpotted()
    {
        if (playerSpottedAudio == null)
            return;

        if (spottedAlertOnlyOnce && spottedAlertPlayed)
            return;

        spottedAlertPlayed = true;

        if (interruptForDanger)
        {
            PlayPriorityDialogue(playerSpottedAudio);
        }
        else
        {
            PlayDialogue(playerSpottedAudio);
        }
    }

    // =========================================
    // COMENTARIOS DOS LOCAIS
    // =========================================

    public void PlayLocationComment(int locationIndex)
    {
        if (locationIndex < 0 ||
            locationIndex >= locationComments.Count)
            return;

        LocationComment location =
            locationComments[locationIndex];

        if (location == null || location.audio == null)
            return;

        if (location.playOnlyOnce &&
            visitedLocations.Contains(locationIndex))
            return;

        visitedLocations.Add(locationIndex);

        PlayDialogue(location.audio);
    }

    // =========================================
    // ALERTA PRIORITARIO
    // =========================================

    public void PlayPriorityDialogue(AudioClip clip)
    {
        if (clip == null || voiceSource == null)
            return;

        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        dialogueQueue.Clear();

        voiceSource.Stop();

        isPlaying = false;

        playbackCoroutine = StartCoroutine(
            PlaySingleDialogue(clip)
        );
    }

    // =========================================
    // FILA DE DIALOGOS
    // =========================================

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
            AudioClip clip = dialogueQueue.Dequeue();

            if (clip == null)
                continue;

            voiceSource.clip = clip;
            voiceSource.Play();

            yield return new WaitForSeconds(clip.length);

            if (dialogueDelay > 0f)
                yield return new WaitForSeconds(dialogueDelay);
        }

        isPlaying = false;
        playbackCoroutine = null;
    }

    // =========================================
    // REPRODUCAO DE FALA PRIORITARIA
    // =========================================

    private IEnumerator PlaySingleDialogue(AudioClip clip)
    {
        isPlaying = true;

        voiceSource.clip = clip;
        voiceSource.Play();

        yield return new WaitForSeconds(clip.length);

        isPlaying = false;
        playbackCoroutine = null;

        StartDialogueQueue();
    }

    // =========================================
    // PARAR TODAS AS FALAS
    // =========================================

    public void StopAllDialogue()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        dialogueQueue.Clear();

        if (voiceSource != null)
            voiceSource.Stop();

        isPlaying = false;
    }

    // =========================================
    // ESTADO DA VOZ
    // =========================================

    public bool IsSpeaking()
    {
        return isPlaying;
    }

    // =========================================
    // REINICIAR ALERTAS
    // =========================================

    public void ResetDetectionAlerts()
    {
        radarAlertPlayed = false;
        spottedAlertPlayed = false;
    }
}
