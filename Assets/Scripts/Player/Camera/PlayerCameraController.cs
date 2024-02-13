using System;
using UnityEngine;

public class PlayerCameraController
{
    private AnimationCurve cameraMovementCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    private Camera playerCamera;
    private Input input;

    private float _rotationSensitivity = 5f;

    private Quaternion targetRotation;
    private float targetOrthoSize;

    private Vector3 flattenedForward = Vector3.forward;

    public Vector3 FlattenedForward => flattenedForward;

    public PlayerCameraController(Camera playerCamera, Input input)
    {
        this.playerCamera = playerCamera;
        this.input = input;
        targetOrthoSize = playerCamera.orthographicSize;
        targetRotation = this.playerCamera.transform.rotation;
    }
    
    public void UpdateCamera(Vector3 focus, bool isTargeting) {
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

    private void UpdateCameraPosition(Vector3 focus)
    {
        playerCamera.transform.position = 
            Vector3.Lerp(
                playerCamera.transform.position, 
                focus + Vector3.up * (targetOrthoSize * (1 - targetRotation.eulerAngles.x / 70)), 
                cameraMovementCurve.Evaluate(Time.deltaTime) * (22 / targetOrthoSize));
    }

    private void UpdateCameraZoom()
    {
        targetOrthoSize -= input.GetCameraZoomInput().y * 4;
        targetOrthoSize = Math.Clamp(targetOrthoSize, 2, 22);
        
        playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, 
            targetOrthoSize,
            cameraMovementCurve.Evaluate(Time.deltaTime) * (22 / targetOrthoSize));
    }

    private void UpdateCameraRotation(bool isTargeting) {
        float rotationStrength = _rotationSensitivity * 100;
        
        Vector2 cameraRotInput = input.GetCameraRotationInput();
        targetRotation = Quaternion.Euler(
            Mathf.Clamp(targetRotation.eulerAngles.x - cameraRotInput.y * rotationStrength * Time.deltaTime, targetOrthoSize, 70), 
            targetRotation.eulerAngles.y + cameraRotInput.x * rotationStrength * Time.deltaTime,
            0);
        
        playerCamera.transform.rotation = Quaternion.Lerp(
            playerCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * 10);
    }
}