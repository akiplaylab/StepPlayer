using UnityEngine.InputSystem;

public static class KeyBindings
{
    static Keyboard Kb => Keyboard.current;

    public static bool MenuUpPressedThisFrame()
    {
        return ActionPressedThisFrame(KeyBindingConfig.InputAction.MenuUp);
    }

    public static bool MenuDownPressedThisFrame()
    {
        return ActionPressedThisFrame(KeyBindingConfig.InputAction.MenuDown);
    }

    public static bool MenuConfirmPressedThisFrame()
    {
        return ActionPressedThisFrame(KeyBindingConfig.InputAction.MenuConfirm);
    }

    public static bool MenuLeftPressedThisFrame()
    {
        return ActionPressedThisFrame(KeyBindingConfig.InputAction.MenuLeft);
    }

    public static bool MenuRightPressedThisFrame()
    {
        return ActionPressedThisFrame(KeyBindingConfig.InputAction.MenuRight);
    }

    public static bool LanePressedThisFrame(Lane lane)
    {
        return lane switch
        {
            Lane.Left => ActionPressedThisFrame(KeyBindingConfig.InputAction.LaneLeft),
            Lane.Down => ActionPressedThisFrame(KeyBindingConfig.InputAction.LaneDown),
            Lane.Up => ActionPressedThisFrame(KeyBindingConfig.InputAction.LaneUp),
            Lane.Right => ActionPressedThisFrame(KeyBindingConfig.InputAction.LaneRight),
            _ => false,
        };
    }

    static bool ActionPressedThisFrame(KeyBindingConfig.InputAction action)
    {
        var kb = Kb;
        if (kb == null) return false;

        var binding = KeyBindingConfig.GetBinding(action);

        return KeyPressedThisFrame(kb, binding.Primary)
            || KeyPressedThisFrame(kb, binding.Secondary);
    }

    static bool KeyPressedThisFrame(Keyboard kb, Key key)
    {
        if (key == Key.None) return false;

        var control = kb[key];
        return control != null && control.wasPressedThisFrame;
    }
}
