using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] private LayerMask EnemyAndLevel;
    [SerializeField] private GameObject enemy;
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Transform rightSide;
    [SerializeField] private Transform leftSide;
    [SerializeField] private Transform LedgeCheck;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;
    [SerializeField] private float slopeCheckDistance;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool dead = false;


    private Transform playerTransform;
    private Animator m_animator;
    private BoxCollider2D m_boxCollider2D;
    private CircleCollider2D m_circleCollider2D;
    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer spriteRenderer;
    private LayerMask rayLayers;
    private Vector3 initialPosition;
    private Vector2 slopeNormalPerp;
    private RaycastHit2D wallCheck;
    private Color originalColor;
    const float k_GroundedRadius = .2f;
    private float stoppingDistance = 5f;
    private float trackingDistance = 8f;
    private float moveSpeed = 5f;
    private float returnTimer = 0f;
    private float pauseTimer = 0f;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    private float slopeSideAngle;
    private float oldSide;
    private float newSide;
    private int currentHealth;
    private int currentSide = 0;
    private int builtUpDamage;
    private bool m_FacingRight = false;
    private bool m_Grounded;
    private bool m_combatIdle = true;
    private bool isAttacking = false;
    private bool isAtInitial = true;
    private bool canMove = true;
    private bool isOnSlope;
    private bool cantDetect = false;
    private bool isNearLedge;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();
        m_boxCollider2D = GetComponent<BoxCollider2D>();
        currentHealth = maxHealth;
        initialPosition = transform.position;
        rayLayers = LayerMask.GetMask("Player", "Level");
        m_circleCollider2D = GetComponent<CircleCollider2D>();
    }

    private void FixedUpdate()
    {
        m_Grounded = false;
        m_animator.SetBool("Grounded", m_Grounded);

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                m_animator.SetBool("Grounded", m_Grounded);
            }
        }
        SlopeCheck();
        if (m_Grounded && m_Rigidbody2D.velocity.x == 0.0f && isOnSlope)
        {
            m_circleCollider2D.sharedMaterial = fullFriction;
        }
        else
        {
            m_circleCollider2D.sharedMaterial = noFriction;
        }
        LedgeDetect();

        if (!GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            if (builtUpDamage != 0)
            {
                TakeDamage(builtUpDamage);
                builtUpDamage = 0;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            builtUpDamage += damage;
        }
        else
        {
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                enemy.layer = LayerMask.NameToLayer("Body");
                m_animator.SetTrigger("Death");
                m_boxCollider2D.offset = new Vector2(0, 0.32f);
                m_boxCollider2D.size = new Vector2(1.2f, 0.5f);
                dead = true;
                m_Rigidbody2D.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                m_Rigidbody2D.velocity = Vector2.zero;
            }
            else
            {
                //m_animator.SetTrigger("Hurt");
                StartCoroutine(DamageColor());
            }
        }
    }

    private IEnumerator DamageColor()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        spriteRenderer.color = originalColor;
    }

    public void followPlayer()
    {
        Vector3 rayStart = transform.position;
        rayStart.y += 0.5f;
        Vector2 direction = playerTransform.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, direction, Mathf.Infinity, rayLayers);
        Debug.DrawLine(rayStart, hit.point, Color.red);
        direction.y = 0f; // Ignore the y-axis movement
        StartCoroutine(dashIgnore(direction));

        if (!dead && m_Grounded && !GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            m_animator.speed = 1f;
            if (direction.magnitude >= trackingDistance || !hit.collider.CompareTag("Player")) //Make sure this works even if the hit collider cant return anything
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                if (!isAtInitial)
                    returnTimer += Time.fixedDeltaTime;
                if (returnTimer >= 1f && !isAtInitial)
                {
                    ReturnToInitial();
                }
                else if (isAtInitial)
                {
                    Patrol();
                }
            }
            else if (isNearLedge && ((direction.x > 0 && m_FacingRight) || (direction.x < 0 && !m_FacingRight)))
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                if (direction.magnitude <= stoppingDistance && hit.collider.CompareTag("Player") && !cantDetect)
                {
                    m_Rigidbody2D.velocity = Vector2.zero;
                    PlayAttack();
                }
            }
            else if (isNearLedge && !cantDetect && ((direction.x > 0 && !m_FacingRight) || (direction.x < 0 && m_FacingRight)))
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                Flip();
            }
            else if (playerTransform != null && m_Grounded && direction.magnitude <= trackingDistance && hit.collider.CompareTag("Player") && !cantDetect)
            {
                returnTimer = 0f;
                isAtInitial = false;
                if (direction.x > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                // Otherwise if the input is moving the player left and the player is facing right...
                else if (direction.x < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }

                // If the player is within the stopping distance, stop moving
                if (direction.magnitude <= stoppingDistance)
                {
                    m_Rigidbody2D.velocity = Vector2.zero;
                    PlayAttack();
                }
                else
                {
                    // Normalize the direction vector and apply movement speed
                    direction = direction.normalized;
                    if (isOnSlope)
                    {
                        Vector3 targetVelocity = new Vector2(-direction.x * moveSpeed * slopeNormalPerp.x, -direction.x * moveSpeed * slopeNormalPerp.y);
                        m_Rigidbody2D.velocity = targetVelocity;
                    }
                    else if (!isOnSlope)
                    {
                        m_Rigidbody2D.velocity = direction * moveSpeed;
                    }
                }
            }
            else if (cantDetect)
            {
                m_Rigidbody2D.velocity = Vector2.zero;
            }

            if (m_Rigidbody2D.velocity.x != 0)
                m_animator.SetInteger("AnimState", 2);

            //Combat Idle
            else if (m_combatIdle)
                m_animator.SetInteger("AnimState", 1);

            //Idle
            else
                m_animator.SetInteger("AnimState", 0);
        }
        else if (GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            m_Rigidbody2D.velocity = Vector2.zero;
            m_animator.speed = 0f;
        }
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    public void PlayAttack()
    {
        Vector2 attackDirection = playerTransform.position - transform.position;
        if (attackDirection.x > 0 && !m_FacingRight)
        {
            // ... flip the player.
            Flip();
        }
        // Otherwise if the input is moving the player left and the player is facing right...
        else if (attackDirection.x < 0 && m_FacingRight)
        {
            // ... flip the player.
            Flip();
        }

        if (!isAttacking)
        {
            isAttacking = true;
            m_animator.SetTrigger("Attack");
            Instantiate(bullet, attackPoint.position, Quaternion.identity);

            StartCoroutine(AttackComplete());
        }
    }

    private IEnumerator AttackComplete()
    {
        yield return new WaitForSeconds(1f);
        isAttacking = false;
    }

    private void ReturnToInitial()
    {
        Vector2 direction = initialPosition - transform.position;
        direction.y = 0f; // Ignore the y-axis movement

        if (m_Grounded)
        {
            if (direction.sqrMagnitude < 0.01f) // Check if the magnitude is very small
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                returnTimer = 0f; 
                isAtInitial = true;
                canMove = true;
            }
            else
            {
                if (direction.x > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                // Otherwise if the input is moving the player left and the player is facing right...
                else if (direction.x < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }

                direction = direction.normalized;
                if (isOnSlope)
                {
                    Vector3 targetVelocity = new Vector2(-direction.x * moveSpeed * slopeNormalPerp.x, -direction.x * moveSpeed * slopeNormalPerp.y);
                    m_Rigidbody2D.velocity = targetVelocity;
                }
                else if (!isOnSlope)
                {
                    m_Rigidbody2D.velocity = direction * moveSpeed;
                }
            }
        }
    }

    private void Patrol() //Points of failure: Ledges, Walls
    {
        Vector2 rightSideDirection = rightSide.position - transform.position;
        rightSideDirection.y = 0.0f;
        rightSideDirection = rightSideDirection.normalized;
        Vector2 leftSideDirection = leftSide.position - transform.position;
        leftSideDirection.y = 0.0f;
        leftSideDirection = leftSideDirection.normalized;
        if (currentSide == 0)
        {
            if (Mathf.Abs(rightSide.position.x - transform.position.x) < 0.1f)
            {
                canMove = false;
                m_Rigidbody2D.velocity = Vector2.zero;
                pauseTimer += Time.fixedDeltaTime;
                if (pauseTimer >= 1f)
                {
                    currentSide = 1;
                    pauseTimer = 0f;
                    canMove = true;
                }
            }
            else if (canMove)
            {
                if (isOnSlope)
                {
                    Vector3 targetVelocity = new Vector2(-rightSideDirection.x * moveSpeed * slopeNormalPerp.x, -rightSideDirection.x * moveSpeed * slopeNormalPerp.y);
                    m_Rigidbody2D.velocity = targetVelocity;
                }
                else if (!isOnSlope)
                {
                    m_Rigidbody2D.velocity = rightSideDirection * moveSpeed;
                }
            }
            if (rightSideDirection.x > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (rightSideDirection.x < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }
        else if (currentSide == 1)
        {
            if (Mathf.Abs(leftSide.position.x - transform.position.x) < 0.1f)
            {
                canMove = false;
                m_Rigidbody2D.velocity = Vector2.zero;
                pauseTimer += Time.fixedDeltaTime;
                if (pauseTimer >= 1f)
                {
                    currentSide = 0;
                    pauseTimer = 0f;
                    canMove = true;
                }
            }
            else if (canMove)
            {
                if (isOnSlope)
                {
                    Vector3 targetVelocity = new Vector2(-leftSideDirection.x * moveSpeed * slopeNormalPerp.x, -leftSideDirection.x * moveSpeed * slopeNormalPerp.y);
                    m_Rigidbody2D.velocity = targetVelocity;
                }
                else if (!isOnSlope)
                {
                    m_Rigidbody2D.velocity = leftSideDirection * moveSpeed;
                }
            }
            if (leftSideDirection.x > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (leftSideDirection.x < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }
    }

    private void SlopeCheck()
    {
        Vector2 checkPos = (Vector2)transform.position + m_circleCollider2D.offset - Vector2.up * m_circleCollider2D.radius;
        RaycastHit2D hitDown = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, m_WhatIsGround);
        if (hitDown)
        {
            slopeNormalPerp = Vector2.Perpendicular(hitDown.normal).normalized;
            slopeDownAngle = Vector2.Angle(hitDown.normal, Vector2.up);

            if (slopeDownAngle != slopeDownAngleOld)
            {
                isOnSlope = true;
            }

            if ((slopeDownAngleOld < 46 && slopeDownAngleOld > 44) && slopeDownAngle == 0)
            {
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, -1f);
            }

            slopeDownAngleOld = slopeDownAngle;


            Debug.DrawRay(hitDown.point, slopeNormalPerp, Color.yellow);
            Debug.DrawRay(hitDown.point, hitDown.normal, Color.yellow);
        }

        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, m_WhatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, m_WhatIsGround);

        if (slopeHitFront)
        {
            isOnSlope = true; 
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else if (slopeHitBack)
        {
            isOnSlope = true; 
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0.0f;
            isOnSlope = false; 
        }

        if (slopeDownAngle == 0f || !m_Grounded) //Important! without it, will stay forever isOnSlope = true even if you arent on a slope
        {
            isOnSlope = false;
        }
    }

    private IEnumerator dashIgnore(Vector2 side)
    {
        newSide = side.x;
        if (!cantDetect && Mathf.Sign(newSide) != Mathf.Sign(oldSide) && GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().IsDashing())
        {
            cantDetect = true;
            yield return new WaitForSeconds(0.5f);
            cantDetect = false;
        }
        oldSide = newSide;
    }

    private void LedgeDetect()
    {
        RaycastHit2D hitDown = Physics2D.Raycast(LedgeCheck.position, Vector2.down, m_WhatIsGround);
        float ledgeDownAngle = Vector2.Angle(hitDown.normal, Vector2.up);
        if (m_FacingRight)
        {
            wallCheck = Physics2D.Raycast(LedgeCheck.position, Vector2.left, EnemyAndLevel);
        }
        else if (!m_FacingRight)
        {
            wallCheck = Physics2D.Raycast(LedgeCheck.position, Vector2.right, EnemyAndLevel);
        }
        if (m_Grounded)
        {
            if (hitDown.distance >= 0.1f && ledgeDownAngle == 0 && !isOnSlope && wallCheck.collider.gameObject.layer == 3) //Failure points: If it hits another enemies collider, if the stairs lead straight to a ledge
            {
                isNearLedge = true;
            }
            else
            {
                isNearLedge = false;
            }
        }
    }
}
