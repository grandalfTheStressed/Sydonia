using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Input : MonoBehaviour {
    private InputActions inputActions;

    private void Awake() {
        Enable();
    }

    private void OnValidate() {
        Enable();
    }
    
    public bool GetLockOnInput()
    {
        try {
            return inputActions.Player.LockRotation.IsInProgress();
        }
        catch (Exception)
        {
            Enable();
        }

        return false;
    }
    
    public bool GetRunInput()
    {
        try {
            return inputActions.Player.Run.IsInProgress();
        }
        catch (Exception)
        {
            Enable();
        }

        return false;
    }
    
    public bool GetDodgeInput()
    {
        try {
            return inputActions.Player.Roll.IsInProgress();
        }
        catch (Exception)
        {
            Enable();
        }

        return false;
    }

    public Vector2 GetDirectionalInput() {
        try
        {
            return inputActions.Player.Move.ReadValue<Vector2>().normalized;
        }
        catch (Exception)
        {
            Enable();
        }

        return Vector2.zero;
    }

    public Vector2 GetCameraRotationInput() {
        try
        {
            return inputActions.Player.Rotate.ReadValue<Vector2>().normalized;
        }
        catch (Exception)
        {
            Enable();
        }
        
        return Vector2.zero;
    }

    public Vector2 GetCameraZoomInput() {
        try
        {
            return inputActions.Player.Zoom.ReadValue<Vector2>().normalized;
        }
        catch (Exception)
        {
            Enable();
        }
        
        return Vector2.zero;
    }

    private void Enable() {
        inputActions = new InputActions();
        inputActions.Player.Enable();
    }
}
