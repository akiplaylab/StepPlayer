using UnityEngine;

public sealed class NoteView : MonoBehaviour
{
    public Lane lane;
    public double timeSec;

    public void Init(Lane lane, double timeSec)
    {
        this.lane = lane;
        this.timeSec = timeSec;
    }
}
