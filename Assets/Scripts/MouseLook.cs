using UnityEngine;

/// <summary>
/// First-person mouse look. Put on the camera (or camera root).
/// Yaw rotates the player body, pitch rotates only the camera.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [SerializeField] Transform playerBody;
    [SerializeField] float sensitivity = 2f;
    [SerializeField] float minPitch = -85f;
    [SerializeField] float maxPitch = 85f;

    float pitch;

    void Start()
    {
        LockCursor(true);
    }

    void Update()
    {
        // Esc toggles cursor so the pause menu can work later
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0)) LockCursor(true);
            return;
        }

        if (PlayerHealth.Frozen || Interactor.IsInspecting) return;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        pitch = Mathf.Clamp(pitch - my, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        playerBody.Rotate(Vector3.up * mx);
    }

    public void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}