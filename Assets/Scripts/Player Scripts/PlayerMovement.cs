using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial;
    [SerializeField] private PhysicsMaterial2D fullFrictionMaterial;
    [SerializeField] private Slider dashMeterSlider;
    [SerializeField] private Collider2D crouchDisableCollider;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private GameObject player;
    [SerializeField] private LayerMask whatIsGround;
    [Range(0, .3f)][SerializeField] private float movementSmoothing = .05f;
    [SerializeField] private float jumpForce = 750f;
    [SerializeField] private float dashingPower = 12f;
    [SerializeField] private float slideForce = 400f;
    [SerializeField] private float slopeCheckDistance;
    [SerializeField] private float crouchSpeed = 5f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private int numWallJumps = 3;
    [SerializeField] private bool airControl = false;
    [SerializeField] private bool combatIdle = true;

	private bool grounded;            // Whether or not the player is grounded.
    private bool underCeiling;
    private bool canSlide = true;
    private bool finishedSlideDelay = true;
    private bool facingRight = true;  // For determining which way the player is currently facing.
    private bool canDash = true;
    private bool isDashing;
    private bool groundBugStopper = false;
    private bool isOnSlope;
    private bool facingSlope;
    private bool onWall;
    private bool isWallJumping = false;
    private bool canWallJump = true;
    private bool endCondition = false;

    const float groundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    const float ceilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
    const float wallCheckRadius = .2f;
    private float dashDuration = 0.2f;
    private float dashCooldown = 0.1f;
    private float dashMeterAmount = 100;
    private float slopeDownAngle;
    private float oldSlopeDownAngle;
    private float slopeSideAngle;
    private float wallJumpForce = 10f;
    private float wallJumpTime = 0.1f;
    private float groundSpeedThreshold; 
    private float slopeSpeedThreshold;

    private int playerLayer = 6; // Layer index of the player layer
    private int bulletLayer = 8; // Layer index of the bullet layer
    //private int enemyLayer = 3;
    int m_CurrentHealth;

    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private CircleCollider2D circleCollider2D;
    private Animator animator;
    private Vector3 velocity = Vector3.zero;
    private Vector2 slopeNormalPerp;

    private List<bool> m_WallJumpHistory = new List<bool>();

    private void Awake()
	{
        groundSpeedThreshold = runSpeed - 1f;
        slopeSpeedThreshold = runSpeed - 3.5f;
		animator = GetComponent<Animator>();
		rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
		circleCollider2D = GetComponent<CircleCollider2D>();
    }


	private void FixedUpdate()
	{
		GroundCheck();
        if (IsOnGround())
        {
            m_WallJumpHistory.Clear();
        }
        CeilingCheck();
		WallCheck();
        SlopeCheck();
        DashRecover(Time.deltaTime / 2); 
        dashMeterSlider.value = dashMeterAmount;
    }

    public void Move(int move, bool jump, bool crouch)
	{
        if (grounded || airControl)
		{
            if (grounded && move == 0 && isOnSlope && !isDashing)
			{
				circleCollider2D.sharedMaterial = fullFrictionMaterial; 
			}
            else
            {
                circleCollider2D.sharedMaterial = noFrictionMaterial;
            }

            if (isOnSlope && crouch)
			{
                circleCollider2D.sharedMaterial = noFrictionMaterial;
            }
            

            if (!isOnSlope)
			{
                Vector3 targetVelocity;
                if (crouch|| underCeiling)
                {
                    Crouch();
                    animator.SetBool("Sliding", crouch);
                    targetVelocity = rigidBody.velocity;
                    if (canSlide && move != 0 && Mathf.Abs(rigidBody.velocity.x) >= groundSpeedThreshold * Time.timeScale && finishedSlideDelay && (Mathf.Sign(move) == Mathf.Sign(rigidBody.velocity.x)) && grounded)
                    {
                        finishedSlideDelay = false;
                        canSlide = false;
                        rigidBody.AddForce(new Vector2((Mathf.Sign(move) * slideForce / Time.timeScale), 0.0f));
                        StartCoroutine(SlideDelay()); 
                    }
                    else
                    {
                        if (((rigidBody.velocity.x > 0 && move > 0) && (rigidBody.velocity.x > move * crouchSpeed * Time.timeScale)) || ((rigidBody.velocity.x < 0 && move < 0) && (rigidBody.velocity.x < move * crouchSpeed * Time.timeScale)))
                            targetVelocity = rigidBody.velocity;
                        else
                            targetVelocity = new Vector2(move * crouchSpeed, rigidBody.velocity.y); //crouchSpeed here if you ever wanna make it so the player can crawl
                    }
                }
                else
                {
                    animator.SetBool("Sliding", crouch);
                    canSlide = true;
                    ResetRB();
                    if (((rigidBody.velocity.x > 0 && move > 0) && (rigidBody.velocity.x > move * runSpeed * Time.timeScale)) || ((rigidBody.velocity.x < 0 && move < 0) && (rigidBody.velocity.x < move * runSpeed * Time.timeScale)) || (move == 0 && !grounded)) //(move == 0.0f && !m_Grounded) fixes glitch where it stops x movement in air when adding force
                    {
                        targetVelocity = rigidBody.velocity;
                    }
                    else
                    {
                        targetVelocity = new Vector2(move * runSpeed, rigidBody.velocity.y);
                    }
                }

                rigidBody.velocity = Vector3.SmoothDamp(rigidBody.velocity, targetVelocity, ref velocity, movementSmoothing); // Makes the movement just a bit slower than what it would be by assigning the target velocity but makes it feel better
            }
			else if (isOnSlope)
			{
                Vector3 targetVelocity;
                if (crouch) //Add a crouch slide delay
                {
                    animator.SetBool("Sliding", crouch);
                    Crouch();
                    targetVelocity = rigidBody.velocity;
                    circleCollider2D.sharedMaterial = noFrictionMaterial;
                    //Mathf.Abs(m_Rigidbody2D.velocity.x) >= 5f fixes slope slide delay
                    if (canSlide && move != 0.0f && Mathf.Abs(rigidBody.velocity.x) >= slopeSpeedThreshold * Time.timeScale && finishedSlideDelay && (Mathf.Sign(move) == Mathf.Sign(rigidBody.velocity.x))) //Mathf.Abs(m_Rigidbody2D.velocity.x) causes glitch on stair where it stops it momentarily, fixed by last condition
                    {
                        finishedSlideDelay = false;
                        canSlide = false;
                        if (facingSlope)
                        {
                            rigidBody.AddForce(new Vector2((Mathf.Sign(move) * slideForce / Time.timeScale), slideForce));
                        }
                        else if (!facingSlope)
                        {
                            rigidBody.AddForce(new Vector2((Mathf.Sign(move) * slideForce / Time.timeScale), -slideForce));
                        }
                        StartCoroutine(SlideDelay());
                    }
                    /*else
                    {
                        if (m_Rigidbody2D.velocity.magnitude > 7.07f && Mathf.Sign(move) == Mathf.Sign(m_Rigidbody2D.velocity.x))
                            targetVelocity = m_Rigidbody2D.velocity;
                        else
                            targetVelocity = new Vector2(-move * 5f * slopeNormalPerp.x, -move * 5f * slopeNormalPerp.y);
                    }*/ //Works but doesnt have the sliding on stairs u like
                }
                else
                {
                    animator.SetBool("Sliding", crouch);
                    canSlide = true;
                    ResetRB();
                    if (rigidBody.velocity.sqrMagnitude > 200f * Time.timeScale && Mathf.Sign(move) == Mathf.Sign(rigidBody.velocity.x) || move == 0.0f) //|| move == 0.0f fixes bug where it stops velocity midair on slope
                    {
                        targetVelocity = rigidBody.velocity;
                    }
                    else
                    {
                        targetVelocity = new Vector2(-move * runSpeed * slopeNormalPerp.x, -move * runSpeed * slopeNormalPerp.y);
                    }
                }

                rigidBody.velocity = targetVelocity;
            }
			
            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !facingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (move < 0 && facingRight)
			{
				// ... flip the player.
				Flip();
			}
		}

		// If the player should jump...
		if (grounded && jump)
		{
            rigidBody.velocity -= Vector2.up * rigidBody.velocity.y; //Fixes the extra velocity glitch on stairs

            // Add a vertical force to the player.
            float modifiedJumpForce = jumpForce / Time.timeScale; 
			rigidBody.AddForce(new Vector2(0f, modifiedJumpForce));
		}

		else if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > Mathf.Epsilon)
			animator.SetInteger("AnimState", 2);

		//Combat Idle
		else if (combatIdle)
			animator.SetInteger("AnimState", 1);

		//Idle
		else
			animator.SetInteger("AnimState", 0);
	}

	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	public void Dash()
    {
		if (canDash && dashMeterAmount > 1f)
        {
            dashMeterAmount -= 1f; 
			StartCoroutine(DashRoutine());
        }
    }

	private IEnumerator DashRoutine()
    {
		canDash = false;
		isDashing = true;
        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, true);
        //Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
		Color oldColor = GetComponent<SpriteRenderer>().material.color;
        Color newColor = GetComponent<SpriteRenderer>().material.color;
        newColor.a = 0.5f;
        GetComponent<SpriteRenderer>().material.color = newColor;
        float originalGravity = rigidBody.gravityScale;
		rigidBody.gravityScale = 0f;
        if (isOnSlope && facingSlope)
        {
            circleCollider2D.sharedMaterial = noFrictionMaterial;
            rigidBody.velocity = new Vector2(dashingPower * transform.localScale.x, dashingPower);
        }
        else
        {
            rigidBody.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        }
		trail.emitting = true;
		yield return new WaitForSeconds(dashDuration);
        trail.emitting = false;
		rigidBody.gravityScale = originalGravity;
		isDashing = false;
        Physics2D.IgnoreLayerCollision(playerLayer, bulletLayer, false);
        //Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        GetComponent<SpriteRenderer>().material.color = oldColor;
        yield return new WaitForSeconds(dashCooldown);
		canDash = true;
    }

    private void DashRecover(float amount)
    {
        dashMeterAmount += amount;
        if (dashMeterAmount > 3)
            dashMeterAmount = 3;
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool CanDash()
    {
        return canDash;
    }

    public void WallJump()
    {
        if (canWallJump)
        {
            StartCoroutine(WallJumpRoutine());
        }
    }

    private IEnumerator WallJumpRoutine()
	{
        canWallJump = false; 
		isWallJumping = true; 
        if (facingRight)
        {
            Vector2 targetVelocity = new Vector2(-wallJumpForce, wallJumpForce * 1.5f);
            rigidBody.velocity = targetVelocity;
        }
        else if (!facingRight)
        {
            Vector2 targetVelocity = new Vector2(wallJumpForce, wallJumpForce * 1.5f);
            rigidBody.velocity = targetVelocity;
        }
        m_WallJumpHistory.Add(true); 
        yield return new WaitForSeconds(wallJumpTime);
        isWallJumping = false;
        canWallJump = true;
    }

    public bool ReachedWallJumpLimit()
    {
        bool hasReached; 
        if (m_WallJumpHistory.Count >= numWallJumps)
        {
            hasReached = true;
        }
        else
        {
            hasReached = false;
        }

        return hasReached;
    }

    public bool IsWallJumping()
    {
        return isWallJumping; 
    }

    public bool CanWallJump()
    {
        return canWallJump;
    }

    public void Crouch() //Work in progress, Box OG Offset: (-0.02, 0.81) Size: (0.6, 0.9), Circle OG Offset: (-0.02, 0.36) r: 0.3
    {
        boxCollider.offset = new Vector2(-0.02f, 0.51f);
        boxCollider.size = new Vector2(0.6f, 0.3f);
    }

    private void SlidingCrouch()
    {
        boxCollider.offset = new Vector2(-0.02f, 0.36f);
        boxCollider.size = new Vector2(0.9f, 0.6f);
        if (!facingRight)
        {
            circleCollider2D.offset = new Vector2(0.43f, 0.36f);
        }
        else if (facingRight)
        {
            circleCollider2D.offset = new Vector2(-0.48f, 0.36f);
        }
    }

    private IEnumerator SlideDelay()
    {
        yield return new WaitForSeconds(0.5f);
        finishedSlideDelay = true;
    }

    private void ResetRB()
    {
        boxCollider.offset = new Vector2(-0.02f, 0.81f);
        boxCollider.size = new Vector2(0.6f, 0.9f);
        circleCollider2D.offset = new Vector2(-0.02f, 0.36f);
        circleCollider2D.radius = 0.3f; 
    }

	private void SlopeCheck()
	{
        Vector2 checkPos = (Vector2)transform.position + circleCollider2D.offset - Vector2.up * circleCollider2D.radius;
        RaycastHit2D hitDown = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, whatIsGround);
        if (hitDown)
        {
            slopeNormalPerp = Vector2.Perpendicular(hitDown.normal).normalized;
            slopeDownAngle = Vector2.Angle(hitDown.normal, Vector2.up);

            if (slopeDownAngle != oldSlopeDownAngle)
            {
                isOnSlope = true;
            }

            /*
            if ((slopeDownAngleOld < 46 && slopeDownAngleOld > 44) && slopeDownAngle == 0 && m_Grounded) //m_grounded fixes issue where it resets velocity in air
            {
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, 0f);
            }*/

            oldSlopeDownAngle = slopeDownAngle;
        }

        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, whatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, whatIsGround);

        if (slopeHitFront)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
            if (facingRight)
                facingSlope = true;
            else
                facingSlope = false;
        }
        else if (slopeHitBack)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
            if (!facingRight)
                facingSlope = true;
            else
                facingSlope = false;
        }
        else
        {
            slopeSideAngle = 0.0f;
            isOnSlope = false;
            facingSlope = false;
        }

        if (slopeDownAngle == 0f || !grounded) //Important! without it, will stay forever isOnSlope = true even if you arent on a slope
        {
            isOnSlope = false;
        }
    }

	private void WallCheck()
	{
		onWall = false; 
        Collider2D[] colliders = Physics2D.OverlapCircleAll(wallCheck.position, wallCheckRadius, whatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                onWall = true;
            }
            if (colliders[i].gameObject.tag == "Barrels")
            {
                endCondition = true;
            }
        }
        if (GetComponent<PlayerCombat>().Dead() == false && grounded == false)
        {
            animator.SetBool("On Wall", onWall);
        }
        else
        {
            animator.SetBool("On Wall", false);
        }
    }

    public bool IsOnWall()
    {
        return onWall;
    }

	private void GroundCheck()
	{
        grounded = false;
        animator.SetBool("Grounded", grounded);

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundedRadius, whatIsGround);
        if (groundBugStopper == false)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject)
                {
                    grounded = true;
                    animator.SetBool("Grounded", grounded);
                    if (colliders[i].gameObject.tag == "Spike")
                    {
                        Debug.Log("Spiked");
                        GetComponent<PlayerCombat>().TakeDamage(100);
                    }
                    if (colliders[i].gameObject.tag == "Barrels")
                    {
                        endCondition = true;
                    }
                }
            }
        }
    }

    public bool IsOnGround()
    {
        return grounded;
    }

    private void CeilingCheck()
    {
        underCeiling = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(ceilingCheck.position, ceilingRadius, whatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                underCeiling = true;
            }
        }
    }

    public bool ReturnUnderCeiling()
    {
        return (underCeiling);
    }

    public bool ReturnEndCondition()
    {
        return endCondition;
    }
}
