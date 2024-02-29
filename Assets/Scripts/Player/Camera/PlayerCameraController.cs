using System;
using System.Xml.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PlayerCameraController {
    private AnimationCurve cameraMovementCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    private Camera playerCamera;
    private Input input;

    private float rotationSensitivity = 5f;
    private float cameraAnimationSpeed = 0;

    private Quaternion _prevRotation;
    private Vector3 _snapSpaceForward;
    private Vector3 _snapSpaceUp;
    
    private Quaternion targetRotation;
    private int targetOrthoSize;
    private bool snapCameraToGrid;

    private Vector3 flattenedForward = Vector3.forward;

    public Vector3 FlattenedForward => flattenedForward;

    public PlayerCameraController(Camera playerCamera, Input input, bool snapCameraToGrid) {
        this.playerCamera = playerCamera;
        this.input = input;
        this.snapCameraToGrid = snapCameraToGrid;
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
        playerCamera.transform.position = focus + Vector3.up * (2 * (1 - targetRotation.eulerAngles.x / 70));

        if (!snapCameraToGrid) return;

        Vector3 screenPoint = playerCamera.WorldToScreenPoint(playerCamera.transform.position);
        

        screenPoint.x = Mathf.RoundToInt(screenPoint.x);
        screenPoint.y = Mathf.RoundToInt(screenPoint.y);

        playerCamera.transform.position = playerCamera.ScreenToWorldPoint(screenPoint);
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