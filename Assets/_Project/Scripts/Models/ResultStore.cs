public static class ResultStore
{
    public static JudgementSummary Summary { get; set; }
    public static bool HasSummary { get; set; }
    public static string SongTitle { get; set; }
    public static string MusicSource { get; set; }

    public static void Clear()
    {
        HasSummary = false;
        SongTitle = null;
        MusicSource = null;
    }
}
