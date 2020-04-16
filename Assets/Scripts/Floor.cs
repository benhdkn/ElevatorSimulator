using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    #region Fields and properties

    [SerializeField] private bool isEnabled = true;

    [SerializeField] private int index = 0;

    [SerializeField] private Passenger passengerPrefab = null;

    [SerializeField] private Transform spawnAnchor = null;
    [SerializeField] private Transform exitAnchor = null;
    [SerializeField] private Transform[] waitingAnchors = null;

    private bool[] waitingAnchorsAvailabilities;

    private List<Passenger> waitingPassengers = null;

    private static List<Passenger> disabledPassengers = null;

    private float spawnTimer = 0.0f;
    private float spawnDuration = 0.0f;

    private static int numberOfPassengerSpawned = 0; // Across floors

    public int Index { get { return index; } }

    public Transform SpawnAnchor { get { return spawnAnchor; } }
    public Transform ExitAnchor { get { return exitAnchor; } }

    #endregion

    #region Unity execution

    private void Awake() 
    {
        waitingAnchorsAvailabilities = new bool[waitingAnchors.Length];
        for (int i = 0; i < waitingAnchorsAvailabilities.Length; i++) 
        {
            waitingAnchorsAvailabilities[i] = true;
        }

        waitingPassengers = new List<Passenger>();
        disabledPassengers = new List<Passenger>();

        RandomizeSpawnDuration();
    }

    private void Update() 
    {
        if (!isEnabled) {
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnDuration) 
        {
            TrySpawnPassenger();
            RandomizeSpawnDuration();
            spawnTimer = 0.0f; 
        }
    }

    #endregion

    public void TrySpawnPassenger() 
    {        
        int freeAnchorIndex = -1;
        if (IsAnyAnchorFree(out freeAnchorIndex) == true) 
        {
            Passenger spawnedPassenger;
            if (disabledPassengers.Count > 0)
            {
                spawnedPassenger = disabledPassengers[0];
                disabledPassengers.Remove(spawnedPassenger);
            }
            else 
            {
                spawnedPassenger = GameObject.Instantiate(passengerPrefab);
                numberOfPassengerSpawned++;
                spawnedPassenger.name += numberOfPassengerSpawned;
            }
            spawnedPassenger.Initialize(this, waitingAnchors[freeAnchorIndex]);
            CheckIn(freeAnchorIndex, spawnedPassenger);
        }
    }

    private bool IsAnyAnchorFree(out int anchorIndex)
    {
        bool ret = false;
        anchorIndex = -1;
        for (int i = 0; i < waitingAnchorsAvailabilities.Length; i++)
        {
            if (waitingAnchorsAvailabilities[i] == true) 
            {
                anchorIndex = i;
                ret = true;
                break;
            }
        }
        return ret;
    }

    private void RandomizeSpawnDuration() 
    {
        spawnDuration = Random.Range(Config.PassengerSpawnMinDuration, Config.PassengerSpawnMaxDuration);
    }

    public List<Passenger> GetPassengersToBoardIn()
    {
        List<Passenger> ret = new List<Passenger>();
        for (int i = 0; i < waitingPassengers.Count; i++)
        {
            if (waitingPassengers[i].CurrentState == Passenger.State.Waiting && 
                (waitingPassengers[i].CurrentDirection == Elevator.Instance.CurrentDirection || Elevator.Instance.CurrentDirection == Direction.None))
            {
                ret.Add(waitingPassengers[i]);
            }
        }
        return ret;
    }

    public void CheckIn(int waitingAnchorIndex, Passenger passenger)
    {
        waitingAnchorsAvailabilities[waitingAnchorIndex] = false;
        waitingPassengers.Add(passenger);
    }

    public void CheckOut(Transform waitingAnchor, Passenger passenger)
    {
        int waitingAnchorIndex = System.Array.IndexOf(waitingAnchors, waitingAnchor);
        waitingAnchorsAvailabilities[waitingAnchorIndex] = true;
        waitingPassengers.Remove(passenger);
    }

    public void AddToPool(Passenger passenger)
    {
        disabledPassengers.Add(passenger);
    }
}