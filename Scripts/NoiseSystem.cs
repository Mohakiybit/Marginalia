using System;
using UnityEngine;

/// <summary>
/// Loudness values straight from the GDD noise table.
/// Tweak these after playtesting (GDD marks them TBD).
/// </summary>
public static class NoiseLevels
{
    public const float Silent     = 0f;
    public const float CrouchWalk = 1f;
    public const float Walk       = 3f;
    public const float OpenDoor   = 5f;
    public const float Puzzle     = 6f;
    public const float Sprint     = 8f;
    public const float TuningFork = 10f;
}

/// <summary>One sound that happened in the world.</summary>
public readonly struct NoiseEvent
{
    public readonly Vector3 Position;
    public readonly float Loudness;
    public readonly GameObject Source;
    public readonly float Time;

    public NoiseEvent(Vector3 position, float loudness, GameObject source)
    {
        Position = position;
        Loudness = loudness;
        Source = source;
        Time = UnityEngine.Time.time;
    }
}

/// <summary>
/// Simple static event bus. Anything that makes noise calls Emit().
/// The Curator subscribes to OnNoise and pushes events into his heap
/// (loudest first).
/// </summary>
public static class NoiseSystem
{
    public static event Action<NoiseEvent> OnNoise;

    public static void Emit(Vector3 position, float loudness, GameObject source = null)
    {
        if (loudness <= 0f) return;
        OnNoise?.Invoke(new NoiseEvent(position, loudness, source));
    }
}
