using System;
using UnityEngine;

public class Input : MonoBehaviour
{
    private InputActions inputActions;

    private void Awake()
    {
        enable();
    }

    private void OnValidate()
    {
        enable();
    }
    
    public bool GetLockOnInput()
    {
        try
        {
            return Math.Abs(inputActions.Player.LockOn.ReadValue<float>() - 1.0) < .1;
        }
        catch (Exception)
        {
            enable();
        }

        return false;
    }

    public Vector2 GetDirectionalInput()
    {
        try
        {
            return inputActions.Player.Move.ReadValue<Vector2>().normalized;
        }
        catch (Exception)
        {
            enable();
        }

        return Vector2.zero;
    }

    public Vector2 GetCameraRotationInput()
    {
        try
        {
            return inputActions.Player.Rotate.ReadValue<Vector2>().normalized;
        }
        catch (Exception)
        {
            enable();
        }
        
        return Vector2.zero;
    }

    public Vector2 GetCameraZoomInput()
    {
        try
        {
            return inputActions.Player.Zoom.ReadValue<Vector2>().normalized;
        }
        catch (Exception)
        {
            enable();
        }
        
        return Vector2.zero;
    }

    private void enable()
    {
        inputActions = new InputActions();
        inputActions.Player.Enable();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}
