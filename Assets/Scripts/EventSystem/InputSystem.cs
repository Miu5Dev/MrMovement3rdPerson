using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    private MyInputs inputs;

    private void Awake()
    {
        inputs = new MyInputs();

        // Movement
        inputs.Player.Move.performed += OnMovePerformed;
        inputs.Player.Move.canceled += OnMoveCanceled;
        
        // Look - ya NO se usa performed/canceled
        // Se lee en Update cada frame
        
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

    void Update()
    {
        // Leer look input cada frame para que el stick analógico funcione
        Vector2 lookValue = inputs.Player.Look.ReadValue<Vector2>();
        EventBus.Raise(new OnLookInputEvent()
        {
            pressed = lookValue.sqrMagnitude > 0.01f,
            Delta = lookValue
        });
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