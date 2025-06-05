using System;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    private bool isOpen;
    private GridPosition gridPosition;
    private Animator doorAnimator;
    private Action onInteractComplete;

    private float timer;
    private bool isActive;

    private void Awake()
    {
        doorAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);

        if (isOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        timer -= Time.deltaTime;

        if (timer < 0)
        {
            isActive = false;
            onInteractComplete();
        }
    }

    public void Interact(Action onInteractComplete)
    {
        this.onInteractComplete = onInteractComplete;
        isActive = true;
        timer = .5f;

        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }

        doorAnimator.SetBool("IsOpen", isOpen);

    }

    private void OpenDoor()
    {
        isOpen = true;
        Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, isOpen);
    }

    private void CloseDoor()
    {
        isOpen = false;
        Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, isOpen);
    }

}
