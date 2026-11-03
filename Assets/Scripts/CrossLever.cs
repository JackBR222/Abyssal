using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class CrossLever : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform baseTransform;

    [Header("Movimento")]
    [SerializeField] private float maxDistance = 0.15f;
    [SerializeField] private float maxAngle = 25f;
    [SerializeField] private float returnSpeed = 4f;

    [Header("Configuração")]
    [SerializeField] private float deadZone = 0.08f;

    private XRGrabInteractable grab;
    private Rigidbody rb;

    private IXRSelectInteractor interactor;

    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private Vector3 handOffset;
    private Vector3 currentOffset;

    private float forward;
    private float turn;

    public float Forward => forward;
    public float Turn => turn;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (baseTransform == null)
            baseTransform = transform.parent;

        initialPosition = baseTransform.InverseTransformPoint(
            transform.position
        );
        
        initialRotation = Quaternion.Inverse(baseTransform.rotation) *
                          transform.rotation;
        
        rb.isKinematic = true;
        rb.useGravity = false;

        grab.trackPosition = false;
        grab.trackRotation = false;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject;

        Transform attach = interactor.GetAttachTransform(grab);

        // Captura a posição da mão no espaço local da base.
        Vector3 handLocal = baseTransform.InverseTransformPoint(
            attach.position
        );

        // Mantém a distância original entre mão e alavanca.
        handOffset = initialPosition - handLocal;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (interactor == args.interactorObject)
            interactor = null;
    }

    private void LateUpdate()
    {
        // Mantém a alavanca presa à posição central da base.
        transform.position = baseTransform.TransformPoint(initialPosition);

        if (interactor != null)
        {
            Transform attach = interactor.GetAttachTransform(grab);

            // Posição da mão relativa à base ATUAL.
            Vector3 handLocal = baseTransform.InverseTransformPoint(
                attach.position
            );

            Vector3 desiredPosition = handLocal + handOffset;

            Vector3 offset = desiredPosition - initialPosition;

            // Elimina o movimento vertical.
            offset.y = 0f;

            // Movimento em cruz.
            if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
            {
                offset.z = 0f;

                offset.x = Mathf.Clamp(
                    offset.x,
                    -maxDistance,
                    maxDistance
                );
            }
            else
            {
                offset.x = 0f;

                offset.z = Mathf.Clamp(
                    offset.z,
                    -maxDistance,
                    maxDistance
                );
            }

            currentOffset = offset;
        }
        else
        {
            currentOffset = Vector3.MoveTowards(
                currentOffset,
                Vector3.zero,
                returnSpeed * maxDistance * Time.deltaTime
            );
        }

        UpdateHandle();
        UpdateInput();
    }

    private void UpdateHandle()
    {
        float x = currentOffset.x / Mathf.Max(maxDistance, 0.001f);
        float z = currentOffset.z / Mathf.Max(maxDistance, 0.001f);
        
        // Rotaciona explicitamente ao redor dos eixos Right e Forward da base
        Quaternion targetRotation = baseTransform.rotation * initialRotation;
    
        // Inclina para frente/trás (ao redor do eixo X local da base)
        targetRotation = Quaternion.AngleAxis(z * maxAngle, baseTransform.right) * targetRotation;
    
        // Inclina para os lados (ao redor do eixo Z local da base)
        targetRotation = Quaternion.AngleAxis(-x * maxAngle, baseTransform.forward) * targetRotation;

        transform.rotation = targetRotation;
    }

    private void UpdateInput()
    {
        float x = currentOffset.x / Mathf.Max(maxDistance, 0.001f);
        float z = currentOffset.z / Mathf.Max(maxDistance, 0.001f);

        if (Mathf.Abs(x) < deadZone)
            x = 0f;

        if (Mathf.Abs(z) < deadZone)
            z = 0f;

        turn = Mathf.Clamp(x, -1f, 1f);
        forward = Mathf.Clamp(z, -1f, 1f);
    }
}