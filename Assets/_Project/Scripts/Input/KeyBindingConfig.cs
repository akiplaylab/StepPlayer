using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyBindingConfig
{
    const string Prefix = "KeyBinding.";

    public enum InputAction
    {
        MenuUp,
        MenuDown,
        MenuLeft,
        MenuRight,
        MenuConfirm,
        LaneLeft,
        LaneDown,
        LaneUp,
        LaneRight,
    }

    public readonly struct KeyBinding
    {
        public KeyBinding(Key primary, Key secondary)
        {
            Primary = primary;
            Secondary = secondary;
        }

        public Key Primary { get; }
        public Key Secondary { get; }
    }

    public static KeyBinding GetBinding(InputAction action)
    {
        var defaults = GetDefaultBinding(action);

        return new KeyBinding(
            LoadKey(BuildKey(action, "Primary"), defaults.Primary),
            LoadKey(BuildKey(action, "Secondary"), defaults.Secondary));
    }

    public static void SetBinding(InputAction action, Key primary, Key secondary)
    {
        SaveKey(BuildKey(action, "Primary"), primary);
        SaveKey(BuildKey(action, "Secondary"), secondary);
        PlayerPrefs.Save();
    }

    static string BuildKey(InputAction action, string slot)
    {
        return $"{Prefix}{action}.{slot}";
    }

    static void SaveKey(string storageKey, Key key)
    {
        PlayerPrefs.SetString(storageKey, key.ToString());
    }

    static Key LoadKey(string storageKey, Key fallback)
    {
        var raw = PlayerPrefs.GetString(storageKey, fallback.ToString());
        return Enum.TryParse(raw, out Key parsed) ? parsed : fallback;
    }

    static KeyBinding GetDefaultBinding(InputAction action)
    {
        return action switch
        {
            InputAction.MenuUp => new KeyBinding(Key.UpArrow, Key.J),
            InputAction.MenuDown => new KeyBinding(Key.DownArrow, Key.F),
            InputAction.MenuLeft => new KeyBinding(Key.LeftArrow, Key.D),
            InputAction.MenuRight => new KeyBinding(Key.RightArrow, Key.K),
            InputAction.MenuConfirm => new KeyBinding(Key.Enter, Key.None),
            InputAction.LaneLeft => new KeyBinding(Key.D, Key.LeftArrow),
            InputAction.LaneDown => new KeyBinding(Key.F, Key.DownArrow),
            InputAction.LaneUp => new KeyBinding(Key.J, Key.UpArrow),
            InputAction.LaneRight => new KeyBinding(Key.K, Key.RightArrow),
            _ => new KeyBinding(Key.None, Key.None),
        };
    }
}
