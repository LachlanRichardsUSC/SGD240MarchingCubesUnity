using UnityEngine;

/// <summary>
/// Controls a free-moving camera in a 3D environment, allowing for customizable speed, movement, and rotation.
/// </summary>
/// <remarks>
/// This class handles camera movement with adjustable speed, sprinting, and mouse-based rotation with an optional inverted Y-axis.
/// It also manages cursor visibility and locking based on rotation state.
/// </remarks>
public class FreeCamController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("Base movement speed of the camera.")]
    private float movementSpeed = 50.0f;

    [SerializeField, Tooltip("Increment value for adjusting movement speed with the mouse scroll wheel.")]
    private float speedIncrement = 25.0f;

    [SerializeField, Tooltip("Minimum allowable movement speed.")]
    private float minSpeed = 2.0f;

    [SerializeField, Tooltip("Maximum allowable movement speed.")]
    private float maxSpeed = 250.0f;

    [SerializeField, Tooltip("Multiplier applied to movement speed when sprinting.")]
    private float sprintMultiplier = 2.0f;

    [Header("Rotation Settings")]
    [SerializeField, Tooltip("Speed at which the camera rotates based on mouse input.")]
    private float lookSpeed = 2.0f;

    [SerializeField, Tooltip("Invert the Y-axis for camera rotation.")]
    private bool invertY = false;

    private float _yaw = 0.0f;
    private float _pitch = 0.0f;
    private bool _isRotating = false;

    /// <summary>
    /// Initializes the camera by setting the cursor state.
    /// </summary>
    void Start()
    {
        // Start with the cursor visible and unlocked
        SetCursorState(false);
    }

    /// <summary>
    /// Updates camera movement, rotation, and speed adjustment each frame.
    /// </summary>
    void Update()
    {
        HandleRotationInput();
        HandleMovementInput();
        HandleSpeedAdjustment();
    }

    /// <summary>
    /// Handles camera rotation based on mouse input.
    /// </summary>
    private void HandleRotationInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _isRotating = true;
            SetCursorState(true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            _isRotating = false;
            SetCursorState(false);
        }

        if (_isRotating)
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            _yaw += mouseX;
            _pitch -= invertY ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, -90f, 90f);

            transform.localEulerAngles = new Vector3(_pitch, _yaw, 0.0f);
        }
    }

    /// <summary>
    /// Handles camera movement based on keyboard input and adjustable speed.
    /// </summary>
    private void HandleMovementInput()
    {
        float moveSpeed = movementSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1.0f);
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float moveY = 0.0f;

        if (Input.GetKey(KeyCode.E))
        {
            moveY = moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            moveY = -moveSpeed * Time.deltaTime;
        }

        transform.Translate(new Vector3(moveX, moveY, moveZ));
    }

    /// <summary>
    /// Adjusts the camera's movement speed based on mouse scroll wheel input.
    /// </summary>
    private void HandleSpeedAdjustment()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            movementSpeed += scroll * speedIncrement;
            movementSpeed = Mathf.Clamp(movementSpeed, minSpeed, maxSpeed);
        }
    }

    /// <summary>
    /// Sets the cursor's visibility and locking state.
    /// </summary>
    /// <param name="locked">Indicates whether the cursor should be locked and hidden.</param>
    private void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// Ensures that the cursor is visible when the script is disabled.
    /// </summary>
    private void OnDisable()
    {
        SetCursorState(false);
    }
}
