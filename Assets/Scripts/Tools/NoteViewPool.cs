using System.Collections.Generic;
using UnityEngine;

public sealed class NoteViewPool
{
    readonly NoteView prefab;
    readonly Transform parent;
    readonly Stack<NoteView> pool = new();

    public NoteViewPool(NoteView prefab, Transform parent, int prewarm = 0)
    {
        this.prefab = prefab != null ? prefab : throw new System.ArgumentNullException(nameof(prefab));
        this.parent = parent;

        if (prewarm > 0)
        {
            for (int i = 0; i < prewarm; i++)
            {
                var v = CreateNew();
                v.gameObject.SetActive(false);
                pool.Push(v);
            }
        }
    }

    public NoteView Rent()
    {
        var v = pool.Count > 0 ? pool.Pop() : CreateNew();

        var sr = v.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        v.gameObject.SetActive(true);
        return v;
    }

    public void Return(NoteView view)
    {
        if (view == null) return;

        view.gameObject.SetActive(false);

        if (parent != null)
            view.transform.SetParent(parent, worldPositionStays: false);

        pool.Push(view);
    }

    NoteView CreateNew()
    {
        return parent != null
            ? Object.Instantiate(prefab, parent)
            : Object.Instantiate(prefab);
    }
}
