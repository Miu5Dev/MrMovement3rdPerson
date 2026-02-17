using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all input and raises events through the EventBus.
/// Fixed to properly support controllers with continuous input.
/// </summary>
public class InputSystem : MonoBehaviour
{
    private MyInputs inputs;

    private void Awake()
    {
        inputs = new MyInputs();

        // Movement
        inputs.Player.Move.performed += OnMovePerformed;
        inputs.Player.Move.canceled += OnMoveCanceled;
        
        // Look
        inputs.Player.Look.performed += OnLookPerformed;
        inputs.Player.Look.canceled += OnLookCanceled;
        
        // Button inputs
        inputs.Player.Action.performed += OnActionInput;
        inputs.Player.Action.canceled += OnActionInput;
        inputs.Player.Jump.performed += OnJumpInput;
        inputs.Player.Jump.canceled += OnJumpInput;
        inputs.Player.Crouch.performed += OnCrouchInput;
        inputs.Player.Crouch.canceled += OnCrouchInput;
        inputs.Player.Swap.performed += OnSwapInput;
        inputs.Player.Swap.canceled += OnSwapInput;
        
        Debug.Log("[InputSystem] Initialized");
    }

    void OnEnable()
    {
        inputs.Player.Enable();
    }

    void OnDisable()
    {
        inputs.Player.Disable();
    }

    
    // ========================================================================
    // MOVEMENT INPUT
    // ========================================================================
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnMoveInputEvent()
        {
            pressed = context.performed,
            Direction = context.ReadValue<Vector2>()
        });
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnMoveInputEvent()
        {
            pressed = context.performed,
            Direction = context.ReadValue<Vector2>()
        });
    }
    
    // ========================================================================
    // LOOK INPUT
    // ========================================================================
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnLookInputEvent()
        {
            pressed = context.performed,
            Delta = context.ReadValue<Vector2>()
        });
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnLookInputEvent()
        {
            pressed = context.performed,
            Delta = context.ReadValue<Vector2>()
        });
    }
    
    // ========================================================================
    // BUTTON INPUTS
    // ========================================================================

    private void OnActionInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnActionInputEvent()
        {
            pressed = context.performed
        });
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnCrouchInputEvent()
        {
            pressed = context.performed
        });
    }

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnJumpInputEvent()
        {
            pressed = context.performed
        });
    }
    
    private void OnSwapInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new OnSwapInputEvent()
        {
            pressed = context.performed
        });
    }
}
