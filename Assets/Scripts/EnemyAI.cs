using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Stalk, Investigate, Chase, Attack }

    [Header("Referencias")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private Transform visionPoint;
    [SerializeField] private SubAIReactions subAIReactions;

    [Header("Visao")]
    [SerializeField] private float visionDistance = 20f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private float darkVisionDistance = 6f;
    [SerializeField] private float darkVisionAngle = 45f;
    [SerializeField] private float proximityDetectionDistance = 2f;

    [Header("Movimento")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float stalkSpeed = 2.8f;
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Distancias")]
    [SerializeField] private float investigateDistance = 1.5f;

    [Header("Espreita")]
    [Tooltip("Distancia minima que o tubarao tenta manter do jogador durante a espreita.")]
    [SerializeField] private float stalkMinDistance = 8f;
    [Tooltip("Distancia maxima que o tubarao tenta manter do jogador ao escolher uma posicao de espreita.")]
    [SerializeField] private float stalkMaxDistance = 18f;
    [Tooltip("Distancia maxima do jogador para iniciar o comportamento de espreita.")]
    [SerializeField] private float stalkActivationDistance = 35f;
    [Tooltip("Tempo de espera entre tentativas de iniciar uma nova espreita.")]
    [SerializeField] private float stalkAttemptInterval = 8f;
    [Tooltip("Tempo maximo que o tubarao permanece no comportamento de espreita.")]
    [SerializeField] private float stalkDuration = 12f;
    [Tooltip("Tempo que o tubarao espera ao chegar em uma posicao antes de procurar outra.")]
    [SerializeField] private float stalkArrivalWaitTime = 3f;

    [Header("Colliders")]
    [SerializeField] private SphereCollider visionTrigger;
    [SerializeField] private SphereCollider attackTrigger;

    [Header("Investigacao")]
    [SerializeField] private float investigateWaitTime = 4f;
    [SerializeField] private float maxInvestigateTime = 6f;

    [Header("Rota")]
    [SerializeField] private Transform[] route;

    [Header("Musica")]
    [SerializeField] private AudioSource normalMusic;
    [SerializeField] private AudioSource chaseMusic;

    [Header("Sons")]
    [SerializeField] private AudioSource spottedSound;
    [SerializeField] private AudioSource sharkDetectionSound;
    [SerializeField] private bool playDetectionSounds = true;

    [Header("Luz do submarino")]
    [SerializeField] private Light submarineLight;
    [SerializeField] private Color detectedColor = Color.red;

    [Header("Ataque")]
    [SerializeField] private string gameOverScene = "GameOver";
    
    [Header("Recuperacao de caminho")]
    [SerializeField] private int maxStalkPathRetries = 4;
    [SerializeField] private float noPathRecoveryTime = 2f;
    
    private State state = State.Patrol;
    private Transform[] currentRoutePoints;
    private Color originalLightColor;
    private int currentPoint;
    private float investigateTimer;
    private float investigateElapsed;
    private float stalkElapsed;
    private float stalkWaitTimer;
    private float stalkAttemptTimer;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigatePosition;
    private Vector3 stalkPosition;
    private bool lightsAreOn = true;
    private bool playedSpottedSound;
    private bool playedSharkSound;
    private bool isAttacking;
    private bool playerIsHidden;
    private bool playerInAttackTrigger;
    private bool hasStalkDestination;
    private bool stalkingApproachToActivationRange;
    private int stalkPathFailures;
    private float stalkNoPathTimer;
    private float patrolNoPathTimer;
    private float patrolRetryTimer;
    
    private float stalkNavMeshSampleRadius = 5f;
    private float stalkApproachMargin = 4f;
    private int stalkPositionAttempts = 8;

    
    private void Awake()
    {
        SetupTrigger(attackTrigger, false);
        if (submarineLight != null)
            originalLightColor = submarineLight.color;
    }
    private void Start()
    {
        currentRoutePoints = route;
        if (normalMusic != null) normalMusic.Play();
        if (chaseMusic != null) chaseMusic.Stop();
        if (agent != null)
        {
            agent.speed = patrolSpeed;
            if (!agent.isOnNavMesh)
                Debug.LogWarning("EnemyAI: NavMeshAgent nao esta sobre uma NavMesh.", this);
        }
        stalkAttemptTimer = Mathf.Max(0f, stalkAttemptInterval);
        UpdateVisionTrigger();
    }
    private void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (state == State.Chase && playerInAttackTrigger && !playerIsHidden)
        {
            state = State.Attack;
        }
        if (state != State.Chase && state != State.Attack && CanSeePlayer())
        {
            StartChase();
            if (state == State.Chase) return;
        }
        switch (state)
        {
            case State.Patrol: Patrol(); break;
            case State.Stalk: Stalk(); break;
            case State.Investigate: Investigate(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
        }
    }
    // COLLIDERS
    private void SetupTrigger(SphereCollider trigger, bool vision)
    {
        if (trigger == null) return;
        trigger.isTrigger = true;
        EnemyProximityTrigger detector = trigger.GetComponent<EnemyProximityTrigger>();
        if (detector == null)
            detector = trigger.gameObject.AddComponent<EnemyProximityTrigger>();
        detector.Initialize(this, vision);
    }
    // PATRULHA
    private void Patrol()
    {
        if (currentRoutePoints == null || currentRoutePoints.Length == 0)
        {
            TryBeginStalk();
            return;
        }

        if (patrolRetryTimer > 0f)
        {
            patrolRetryTimer -= Time.deltaTime;
            if (patrolRetryTimer <= 0f) AdvanceRoutePoint();
            TryBeginStalk();
            return;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        currentPoint = Mathf.Clamp(currentPoint, 0, currentRoutePoints.Length - 1);
        Transform point = currentRoutePoints[currentPoint];

        if (point == null)
        {
            AdvanceRoutePoint();
            TryBeginStalk();
            return;
        }

        if (!agent.pathPending && (agent.destination - point.position).sqrMagnitude > 0.1f)
            agent.SetDestination(point.position);

        if (!agent.pathPending && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= 0.5f)
            AdvanceRoutePoint();
        else if (!agent.pathPending && (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            patrolNoPathTimer += Time.deltaTime;
            if (patrolNoPathTimer >= Mathf.Max(0.25f, noPathRecoveryTime)) AdvanceRoutePoint();
        }
        else patrolNoPathTimer = 0f;

        TryBeginStalk();
    }

    private void AdvanceRoutePoint()
    {
        if (currentRoutePoints == null || currentRoutePoints.Length == 0 || agent == null || !agent.isOnNavMesh) return;

        patrolNoPathTimer = 0f;
        int count = currentRoutePoints.Length;
        for (int i = 0; i < count; i++)
        {
            currentPoint = (currentPoint + 1) % count;
            Transform point = currentRoutePoints[currentPoint];
            if (point == null) continue;

            if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, Mathf.Max(0.5f, stalkNavMeshSampleRadius), agent.areaMask))
                continue;
            if (!agent.SetDestination(hit.position)) continue;

            agent.isStopped = false;
            patrolRetryTimer = 0f;
            return;
        }

        // Nenhum ponto acessível: aguarda e tenta a rota novamente, sem alternância contínua.
        agent.isStopped = true;
        patrolRetryTimer = Mathf.Max(1f, noPathRecoveryTime * 2f);
    }

    // ESPREITA
    private void TryBeginStalk()
    {
        if (player == null || playerIsHidden || state != State.Patrol) return;
        if (stalkAttemptTimer > 0f)
        {
            stalkAttemptTimer -= Time.deltaTime;
            return;
        }
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool needsApproach = distanceToPlayer > Mathf.Max(stalkMinDistance, stalkActivationDistance);
        if (needsApproach)
        {
            if (TryChooseApproachPosition(out Vector3 approach))
                StartStalk(approach, true);
            else
                ScheduleStalkRetry();
            return;
        }
        if (TryChooseStalkPosition(out Vector3 candidate))
            StartStalk(candidate, false);
        else
            ScheduleStalkRetry();
    }
    private bool TryChooseStalkPosition(out Vector3 chosen)
    {
        chosen = transform.position;
        if (player == null || agent == null || !agent.isOnNavMesh) return false;
        float min = Mathf.Max(0.1f, stalkMinDistance);
        float max = Mathf.Max(min, stalkMaxDistance);
        int attempts = Mathf.Max(1, stalkPositionAttempts);
        for (int i = 0; i < attempts; i++)
        {
            Vector2 random = Random.insideUnitCircle;
            if (random.sqrMagnitude < 0.01f) random = Vector2.right;
            random.Normalize();
            float radius = Random.Range(min, max);
            Vector3 desired = player.position + new Vector3(random.x, 0f, random.y) * radius;
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit,
                    Mathf.Max(0.1f, stalkNavMeshSampleRadius), agent.areaMask))
                continue;
            float distanceToPlayer = Vector3.Distance(hit.position, player.position);
            if (distanceToPlayer < min || distanceToPlayer > max) continue;
            chosen = hit.position;
            return true;
        }
        return false;
    }
    private bool TryChooseApproachPosition(out Vector3 chosen)
    {
        chosen = transform.position;
        if (player == null || agent == null || !agent.isOnNavMesh) return false;
        Vector3 fromPlayerToEnemy = transform.position - player.position;
        fromPlayerToEnemy.y = 0f;
        if (fromPlayerToEnemy.sqrMagnitude < 0.01f)
            fromPlayerToEnemy = -transform.forward;
        fromPlayerToEnemy.Normalize();
        float min = Mathf.Max(0.1f, stalkMinDistance);
        float activationRange = Mathf.Max(min, stalkActivationDistance);
        float margin = Mathf.Clamp(stalkApproachMargin, 0.5f, Mathf.Max(0.5f, activationRange - min));
        float targetRadius = Mathf.Max(min, activationRange - margin);
        int attempts = Mathf.Max(1, stalkPositionAttempts);
        for (int i = 0; i < attempts; i++)
        {
            float angle = Random.Range(-55f, 55f);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * fromPlayerToEnemy;
            float radius = Random.Range(targetRadius, activationRange);
            Vector3 desired = player.position + direction * radius;
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit,
                    Mathf.Max(0.1f, stalkNavMeshSampleRadius), agent.areaMask))
                continue;
            float playerDistance = Vector3.Distance(hit.position, player.position);
            if (playerDistance < min || playerDistance > activationRange + 0.25f)
                continue;
            chosen = hit.position;
            return true;
        }
        return false;
    }
    private void StartStalk(Vector3 destination, bool approachingActivationRange)
    {
        if (state == State.Chase || state == State.Attack || playerIsHidden || agent == null || !agent.isOnNavMesh) return;
        state = State.Stalk;
        stalkPosition = destination;
        stalkElapsed = 0f;
        stalkWaitTimer = Mathf.Max(0f, stalkArrivalWaitTime);
        stalkPathFailures = 0;
        stalkNoPathTimer = 0f;
        stalkingApproachToActivationRange = approachingActivationRange;
        hasStalkDestination = true;
        agent.isStopped = false;
        agent.speed = Mathf.Max(0.1f, stalkSpeed);
        if (!agent.SetDestination(stalkPosition))
        {
            hasStalkDestination = false;
            stalkPathFailures++;
            ScheduleStalkRetry();
            ReturnToPatrol();
        }
    }
    private void ScheduleStalkRetry()
    {
        stalkAttemptTimer = Mathf.Max(0.25f, stalkAttemptInterval);
    }
    private void Stalk()
    {
        if (CanSeePlayer()) { StartChase(); return; }
        if (player == null || playerIsHidden) { ReturnToPatrol(); return; }
        stalkElapsed += Time.deltaTime;
        if (stalkElapsed >= Mathf.Max(0.1f, stalkDuration))
        {
            FinishStalk();
            return;
        }
        agent.isStopped = false;
        agent.speed = Mathf.Max(0.1f, stalkSpeed);
        if (!hasStalkDestination)
        {
            FinishStalk();
            return;
        }
        if (!agent.pathPending && (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            stalkNoPathTimer += Time.deltaTime;
            if (stalkNoPathTimer >= Mathf.Max(0.25f, noPathRecoveryTime))
            {
                stalkNoPathTimer = 0f;
                // Conta falhas durante toda a espreita; um caminho válido não apaga falhas anteriores.
                stalkPathFailures++;
                if (stalkPathFailures >= Mathf.Max(1, maxStalkPathRetries))
                {
                    FinishStalk();
                    return;
                }
                TryPickStalkRetry();
            }
            return;
        }
        stalkNoPathTimer = 0f;
        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance))
        {
            agent.isStopped = true;
            stalkWaitTimer -= Time.deltaTime;
            if (stalkWaitTimer <= 0f)
            {
                if (stalkingApproachToActivationRange)
                {
                    stalkingApproachToActivationRange = false;
                    if (TryChooseStalkPosition(out Vector3 localPosition) && TrySetStalkDestination(localPosition))
                    {
                        stalkWaitTimer = Mathf.Max(0f, stalkArrivalWaitTime);
                        agent.isStopped = false;
                    }
                    else FinishStalk();
                    return;
                }
                if (TryChooseStalkPosition(out Vector3 next) && TrySetStalkDestination(next))
                {
                    stalkWaitTimer = Mathf.Max(0f, stalkArrivalWaitTime);
                    agent.isStopped = false;
                }
                else FinishStalk();
            }
        }
    }
    private bool TryPickStalkRetry()
    {
        Vector3 retryPosition;
        bool found;
        if (stalkingApproachToActivationRange)
            found = TryChooseApproachPosition(out retryPosition);
        else
            found = TryChooseStalkPosition(out retryPosition);
        if (!found || !TrySetStalkDestination(retryPosition)) return false;
        return true;
    }
    private bool TrySetStalkDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return false;
        if (!agent.SetDestination(destination)) return false;
        stalkPosition = destination;
        hasStalkDestination = true;
        agent.isStopped = false;
        return true;
    }
    private void FinishStalk()
    {
        hasStalkDestination = false;
        stalkingApproachToActivationRange = false;
        ScheduleStalkRetry();
        ReturnToPatrol();
    }
    // INVESTIGACAO
    private void Investigate()
    {
        if (CanSeePlayer()) { StartChase(); return; }
        investigateElapsed += Time.deltaTime;
        if (investigateElapsed >= maxInvestigateTime) { ReturnToPatrol(); return; }
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        if (!agent.pathPending && (agent.destination - investigatePosition).sqrMagnitude > 0.1f)
            agent.SetDestination(investigatePosition);
        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= investigateDistance)
        {
            agent.isStopped = true;
            investigateTimer -= Time.deltaTime;
            if (investigateTimer <= 0f) ReturnToPatrol();
        }
    }
    // PERSEGUICAO
    private void Chase()
    {
        if (playerInAttackTrigger && !playerIsHidden) { state = State.Attack; return; }
        if (player == null || playerIsHidden || !CanSeePlayer())
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
        float distanceToPlayer = direction.magnitude;

        // Detecção de proximidade em qualquer direção.
        if (distanceToPlayer <= proximityDetectionDistance)
        {
            return HasLineOfSight(origin, direction, distanceToPlayer);
        }

        float distance = lightsAreOn ? visionDistance : darkVisionDistance;
        float angle = lightsAreOn ? visionAngle : darkVisionAngle;

        if (distanceToPlayer > distance ||
            Vector3.Angle(transform.forward, direction) > angle * 0.5f)
            return false;

        return HasLineOfSight(origin, direction, distanceToPlayer);
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 direction, float distance)
    {
        return Physics.Raycast(
            origin,
            direction.normalized,
            out RaycastHit hit,
            distance
        ) &&
        (hit.transform == player || hit.transform.IsChildOf(player));
    }

    // EVENTOS DOS COLLIDERS
    public void SetPlayerInTrigger(bool vision, bool inside)
    {
        if (vision) return; // A visao e confirmada por CanSeePlayer(), nao pelo trigger.

        playerInAttackTrigger = inside;
        if (inside && !playerIsHidden && state == State.Chase)
            state = State.Attack;
    }
    // INICIAR PERSEGUICAO
    private void StartChase()
    {
        if (state == State.Chase || state == State.Attack || playerIsHidden || !CanSeePlayer()) return;
        state = State.Chase;
        if (player != null) lastKnownPlayerPosition = player.position;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        if (spottedSound != null && !playedSpottedSound) { spottedSound.Play(); playedSpottedSound = true; }
        if (playDetectionSounds && sharkDetectionSound != null && !playedSharkSound)
        { sharkDetectionSound.Play(); playedSharkSound = true; }
        if (subAIReactions != null)
            subAIReactions.OnPlayerSpotted();
        if (normalMusic != null)
            normalMusic.Stop();
        if (chaseMusic != null && !chaseMusic.isPlaying)
            chaseMusic.Play();
        if (submarineLight != null)
            submarineLight.color = detectedColor;
    }
    // INICIAR INVESTIGACAO
    private void StartInvestigation(Vector3 position)
    {
        if (state == State.Attack || state == State.Investigate) return;
        state = State.Investigate;
        investigatePosition = position;
        investigateTimer = investigateWaitTime;
        investigateElapsed = 0f;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, agent.areaMask))
            agent.SetDestination(hit.position);
        else
            ReturnToPatrol();
    }
    // VOLTAR A PATRULHA
    private void ReturnToPatrol()
    {
        state = State.Patrol;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }
        playedSpottedSound = false;
        playedSharkSound = false;
        playerInAttackTrigger = false;
        if (chaseMusic != null) chaseMusic.Stop();
        if (normalMusic != null && !normalMusic.isPlaying) normalMusic.Play();
        if (submarineLight != null) submarineLight.color = originalLightColor;
        patrolNoPathTimer = 0f;
        if (currentRoutePoints != null && currentRoutePoints.Length > 0 && agent != null && agent.isOnNavMesh)
        {
            currentPoint = Mathf.Clamp(currentPoint, 0, currentRoutePoints.Length - 1);
            Transform point = currentRoutePoints[currentPoint];
            if (point != null && NavMesh.SamplePosition(point.position, out NavMeshHit hit,
                    Mathf.Max(0.5f, stalkNavMeshSampleRadius), agent.areaMask) && agent.SetDestination(hit.position))
            {
                patrolRetryTimer = 0f;
            }
            else
            {
                // Se o ponto atual nao for acessivel, procura outro; se nenhum servir, aguarda a recuperacao.
                AdvanceRoutePoint();
            }
        }
    }
    // ILUMINACAO
    public void SetLightsOn(bool on)
    {
        lightsAreOn = on;
        UpdateVisionTrigger();
    }
    private void UpdateVisionTrigger()
    {
        if (visionTrigger != null)
            visionTrigger.radius = lightsAreOn ? visionDistance : darkVisionDistance;
    }
    // ESCONDER JOGADOR
    public void SetPlayerHidden(bool hidden)
    {
        playerIsHidden = hidden;
        if (hidden && state == State.Chase) StartInvestigation(lastKnownPlayerPosition);
        if (!hidden && (state == State.Investigate || state == State.Stalk) && CanSeePlayer()) StartChase();
    }
    // ULTIMA POSICAO CONHECIDA
    public void SetLastKnownPosition(Vector3 position)
    {
        lastKnownPlayerPosition = position;
        if (state == State.Chase) StartInvestigation(position);
    }
    // OUVIR RUIDOS
    public void HearNoise(Vector3 position)
    {
        if (state != State.Chase && state != State.Attack) StartInvestigation(position);
    }
    // DEBUG
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = visionPoint != null ? visionPoint.position : transform.position + Vector3.up;
        float distance = lightsAreOn ? visionDistance : darkVisionDistance;
        float angle = lightsAreOn ? visionAngle : darkVisionAngle;
        Gizmos.color = lightsAreOn ? Color.red : Color.blue;
        Gizmos.DrawRay(origin, transform.forward * distance);
        Gizmos.DrawRay(origin, Quaternion.Euler(0, -angle * 0.5f, 0) * transform.forward * distance);
        Gizmos.DrawRay(origin, Quaternion.Euler(0, angle * 0.5f, 0) * transform.forward * distance);
        if (state == State.Investigate) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(investigatePosition, 0.3f); }
        if (state == State.Stalk) { Gizmos.color = Color.yellow; Gizmos.DrawSphere(stalkPosition, 0.3f); }
    }
}
// COMPONENTE AUXILIAR DOS COLLIDERS
public class EnemyProximityTrigger : MonoBehaviour
{
    private EnemyAI enemy;
    private bool isVision;
    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
    private readonly List<Collider> staleColliders = new List<Collider>();
    public void Initialize(EnemyAI owner, bool vision)
    {
        enemy = owner;
        isVision = vision;
        playerColliders.Clear();
        staleColliders.Clear();
        if (enemy != null) enemy.SetPlayerInTrigger(isVision, false);
    }
    private bool IsPlayer(Collider other)
    {
        return other != null && (other.CompareTag("Player") || other.transform.root.CompareTag("Player"));
    }
    private void Update()
    {
        if (playerColliders.Count == 0) return;
        staleColliders.Clear();
        foreach (Collider collider in playerColliders)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                staleColliders.Add(collider);
        }
        if (staleColliders.Count == 0) return;
        foreach (Collider collider in staleColliders) playerColliders.Remove(collider);
        if (enemy != null) enemy.SetPlayerInTrigger(isVision, playerColliders.Count > 0);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (enemy == null || !IsPlayer(other)) return;
        playerColliders.Add(other);
        enemy.SetPlayerInTrigger(isVision, playerColliders.Count > 0);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        if (playerColliders.Remove(other) && enemy != null)
            enemy.SetPlayerInTrigger(isVision, playerColliders.Count > 0);
    }
}
