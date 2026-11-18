
using UnityEngine;

public class SubAIAreaTrigger : MonoBehaviour
{
    [Header("Referencia")]
    [SerializeField] private SubAIReactions submarineReactions;

    [Header("Comentario")]
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

        if (activated && activateOnlyOnce)
            return;

        if (submarineReactions == null)
        {
            Debug.LogWarning(
                "SubmarineLocationTrigger: SubAIReactions nao atribuido!",
                gameObject
            );

            return;
        }

        activated = true;

        submarineReactions.PlayLocationComment(locationIndex);
    }

    // RESETAR TRIGGER
    public void ResetTrigger()
    {
        activated = false;
    }
}
