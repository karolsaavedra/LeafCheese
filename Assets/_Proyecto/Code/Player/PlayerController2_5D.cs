using UnityEngine;
using UnityEngine.InputSystem;
//Holiwis

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController2_5D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float airControl = 0.6f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 8.5f;
    [SerializeField] private float coyoteTime = 0.10f;
    [SerializeField] private float jumpBuffer = 0.10f;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.35f;
    [SerializeField] private float wallJumpUpForce = 8.5f;
    [SerializeField] private float wallJumpSideForce = 7.5f;
    [SerializeField] private float wallGraceTime = 0.04f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Detección Suelo")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Plano 2.5D")]
    [SerializeField] private bool lockZ = true;
    private float fixedZ;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private bool jumpPressed;

    private bool isGrounded;
    private float lastTimeGrounded;
    private float lastTimeJumpPressed;

    private bool touchingWallLeft;
    private bool touchingWallRight;
    private float lastTimeOnWall;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0f, -capsule.height * 0.5f + 0.05f, 0f);
            groundCheck = gc.transform;
        }

        if (lockZ) fixedZ = transform.position.z;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ |
                         (lockZ ? RigidbodyConstraints.FreezePositionZ : 0);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) lastTimeGrounded = Time.time;

        touchingWallLeft = false;
        touchingWallRight = false;

        if (!isGrounded)
        {
            Vector3 origin = transform.position + Vector3.up * (capsule.height * 0.4f);
            touchingWallLeft  = Physics.Raycast(origin, Vector3.left, wallCheckDistance, wallLayer);
            touchingWallRight = Physics.Raycast(origin, Vector3.right, wallCheckDistance, wallLayer);

            if (touchingWallLeft || touchingWallRight)
                lastTimeOnWall = Time.time;
        }

        if (jumpPressed)
        {
            lastTimeJumpPressed = Time.time;
            jumpPressed = false;
        }

        TryPerformJump();
    }

    void FixedUpdate()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float currentSpeed = rb.linearVelocity.x;
        float accel = isGrounded ? acceleration : acceleration * airControl;
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);

        Vector3 vel = rb.linearVelocity;
        vel.x = newSpeed;
        rb.linearVelocity = vel;

        if (lockZ && Mathf.Abs(rb.position.z - fixedZ) > 0.0001f)
        {
            rb.position = new Vector3(rb.position.x, rb.position.y, fixedZ);
            Vector3 v = rb.linearVelocity; v.z = 0f; rb.linearVelocity = v;
        }
    }

    private void TryPerformJump()
    {
        bool canWallJump = !isGrounded && (Time.time - lastTimeOnWall) <= wallGraceTime;
        bool jumpBuffered = (Time.time - lastTimeJumpPressed) <= jumpBuffer;
        bool canCoyote = (Time.time - lastTimeGrounded) <= coyoteTime;

        if (canWallJump && jumpBuffered)
        {
            int dir = touchingWallLeft ? 1 : (touchingWallRight ? -1 : 0);
            Vector3 v = rb.linearVelocity; v.y = 0f; rb.linearVelocity = v;
            Vector3 impulse = new Vector3(dir * wallJumpSideForce, wallJumpUpForce, 0f);
            rb.AddForce(impulse, ForceMode.VelocityChange);
            lastTimeJumpPressed = -999f;
            return;
        }

        if (jumpBuffered && canCoyote)
        {
            Vector3 v = rb.linearVelocity; v.y = 0f; rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            lastTimeJumpPressed = -999f;
        }
    }

private void OnMove(InputValue value)
{
    // Como Move es un eje 1D (float), leemos un float y lo convertimos a Vector2
    float inputX = value.Get<float>();
    moveInput = new Vector2(inputX, 0f);
}


    private void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpPressed = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Gizmos.DrawLine(origin, origin + Vector3.left * wallCheckDistance);
        Gizmos.DrawLine(origin, origin + Vector3.right * wallCheckDistance);
    }
}
