using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Chase, Attack }

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

    [Header("Distâncias")]
    [SerializeField] private float investigateDistance = 1.5f;

    [Header("Colliders circulares")]
    [SerializeField] private SphereCollider visionTrigger;
    [SerializeField] private SphereCollider attackTrigger;

    [Header("Investigação")]
    [SerializeField] private float investigateWaitTime = 4f;
    [SerializeField] private float maxInvestigateTime = 6f;

    [Header("Rotas")]
    [SerializeField] private Transform[] route1;
    [SerializeField] private Transform[] route2;
    [SerializeField] private Transform[] route3;
    [SerializeField] private Transform[] route4;
    [SerializeField] private Transform[] route5;
    [SerializeField] private Transform[] route6;
    [SerializeField] private Transform[] route7;
    [SerializeField] private Transform[] route8;
    [SerializeField] private int currentRoute = 1;

    [Header("Música")]
    [SerializeField] private AudioSource normalMusic;
    [SerializeField] private AudioSource chaseMusic;

    [Header("Som ao detectar")]
    [SerializeField] private AudioSource spottedSound;

    private Transform[] currentRoutePoints;
    private int currentPoint;
    private State state = State.Patrol;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigatePosition;

    private float investigateTimer;
    private float investigateElapsed;

    private bool playedSpottedSound;
    private bool isAttacking;
    private bool playerIsHidden;
    private bool playerInVisionTrigger;
    private bool playerInAttackTrigger;

    private void Awake()
    {
        SetupTriggers();
    }

    private void SetupTriggers()
    {
        SetupTrigger(visionTrigger, true);
        SetupTrigger(attackTrigger, false);
    }

    private void SetupTrigger(SphereCollider trigger, bool isVision)
    {
        if (trigger == null) return;

        trigger.isTrigger = true;

        EnemyProximityTrigger detector =
            trigger.GetComponent<EnemyProximityTrigger>();

        if (detector == null)
            detector = trigger.gameObject.AddComponent<EnemyProximityTrigger>();

        detector.Initialize(this, isVision);
    }

    private void Start()
    {
        SelectRoute(currentRoute);

        if (normalMusic != null)
            normalMusic.Play();

        if (agent != null)
            agent.speed = patrolSpeed;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Patrol: Patrol(); break;
            case State.Investigate: Investigate(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
        }
    }

    // PATRULHA
    private void Patrol()
    {
        if (currentRoutePoints == null || currentRoutePoints.Length == 0)
            return;

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        Transform point = currentRoutePoints[currentPoint];
        if (point == null) return;

        agent.SetDestination(point.position);

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            currentPoint = (currentPoint + 1) % currentRoutePoints.Length;

        if (CanSeePlayer() || playerInVisionTrigger)
            StartChase();
    }

    // INVESTIGAÇÃO
    private void Investigate()
    {
        if (CanSeePlayer() || playerInVisionTrigger)
        {
            StartChase();
            return;
        }

        investigateElapsed += Time.deltaTime;

        if (investigateElapsed >= maxInvestigateTime)
        {
            ReturnToPatrol();
            return;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(investigatePosition);

        if (Vector3.Distance(transform.position, investigatePosition)
            <= investigateDistance)
        {
            agent.isStopped = true;
            investigateTimer -= Time.deltaTime;

            if (investigateTimer <= 0f)
                ReturnToPatrol();
        }
    }

    // PERSEGUIÇÃO
    private void Chase()
    {
        if (player == null || playerIsHidden)
        {
            StartInvestigation(lastKnownPlayerPosition);
            return;
        }

        if (playerInAttackTrigger)
        {
            state = State.Attack;
            return;
        }

        if (!CanSeePlayer() && !playerInVisionTrigger)
        {
            StartInvestigation(lastKnownPlayerPosition);
            return;
        }

        lastKnownPlayerPosition = player.position;

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(lastKnownPlayerPosition);
    }

    // ATAQUE
    private void Attack()
    {
        if (isAttacking) return;

        isAttacking = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        SceneManager.LoadScene("GameOver");
    }

    // DETECÇÃO VISUAL
    private bool CanSeePlayer()
    {
        if (player == null || playerIsHidden)
            return false;

        Vector3 origin = visionPoint != null
            ? visionPoint.position
            : transform.position + Vector3.up;

        Vector3 direction = player.position - origin;

        if (direction.magnitude > visionDistance)
            return false;

        if (Vector3.Angle(transform.forward, direction)
            > visionAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, direction.normalized,
            out RaycastHit hit, visionDistance))
        {
            return hit.transform == player ||
                   hit.transform.IsChildOf(player);
        }

        return false;
    }

    // EVENTOS DOS COLLIDERS
    public void SetPlayerInTrigger(bool isVision, bool inside)
    {
        if (isVision)
        {
            playerInVisionTrigger = inside;

            if (inside && !playerIsHidden &&
                state != State.Chase && state != State.Attack)
                StartChase();
        }
        else
        {
            playerInAttackTrigger = inside;

            if (inside && !playerIsHidden &&
                state == State.Chase)
                state = State.Attack;
        }
    }

    // COMEÇAR PERSEGUIÇÃO
    private void StartChase()
    {
        if (state == State.Chase || state == State.Attack ||
            playerIsHidden)
            return;

        state = State.Chase;

        if (player != null)
            lastKnownPlayerPosition = player.position;

        agent.isStopped = false;
        agent.speed = chaseSpeed;

        if (spottedSound != null && !playedSpottedSound)
        {
            spottedSound.Play();
            playedSpottedSound = true;
        }

        if (normalMusic != null)
            normalMusic.Stop();

        if (chaseMusic != null && !chaseMusic.isPlaying)
            chaseMusic.Play();
    }

    // COMEÇAR INVESTIGAÇÃO
    private void StartInvestigation(Vector3 position)
    {
        if (state == State.Attack) return;

        state = State.Investigate;
        investigatePosition = position;
        investigateTimer = investigateWaitTime;
        investigateElapsed = 0f;

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(position);
    }

    // VOLTAR PARA PATRULHA
    private void ReturnToPatrol()
    {
        state = State.Patrol;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        playedSpottedSound = false;

        if (chaseMusic != null)
            chaseMusic.Stop();

        if (normalMusic != null && !normalMusic.isPlaying)
            normalMusic.Play();

        if (currentRoutePoints != null &&
            currentRoutePoints.Length > 0)
            agent.SetDestination(currentRoutePoints[currentPoint].position);
    }

    // TROCAR ROTA
    public void SelectRoute(int route)
    {
        currentRoute = Mathf.Clamp(route, 1, 8);

        currentRoutePoints = currentRoute switch
        {
            1 => route1,
            2 => route2,
            3 => route3,
            4 => route4,
            5 => route5,
            6 => route6,
            7 => route7,
            8 => route8,
            _ => route1
        };

        currentPoint = 0;

        if (currentRoutePoints == null || currentRoutePoints.Length == 0)
            return;

        if (state == State.Chase || state == State.Investigate ||
            state == State.Attack)
            return;

        agent.SetDestination(currentRoutePoints[currentPoint].position);
    }

    // ESCONDER PLAYER
    public void SetPlayerHidden(bool hidden)
    {
        playerIsHidden = hidden;

        if (hidden && state == State.Chase)
            StartInvestigation(lastKnownPlayerPosition);
    }

    // DEFINIR ÚLTIMA POSIÇÃO
    public void SetLastKnownPosition(Vector3 position)
    {
        lastKnownPlayerPosition = position;

        if (state == State.Chase)
            StartInvestigation(position);
    }

    // OUVIR SOM
    public void HearNoise(Vector3 position)
    {
        if (state != State.Chase && state != State.Attack)
            StartInvestigation(position);
    }

    // DEBUG VISUAL
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = visionPoint != null
            ? visionPoint.position
            : transform.position + Vector3.up;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, transform.forward * visionDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin,
            Quaternion.Euler(0f, -visionAngle * 0.5f, 0f)
            * transform.forward * visionDistance);

        Gizmos.DrawRay(origin,
            Quaternion.Euler(0f, visionAngle * 0.5f, 0f)
            * transform.forward * visionDistance);

        if (state == State.Investigate)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(investigatePosition, 0.3f);
        }
    }
}

// AUXILIAR: identifica separadamente cada collider de proximidade.
public class EnemyProximityTrigger : MonoBehaviour
{
    private EnemyAI enemy;
    private bool isVision;
    private int playerColliderCount;

    public void Initialize(EnemyAI owner, bool vision)
    {
        enemy = owner;
        isVision = vision;
    }

    private bool IsPlayer(Collider other)
    {
        Transform target = enemy != null
            ? other.transform
            : null;

        return target != null &&
               (target.CompareTag("Player") ||
                target.root.CompareTag("Player"));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enemy == null || !IsPlayer(other)) return;

        playerColliderCount++;
        enemy.SetPlayerInTrigger(isVision, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (enemy == null || !IsPlayer(other)) return;

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);

        if (playerColliderCount == 0)
            enemy.SetPlayerInTrigger(isVision, false);
    }
}