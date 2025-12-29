using System;

[Serializable]
public class ChartJson
{
    public string musicFile;
    public int bpm;
    public float offsetSec;
    public Measure[] measures;

    [Serializable]
    public class Measure
    {
        public int subdiv;
        public string[] rows;
    }
}
