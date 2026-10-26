
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform viewCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("PC Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private float pitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (viewCamera == null)
        {
            Debug.LogError(
                "PlayerMovement: View Camera が設定されていません。");
            enabled = false;
            return;
        }

        if (!XRSettings.isDeviceActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        bool isVR = XRSettings.isDeviceActive;

        Vector2 moveInput = GetMoveInput(isVR);

        // カメラの向きを水平面に投影する
        Vector3 forward = viewCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = viewCamera.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection =
            forward * moveInput.y +
            right * moveInput.x;

        // 斜め移動が速くなりすぎないようにする
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        characterController.Move(
            moveDirection * moveSpeed * Time.deltaTime);

        // 重力
        if (characterController.isGrounded &&
            verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        characterController.Move(
            verticalVelocity * Time.deltaTime);

        // PCではマウス、VRではヘッドトラッキングを使用
        if (!isVR)
        {
            HandleMouseLook();
        }
    }

    private Vector2 GetMoveInput(bool isVR)
    {
        if (isVR)
        {
            // Meta Quest 左コントローラーのスティック
            return OVRInput.Get(
                OVRInput.Axis2D.PrimaryThumbstick);
        }

        // PC：WASD
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        // Escでカーソルを解放、クリックで再ロック
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // 左右：Player全体を旋回
        transform.Rotate(
            0f, mouseX, 0f, Space.World);

        // 上下：カメラの視点を回転
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        viewCamera.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }
}
