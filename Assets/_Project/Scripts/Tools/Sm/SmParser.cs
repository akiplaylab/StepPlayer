using System;
using System.Collections.Generic;

public static class SmParser
{
    public static Dictionary<string, string> ParseHeader(string content)
    {
        return SmTagParser.ParseHeader(content);
    }
}
