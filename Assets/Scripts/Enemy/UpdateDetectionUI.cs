using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateDetectionUI : MonoBehaviour
{
    [Header("UI References")]
    public Image idleImage;
    public Image alertImage;
    public Image detectionImage; // New glow image

    [Header("UI Animation Settings")]
    public float normalLerpSpeed = 5f;
    public float alertLerpSpeed = 10f;

    [Header("Glow Settings")]
    public bool enableGlow = true;
    public Color glowColorA = Color.red;
    public Color glowColorB = Color.yellow;
    public float glowSpeed = 2f;

    private float currentFill = 0f;
    private float targetFill = 0f;
    private bool isFullyAlerted = false;

    void OnEnable()
    {
        SignalManager.Instance.Subscribe<DetectionProgressSignal>(OnUpdateDetectionUI);
    }

    void OnDisable()
    {
        SignalManager.Instance.Unsubscribe<DetectionProgressSignal>(OnUpdateDetectionUI);
    }

    void Update() 
    {
        // Choose faster lerp if fully detected
        float lerpSpeed = isFullyAlerted ? alertLerpSpeed : normalLerpSpeed;

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);

        if (alertImage != null)
            alertImage.fillAmount = currentFill;

        if (idleImage != null)
            idleImage.fillAmount = 1f - currentFill;

        isFullyAlerted = currentFill >= 0.90f;

        if (detectionImage != null)
        {
            if (isFullyAlerted && enableGlow)
            {
                // Glow using color ping-pong
                float t = Mathf.PingPong(Time.time * glowSpeed, 1f);
                detectionImage.color = Color.Lerp(glowColorA, glowColorB, t);
                detectionImage.fillAmount = 1f;
            }
            else
            {
                // Reset to default state
                detectionImage.color = glowColorA;
                detectionImage.fillAmount = currentFill;
            }
        }
        if (detectionImage.fillAmount >= 1)
        {
            SignalManager.Instance.Fire(new SpottedAlertSignal() { isSpotted = true });
        }
    }

    private void OnUpdateDetectionUI(DetectionProgressSignal signal)
    {
        targetFill = Mathf.Clamp01(signal.IdleFillAmount);
    }

    public void ResetUIInstant()
    {
        currentFill = 0f;
        targetFill = 0f;
        isFullyAlerted = false;

        if (alertImage != null)
            alertImage.fillAmount = 0f;

        if (idleImage != null)
            idleImage.fillAmount = 1f;

        if (detectionImage != null)
        {
            detectionImage.fillAmount = 0f;
            detectionImage.color = glowColorA;
        }
    }
}
