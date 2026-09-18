using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("Referências")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;

    [Header("Visão")]
    [SerializeField] private Transform visionPoint;
    [SerializeField] private float visionDistance = 20f;
    [SerializeField] private float visionAngle = 90f;

    [Header("Movimento")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float attackDistance = 1.8f;

    [Header("Rotas")]
    [SerializeField] private Transform[] route1;
    [SerializeField] private Transform[] route2;
    [SerializeField] private Transform[] route3;

    [SerializeField] private int currentRoute = 1;

    [Header("Música")]
    [SerializeField] private AudioSource normalMusic;
    [SerializeField] private AudioSource chaseMusic;

    [Header("Som ao detectar")]
    [SerializeField] private AudioSource spottedSound;

    private Transform[] currentRoutePoints;
    private int currentPoint = 0;

    private State state = State.Patrol;

    private bool playedSpottedSound = false;


    private void Start()
    {
        SelectRoute(currentRoute);

        if (normalMusic != null)
            normalMusic.Play();

        agent.speed = patrolSpeed;
    }


    private void Update()
    {
        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Attack:
                Attack();
                break;
        }
    }


    // PATRULHA
    private void Patrol()
    {
        if (currentRoutePoints == null ||
            currentRoutePoints.Length == 0)
            return;

        Transform point =
            currentRoutePoints[currentPoint];

        agent.speed = patrolSpeed;
        agent.SetDestination(point.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.5f)
        {
            currentPoint++;

            if (currentPoint >= currentRoutePoints.Length)
            {
                currentPoint = 0;
            }
        }

        if (CanSeePlayer())
        {
            StartChase();
        }
    }


    // PERSEGUIÇÃO
    private void Chase()
    {
        if (player == null)
            return;

        agent.speed = chaseSpeed;

        agent.SetDestination(player.position);

        if (!CanSeePlayer())
        {
            // Por enquanto continua perseguindo.
            // Podemos adicionar investigação depois.
        }

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distance <= attackDistance)
        {
            state = State.Attack;
        }
    }


    // ATAQUE / MORTE
    private void Attack()
    {
        agent.isStopped = true;

        SceneManager.LoadScene("GameOver");
    }


    // DETECÇÃO
    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin =
            visionPoint != null
                ? visionPoint.position
                : transform.position + Vector3.up;

        Vector3 direction =
            player.position - origin;

        float distance = direction.magnitude;

        if (distance > visionDistance)
            return false;

        direction.Normalize();

        float angle =
            Vector3.Angle(
                transform.forward,
                direction
            );

        if (angle > visionAngle * 0.5f)
            return false;

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            visionDistance))
        {
            if (hit.transform == player ||
                hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }


    // COMEÇAR PERSEGUIÇÃO
    private void StartChase()
    {
        if (state == State.Chase)
            return;

        state = State.Chase;

        agent.speed = chaseSpeed;

        // Som de descoberta
        if (!playedSpottedSound)
        {
            if (spottedSound != null)
                spottedSound.Play();

            playedSpottedSound = true;
        }

        // Troca música
        if (normalMusic != null)
            normalMusic.Stop();

        if (chaseMusic != null &&
            !chaseMusic.isPlaying)
        {
            chaseMusic.Play();
        }
    }


    // TROCAR ROTA
    public void SelectRoute(int route)
    {
        currentRoute = Mathf.Clamp(route, 1, 3);

        switch (currentRoute)
        {
            case 1:
                currentRoutePoints = route1;
                break;

            case 2:
                currentRoutePoints = route2;
                break;

            case 3:
                currentRoutePoints = route3;
                break;
        }

        currentPoint = 0;

        if (currentRoutePoints != null &&
            currentRoutePoints.Length > 0)
        {
            agent.SetDestination(
                currentRoutePoints[0].position
            );
        }
    }


    // DEBUG VISUAL
    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            visionPoint != null
                ? visionPoint.position
                : transform.position + Vector3.up;

        Gizmos.color = Color.red;

        Gizmos.DrawRay(
            origin,
            transform.forward * visionDistance
        );
    }
}
