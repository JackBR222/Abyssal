using UnityEngine;

public class ControladorPorta : MonoBehaviour
{
    [SerializeField] private Animator animator;       
    private bool estaAberta = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            estaAberta = !estaAberta;
            animator.SetBool("estaAberta", estaAberta);

          // SetBool("nome", valor)
          // SetTrigger("nome)
          // SetFloat("nome", valor) / SetInteger("nome", valor)
        }
    }
}
