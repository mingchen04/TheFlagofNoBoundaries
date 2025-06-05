using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform cameraTransform;

    private enum Mode
    {
        LookAt,
        LookAtInvered,
        CameraForward,
        CameraForwardInveted
    }
    [SerializeField] private Mode mode;

    private void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        switch (mode)
        {
            case Mode.LookAt:
                transform.LookAt(cameraTransform);
                break;
            case Mode.LookAtInvered:
                //·´×ª
                Vector3 dirFromCamera = transform.position - cameraTransform.position;
                transform.LookAt(transform.position + dirFromCamera);
                break;
            case Mode.CameraForward:
                transform.forward = cameraTransform.forward;
                break;
            case Mode.CameraForwardInveted:
                transform.forward = -cameraTransform.forward;
                break;
        }
    }
}
