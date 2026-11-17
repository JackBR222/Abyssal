
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SubmarineController : MonoBehaviour
{
    [Header("Alavancas")]
    [SerializeField] private CrossLever leftLever;
    [SerializeField] private CrossLever rightLever;

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 40f;
    [SerializeField] private float acceleration = 0.8f;
    [SerializeField] private float deceleration = 0.15f;

    [Header("Inércia")]
    [SerializeField] private float turnAcceleration = 1.5f;
    [SerializeField] private float turnDeceleration = 0.4f;

    [Header("Física")]
    [SerializeField] private Rigidbody submarineRb;

    [Header("Áudio")]
    [SerializeField] private AudioClip engineSound;

    [Range(0f, 1f)]
    [SerializeField] private float minVolume = 0.1f, maxVolume = 0.8f;

    [SerializeField] private float minPitch = 0.6f, maxPitch = 1.5f;

    private AudioSource audioSource;

    private float currentSpeed;
    private float currentTurn;

    private void Start()
    {
        if (submarineRb == null)
            submarineRb = GetComponent<Rigidbody>();

        if (submarineRb == null)
        {
            Debug.LogError("Submarino precisa de um Rigidbody!");
            enabled = false;
            return;
        }

        if (leftLever == null || rightLever == null)
        {
            Debug.LogError("Atribua as duas alavancas no Inspector!");
            enabled = false;
            return;
        }

        submarineRb.constraints |=
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        audioSource = GetComponent<AudioSource>();

        audioSource.clip = engineSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = minVolume;
        audioSource.pitch = minPitch;

        if (engineSound != null)
            audioSource.Play();
        else
            Debug.LogWarning("Atribua o som do motor no Inspector!");
    }

    private void FixedUpdate()
    {
        // Lê os valores das duas alavancas.
        float forward =
            (leftLever.Forward + rightLever.Forward) / 2f;

        float turn =
            (leftLever.Turn + rightLever.Turn) / 2f;

        // MOVIMENTO DE AVANÇO E RECUO
        float targetSpeed = forward * moveSpeed;

        float speedRate = Mathf.Abs(forward) > 0.01f
            ? acceleration
            : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedRate * moveSpeed * Time.fixedDeltaTime
        );

        // ROTAÇÃO
        float targetTurn = turn;

        float turnRate = Mathf.Abs(turn) > 0.01f
            ? turnAcceleration
            : turnDeceleration;

        currentTurn = Mathf.MoveTowards(
            currentTurn,
            targetTurn,
            turnRate * Time.fixedDeltaTime
        );

        // MOVIMENTAÇÃO FÍSICA
        submarineRb.MovePosition(
            submarineRb.position +
            transform.forward * currentSpeed * Time.fixedDeltaTime
        );

        // ROTAÇÃO FÍSICA
        submarineRb.MoveRotation(
            submarineRb.rotation *
            Quaternion.Euler(
                0f,
                currentTurn * rotateSpeed * Time.fixedDeltaTime,
                0f
            )
        );

        UpdateAudio();
    }

    private void UpdateAudio()
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        float intensity = Mathf.Max(
            Mathf.Abs(currentSpeed / Mathf.Max(moveSpeed, 0.01f)),
            Mathf.Abs(currentTurn)
        );

        intensity = Mathf.Clamp01(intensity);

        audioSource.volume = Mathf.Lerp(
            audioSource.volume,
            Mathf.Lerp(minVolume, maxVolume, intensity),
            Time.fixedDeltaTime * 5f
        );

        audioSource.pitch = Mathf.Lerp(
            audioSource.pitch,
            Mathf.Lerp(minPitch, maxPitch, intensity),
            Time.fixedDeltaTime * 5f
        );
    }
}
