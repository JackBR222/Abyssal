
using UnityEngine;
using UnityEngine.Events;

public class AreaTrigger : MonoBehaviour
{
    [Header("Evento")]
    [SerializeField] private UnityEvent onPlayerEnter;

    [Header("Configuracoes")]
    [SerializeField] private bool activateOnlyOnce = true;
    [SerializeField] private bool destroyAfterActivation = true;

    private bool hasActivated;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") &&
            !other.transform.root.CompareTag("Player"))
            return;

        if (activateOnlyOnce && hasActivated)
            return;

        hasActivated = true;

        onPlayerEnter.Invoke();

        if (destroyAfterActivation)
            Destroy(gameObject);
    }

    // RESETAR TRIGGER
    public void ResetTrigger()
    {
        hasActivated = false;
    }
}
