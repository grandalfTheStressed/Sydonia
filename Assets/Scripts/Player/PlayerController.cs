using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Input input;
    [SerializeField] private float acceleration = 32f;
    [SerializeField] private float drag = 8f;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private Camera playerCamera;

    private PlayerCameraController playerCameraController;
    private TargetTracker targetTracker;

    private GameObject target;
    
    private bool isWalking;
    private bool isTargeting;
    private float movementForce;
    private float dragHack;

    public void Start()
    {
        playerCameraController = new PlayerCameraController(playerCamera, input);
        targetTracker = GetComponentInChildren<TargetTracker>();
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;
        rigidBody.drag = drag;
        movementForce = rigidBody.mass * acceleration;
        dragHack = Mathf.Pow(1 - Time.fixedDeltaTime, 2);
    }

    private void OnValidate()
    {
        Start();
    }

    private void Update()
    {
        ReadInputs();
    }

    private void ReadInputs() {
        Vector3 focus = UpdateTargeting();
        UpdateCamera(focus, isTargeting);
    }
    
    private Vector3 UpdateTargeting() {

        isTargeting = input.GetLockOnInput();
        
        if (isTargeting && target != null) {
            return transform.position - (transform.position - target.transform.position) * .5f;
        }
        
        target = targetTracker.UpdateClosetTarget();
        
        return transform.position;
    }

    private void UpdateCamera(Vector3 focus, bool isTargeting) {
        playerCameraController.UpdateCamera(focus, isTargeting);
    }

    private void FixedUpdate()
    {
        ReadFixedUpdateInputs();
    }
    
    private void ReadFixedUpdateInputs()
    {
        Vector2 inputVector = input.GetDirectionalInput();
        Vector3 directionalInput = new Vector3(inputVector.x, 0, inputVector.y);
        
        isWalking = directionalInput != Vector3.zero;
        
        UpdateMovement(directionalInput);
    }

    private void UpdateMovement(Vector3 directionalInput)
    {
        Vector3 moveDir = UpdatePlayerForward(directionalInput);
        
        if (!isWalking) return;

        bool grounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, 1.1f);
        rigidBody.drag = grounded ? drag : 0;
        
        float forceScale = grounded ? 1.0f : 0.1f;
        rigidBody.AddForce(moveDir.normalized * (movementForce * forceScale));
    }

    private Vector3 UpdatePlayerForward(Vector3 directionalInput) {
        Vector3 moveDir;
        
        if (isTargeting && target != null) {
            Vector3 targetDir = transform.position - target.transform.position;
            targetDir.y = 0;
            transform.forward = -targetDir.normalized;

            float centripAccel = rigidBody.velocity.sqrMagnitude / dragHack / targetDir.magnitude;
            
            if(centripAccel > 0 && centripAccel < acceleration)
                rigidBody.AddForce(transform.forward * (centripAccel * rigidBody.mass));
            
            moveDir =  transform.forward * directionalInput.z + 
                       transform.right * directionalInput.x;
        }
        else {
            Vector3 camForward = playerCameraController.FlattenedForward;
            Vector3 camRight = playerCamera.transform.right;
        
            moveDir =  camForward * directionalInput.z + 
                       camRight * directionalInput.x;

            if (isWalking) {
                transform.forward = moveDir.normalized;
            }
        }

        return moveDir;
    }

    public bool IsWalking()
    {
        return isWalking;
    }
}
