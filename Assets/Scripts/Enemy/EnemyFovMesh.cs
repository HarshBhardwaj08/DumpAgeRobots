using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFovMesh : MonoBehaviour
{
    [Header("View Settings")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 120f;
    public float eyeHeight = 1.6f;

    [Header("Target Settings")]
    public Transform player;
    public LayerMask obstacleMask;

    [Header("Detection Settings")]
    public float baseDetectionTime = 1.0f;
    public float memoryDuration = 3f;
    public float closeDetectionSpeed = 2f;
    public float farDetectionSpeed = 0.5f;
    public float detectionDecaySpeed = 5f;

    [Header("Mesh Display")]
    public int meshResolution = 60;
    public Material fovMaterial;
    public bool smartMeshVisibility = true;

    public float DetectionProgress { get; private set; } = 0f;
    public bool PlayerVisible { get; private set; } = false;
    public bool IsAlerted => memoryTimer > 0f;

    private float detectionTimer = 0f;
    private float memoryTimer = 0f;

    private Mesh viewMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Camera mainCamera;

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
    private Vector3 PlayerHead => player ? player.position + Vector3.up * 1.6f : Vector3.zero;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mainCamera = Camera.main;

        viewMesh = new Mesh { name = "FOV Mesh" };
        meshFilter.mesh = viewMesh;

        if (fovMaterial != null)
            meshRenderer.material = fovMaterial;
    }

    private void Start()
    {
        InvokeRepeating(nameof(CheckPlayerVisibility), 0f, 0.1f);
        NotifyProgress(0f);
    }

    private void LateUpdate()
    {
        if (!smartMeshVisibility || ShouldDrawMesh())
        {
            meshRenderer.enabled = true;
            DrawFOVMesh();
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    private void CheckPlayerVisibility()
    {
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
            Vector3 dir = DirFromAngle(angle, false);

            if (Physics.Raycast(EyePosition, dir, out RaycastHit hit, viewRadius, obstacleMask | (1 << player.gameObject.layer)))
            {
                if (hit.transform == player)
                    return true;
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
        if (SignalManager.Instance != null)
            SignalManager.Instance.Fire(new DetectionProgressSignal { IdleFillAmount = DetectionProgress });
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool global)
    {
        if (!global)
            angleInDegrees += transform.eulerAngles.y;

        return Quaternion.Euler(0, angleInDegrees, 0) * Vector3.forward;
    }

    private void DrawFOVMesh()
    {
        int stepCount = meshResolution;
        float angleStep = viewAngle / stepCount;

        Vector3[] vertices = new Vector3[stepCount + 2];
        int[] triangles = new int[stepCount * 3];

        vertices[0] = transform.InverseTransformPoint(EyePosition);

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = -viewAngle / 2f + angleStep * i;
            Vector3 dir = DirFromAngle(angle, false);
            Vector3 end = EyePosition + dir * viewRadius;

            vertices[i + 1] = transform.InverseTransformPoint(end);

            if (i < stepCount)
            {
                int triIndex = i * 3;
                triangles[triIndex] = 0;
                triangles[triIndex + 1] = i + 1;
                triangles[triIndex + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    /// <summary>
    /// Smart visibility: Should we render the FOV cone this frame?
    /// </summary>
    private bool ShouldDrawMesh()
    {
        if (DetectionProgress > 0.01f) return true;

        if (mainCamera == null) return false;

        Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
        bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        return onScreen;
    }
}
