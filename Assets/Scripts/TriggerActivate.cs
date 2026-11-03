using UnityEngine;

public class TriggerActivate : MonoBehaviour
{
    [SerializeField] private GameObject enemyObject;
    
    private bool hasActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated || !other.CompareTag("Player")) return;

        hasActivated = true;

        if (enemyObject != null)
        {
            enemyObject.SetActive(true);
        }
        
        Destroy(gameObject); 
    }
}
