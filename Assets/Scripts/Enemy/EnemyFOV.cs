using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyFOV : MonoBehaviour
{
    [Header("View Settings")]
    public float viewRadius = 10f;
    [Range(0, 360)]
    public float viewAngle = 120f;
    public float eyeHeight = 1.6f;

    [Header("Target Settings")]
    public Transform player;
    public LayerMask obstacleMask;

    [Header("Detection Settings")]
    public float baseDetectionTime = 1.0f;
    public float memoryDuration = 3f;

    [Header("Detection Speed Control")]
    public float closeDetectionSpeed = 2f;
    public float farDetectionSpeed = 0.5f;

    [Header("Detection Cooldown Control")]
    public float detectionDecaySpeed = 5f;

    [Header("Debug / Info")]
    [Range(0f, 1f)] public float edgeFactor = 1f;
    public bool showDebugRays = true;

    public float DetectionProgress { get; private set; } = 0f;
    public bool PlayerVisible { get; private set; } = false;
    public bool IsAlerted => memoryTimer > 0f;

    private float detectionTimer = 0f;
    private float memoryTimer = 0f;

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
    private Vector3 PlayerHead => player ? player.position + Vector3.up * 1.6f : Vector3.zero;

    // For gizmos only
    private struct DebugRayInfo
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
    }
    private List<DebugRayInfo> gizmoRays = new List<DebugRayInfo>();

    private void Start()
    {
        InvokeRepeating(nameof(CheckPlayerVisibility), 0f, 0.1f);
        NotifyProgress(0f);
    }

    private void CheckPlayerVisibility()
    {
        gizmoRays.Clear(); // clear previous frame's rays
        PlayerVisible = false;

        if (player == null || Vector3.Distance(EyePosition, PlayerHead) > viewRadius)
        {
            ReduceDetection();
            return;
        }

        if (CanSeePlayer())
        {
            HandlePlayerSeen();
        }
        else
        {
            ReduceDetection();
        }
    }

    private bool CanSeePlayer()
    {
        int rayCount = 30;
        float angleStep = viewAngle / rayCount;
        float startAngle = -viewAngle / 2f;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            Vector3 end = EyePosition + dir * viewRadius;
            if (Physics.Raycast(EyePosition, dir, out RaycastHit hit, viewRadius, obstacleMask | (1 << player.gameObject.layer)))
            {
                end = hit.point;
                Color rayColor = (hit.transform == player) ? Color.red : Color.green;

                gizmoRays.Add(new DebugRayInfo
                {
                    start = EyePosition,
                    end = end,
                    color = rayColor
                });

                if (hit.transform == player)
                    return true;
            }
            else
            {
                gizmoRays.Add(new DebugRayInfo
                {
                    start = EyePosition,
                    end = end,
                    color = Color.yellow
                });
            }
        }

        return false;
    }

    private void HandlePlayerSeen()
    {
        PlayerVisible = true;
        memoryTimer = memoryDuration;

        float distToPlayer = Vector3.Distance(EyePosition, PlayerHead);
        float proximityFactor = Mathf.InverseLerp(viewRadius, 0f, distToPlayer);
        float detectionSpeed = Mathf.Lerp(farDetectionSpeed, closeDetectionSpeed, proximityFactor);

        detectionTimer += Time.deltaTime * detectionSpeed;
        detectionTimer = Mathf.Min(detectionTimer, baseDetectionTime);

        NotifyProgress(detectionTimer / baseDetectionTime);
    }

    private void ReduceDetection()
    {
        float decayMultiplier = memoryTimer > 0f ? 0.5f : 1f;
        memoryTimer = Mathf.Max(0f, memoryTimer - Time.deltaTime);

        detectionTimer = Mathf.Max(0f, detectionTimer - Time.deltaTime * detectionDecaySpeed * decayMultiplier);
        NotifyProgress(detectionTimer / baseDetectionTime);
    }

    private void NotifyProgress(float normalizedProgress)
    {
        DetectionProgress = Mathf.Clamp01(normalizedProgress);
        SignalManager.Instance.Fire(new DetectionProgressSignal { IdleFillAmount = DetectionProgress });
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool global)
    {
        if (!global)
            angleInDegrees += transform.eulerAngles.y;

        return Quaternion.Euler(0, angleInDegrees, 0) * Vector3.forward;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !showDebugRays) return;

        // View radius circle
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(EyePosition, viewRadius);

        // Rays
        foreach (var ray in gizmoRays)
        {
            Gizmos.color = ray.color;
            Gizmos.DrawLine(ray.start, ray.end);
        }
    }
}
