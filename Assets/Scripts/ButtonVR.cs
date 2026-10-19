using UnityEngine;
using UnityEngine.Events;

public class ButtonVR : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onPress;
    public UnityEvent onRelease;

    [Header("Configurações")]
    public float pressOffset = 0.012f;
    public Light flashlight;

    [Header("Configuração da Lanterna")]
    public bool flashlightStartsOn = false;

    private GameObject presser;
    private AudioSource sound;
    private bool isPressed;
    private Vector3 startPosition;

    void Start()
    {
        sound = GetComponent<AudioSource>();
        isPressed = false;

        if (button != null)
        {
            startPosition = button.transform.localPosition;
        }

        if (flashlight != null)
        {
            flashlight.enabled = flashlightStartsOn;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed && button != null)
        {
            button.transform.localPosition = new Vector3(
                startPosition.x,
                startPosition.y - pressOffset,
                startPosition.z
            );

            presser = other.gameObject;
            onPress.Invoke();

            if (sound != null)
            {
                sound.Play();
            }

            isPressed = true;
            ToggleFlashlight();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isPressed && other.gameObject == presser && button != null)
        {
            button.transform.localPosition = startPosition;

            onRelease.Invoke();
            isPressed = false;
        }
    }

    public void ToggleFlashlight()
    {
        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
        }
        else
        {
            Debug.LogWarning(
                "Nenhuma luz foi atribuída no campo 'Flashlight'!"
            );
        }
    }
}