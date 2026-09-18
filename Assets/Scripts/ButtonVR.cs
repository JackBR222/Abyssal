using UnityEngine;
using UnityEngine.Events;

public class ButtonVR : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onPress;
    public UnityEvent onRelease;
    
    [Header("Configurações")]
    public float pressOffset = 0.012f; 
    public Light luzLanterna; 

    GameObject presser;
    AudioSource sound;
    bool isPressed;
    private Vector3 startPosition;

    void Start()
    {
        sound = GetComponent<AudioSource>();
        isPressed = false;

        if (button != null)
        {
            startPosition = button.transform.localPosition;
        }
        
        if (luzLanterna != null)
        {
            luzLanterna.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed && button != null)
        {
            button.transform.localPosition = new Vector3(startPosition.x, startPosition.y - pressOffset, startPosition.z);
            
            presser = other.gameObject;
            onPress.Invoke();
            
            if (sound != null) sound.Play();
            isPressed = true;

            ToggleLantern();
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

    public void ToggleLantern()
    {
        if (luzLanterna != null)
        {
            luzLanterna.enabled = !luzLanterna.enabled;
        }
        else
        {
            Debug.LogWarning("Nenhuma Light foi atribuída no campo 'luzLanterna'!");
        }
    }
}