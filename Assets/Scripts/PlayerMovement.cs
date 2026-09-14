using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    [Range(0.1f, 2.0f)]
    public float mouseSensitivity = 0.6f;
    public Transform playerCamera; // Drag your FirstPersonPosition here

    public GameObject toDeactivate;

    private CharacterController controller;
    [SerializeField]private float cameraPitch = 0f;
    private Vector3 velocity;

    [Header("Stealth Mechanics")]
    public GameObject ripplePrefab;
    public float sprintRippleTimer = 0f;

    [Header("Jump & Landing Settings")]
    public float jumpHeight = 2.2f;
    public float landingNoiseRadius = 14f;
    private bool wasGrounded = true;
    private float airTime = 0f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    void Awake()
    {
        // Disable redundant CapsuleCollider if present on the same GameObject to prevent PhysX self-collision
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Lock the mouse cursor only if game is not starting paused
        if (!MenuManager.isGamePaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        StartCoroutine(DeactivateCamera());
    }

    IEnumerator DeactivateCamera()
    {
        yield return new WaitForSecondsRealtime(1f);

        if (toDeactivate != null)
        {
            toDeactivate.SetActive(false);
        }
    }

    void Update()
    {
        if (MenuManager.isGamePaused) return;

        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // Get mouse input (Legacy Input System)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the camera up and down (Pitch)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 38f); // Prevent snapping neck backwards

        if (playerCamera != null)
        {
            playerCamera.localEulerAngles = Vector3.right * cameraPitch;
        }

        // Rotate the player body left and right (Yaw)
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        if (controller == null) return;

        // 1. Ground detection & Coyote time buffer
        bool isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            coyoteTimer = 0.15f; // Grace period for jumping

            if (velocity.y < 0f)
            {
                velocity.y = -2f; // Slight downward force to keep grounded flush to geometry
            }

            // Detect landing from airborne
            if (!wasGrounded && airTime > 0.18f)
            {
                OnPlayerLanded();
            }
            airTime = 0f;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            airTime += Time.deltaTime;
        }

        // 2. Buffer Jump Input
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = 0.15f;
        }
        else if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // 3. Jump Trigger
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // 4. Apply Gravity to vertical velocity
        velocity.y += gravity * Time.deltaTime;

        // 5. Gather horizontal movement input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // 6. Unified Single CharacterController Move (prevents collision jitter & skipped grounded checks)
        Vector3 displacement = (move * currentSpeed + Vector3.up * velocity.y) * Time.deltaTime;
        controller.Move(displacement);

        wasGrounded = controller.isGrounded;

        // 7. Spawn Sound Ripples when sprinting
        if (Input.GetKey(KeyCode.LeftShift) && (x != 0 || z != 0) && controller.isGrounded)
        {
            sprintRippleTimer -= Time.deltaTime;
            if (sprintRippleTimer <= 0f)
            {
                AlertEnemiesNear(transform.position, 10f);
                SpawnSoundRipple(10f);
                sprintRippleTimer = 0.4f; // Wait 0.4 seconds before next sprint ripple
            }
        }
        else 
        {
            sprintRippleTimer = 0f; // Reset timer when walking/stopped
        }
    }

    private void OnPlayerLanded()
    {
        // Impact thud creates an acoustic ripple and alerts nearby hostiles
        AlertEnemiesNear(transform.position, landingNoiseRadius);
        SpawnSoundRipple(landingNoiseRadius);
    }

    private void SpawnSoundRipple(float radius)
    {
        if (ripplePrefab == null) return;

        Vector3 spawnPos = transform.position;

        // Raycast down to snap ripple flush with ground surface
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPos = groundHit.point + Vector3.up * 0.01f;
        }

        GameObject ripple = Instantiate(ripplePrefab, spawnPos, Quaternion.identity);
        SoundRipple sr = ripple.GetComponent<SoundRipple>();
        if (sr != null)
        {
            sr.maxRadius = radius;
        }
    }

    private void AlertEnemiesNear(Vector3 pos, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore);
        System.Collections.Generic.HashSet<EnemyAI> alerted = new System.Collections.Generic.HashSet<EnemyAI>();

        foreach (var col in hitColliders)
        {
            EnemyAI enemy = col.GetComponentInParent<EnemyAI>();
            if (enemy != null && !alerted.Contains(enemy))
            {
                alerted.Add(enemy);
                enemy.InvestigateSound(pos);
            }
        }
    }
}