using UnityEngine;

/// <summary>
/// A reading lamp: look at it and press E to light it. It becomes your
/// respawn point and heals your hits (GDD: "back to the last reading lamp").
/// Setup: put on a mesh with a Collider. Optionally add a child Light and
/// assign it to Lamp Light. Optionally assign Respawn Point (an empty object
/// where you want the player to stand), otherwise it uses a spot in front.
/// </summary>
public class ReadingLamp : MonoBehaviour, IInteractable
{
    [SerializeField] Light lampLight;
    [SerializeField] Transform respawnPoint;
    [SerializeField] float respawnOffset = 1.2f;

    bool lit;

    public string Prompt => lit ? "Reading lamp (lit)" : "[E] Light the reading lamp";

    void Awake()
    {
        if (lampLight != null) lampLight.enabled = false;
    }

    public void Interact(Interactor who)
    {
        PlayerHealth health = who.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        Vector3 pos;
        Quaternion rot;
        if (respawnPoint != null)
        {
            pos = respawnPoint.position;
            rot = respawnPoint.rotation;
        }
        else
        {
            pos = transform.position + transform.forward * respawnOffset;
            pos.y = transform.position.y;
            rot = Quaternion.LookRotation(-transform.forward, Vector3.up);
        }

        health.SetCheckpoint(pos, rot);
        lit = true;
        if (lampLight != null) lampLight.enabled = true;
        who.ShowMessage("Checkpoint saved.");
    }
}