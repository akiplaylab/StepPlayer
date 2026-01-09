using System;

[Serializable]
public class ChartJson
{
    public string musicFile;
    public int bpm;
    public float offsetSec;
    public Measure[] measures;
}
