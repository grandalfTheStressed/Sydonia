using System;
using UnityEngine;

public class PlayerCameraController {
    private AnimationCurve cameraMovementCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    private Camera playerCamera;
    private Input input;

    private float rotationSensitivity = 5f;
    private float cameraAnimationSpeed = 0;

    private Quaternion targetRotation;
    private int targetOrthoSize;

    private Vector3 flattenedForward = Vector3.forward;

    public Vector3 FlattenedForward => flattenedForward;

    public PlayerCameraController(Camera playerCamera, Input input) {
        this.playerCamera = playerCamera;
        this.input = input;
        targetOrthoSize = (int)playerCamera.orthographicSize;
        targetRotation = this.playerCamera.transform.rotation;
    }
    
    public void UpdateCamera(Vector3 focus, bool isTargeting) {
        cameraAnimationSpeed = cameraMovementCurve.Evaluate(Time.deltaTime) * 11f;
        CalcFlattenedForward();
        UpdateCameraPosition(focus);
        UpdateCameraRotation(isTargeting);
        UpdateCameraZoom();
    }

    private void CalcFlattenedForward() {
        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        flattenedForward = Vector3.Normalize(cameraForward);
    }

    private void UpdateCameraPosition(Vector3 focus) {
        playerCamera.transform.position = 
            Vector3.Lerp(
                playerCamera.transform.position, 
                focus + Vector3.up * (2 * (1 - targetRotation.eulerAngles.x / 70)), 
                cameraAnimationSpeed);
    }

    private void UpdateCameraZoom() {
        float zoom = input.GetCameraZoomInput().y * 2;
        int unclampedOrthoSize = (int)(targetOrthoSize - zoom);
        int clampedOrthoSize = Math.Clamp(unclampedOrthoSize, 2, 22);
        
        if (unclampedOrthoSize != clampedOrthoSize) return;

        targetOrthoSize = clampedOrthoSize;

        float cameraOrthographicSize = playerCamera.orthographicSize;
        playerCamera.orthographicSize = Mathf.Lerp(cameraOrthographicSize, 
            targetOrthoSize,
            cameraAnimationSpeed * Mathf.Abs(targetOrthoSize - cameraOrthographicSize));
        
        targetRotation = Quaternion.Euler(
            Mathf.Clamp(targetRotation.eulerAngles.x - zoom, targetOrthoSize + 10, 70),
            targetRotation.eulerAngles.y,
            0);
    }

    private void UpdateCameraRotation(bool isTargeting) {
        float rotationStrength = rotationSensitivity * 100;
        
        Vector2 cameraRotInput = input.GetCameraRotationInput();
        targetRotation = Quaternion.Euler(
            targetRotation.eulerAngles.x, 
            targetRotation.eulerAngles.y + cameraRotInput.x * rotationStrength * Time.deltaTime,
            0);
        
        playerCamera.transform.rotation = Quaternion.Lerp(
            playerCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * cameraAnimationSpeed * 1000);
    }
}