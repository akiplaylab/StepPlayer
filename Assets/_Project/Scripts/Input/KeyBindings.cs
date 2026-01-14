using UnityEngine.InputSystem;

public static class KeyBindings
{
    static Keyboard Kb => Keyboard.current;

    public static bool MenuUpPressedThisFrame()
    {
        var kb = Kb;
        return kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame);
    }

    public static bool MenuDownPressedThisFrame()
    {
        var kb = Kb;
        return kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame);
    }

    public static bool MenuConfirmPressedThisFrame()
    {
        var kb = Kb;
        return kb != null && kb.enterKey.wasPressedThisFrame;
    }

    public static bool MenuLeftPressedThisFrame()
    {
        var kb = Kb;
        return kb != null && kb.leftArrowKey.wasPressedThisFrame;
    }

    public static bool MenuRightPressedThisFrame()
    {
        var kb = Kb;
        return kb != null && kb.rightArrowKey.wasPressedThisFrame;
    }

    public static bool LanePressedThisFrame(Lane lane)
    {
        var kb = Kb;
        if (kb == null) return false;

        return lane switch
        {
            Lane.Left => kb.leftArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame,
            Lane.Down => kb.downArrowKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame,
            Lane.Up => kb.upArrowKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame,
            Lane.Right => kb.rightArrowKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame,
            _ => false,
        };
    }
}
