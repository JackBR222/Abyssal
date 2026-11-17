using UnityEngine;

public class SubmarineLocationTrigger : MonoBehaviour
{
    [Header("Referencia do Submarino")]
    [SerializeField] private SubmarineAI submarineAI;

    [Header("Configuracao do Comentario")]
    [SerializeField] private int locationIndex = 0;

    [Header("Configuracoes")]
    [SerializeField] private bool activateOnlyOnce = true;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") &&
            !other.transform.root.CompareTag("Player"))
        {
            return;
        }

        // Impede repeticoes.
        if (activated && activateOnlyOnce)
            return;

        if (submarineAI == null)
        {
            Debug.LogWarning(
                "SubmarineLocationTrigger: SubmarineAI nao atribuido!",
                gameObject
            );

            return;
        }

        activated = true;

        submarineAI.PlayLocationComment(locationIndex);
    }

    public void ResetTrigger()
    {
        activated = false;
    }
}