public readonly struct BpmChange
{
    public readonly double Beat;
    public readonly double Bpm;

    public BpmChange(double beat, double bpm)
    {
        Beat = beat;
        Bpm = bpm;
    }
}
