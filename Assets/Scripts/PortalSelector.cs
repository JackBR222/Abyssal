using System.Collections.Generic;
using UnityEngine;

public class PortalSelector : MonoBehaviour
{
    [Header("Configurações do Portal")]
    [Tooltip("Arraste todos os objetos de portal possíveis para esta lista.")]
    public List<GameObject> portals = new List<GameObject>();

    [Tooltip("Se marcado, um portal será escolhido automaticamente assim que o jogo começar.")]
    public bool selectOnStart = true;

    private int currentIndex = -1;

    private void Start()
    {
        if (selectOnStart)
        {
            ActivateRandomPortal();
        }
    }

    public void ActivateRandomPortal()
    {
        if (portals == null || portals.Count == 0)
        {
            Debug.LogWarning("A lista de portais está vazia! Adicione objetos no Inspector.");
            return;
        }

        DeactivateAll();

        int newIndex = Random.Range(0, portals.Count);

        if (portals.Count > 1)
        {
            while (newIndex == currentIndex)
            {
                newIndex = Random.Range(0, portals.Count);
            }
        }

        currentIndex = newIndex;

        if (portals[currentIndex] != null)
        {
            portals[currentIndex].SetActive(true);
            Debug.Log("Portal ativado: " + portals[currentIndex].name);
        }
    }

    public void DeactivateAll()
    {
        foreach (GameObject portal in portals)
        {
            if (portal != null)
            {
                portal.SetActive(false);
            }
        }
    }
}
