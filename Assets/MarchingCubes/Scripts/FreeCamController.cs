using UnityEngine;

public class FreeCamController : MonoBehaviour
{
    public float movementSpeed = 50.0f; // Base movement speed
    public float speedIncrement = 25.0f; // Amount to increase/decrease speed with scroll wheel
    public float minSpeed = 2.0f; // Minimum speed limit
    public float maxSpeed = 250.0f; // Maximum speed limit
    public float lookSpeed = 2.0f; // Sensitivity of the mouse look
    public float sprintMultiplier = 2.0f; // Speed multiplier for sprinting
    public bool invertY = false; // Option to invert the Y axis

    private float yaw = 0.0f; // Horizontal rotation (around the Y axis)
    private float pitch = 0.0f; // Vertical rotation (around the X axis)

    void Start()
    {
        // Lock the cursor and hide it during the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Camera Rotation (Mouse)
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        yaw += mouseX;
        pitch -= invertY ? -mouseY : mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Limit vertical rotation

        transform.localEulerAngles = new Vector3(pitch, yaw, 0.0f);

        // Adjust movement speed using the scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            movementSpeed += scroll * speedIncrement;
            movementSpeed = Mathf.Clamp(movementSpeed, minSpeed, maxSpeed); // Clamp to min/max speed
        }

        // Camera Movement (WASD)
        float moveSpeed = movementSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed *= sprintMultiplier; // Apply sprint multiplier when holding Shift
        }

        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime; // A/D
        float moveZ = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime; // W/S
        float moveY = 0.0f;

        if (Input.GetKey(KeyCode.E)) // Upward movement (E)
        {
            moveY = moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.Q)) // Downward movement (Q)
        {
            moveY = -moveSpeed * Time.deltaTime;
        }

        transform.Translate(new Vector3(moveX, moveY, moveZ)); // Apply movement
    }
}
