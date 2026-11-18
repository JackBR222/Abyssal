
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class SubAITutorial : MonoBehaviour
{
    [System.Serializable]
    public class Dialogue
    {
        public string dialogueName;
        public AudioClip audio;

        [Header("Visual")]
        public Sprite imageSprite;

        [Header("Animacao")]
        public Sprite[] animationSprites;

        [Range(1f, 30f)]
        public float frameRate = 8f;

        public bool loopAnimation = true;
    }

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Inicio")]
    [SerializeField] private bool playTutorialOnStart = true;
    [SerializeField] private float tutorialStartDelay = 3f;

    [Header("Falas")]
    [SerializeField] private List<Dialogue> tutorialAudios =
        new List<Dialogue>();

    [Header("Encerramento Antecipado")]
    [SerializeField] private AudioClip tutorialSkipAudio;

    [Header("Visual")]
    [SerializeField] private Image tutorialImage;
    [SerializeField] private bool hideImageWhenNoSprite = true;

    [Header("Conclusao")]
    [SerializeField] private UnityEvent onTutorialCompleted;

    [Header("Configuracoes")]
    [SerializeField] private float dialogueDelay = 0.5f;

    // MEMORIA COMPARTILHADA DURANTE A SESSAO
    private static bool tutorialCompletedThisSession;

    private Coroutine tutorialCoroutine;
    private Coroutine visualCoroutine;

    private bool tutorialStarted;
    private bool tutorialFinished;
    private bool isPlaying;

    private void Start()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        if (voiceSource == null)
        {
            Debug.LogError(
                "SubAITutorial: AudioSource nao encontrado!");
            return;
        }

        // VERIFICAR SE JA FOI CONCLUIDO NA SESSAO
        if (HasCompletedTutorial())
        {
            tutorialStarted = true;
            tutorialFinished = true;

            HideTutorialImage();
            InvokeTutorialCompletedEvents();

            return;
        }

        if (playTutorialOnStart)
            StartTutorial();
    }

    // MEMORIA
    public bool HasCompletedTutorial()
    {
        return tutorialCompletedThisSession;
    }

    private void SaveTutorialCompletion()
    {
        tutorialCompletedThisSession = true;
    }

    // INICIAR TUTORIAL
    public void StartTutorial()
    {
        if (tutorialStarted || tutorialFinished)
            return;

        if (HasCompletedTutorial())
        {
            tutorialStarted = true;
            tutorialFinished = true;

            HideTutorialImage();
            InvokeTutorialCompletedEvents();

            return;
        }

        if (voiceSource == null)
            return;

        tutorialStarted = true;

        tutorialCoroutine = StartCoroutine(
            TutorialRoutine()
        );
    }

    private IEnumerator TutorialRoutine()
    {
        if (tutorialStartDelay > 0f)
            yield return new WaitForSeconds(tutorialStartDelay);

        foreach (Dialogue dialogue in tutorialAudios)
        {
            if (dialogue == null || dialogue.audio == null)
                continue;

            PlayDialogueVisual(dialogue);

            voiceSource.clip = dialogue.audio;
            voiceSource.Play();

            isPlaying = true;

            yield return new WaitWhile(
                () => voiceSource != null && voiceSource.isPlaying
            );

            isPlaying = false;

            StopDialogueVisual();

            if (dialogueDelay > 0f)
                yield return new WaitForSeconds(dialogueDelay);
        }

        tutorialCoroutine = null;

        HideTutorialImage();
        CompleteTutorial();
    }

    // ENCERRAR ANTECIPADAMENTE
    public void FinishTutorialEarly()
    {
        if (tutorialFinished || HasCompletedTutorial())
            return;

        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }

        StopDialogueVisual();

        if (voiceSource != null)
            voiceSource.Stop();

        isPlaying = false;
        tutorialStarted = true;

        if (tutorialSkipAudio != null)
        {
            tutorialCoroutine = StartCoroutine(
                PlayTutorialOutro()
            );
        }
        else
        {
            HideTutorialImage();
            CompleteTutorial();
        }
    }

    private IEnumerator PlayTutorialOutro()
    {
        HideTutorialImage();

        voiceSource.clip = tutorialSkipAudio;
        voiceSource.Play();

        isPlaying = true;

        yield return new WaitWhile(
            () => voiceSource != null && voiceSource.isPlaying
        );

        isPlaying = false;
        tutorialCoroutine = null;

        CompleteTutorial();
    }

    // CONCLUIR TUTORIAL
    private void CompleteTutorial()
    {
        if (tutorialFinished)
            return;

        tutorialFinished = true;
        tutorialStarted = true;
        isPlaying = false;

        StopDialogueVisual();
        HideTutorialImage();

        SaveTutorialCompletion();

        Debug.Log("SubAITutorial: Tutorial concluido!");

        InvokeTutorialCompletedEvents();
    }

    private void InvokeTutorialCompletedEvents()
    {
        if (onTutorialCompleted != null)
            onTutorialCompleted.Invoke();
    }

    // VISUAL DAS FALAS
    private void PlayDialogueVisual(Dialogue dialogue)
    {
        StopDialogueVisual();

        if (tutorialImage == null)
            return;

        if (dialogue.animationSprites != null &&
            dialogue.animationSprites.Length > 0)
        {
            visualCoroutine = StartCoroutine(
                AnimateTutorialImage(dialogue)
            );

            return;
        }

        if (dialogue.imageSprite != null)
        {
            tutorialImage.sprite = dialogue.imageSprite;
            tutorialImage.enabled = true;
        }
        else
        {
            HideTutorialImage();
        }
    }

    private IEnumerator AnimateTutorialImage(Dialogue dialogue)
    {
        Sprite[] frames = dialogue.animationSprites;

        if (frames == null || frames.Length == 0)
            yield break;

        tutorialImage.enabled = true;

        int currentFrame = 0;

        float frameDuration =
            1f / Mathf.Max(1f, dialogue.frameRate);

        while (true)
        {
            if (frames[currentFrame] != null)
                tutorialImage.sprite = frames[currentFrame];

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
                        tutorialImage.sprite = frames[currentFrame];

                    visualCoroutine = null;
                    yield break;
                }
            }
        }
    }

    private void StopDialogueVisual()
    {
        if (visualCoroutine != null)
        {
            StopCoroutine(visualCoroutine);
            visualCoroutine = null;
        }
    }

    private void HideTutorialImage()
    {
        if (tutorialImage == null)
            return;

        if (hideImageWhenNoSprite)
            tutorialImage.enabled = false;
    }

    // CONSULTAR ESTADO
    public bool IsTutorialStarted()
    {
        return tutorialStarted;
    }

    public bool IsTutorialFinished()
    {
        return tutorialFinished;
    }

    public bool IsSpeaking()
    {
        return isPlaying;
    }

    // RESETAR TUTORIAL
    public void ResetTutorial()
    {
        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }

        StopDialogueVisual();

        if (voiceSource != null)
            voiceSource.Stop();

        HideTutorialImage();

        tutorialCompletedThisSession = false;

        tutorialStarted = false;
        tutorialFinished = false;
        isPlaying = false;
    }

    // PARAR TUTORIAL
    public void StopTutorial()
    {
        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }

        StopDialogueVisual();

        if (voiceSource != null)
            voiceSource.Stop();

        HideTutorialImage();

        isPlaying = false;
    }
}
