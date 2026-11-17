using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Investigate,
        Chase,
        Attack
    }

    [Header("Referencias")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;

    [Header("Visao")]
    [SerializeField] private Transform visionPoint;
    [SerializeField] private float visionDistance = 20f;
    [SerializeField] private float visionAngle = 90f;

    [Header("Visao no escuro")]
    [SerializeField] private float darkVisionDistance = 6f;
    [SerializeField] private float darkVisionAngle = 45f;

    private bool lightsAreOn = true;

    [Header("Movimento")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Distancias")]
    [SerializeField] private float investigateDistance = 1.5f;

    [Header("Colliders")]
    [SerializeField] private SphereCollider visionTrigger;
    [SerializeField] private SphereCollider attackTrigger;

    [Header("Investigacao")]
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

    [Header("Musica")]
    [SerializeField] private AudioSource normalMusic;
    [SerializeField] private AudioSource chaseMusic;

    [Header("Sons de deteccao")]
    [SerializeField] private AudioSource spottedSound;
    [SerializeField] private AudioSource sharkDetectionSound;
    [SerializeField] private bool playDetectionSounds = true;

    [Header("Ataque")]
    [SerializeField] private string gameOverScene = "GameOver";

    private Transform[] currentRoutePoints;
    private int currentPoint;

    private State state = State.Patrol;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigatePosition;

    private float investigateTimer;
    private float investigateElapsed;

    private bool playedSpottedSound;
    private bool playedSharkSound;
    private bool isAttacking;
    private bool playerIsHidden;

    private bool playerInVisionTrigger;
    private bool playerInAttackTrigger;

    private void Awake()
    {
        SetupTriggers();
    }

    private void Start()
    {
        SelectRoute(currentRoute);

        if (normalMusic != null)
            normalMusic.Play();

        if (chaseMusic != null)
            chaseMusic.Stop();

        if (agent != null)
            agent.speed = patrolSpeed;

        UpdateVisionTrigger();
    }

    private void Update()
    {
        if (agent == null)
            return;

        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Investigate:
                Investigate();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Attack:
                Attack();
                break;
        }
    }

    // CONFIGURACAO DOS COLLIDERS

    private void SetupTriggers()
    {
        SetupTrigger(visionTrigger, true);
        SetupTrigger(attackTrigger, false);
    }

    private void SetupTrigger(SphereCollider trigger, bool isVision)
    {
        if (trigger == null)
            return;

        trigger.isTrigger = true;

        EnemyProximityTrigger detector =
            trigger.GetComponent<EnemyProximityTrigger>();

        if (detector == null)
        {
            detector = trigger.gameObject.AddComponent<EnemyProximityTrigger>();
        }

        detector.Initialize(this, isVision);
    }

    // PATRULHA

    private void Patrol()
    {
        if (currentRoutePoints == null ||
            currentRoutePoints.Length == 0)
            return;

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        Transform point = currentRoutePoints[currentPoint];

        if (point == null)
            return;

        if (!agent.pathPending &&
            (agent.destination - point.position).sqrMagnitude > 0.1f)
        {
            agent.SetDestination(point.position);
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.5f)
        {
            currentPoint =
                (currentPoint + 1) % currentRoutePoints.Length;

            if (currentRoutePoints[currentPoint] != null)
            {
                agent.SetDestination(
                    currentRoutePoints[currentPoint].position);
            }
        }

        if (CanSeePlayer() || playerInVisionTrigger)
            StartChase();
    }

    // INVESTIGACAO

    private void Investigate()
    {
        if (CanSeePlayer())
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

        if (!agent.pathPending &&
            (agent.destination - investigatePosition).sqrMagnitude > 0.1f)
        {
            agent.SetDestination(investigatePosition);
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= investigateDistance)
        {
            agent.isStopped = true;

            investigateTimer -= Time.deltaTime;

            if (investigateTimer <= 0f)
                ReturnToPatrol();
        }
    }

    // PERSEGUICAO

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
        if (isAttacking)
            return;

        isAttacking = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        SceneManager.LoadScene(gameOverScene);
    }

    // DETECCAO VISUAL

    private bool CanSeePlayer()
    {
        if (player == null || playerIsHidden)
            return false;

        Vector3 origin = visionPoint != null
            ? visionPoint.position
            : transform.position + Vector3.up;

        Vector3 direction = player.position - origin;

        float currentDistance = lightsAreOn
            ? visionDistance
            : darkVisionDistance;

        float currentAngle = lightsAreOn
            ? visionAngle
            : darkVisionAngle;

        if (direction.magnitude > currentDistance)
            return false;

        if (Vector3.Angle(transform.forward, direction) >
            currentAngle * 0.5f)
            return false;

        if (Physics.Raycast(
            origin,
            direction.normalized,
            out RaycastHit hit,
            currentDistance))
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

            if (inside &&
                !playerIsHidden &&
                state != State.Chase &&
                state != State.Attack &&
                CanSeePlayer())
            {
                StartChase();
            }
        }
        else
        {
            playerInAttackTrigger = inside;

            if (inside &&
                !playerIsHidden &&
                state == State.Chase)
            {
                state = State.Attack;
            }
        }
    }

    // COMECAR PERSEGUICAO

    private void StartChase()
    {
        if (state == State.Chase ||
            state == State.Attack ||
            playerIsHidden)
            return;

        state = State.Chase;

        if (player != null)
            lastKnownPlayerPosition = player.position;

        agent.isStopped = false;
        agent.speed = chaseSpeed;

        // SFX original de deteccao.
        if (spottedSound != null && !playedSpottedSound)
        {
            spottedSound.Play();
            playedSpottedSound = true;
        }

        // Novo som proprio do tubarao.
        if (playDetectionSounds &&
            sharkDetectionSound != null &&
            !playedSharkSound)
        {
            sharkDetectionSound.Play();
            playedSharkSound = true;
        }

        // Para musica normal.
        if (normalMusic != null)
            normalMusic.Stop();

        // Inicia musica de perseguicao.
        if (chaseMusic != null && !chaseMusic.isPlaying)
            chaseMusic.Play();
    }

    // COMECAR INVESTIGACAO

    private void StartInvestigation(Vector3 position)
    {
        if (state == State.Attack ||
            state == State.Investigate)
            return;

        state = State.Investigate;

        investigatePosition = position;

        investigateTimer = investigateWaitTime;
        investigateElapsed = 0f;

        agent.isStopped = false;
        agent.speed = chaseSpeed;

        agent.SetDestination(position);

        // A musica de perseguicao continua tocando.
    }

    // VOLTAR PARA PATRULHA

    private void ReturnToPatrol()
    {
        state = State.Patrol;

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        playedSpottedSound = false;
        playedSharkSound = false;

        playerInAttackTrigger = false;

        // Para musica de perseguicao.
        if (chaseMusic != null)
            chaseMusic.Stop();

        // Retoma musica normal.
        if (normalMusic != null && !normalMusic.isPlaying)
            normalMusic.Play();

        if (currentRoutePoints != null &&
            currentRoutePoints.Length > 0 &&
            currentRoutePoints[currentPoint] != null)
        {
            agent.SetDestination(
                currentRoutePoints[currentPoint].position);
        }
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

        if (currentRoutePoints == null ||
            currentRoutePoints.Length == 0)
            return;

        if (state == State.Chase ||
            state == State.Investigate ||
            state == State.Attack)
            return;

        if (agent != null &&
            currentRoutePoints[0] != null)
        {
            agent.SetDestination(
                currentRoutePoints[0].position);
        }
    }

    // SISTEMA DE ILUMINACAO

    public void SetLightsOn(bool lightsOn)
    {
        lightsAreOn = lightsOn;
        UpdateVisionTrigger();
    }

    private void UpdateVisionTrigger()
    {
        if (visionTrigger == null)
            return;

        visionTrigger.radius = lightsAreOn
            ? visionDistance
            : darkVisionDistance;
    }

    // ESCONDER JOGADOR

    public void SetPlayerHidden(bool hidden)
    {
        playerIsHidden = hidden;

        if (hidden && state == State.Chase)
        {
            StartInvestigation(lastKnownPlayerPosition);
        }

        if (!hidden && state == State.Investigate)
        {
            if (CanSeePlayer())
                StartChase();
        }
    }

    // ULTIMA POSICAO CONHECIDA

    public void SetLastKnownPosition(Vector3 position)
    {
        lastKnownPlayerPosition = position;

        if (state == State.Chase)
            StartInvestigation(position);
    }

    // OUVIR RUIDOS

    public void HearNoise(Vector3 position)
    {
        if (state != State.Chase &&
            state != State.Attack)
        {
            StartInvestigation(position);
        }
    }

    // DEBUG VISUAL

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = visionPoint != null
            ? visionPoint.position
            : transform.position + Vector3.up;

        float distance = lightsAreOn
            ? visionDistance
            : darkVisionDistance;

        float angle = lightsAreOn
            ? visionAngle
            : darkVisionAngle;

        Gizmos.color = lightsAreOn
            ? Color.red
            : Color.blue;

        Gizmos.DrawRay(
            origin,
            transform.forward * distance);

        Gizmos.DrawRay(
            origin,
            Quaternion.Euler(0f, -angle * 0.5f, 0f)
            * transform.forward * distance);

        Gizmos.DrawRay(
            origin,
            Quaternion.Euler(0f, angle * 0.5f, 0f)
            * transform.forward * distance);

        if (state == State.Investigate)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(investigatePosition, 0.3f);
        }
    }
}

// COMPONENTE AUXILIAR DOS COLLIDERS

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
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enemy == null || !IsPlayer(other))
            return;

        playerColliderCount++;

        enemy.SetPlayerInTrigger(isVision, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (enemy == null || !IsPlayer(other))
            return;

        playerColliderCount = Mathf.Max(
            0,
            playerColliderCount - 1);

        if (playerColliderCount == 0)
            enemy.SetPlayerInTrigger(isVision, false);
    }
}