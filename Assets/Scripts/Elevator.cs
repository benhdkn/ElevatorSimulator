using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour 
{
    #region Singleton

    private static Elevator instance;
	public static Elevator Instance 
    {
		get 
        {
			if (instance == null) 
            {
				instance = GameObject.FindObjectOfType<Elevator>();
				if (!instance) 
                {
					Debug.LogError("No Elevator found in this scene.");
				}
			}
			return instance;
		}
	}

    #endregion

    #region Enums

    public enum State 
    {
        None,
        Moving,
        MovingEnd,
        BoardingStart,
        BoardingOut,
        BoardingIn,
        BoardingEnd,
        Idle,
    }

    #endregion

    #region Fields and properties

    [SerializeField] private Transform root = null;

    [SerializeField] private List<Floor> floors = null;

    [SerializeField] private Transform[] anchors = null;

    private bool[] anchorsAvailabilities;

    private State currentState = State.Idle;
    private State previousState = State.Idle;

    private Direction currentDirection = Direction.Up;

    private float stateTimer = 0.0f;

    private int currentFloorIndex = 0;

    private float duration = 0.0f;
    private float savedYPosition = 0.0f;
    private float targetYPosition = 0.0f;

    private List<Passenger> passengersToBoardIn = null;
    private List<Passenger> passengersBoarded = null;
    private List<Passenger> passengersToBoardOut = null;

    private List<int> upFloorQueue = null;
    private List<int> downFloorQueue = null;

    public List<Floor> Floors { get { return floors; } }

    public int CurrentFloorIndex { get { return currentFloorIndex; } }

    public Direction CurrentDirection { get { return currentDirection; } }

    public delegate void IsBoardingHandler(List<Passenger> passengersToBoard);
    public event IsBoardingHandler IsBoardingIn;
    public event IsBoardingHandler IsBoardingOut;

    #endregion

    #region Unity execution

    private void Awake() 
    {
        anchorsAvailabilities = new bool[anchors.Length];
        for (int i = 0; i < anchorsAvailabilities.Length; i++) 
        {
            anchorsAvailabilities[i] = true;
        }

        passengersToBoardIn = new List<Passenger>();
        passengersBoarded = new List<Passenger>();
        passengersToBoardOut = new List<Passenger>();

        upFloorQueue = new List<int>();
        downFloorQueue = new List<int>();
    }

    private void Update() 
    {
        // State change
        if (previousState != currentState) 
        {
            stateTimer = 0.0f;
            savedYPosition = root.transform.position.y;
            
            if (currentState == State.MovingEnd)
            {
                // Remove the current movement from the queue
                if (currentDirection == Direction.Up)
                {
                    upFloorQueue.RemoveAt(0);
                }
                else if (currentDirection == Direction.Down) 
                {
                    downFloorQueue.RemoveAt(0);
                }

                // Check for change of direction for the elevator if we reach ends
                // Has to be done here so we can compare the passengers direction and elevator following one
                TryInvertDirectionOnEnds();

                // If both queues are empty, clear the direction
                if (upFloorQueue.Count == 0 && downFloorQueue.Count == 0) 
                {
                    currentDirection = Direction.None;
                }

                currentState = State.BoardingStart; 
            }
            
            if (currentState == State.BoardingStart) 
            {
                passengersToBoardOut = GetPassengersToBoardOut();
                passengersToBoardIn = Floors[currentFloorIndex].GetPassengersToBoardIn();
                if (passengersToBoardOut.Count > 0) 
                {
                    currentState = State.BoardingOut;
                }
                else 
                {
                    if (passengersToBoardIn.Count > 0) 
                    {
                        currentState = State.BoardingIn;
                    }
                    else 
                    {
                        currentState = State.BoardingEnd;
                    }
                }
            }
            
            if (currentState == State.BoardingOut)
            {   
                // Once this event is raised, we'll have to wait until everyone is out the elevator to proceed
                // See PassengerIsOnExitHandler() and TryCompleteBoardingOutState()
                RaiseIsBoardingOutEvent();
            }
            
            if (currentState == State.BoardingIn)
            {
                // If the elevator there's no room at that point, we'll skip the boarding in
                TryCompleteBoardingInState();
                
                // Once this event is raised, we'll have to wait until everyone is in the elevator to proceed
                // See PassengerIsInElevatorHandler() and TryCompleteBoardingInState()
                RaiseIsBoardingInEvent();
            }
            
            if (currentState == State.BoardingEnd)
            {
                if (upFloorQueue.Count > 0 || downFloorQueue.Count > 0)
                {
                    // This bellow happens when the elevator was idle and called by a single floor
                    if (currentDirection == Direction.None)
                    {
                        // We arbitrary try the upFloorQueue but in case both were set, we should watch for which one was set first and set accordlingly
                        currentDirection = upFloorQueue.Count > 0 ? Direction.Up : Direction.Down;
                    }

                    MoveToNextFloorInQueue(); // Will set the state to Moving
                }
                
                if (upFloorQueue.Count == 0 && downFloorQueue.Count == 0)
                {
                    currentState = State.Idle;
                }
            }

            if (currentState == State.Idle) 
            {
                currentDirection = Direction.None;
            }

            previousState = currentState;
        }
    }

    private void LateUpdate() 
    {
        // State update
        if (currentState == State.Moving) 
        {
            stateTimer += Time.deltaTime;

            if (currentState == State.Moving) 
            {
                if (stateTimer < duration) 
                {
                    float lerpYPosition = Mathf.Lerp(savedYPosition, targetYPosition, stateTimer / duration);
                    Vector3 lerpPosition = root.transform.position;
                    lerpPosition.y = lerpYPosition;
                    root.transform.position = lerpPosition;
                }
                else
                {
                    Vector3 finalPosition = root.transform.position;
                    finalPosition.y = targetYPosition;
                    root.transform.position = finalPosition;

                    currentState = State.MovingEnd;
                }

                // Set the current floor index based on the position of the elevator in real time
                currentFloorIndex = System.Convert.ToInt32(root.transform.position.y / Config.DistanceBetweenFloors);
            }
        }
    }

    #endregion

    #region Anchors

    public bool IsAnyAnchorFree(out Transform anchor)
    {
        bool ret = false;
        anchor = null;
        for (int i = 0; i < anchorsAvailabilities.Length; i++)
        {
            if (anchorsAvailabilities[i] == true) 
            {
                anchor = anchors[i];
                ret = true;
                break;
            }
        }
        return ret;
    }

    public bool IsAnyAnchorFree() 
    {
        return IsAnyAnchorFree(out _);
    }

    public void CheckIn(Transform anchor, Passenger passenger)
    {
        int anchorIndex = System.Array.IndexOf(anchors, anchor);
        anchorsAvailabilities[anchorIndex] = false;
        passengersBoarded.Add(passenger);

        passenger.IsInElevator += PassengerIsInElevatorHandler;
        passenger.IsOnExit += PassengerIsOnExitHandler;
    }

    public void CheckOut(Transform anchor, Passenger passenger)
    {
        int anchorIndex = System.Array.IndexOf(anchors, anchor);
        anchorsAvailabilities[anchorIndex] = true;
        passengersBoarded.Remove(passenger);
    }

    #endregion

    #region Floor movement

    public void Request(int floorIndex) 
    {
        // Add to queues
        if (currentFloorIndex == floorIndex) // Meaning request to come back
        {
            if (currentDirection == Direction.Up)
            {
                AddToDownQueue(floorIndex);
            }
            else if (currentDirection == Direction.Down)
            {
                AddToUpQueue(floorIndex);
            }
        }
        else 
        {
            if (currentFloorIndex - floorIndex < 0)
            {
                AddToUpQueue(floorIndex);
            }
            else 
            {
                AddToDownQueue(floorIndex);
            }
        }
        
        // Move
        if (currentState == State.Idle)
        {
            currentDirection = (currentFloorIndex - floorIndex < 0) ? Direction.Up : Direction.Down;
            MoveToNextFloorInQueue();
        }
        // Request for floors on the way
        else if (currentState == State.Moving)
        {
            // If we just left the floor we do nothing 
            if (currentFloorIndex - floorIndex == 0) 
            {
                return;
            }
            // But if it's on the way we'll stop by
            if (currentFloorIndex - floorIndex > 0 && currentDirection == Direction.Down ||
                currentFloorIndex - floorIndex < 0 && currentDirection == Direction.Up)
            {
                MoveToNextFloorInQueue();
            }
        }
    }

    private void AddToDownQueue(int floorIndex) 
    {
        if (!downFloorQueue.Contains(floorIndex))
        {
            downFloorQueue.Add(floorIndex);
            downFloorQueue.Sort();
            downFloorQueue.Reverse();
        }
    }

    private void AddToUpQueue(int floorIndex)
    {
        if (!upFloorQueue.Contains(floorIndex)) 
        {
            upFloorQueue.Add(floorIndex);
            upFloorQueue.Sort();
        }
    }

    public void MoveToNextFloorInQueue() 
    {
        if (upFloorQueue.Count == 0 && downFloorQueue.Count == 0) 
        {
            Debug.LogWarning("Queues are empty, changing state to Idle");
            currentState = State.Idle;
            return;
        }

        if (currentDirection == Direction.Up) 
        {
            if (upFloorQueue.Count > 0) 
            {
                StartMovingState(upFloorQueue[0]);
            }
            else
            {
                if (downFloorQueue.Count > 0)
                {
                    currentDirection = Direction.Down;
                    StartMovingState(downFloorQueue[0]);
                }
            }
        }
        else if (currentDirection == Direction.Down) 
        {
            if (downFloorQueue.Count > 0)
            {
                StartMovingState(downFloorQueue[0]);
            }
            else
            {
                if (upFloorQueue.Count > 0)
                {
                    currentDirection = Direction.Up;
                    StartMovingState(upFloorQueue[0]);
                }
            }
        }
        else 
        {
            Debug.LogWarning("Current direction isn't valid for movement, changing state to Idle");
            currentState = State.Idle;
        }
    }

    private void StartMovingState(int floorIndex) 
    {
        float distance = Mathf.Abs(root.transform.position.y - (floorIndex * Config.DistanceBetweenFloors));
        duration = distance / Config.ElevatorSpeed;
        targetYPosition = GetFloorYCoordinate(floorIndex);

        // If we reroute the elevator while it's moving
        if (currentState == State.Moving) 
        {
            savedYPosition = root.transform.position.y;
            stateTimer = 0.0f;
        }

        currentState = State.Moving;
    }

    private void TryInvertDirectionOnEnds() 
    {
        if (IsAtTopFloor()) 
        {
            currentDirection = Direction.Down;
        }
        else if (IsAtGroundFloor())
        {
            currentDirection = Direction.Up;
        }
    }

    private bool IsAtTopFloor() 
    {
        return currentFloorIndex == Config.NumberOfFloor - 1;
    }

    private bool IsAtGroundFloor() 
    {
        return currentFloorIndex == 0;
    }

    private float GetFloorYCoordinate(int floorIndex)
    {
        return floorIndex * Config.DistanceBetweenFloors;
    }

    #endregion

    #region Boarding

    // Out

    private void RaiseIsBoardingOutEvent()
    {
        if (IsBoardingOut != null)
        {
            IsBoardingOut(passengersToBoardOut);
        }
    }

    private List<Passenger> GetPassengersToBoardOut() 
    {
        List<Passenger> ret = new List<Passenger>();
        for (int i = 0; i < passengersBoarded.Count; i++)
        {
            if (passengersBoarded[i].TargetFloorIndex == currentFloorIndex && passengersBoarded[i].CurrentState == Passenger.State.InElevator)
            {
                ret.Add(passengersBoarded[i]);
            } 
        }
        return ret;
    }

    public bool IsInBoardingOutList(Passenger passenger) 
    {
        return passengersToBoardOut.Contains(passenger);
    }

    private void PassengerIsOnExitHandler(Passenger sender)
    {
        passengersToBoardOut.Remove(sender);
        
        TryCompleteBoardingOutState();

        sender.IsOnExit -= PassengerIsOnExitHandler;
    }

    private void TryCompleteBoardingOutState() 
    {
        if (passengersToBoardOut.Count == 0)
        {
            if (passengersToBoardIn.Count > 0) 
            {
                currentState = State.BoardingIn;
            }
            else 
            {
                currentState = State.BoardingEnd;
            }
        }
    }

    // In

    private void RaiseIsBoardingInEvent()
    {
        if (IsBoardingIn != null)
        {
            IsBoardingIn(passengersToBoardIn);
        }
    }

    public bool IsInBoardingInList(Passenger passenger) 
    {
        return passengersToBoardIn.Contains(passenger);
    }

    private void PassengerIsInElevatorHandler(Passenger sender)
    {
        passengersToBoardIn.Remove(sender);
        
        TryCompleteBoardingInState();

        sender.IsInElevator -= PassengerIsInElevatorHandler;
    }

    private void TryCompleteBoardingInState() 
    {
        // If we don't have any room left in the elevator
        if (!IsAnyAnchorFree()) 
        {
            // Check if everyone is arrived in the elevator before leaving
            for (int i = 0; i < passengersBoarded.Count; i++)
            {
                if (passengersBoarded[i].CurrentState != Passenger.State.InElevator) 
                {
                    return;
                }
            }
            // Clear any passengers left in the list, they'll get the next one
            passengersToBoardIn.Clear();
            currentState = State.BoardingEnd;
        }
        else 
        {
            if (passengersToBoardIn.Count > 0)
            {
                return;
            }
        }
        
        currentState = State.BoardingEnd;
    }

    #endregion
}