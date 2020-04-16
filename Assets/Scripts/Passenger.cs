using System.Collections.Generic;
using UnityEngine;

public class Passenger : MonoBehaviour
{
    #region Enums

    public enum State 
    {
        None,
        MovingToFloor,
        Waiting,
        MovingToElevator,
        InElevator,
        MovingToExit,
        Disabled,
    }

    #endregion

    #region Fields and properties

    [SerializeField] private Transform root;

    private State currentState = State.None;
    private State previousState = State.None;

    private float stateTimer = 0.0f;
    private float duration = 0.0f;

    private Vector3 savedPosition = Vector3.zero;
    private Vector3 targetPosition = Vector3.zero;

    private Transform floorAnchor = null;
    private Transform elevatorAnchor = null;
    
    private int currentFloorIndex = 0;
    private int targetFloorIndex = 0;

    public State CurrentState { get { return currentState; } }

    public int TargetFloorIndex { get { return targetFloorIndex; } }

    public  Direction CurrentDirection { get { return (currentFloorIndex - targetFloorIndex < 0) ? Direction.Up : Direction.Down; } }

    public delegate void StateHandler(Passenger sender);
    public event StateHandler IsInElevator;
    public event StateHandler IsOnExit;

    #endregion

    #region Unity execution

    private void OnEnable() 
    {
        Elevator.Instance.IsBoardingIn += ElevatorIsBoardingInHandler;
        Elevator.Instance.IsBoardingOut += ElevatorIsBoardingOutHandler;
    }

    private void OnDisable()
    {
        Elevator.Instance.IsBoardingIn -= ElevatorIsBoardingInHandler;
        Elevator.Instance.IsBoardingOut -= ElevatorIsBoardingOutHandler;
    }

    private void Update() 
    {
        // State change
        if (previousState != currentState) 
        {
            stateTimer = 0.0f;
            savedPosition = root.transform.position;
            
            if (currentState == State.Waiting)
            {
                Direction direction = (targetFloorIndex - currentFloorIndex > 0) ? Direction.Up : Direction.Down;
                Elevator.Instance.Request(currentFloorIndex);
            }
            else if (currentState == State.InElevator)
            {
                Elevator.Instance.Floors[currentFloorIndex].CheckOut(floorAnchor, this);
                floorAnchor = null;
            }
            else if (currentState == State.Disabled) 
            {
                root.gameObject.SetActive(false);
                Elevator.Instance.Floors[currentFloorIndex].AddToPool(this);
            }

            if (previousState == State.MovingToElevator)
            {
                Elevator.Instance.Request(targetFloorIndex);
                RaiseIsInElevatorEvent();
            }
            else if (previousState == State.MovingToExit)
            {
                RaiseIsOnExitEvent();
            }

            previousState = currentState;
        }

        // State update
        if (currentState != State.None) 
        {
            stateTimer += Time.deltaTime;

            if (currentState == State.MovingToFloor || 
                currentState == State.MovingToElevator || 
                currentState == State.MovingToExit) 
            {
                if (stateTimer < duration) 
                {
                    Vector3 lerpPosition = Vector3.Lerp(savedPosition, targetPosition, stateTimer / duration);
                    root.transform.position = lerpPosition;
                }
                else
                {
                    root.transform.position = targetPosition;
                    if (currentState == State.MovingToElevator) 
                    {
                        currentState = State.InElevator;
                    }
                    else if (currentState == State.MovingToFloor)
                    {
                        currentState = State.Waiting;
                    }
                    else if (currentState == State.MovingToExit)
                    {
                        currentState = State.Disabled;
                    }
                }
            }
        }
    }

    #endregion

    public void Initialize(Floor floor, Transform floorAnchor) 
    {
        root.gameObject.SetActive(true);
        SetRandomColor();

        currentFloorIndex = floor.Index;
        this.floorAnchor = floorAnchor;

        SetTargetFloorIndex();

        root.transform.position = floor.SpawnAnchor.position;

        MoveToFloorAnchor(floorAnchor);
    }

    private void SetRandomColor()
    {
        Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
        Color randomColor = ColorHelper.GetRandomPassengerColor();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.SetColor("_Color", randomColor);
        }
    }

    private void SetTargetFloorIndex()
    {
        List<int> validFloorIndexes = new List<int>();
        for (int i = 0; i < Config.NumberOfFloor; i++) 
        {
            if (i != currentFloorIndex) 
            {
                validFloorIndexes.Add(i);
            }
        }
        targetFloorIndex = validFloorIndexes[Random.Range(0, validFloorIndexes.Count)];
    }

    private void MoveToFloorAnchor(Transform floorAnchor) 
    {
        this.floorAnchor = floorAnchor;

        duration = Vector3.Distance(floorAnchor.position, root.transform.position) / Config.PassengerSpeed;
        targetPosition = floorAnchor.position;

        currentState = State.MovingToFloor;
    }

    private void ElevatorIsBoardingOutHandler(List<Passenger> passengersToBoardOut)
    {
        if (passengersToBoardOut.Contains(this))
        {
            Elevator.Instance.CheckOut(elevatorAnchor, this);
            elevatorAnchor = null;
            MoveToExitAnchor();
        }
    }

    private void ElevatorIsBoardingInHandler(List<Passenger> passengersToBoardIn)
    {
        if (passengersToBoardIn.Contains(this))
        {
            if (CurrentDirection == Elevator.Instance.CurrentDirection || Elevator.Instance.CurrentDirection == Direction.None)
            {
                if (Elevator.Instance.IsAnyAnchorFree(out elevatorAnchor) == true)
                {
                    Elevator.Instance.CheckIn(elevatorAnchor, this);
                    MoveToElevatorAnchor();
                }
            }
            else
            {
                Elevator.Instance.Request(currentFloorIndex);
            }
        }
    }

    private void MoveToElevatorAnchor()
    {
        root.SetParent(elevatorAnchor);

        duration = Vector3.Distance(elevatorAnchor.position, root.transform.position) / Config.PassengerSpeed;
        targetPosition = elevatorAnchor.position;
        
        currentState = State.MovingToElevator;
    }

    private void RaiseIsInElevatorEvent()
    {
        if (IsInElevator != null)
        {
            IsInElevator(this);
        }
    }

    private void MoveToExitAnchor() 
    {
        currentFloorIndex = Elevator.Instance.CurrentFloorIndex;

        Transform exitAnchor = Elevator.Instance.Floors[currentFloorIndex].ExitAnchor;
        duration = Vector3.Distance(exitAnchor.position, root.transform.position) / Config.PassengerSpeed;
        targetPosition = exitAnchor.position;

        root.SetParent(this.transform);
        
        currentState = State.MovingToExit;
    }

    private void RaiseIsOnExitEvent()
    {
        if (IsOnExit != null)
        {
            IsOnExit(this);
        }
    }
}