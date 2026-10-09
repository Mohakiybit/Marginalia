using UnityEngine;

/// <summary>
/// Plays footstep sounds based on distance walked and the player's move state.
/// Louder and faster when sprinting, quiet and slow when crouching, so the
/// sound matches what the Curator hears through NoiseSystem.
/// Setup: add to the Player object. Drag a few footstep clips into
/// "Footstep Clips" in the Inspector.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(CharacterController))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Add 3 or more so steps don't sound repetitive.")]
    [SerializeField] AudioClip[] footstepClips;

    [Header("Distance between steps (meters)")]
    [SerializeField] float crouchStride = 1.0f;
    [SerializeField] float walkStride = 1.6f;
    [SerializeField] float sprintStride = 2.2f;

    [Header("Volume per state (0 to 1)")]
    [SerializeField] float crouchVolume = 0.1f;
    [SerializeField] float walkVolume = 0.4f;
    [SerializeField] float sprintVolume = 1.0f;

    [Header("Variation")]
    [SerializeField] float pitchMin = 0.92f;
    [SerializeField] float pitchMax = 1.08f;

    PlayerMovement player;
    CharacterController controller;
    AudioSource source;
    Vector3 lastPos;
    float distanceAccumulated;
    int lastClipIndex = -1;
    bool warned;

    void Awake()
    {
        player = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        source = GetComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D: the player's own footsteps
        lastPos = transform.position;
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPos;
        delta.y = 0f;
        lastPos = transform.position;

        if (!controller.isGrounded || player.State == MoveState.Idle)
        {
            distanceAccumulated = 0f;
            return;
        }

        distanceAccumulated += delta.magnitude;

        float stride = GetStride(player.State);
        if (distanceAccumulated >= stride)
        {
            distanceAccumulated -= stride;
            PlayStep(GetVolume(player.State));
        }
    }

    float GetStride(MoveState state)
    {
        switch (state)
        {
            case MoveState.Crouching: return crouchStride;
            case MoveState.Sprinting: return sprintStride;
            default: return walkStride;
        }
    }

    float GetVolume(MoveState state)
    {
        switch (state)
        {
            case MoveState.Crouching: return crouchVolume;
            case MoveState.Sprinting: return sprintVolume;
            default: return walkVolume;
        }
    }

    void PlayStep(float volume)
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            if (!warned)
            {
                Debug.LogWarning("FootstepAudio: no clips assigned yet. Add some in the Inspector.", this);
                warned = true;
            }
            return;
        }

        // Pick a random clip, but never the same one twice in a row
        int index = Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;
        lastClipIndex = index;

        source.pitch = Random.Range(pitchMin, pitchMax);
        source.PlayOneShot(footstepClips[index], volume);
    }
}