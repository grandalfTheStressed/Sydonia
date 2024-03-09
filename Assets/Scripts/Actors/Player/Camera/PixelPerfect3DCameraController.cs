using System;
using System.Xml.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.Serialization;

public class PixelPerfect3DCameraController : MonoBehaviour {
    [SerializeField] private Input input;
    
    [SerializeField] private GameObject focus;
    
    [SerializeField] private bool snapCameraToGrid;

    [SerializeField][Range(0.01f, 1.25f)]private float scale = 1.0f;
    [SerializeField][Min(0)] private float maxDistance = 6;
    [SerializeField][Range(1f, 3f)]private float rotationSensitivity = 1.2f;
    
    [SerializeField] private AnimationCurve RotationTweenCurve;
    [SerializeField] private AnimationCurve PositionTweenCurve;

    private Vector2 cameraOffset;

    public Vector2 CameraOffset => cameraOffset;

    public float Scale => scale;

   private Camera cam;

    private Vector3 targetPosition;
    private int targetOrthoSize;
    private bool lerpToPosition;
    private float rotationElapsedTime = 0;
    
    public void Start() {
        lerpToPosition = false;
        cameraOffset = Vector2.zero;
        cam = GetComponentInParent<Camera>();
        targetPosition = cam.transform.position;
        targetOrthoSize = (int)cam.orthographicSize;
    }

    private void OnValidate()
    {
        Start();
    }

    private void Update()
    {
        UpdateCamera();   
    }
    
    public void UpdateCamera() {
        UpdateCameraRotation();
        UpdateCameraZoom();
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition() {
        
        Vector3 pointToFocus = focus.transform.position + Vector3.up;

        float distanceFromFocus = Vector3.Distance(cam.transform.position, pointToFocus);
        
        lerpToPosition = distanceFromFocus > maxDistance || (distanceFromFocus > .1f && lerpToPosition);

        if (lerpToPosition) {
            float maxSpeed = Mathf.Clamp(maxDistance / Time.deltaTime, .1f, float.MaxValue);
            float currentSpeed = Mathf.Clamp((distanceFromFocus - maxDistance) / maxDistance * maxSpeed, 6f, maxSpeed);
            
            targetPosition = Vector3.MoveTowards(
                targetPosition,
                pointToFocus,
                currentSpeed * Time.deltaTime);
        }

        if (snapCameraToGrid) SnapToPixelGrid();
    }


    private void SnapToPixelGrid() {
        Vector3 focusPosition = targetPosition;
        Transform camTransform = cam.transform;
        Vector3 cameraPosition = camTransform.position;

        //scale = render scale
        float pixelSize = 2f * cam.orthographicSize / (int)(cam.pixelHeight * scale);

        Vector3 worldOffset = focusPosition - cameraPosition;
        
        Vector3 localOffset = cam.transform.InverseTransformDirection(worldOffset) / pixelSize;
        
        Vector3 snapSpacePosition = SnapToGrid(localOffset, pixelSize);
        
        cameraPosition +=
            camTransform.right * snapSpacePosition.x 
            + camTransform.up * snapSpacePosition.y
            + camTransform.forward * snapSpacePosition.z;

        cam.transform.position = cameraPosition;

        Vector3 camOffset = cam.transform.InverseTransformDirection(focusPosition - cameraPosition);
        cameraOffset.x = camOffset.x / (cam.orthographicSize * 2 * cam.aspect);
        cameraOffset.y = camOffset.y / (cam.orthographicSize * 2);
    }
    
    Vector3 SnapToGrid(Vector3 position, float gridSize)
    {
        return new Vector3(
            Mathf.RoundToInt(position.x) * gridSize,
            Mathf.RoundToInt(position.y) * gridSize,
            Mathf.RoundToInt(position.z) * gridSize
        );
    }

    private void UpdateCameraZoom() {
        float zoom = input.GetCameraZoomInput().y * 2;
        int unclampedOrthoSize = (int)(targetOrthoSize - zoom);
        int clampedOrthoSize = Mathf.Clamp(unclampedOrthoSize, 2, 22);

        if (unclampedOrthoSize != clampedOrthoSize) return;
        
        targetOrthoSize = clampedOrthoSize;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, Mathf.Abs(targetOrthoSize - cam.orthographicSize));
    }

    private void UpdateCameraRotation() {
        
        Vector2 cameraRotInput = input.GetCameraRotationInput();
        bool lockRotation = input.GetLockOnInput();

        float rotationY = cam.transform.rotation.eulerAngles.y;

        if (cameraRotInput.x != 0.0f) {
            rotationY = (rotationY + Time.deltaTime * 100 * rotationSensitivity * cameraRotInput.x) % 360;
            lerpToPosition = true;
        }
        
        float rotationX = targetOrthoSize + 10;
        cam.transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);

        if (lockRotation) {
            rotationElapsedTime = 0;
            return;
        }

        rotationElapsedTime += Time.deltaTime;

        float snapRotation = (Mathf.Round(cam.transform.rotation.eulerAngles.y / 45) * 45) - cam.transform.rotation.eulerAngles.y;

        rotationY = cam.transform.rotation.eulerAngles.y + RotationTweenCurve.Evaluate(rotationElapsedTime) * snapRotation;
        
        cam.transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
    }
}