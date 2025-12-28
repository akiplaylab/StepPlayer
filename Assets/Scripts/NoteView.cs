using UnityEngine;

public sealed class NoteView : MonoBehaviour
{
    public Lane lane;
    public double timeSec;
    public NoteDivision division;

    private Color quarterColor = Color.red;
    private Color eighthColor = Color.blue;
    private Color sixteenthColor = Color.yellow;

    [SerializeField] SpriteRenderer spriteRenderer;

    public void Init(Lane lane, double timeSec, NoteDivision division)
    {
        this.lane = lane;
        this.timeSec = timeSec;
        this.division = division;

        ApplyRotation();
        ApplyColor(division);
    }

    private void OnValidate()
    {
        ApplyRotation();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void ApplyRotation()
    {
        float z = lane switch
        {
            Lane.Left => 0f,
            Lane.Down => 90f,
            Lane.Up => -90f,
            Lane.Right => 180f,
            _ => 0f
        };
        transform.localRotation = Quaternion.Euler(0, 0, z);
    }

    private void ApplyColor(NoteDivision division)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.color = division switch
        {
            NoteDivision.Quarter => quarterColor,
            NoteDivision.Eighth => eighthColor,
            NoteDivision.Sixteenth => sixteenthColor,
            _ => Color.white
        };
    }
}
