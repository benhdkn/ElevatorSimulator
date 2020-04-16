using UnityEngine;

public class ManualControls : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) 
        {
            Elevator.Instance.Request(0);
        }
        if (Input.GetKeyDown(KeyCode.Z)) 
        {
            Elevator.Instance.Request(1);
        }
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            Elevator.Instance.Request(2);
        }
        if (Input.GetKeyDown(KeyCode.R)) 
        {
            Elevator.Instance.Request(3);
        }

        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            Elevator.Instance.MoveToNextFloorInQueue();
        }
    }
}
