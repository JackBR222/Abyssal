
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    [Header("Radar Settings")]
    [SerializeField] private Transform radarOrigin;
    [SerializeField] private Transform sweepTransform;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float detectionDistance = 150f;
    [SerializeField] private float detectionRadius = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Radar UI")]
    [SerializeField] private RectTransform radarUI;
    [SerializeField] private RectTransform pingPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip detectionSound;

    [Header("Sub AI Reactions")]
    [SerializeField] private bool enableAIComments = true;
    [SerializeField] private SubAIReactions subAIReactions;

    private HashSet<Transform> detectedEnemies =
        new HashSet<Transform>();

    private float sweepAngle;

    private void Update()
    {
        if (radarOrigin == null || sweepTransform == null)
            return;

        UpdateSweep();
        DetectEnemies();
    }

    private void UpdateSweep()
    {
        float previousAngle = sweepAngle;

        // Gira o feixe do sonar.
        sweepAngle = Mathf.Repeat(
            sweepAngle + rotationSpeed * Time.deltaTime,
            360f
        );

        // Limpa os contatos ao completar uma volta.
        if (sweepAngle < previousAngle)
            detectedEnemies.Clear();

        // Atualiza a animação visual.
        sweepTransform.localRotation = Quaternion.Euler(
            0f, 0f, 90f - sweepAngle
        );
    }

    private void DetectEnemies()
    {
        // Calcula a direção do feixe em relação ao submarino.
        Vector3 localDirection = Quaternion.Euler(
            0f, sweepAngle, 0f
        ) * Vector3.forward;

        Vector3 direction = radarOrigin.TransformDirection(
            localDirection
        ).normalized;

        Debug.DrawRay(
            radarOrigin.position,
            direction * detectionDistance,
            Color.green
        );

        // Procura inimigos dentro do alcance do sonar.
        RaycastHit[] hits = Physics.SphereCastAll(
            radarOrigin.position,
            detectionRadius,
            direction,
            detectionDistance,
            enemyLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (RaycastHit hit in hits)
        {
            Collider enemy = hit.collider;

            if (enemy == null)
                continue;

            // Identifica o inimigo pelo objeto raiz.
            Transform enemyRoot = enemy.transform.root;

            // Evita detectar vários colliders do mesmo inimigo.
            if (detectedEnemies.Contains(enemyRoot))
                continue;

            detectedEnemies.Add(enemyRoot);

            Debug.Log("Sonar detectou: " + enemyRoot.name);

            CreateEnemyPing(hit.point);

            // Comunica a detecção à Sub AI.
            if (enableAIComments && subAIReactions != null)
            {
                subAIReactions.OnSharkDetectedByRadar();
            }

            // Som de detecção do radar.
            if (audioSource != null && detectionSound != null)
            {
                audioSource.PlayOneShot(detectionSound);
            }
        }
    }

    private void CreateEnemyPing(Vector3 enemyPosition)
    {
        if (radarUI == null || pingPrefab == null)
            return;

        // Converte a posição do inimigo para o espaço do submarino.
        Vector3 localPosition = radarOrigin.InverseTransformPoint(
            enemyPosition
        );

        Vector2 radarPosition = new Vector2(
            localPosition.x,
            localPosition.z
        );

        // Converte a distância real para a escala da interface.
        float radarRadius = Mathf.Min(
            radarUI.rect.width,
            radarUI.rect.height
        ) * 0.5f;

        Vector2 normalizedPosition = radarPosition / detectionDistance;

        normalizedPosition = Vector2.ClampMagnitude(
            normalizedPosition,
            1f
        );

        Vector2 finalPosition = normalizedPosition * radarRadius;

        // Cria o contato visual no radar.
        RectTransform ping = Instantiate(pingPrefab, radarUI);

        ping.anchoredPosition = finalPosition;

        RadarPing pingEffect = ping.GetComponent<RadarPing>();

        if (pingEffect != null)
            pingEffect.Refresh();
    }
}
