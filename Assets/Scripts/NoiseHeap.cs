using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Max-heap (priority queue) of noise events. The loudest noise is always on top.
/// If two noises are equally loud, the more recent one wins.
/// Written by hand because Unity's C# has no built-in PriorityQueue.
/// </summary>
public class NoiseHeap
{
    readonly List<NoiseEvent> data = new List<NoiseEvent>();

    public int Count => data.Count;

    public void Clear() => data.Clear();

    public void Push(NoiseEvent e)
    {
        data.Add(e);
        SiftUp(data.Count - 1);
    }

    public bool TryPeek(out NoiseEvent e)
    {
        if (data.Count == 0) { e = default; return false; }
        e = data[0];
        return true;
    }

    public bool TryPop(out NoiseEvent e)
    {
        if (data.Count == 0) { e = default; return false; }

        e = data[0];
        int last = data.Count - 1;
        data[0] = data[last];
        data.RemoveAt(last);
        if (data.Count > 0) SiftDown(0);
        return true;
    }

    // Is a more important than b?
    static bool HigherPriority(NoiseEvent a, NoiseEvent b)
    {
        if (!Mathf.Approximately(a.Loudness, b.Loudness)) return a.Loudness > b.Loudness;
        return a.Time > b.Time;
    }

    void SiftUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (!HigherPriority(data[i], data[parent])) break;
            Swap(i, parent);
            i = parent;
        }
    }

    void SiftDown(int i)
    {
        int n = data.Count;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int best = i;

            if (left < n && HigherPriority(data[left], data[best])) best = left;
            if (right < n && HigherPriority(data[right], data[best])) best = right;
            if (best == i) break;

            Swap(i, best);
            i = best;
        }
    }

    void Swap(int a, int b)
    {
        NoiseEvent tmp = data[a];
        data[a] = data[b];
        data[b] = tmp;
    }
}