using System;
using UnityEngine;

public class BarrelInteract : MonoBehaviour, IInteractable
{
    private GridPosition gridPosition;
    private Action onInteractComplete;

    private float timer;
    private bool isActive;
    private DestructibleCrate destructibleCrate;

    public void Interact(Action onInteractComplete)
    {
        isActive = true;
        this.onInteractComplete = onInteractComplete;
        timer = 0.5f;
    }

    private void Awake()
    {
        destructibleCrate = GetComponent<DestructibleCrate>();
    }

    private void Start()
    {   
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);
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
            destructibleCrate.Damage();
            onInteractComplete();
        }
    }

}
