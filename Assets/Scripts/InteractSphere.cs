using System;
using UnityEngine;

public class InteractSphere : MonoBehaviour, IInteractable
{
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;
    [SerializeField] private MeshRenderer sphereMeshRenderer;

    private GridPosition gridPosition;
    private Action onInteractComplete;
    private bool isGreen;
    private float timer;
    private bool isActive;

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);

        SetColorGreen();
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        timer -= Time.deltaTime;

        if (timer < 0f)
        {
            isActive = false;
            onInteractComplete();
        }

    }

    private void SetColorGreen()
    {
        isGreen = true;
        sphereMeshRenderer.material = greenMaterial;
    }

    private void SetColorRed()
    {
        isGreen = false;
        sphereMeshRenderer.material = redMaterial;
    }

    public void Interact(Action onInteractComplete)
    {
        isActive = true;
        timer = .5f;
        this.onInteractComplete = onInteractComplete;

        if (isGreen)
        {
            SetColorRed();
        }
        else
        {
            SetColorGreen();
        }
    }
}
