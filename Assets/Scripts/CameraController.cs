using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera cinemachineVirtualCamera;
    private const float MIN_FOLLOW_Y_OFFSET = 2f;
    private const float MAX_FOLLOW_Y_OFFSET = 20f;

    private CinemachineTransposer cinemachineTransposer;
    private Vector3 targetFollowOffset;
    private Vector3 targetInputMoveDir;
    private Vector3 targetRotationVector;

    private void Start()
    {
        cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        targetFollowOffset = cinemachineTransposer.m_FollowOffset;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        Vector3 inputMoveDir = new Vector3(InputManager.Instance.GetCameraMoveVector().x, 0, InputManager.Instance.GetCameraMoveVector().y);

        float moveSpeed = 10f;
        if (targetInputMoveDir != Vector3.zero || inputMoveDir != Vector3.zero)
        {
            targetInputMoveDir = Vector3.Lerp(targetInputMoveDir, inputMoveDir, Time.deltaTime * moveSpeed);
            Vector3 moveVector = transform.forward * targetInputMoveDir.z + transform.right * targetInputMoveDir.x;
            transform.position += moveSpeed * Time.deltaTime * moveVector;
        }
    }

    private void HandleRotation()
    {
        Vector3 rotationVector = new Vector3(0, InputManager.Instance.GetCameraRotationAmount(), 0);

        float rotationspeed = 100f;
        if (targetRotationVector != Vector3.zero || rotationVector != Vector3.zero)
        {
            targetRotationVector = Vector3.Lerp(targetRotationVector, rotationVector, Time.deltaTime * 3f);
            transform.eulerAngles += rotationspeed * Time.deltaTime * targetRotationVector;
        }
    }

    private void HandleZoom()
    {
        //if (EventSystem.current.IsPointerOverGameObject())
        //{
        //    return;
        //}

        float zoomIncreaseAmount = 1f;
        targetFollowOffset.y += InputManager.Instance.GetCameraZoomAmout() * zoomIncreaseAmount;

        targetFollowOffset.y = Mathf.Clamp(targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);
        float zoomSpeed = 5f;
        cinemachineTransposer.m_FollowOffset = Vector3.Lerp(cinemachineTransposer.m_FollowOffset, targetFollowOffset, Time.deltaTime * zoomSpeed);
    }
}
