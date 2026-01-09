using System;
using UnityEngine;

public static class ColorUtil
{
    public static Color HexToColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("hex is null or empty");

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6)
            throw new ArgumentException("hex must be 6 characters (RRGGBB)");

        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);

        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
