using UnityEngine;

public abstract class InputEventBase
{
    public bool pressed;
}

// ============================================================================
// INPUT EVENTS
// ============================================================================

public class OnMoveInputEvent : InputEventBase
{
    public Vector2 Direction;
}

public class OnLookInputEvent : InputEventBase
{
    public Vector2 Delta;
}

public class OnActionInputEvent : InputEventBase
{
}

public class OnCrouchInputEvent : InputEventBase
{
}

public class OnJumpInputEvent : InputEventBase
{
}

public class OnSwapInputEvent : InputEventBase
{
}