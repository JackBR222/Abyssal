
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class VRLightSwitch : MonoBehaviour
{
    [Header("Botão")]
    public GameObject button;
    public float pressOffset = 0.012f;

    [Header("Luzes controladas")]
    public List<Light> lights = new List<Light>();

    [Header("Estado inicial")]
    public bool lightsStartOn = true;

    [Header("Inimigos afetados")]
    public List<EnemyAI> enemies = new List<EnemyAI>();

    [Header("Eventos")]
    public UnityEvent onPress;
    public UnityEvent onRelease;

    private GameObject presser;
    private AudioSource sound;
    private bool isPressed;
    private Vector3 startPosition;

    private void Start()
    {
        sound = GetComponent<AudioSource>();

        if (button != null)
            startPosition = button.transform.localPosition;

        // Define o estado inicial das luzes.
        SetLights(lightsStartOn);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPressed || button == null)
            return;

        button.transform.localPosition = new Vector3(
            startPosition.x,
            startPosition.y - pressOffset,
            startPosition.z
        );

        presser = other.gameObject;

        onPress.Invoke();

        if (sound != null)
            sound.Play();

        isPressed = true;

        // Alterna todas as luzes.
        ToggleLights();
    }

    private void OnTriggerExit(Collider other)
    {
        if (isPressed && other.gameObject == presser)
        {
            if (button != null)
                button.transform.localPosition = startPosition;

            onRelease.Invoke();
            isPressed = false;
            presser = null;
        }
    }

    // Liga ou desliga todas as luzes.
    public void ToggleLights()
    {
        bool newState = !AreLightsOn();

        SetLights(newState);
    }

    // Define diretamente o estado das luzes.
    public void SetLights(bool turnOn)
    {
        foreach (Light lightSource in lights)
        {
            if (lightSource != null)
                lightSource.enabled = turnOn;
        }

        UpdateEnemiesVision(turnOn);
    }

    // Verifica se todas as luzes estão ligadas.
    private bool AreLightsOn()
    {
        foreach (Light lightSource in lights)
        {
            if (lightSource != null && !lightSource.enabled)
                return false;
        }

        return true;
    }

    // Atualiza a visão dos inimigos.
    private void UpdateEnemiesVision(bool lightsOn)
    {
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null)
                enemy.SetLightsOn(lightsOn);
        }
    }
}
