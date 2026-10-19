using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(AudioSource))]
public class SubmarineController : MonoBehaviour
{
    [Header("Alavancas")]
    [SerializeField] private HingeJoint leftLeverHinge, rightLeverHinge;

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

    private Rigidbody leftLeverRb, rightLeverRb;
    private XRGrabInteractable leftGrab, rightGrab;
    private AudioSource audioSource;

    private float currentSpeed;
    private float currentTurn;

    void Start()
    {
        if (submarineRb == null)
            submarineRb = GetComponent<Rigidbody>();

        submarineRb.constraints = RigidbodyConstraints.FreezeRotationX |
                                  RigidbodyConstraints.FreezeRotationZ;

        if (leftLeverHinge == null || rightLeverHinge == null)
        {
            Debug.LogError("Atribua os HingeJoints das alavancas no Inspector!");
            enabled = false;
            return;
        }

        leftLeverRb = leftLeverHinge.GetComponent<Rigidbody>();
        rightLeverRb = rightLeverHinge.GetComponent<Rigidbody>();
        leftGrab = leftLeverHinge.GetComponent<XRGrabInteractable>();
        rightGrab = rightLeverHinge.GetComponent<XRGrabInteractable>();

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

    void Update()
    {
        UpdateLeverKinematic(leftLeverHinge, leftLeverRb, leftGrab);
        UpdateLeverKinematic(rightLeverHinge, rightLeverRb, rightGrab);
    }

    void FixedUpdate()
    {
        float left = GetLeverValue(leftLeverHinge);
        float right = GetLeverValue(rightLeverHinge);

        float forward = (left + right) / 2f;
        float turn = left - right;

        // Inércia de avanço: acelera e desacelera gradualmente.
        float targetSpeed = forward * moveSpeed;

        float speedRate = Mathf.Abs(forward) > 0.01f
            ? acceleration
            : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedRate * moveSpeed * Time.fixedDeltaTime
        );

        // Inércia de rotação: as curvas também ficam mais suaves.
        float targetTurn = turn;

        float turnRate = Mathf.Abs(turn) > 0.01f
            ? turnAcceleration
            : turnDeceleration;

        currentTurn = Mathf.MoveTowards(
            currentTurn,
            targetTurn,
            turnRate * Time.fixedDeltaTime
        );

        // Movimento com velocidade persistente.
        submarineRb.MovePosition(
            submarineRb.position +
            transform.forward * currentSpeed * Time.fixedDeltaTime
        );

        // Rotação gradual com inércia.
        submarineRb.MoveRotation(
            submarineRb.rotation *
            Quaternion.Euler(
                0f,
                currentTurn * rotateSpeed * Time.fixedDeltaTime,
                0f
            )
        );

        // Áudio acompanha a velocidade e a intensidade da curva.
        if (audioSource != null && audioSource.isPlaying)
        {
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

    private float GetLeverValue(HingeJoint hinge)
    {
        if (hinge == null)
            return 0f;

        float angle = hinge.angle;
        JointLimits limits = hinge.limits;

        float value = angle > 0 && limits.max != 0
            ? angle / limits.max
            : angle < 0 && limits.min != 0
                ? -(angle / limits.min)
                : 0f;

        return Mathf.Abs(value) < 0.05f
            ? 0f
            : Mathf.Clamp(value, -1f, 1f);
    }

    private void UpdateLeverKinematic(
        HingeJoint hinge,
        Rigidbody rb,
        XRGrabInteractable grab)
    {
        if (hinge == null || rb == null || grab == null)
            return;

        float value = GetLeverValue(hinge);
        bool neutral = Mathf.Abs(value) < 0.05f;

        if (!grab.isSelected && neutral)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = false;
        }
    }
}