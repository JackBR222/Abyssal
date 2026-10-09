using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(AudioSource))]
public class SubmarineController : MonoBehaviour
{
    [Header("Alavancas Físicas")]
    [SerializeField] private HingeJoint leftLeverHinge;
    [SerializeField] private HingeJoint rightLeverHinge;

    [Header("Configurações de Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 40f;

    [Header("Física do Submarino")]
    [SerializeField] private Rigidbody submarineRb;

    [Header("Configurações de Áudio")]
    [SerializeField] private AudioClip engineSound;
    [Range(0f, 1f)] [SerializeField] private float minVolume = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float maxVolume = 0.8f;
    [SerializeField] private float minPitch = 0.6f;
    [SerializeField] private float maxPitch = 1.5f;

    private Rigidbody leftLeverRb;
    private Rigidbody rightLeverRb;
    private XRGrabInteractable leftGrab;
    private XRGrabInteractable rightGrab;
    private AudioSource audioSource;

    void Start()
    {
        if (submarineRb == null) 
            submarineRb = GetComponent<Rigidbody>();
        
        // Trava rotação em X e Z para evitar que o submarino capote
        submarineRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (leftLeverHinge == null || rightLeverHinge == null)
        {
            Debug.LogError("Por favor, atribua os HingeJoints das alavancas no Inspector!");
            return;
        }

        // Cache das referências das alavancas
        leftLeverRb = leftLeverHinge.GetComponent<Rigidbody>();
        rightLeverRb = rightLeverHinge.GetComponent<Rigidbody>();
        leftGrab = leftLeverHinge.GetComponent<XRGrabInteractable>();
        rightGrab = rightLeverHinge.GetComponent<XRGrabInteractable>();

        SetupAudio();
    }

    void Update()
    {
        // Controla o estado kinematic das alavancas na zona morta
        UpdateLeverKinematic(leftLeverHinge, leftLeverRb, leftGrab);
        UpdateLeverKinematic(rightLeverHinge, rightLeverRb, rightGrab);
    }

    void FixedUpdate()
    {
        if (leftLeverHinge == null || rightLeverHinge == null) return;

        // Obtém a entrada normalizada (-1 a 1) das alavancas
        float leftLeverValue = GetLeverValue(leftLeverHinge);
        float rightLeverValue = GetLeverValue(rightLeverHinge);

        // Movimento para frente/trás (média das alavancas)
        float forwardInput = (leftLeverValue + rightLeverValue) / 2f;
        Vector3 movement = transform.forward * forwardInput * moveSpeed * Time.fixedDeltaTime;
        submarineRb.MovePosition(submarineRb.position + movement);

        // Rotação/Giro (diferença entre as alavancas)
        float turnInput = leftLeverValue - rightLeverValue;
        float turnAmount = turnInput * rotateSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        submarineRb.MoveRotation(submarineRb.rotation * turnRotation);

        // Atualiza volume e pitch do motor
        UpdateEngineAudio(forwardInput, turnInput);
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = engineSound;
        audioSource.loop = true;
        audioSource.playOnAwake = true;

        if (engineSound != null)
        {
            // CORREÇÃO: Força áudio a iniciar no mínimo, evitando o estouro de som alto
            audioSource.volume = minVolume;
            audioSource.pitch = minPitch;

            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Nenhum AudioClip de motor foi atribuído no Inspector!");
        }
    }

    private void UpdateEngineAudio(float forward, float turn)
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        // Define a intensidade com base no maior comando atual
        float inputIntensity = Mathf.Max(Mathf.Abs(forward), Mathf.Abs(turn));

        // Transiciona suavemente o volume e tom (pitch) usando Lerp
        audioSource.volume = Mathf.Lerp(audioSource.volume, Mathf.Lerp(minVolume, maxVolume, inputIntensity), Time.fixedDeltaTime * 5f);
        audioSource.pitch = Mathf.Lerp(audioSource.pitch, Mathf.Lerp(minPitch, maxPitch, inputIntensity), Time.fixedDeltaTime * 5f);
    }

    private void UpdateLeverKinematic(HingeJoint hinge, Rigidbody rb, XRGrabInteractable grab)
    {
        if (hinge == null || rb == null || grab == null) return;

        float currentAngle = hinge.angle;
        JointLimits limits = hinge.limits;
        float normalized = 0f;

        if (currentAngle > 0 && limits.max != 0) normalized = currentAngle / limits.max;
        else if (currentAngle < 0 && limits.min != 0) normalized = -(currentAngle / limits.min);

        bool isInNeutralZone = Mathf.Abs(normalized) < 0.05f;

        // Trava a alavanca se solta na zona neutra para evitar oscilações físicas
        if (!grab.isSelected && isInNeutralZone)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        else
        {
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
            }
        }
    }

    private float GetLeverValue(HingeJoint hinge)
    {
        float currentAngle = hinge.angle;
        JointLimits limits = hinge.limits;
        float normalized = 0f;

        // Converte o ângulo físico em um valor de -1 a 1
        if (currentAngle > 0 && limits.max != 0)
        {
            normalized = currentAngle / limits.max;
        }
        else if (currentAngle < 0 && limits.min != 0)
        {
            normalized = -(currentAngle / limits.min);
        }

        // Aplica zona morta de 5%
        if (Mathf.Abs(normalized) < 0.05f) 
            normalized = 0f;

        return Mathf.Clamp(normalized, -1f, 1f);
    }
}
