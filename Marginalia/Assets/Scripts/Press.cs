using UnityEngine;

public class DoubleDoor : MonoBehaviour
{
    public Animation LeftDoor;
    public Animation RightDoor;

    public float interactDistance = 2f;
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, player.position) <= interactDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                LeftDoor.Play("LeftDoor (Animation)");
                RightDoor.Play("RightDoor (Animation)");
            }
        }
        float distance = Vector3.Distance(transform.position, player.position);
        Debug.Log("Distance: " + distance);
    }
}