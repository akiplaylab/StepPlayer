using UnityEngine;

public sealed class NoteView : MonoBehaviour
{
    public Lane Lane { get; private set; }
    public double TimeSec { get; private set; }
    public NoteDivision Division { get; private set; }

    [Header("Division Colors")]
    [SerializeField] Color quarterColor = Color.red;
    [SerializeField] Color eighthColor = Color.blue;
    [SerializeField] Color sixteenthColor = Color.yellow;

    [SerializeField] SpriteRenderer spriteRenderer;

    public void Init(Note note)
    {
        Lane = note.Lane;
        TimeSec = note.TimeSec;
        Division = note.Division;

        ApplyRotation();
        ApplyColor();
    }

    void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void ApplyRotation()
    {
        float z = Lane switch
        {
            Lane.Left => 0f,
            Lane.Down => 90f,
            Lane.Up => -90f,
            Lane.Right => 180f,
            _ => 0f
        };
        transform.localRotation = Quaternion.Euler(0, 0, z);
    }

    private void ApplyColor()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = Division switch
        {
            NoteDivision.Quarter => quarterColor,
            NoteDivision.Eighth => eighthColor,
            NoteDivision.Sixteenth => sixteenthColor,
            _ => Color.white
        };
    }
}
