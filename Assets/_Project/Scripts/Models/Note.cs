public sealed class Note
{
    public double TimeSec { get; }
    public Lane Lane { get; }
    public NoteDivision Division { get; }

    public Note(double timeSec, Lane lane, NoteDivision division)
    {
        TimeSec = timeSec;
        Lane = lane;
        Division = division;
    }
}
