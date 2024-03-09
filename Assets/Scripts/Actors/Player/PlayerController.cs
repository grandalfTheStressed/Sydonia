using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Input input;
    [SerializeField] private float acceleration = 32f;
    [SerializeField] private float dodgeAcceleration = 32f;
    [SerializeField][Range(5f, 12f)] private float runTopSpeed = 32f;
    [SerializeField][Range(.1f, 5f)] private float WalkTopSpeed = 32f;
    [SerializeField] private float drag = 8f;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private Camera cam;

    private TargetTracker targetTracker;
    private GameObject target;
    private Animator animator;
    
    private float topSpeed;
    private bool isMoving;
    private bool isDodging;
    private bool isTargeting;
    private float moveForce;
    private float dodgeForce;
    private float dragHack;
    private bool grounded;
    private static readonly int _IsWalking = Animator.StringToHash("Is Walking");
    private static readonly int _IsRunning = Animator.StringToHash("Is Running");
    private static readonly int _Dodge = Animator.StringToHash("Dodge");

    public void Start()
    {
        targetTracker = GetComponentInChildren<TargetTracker>();
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        rigidBody.freezeRotation = true;
        rigidBody.drag = drag;
        moveForce = rigidBody.mass * acceleration;
        dodgeForce = rigidBody.mass * dodgeAcceleration;
        dragHack = Mathf.Pow(1 - Time.fixedDeltaTime, 5);
    }

    private void OnValidate()
    {
        Start();
    }

    private void FixedUpdate()
    {
        UpdateMovement();
        UpdateAnimatorProps();
        Vector3 focus = UpdateTargeting();
    }

    private void UpdateMovement() {

        Vector3 directionalInput = UpdateDirectionalInput();
        
        UpdatePlayerForward(directionalInput);
        
        UpdateDodgeRoll();

        CheckIfAirborne();
        
        UpdateMovementForce();
        
        GovernSpeed();

        bool sliding = rigidBody.velocity.sqrMagnitude > 0 && !isMoving;
        
    }

    private Vector3 UpdateDirectionalInput() {
        Vector2 inputVector = input.GetDirectionalInput();
        Vector3 directionalInput = new Vector3(inputVector.x, 0, inputVector.y);
        
        isMoving = directionalInput != Vector3.zero;

        topSpeed = input.GetRunInput() || isDodging ? runTopSpeed : WalkTopSpeed;

        return directionalInput;
    }

    private void CheckIfAirborne() {
        grounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, 1.1f);
    }

    private void UpdateMovementForce() {
        
        Vector3 moveDir = transform.forward;
        
        float forceScale = grounded ? 1.0f : 0.1f;
        
        rigidBody.drag = grounded && !isMoving ? drag : 0;

        float force = forceScale;

        if (isDodging) {
            force *= dodgeForce;
        }
        else if (isMoving) {
            force *= moveForce;
        }
        else {
            force *= 0;
        }

        rigidBody.AddForce(moveDir.normalized * force);
    }

    private void UpdateDodgeRoll() {

        isDodging = animator.GetBool(_Dodge) ||
                    animator.GetAnimatorTransitionInfo(0).IsName("Start Roll") ||
                    animator.GetCurrentAnimatorStateInfo(0).IsName("Roll") ||
                    animator.GetAnimatorTransitionInfo(0).IsName("Stop Roll");

        isMoving = isMoving || isDodging;
        
        bool dodgeInput = input.GetDodgeInput();

        if (!dodgeInput || isDodging || !grounded) return;


        rigidBody.velocity = Vector3.zero;
        animator.SetTrigger(_Dodge);
    }

    private void GovernSpeed() {
        if (!grounded || !isMoving) return;

        float currentSpeed = rigidBody.velocity.magnitude;
        
        if (currentSpeed < topSpeed) return;

        rigidBody.velocity = topSpeed * rigidBody.velocity.normalized;
    }
    
    private void UpdatePlayerForward(Vector3 directionalInput) {
        Vector3 moveDir;
        
        //float centripAccel = rigidBody.velocity.sqrMagnitude / dragHack / targetDir.magnitude;

        Vector3 camForward = CalcFlattenedCameraForward();
        Vector3 camRight = cam.transform.right;
        
        moveDir = camForward * directionalInput.z + 
                  camRight * directionalInput.x;

        if (isMoving) transform.forward = moveDir.normalized;
    }
    
    private Vector3 UpdateTargeting() {

        isTargeting = input.GetLockOnInput();
        
        if (isTargeting && target != null) {
            return transform.position - (transform.position - target.transform.position) * .5f;
        }
        
        target = targetTracker.UpdateClosetTarget();
        
        return transform.position;
    }

    private void UpdateAnimatorProps() {
        animator.SetBool(_IsWalking, topSpeed >= WalkTopSpeed && isMoving);
        animator.SetBool(_IsRunning, topSpeed >= runTopSpeed && isMoving);
    }
    
    private Vector3 CalcFlattenedCameraForward() {
        Vector3 cameraForward = cam.transform.forward;
        cameraForward.y = 0;
        return Vector3.Normalize(cameraForward);
    }
}
