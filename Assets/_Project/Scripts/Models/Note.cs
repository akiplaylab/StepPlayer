public sealed class Note
{
    public double Beat { get; }
    public Lane Lane { get; }

    public Note(double beat, Lane lane)
    {
        Beat = beat;
        Lane = lane;
    }
}
