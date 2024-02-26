using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyShield : MonoBehaviour
{
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] private LayerMask m_EnemyLevelLayers;
    [SerializeField] private LayerMask m_PlayerLayers;
    [SerializeField] private GameObject m_Enemy;
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Transform m_RightSide;
    [SerializeField] private Transform m_LeftSide;
    [SerializeField] private Transform m_LedgeCheck;
    [SerializeField] private Transform m_AttackPoint;
    [SerializeField] private PhysicsMaterial2D m_NoFrictionMaterial;
    [SerializeField] private PhysicsMaterial2D m_FullFrictionMaterial;
    [SerializeField] private GameObject m_Shield; //
    [SerializeField] private float m_SlopeCheckDistance;
    [SerializeField] private int m_MaxHealth = 100;
    [SerializeField] private int m_Damage = 100;
    [SerializeField] private bool m_Dead = false;


    private Transform m_PlayerTransform;
    private Animator m_animator;
    private BoxCollider2D m_boxCollider2D;
    private CircleCollider2D m_circleCollider2D;
    private Rigidbody2D m_Rigidbody2D;
    private LayerMask m_RayLayers;
    private Vector3 m_InitialPosition;
    private Vector2 m_SlopeNormalPerp;
    private RaycastHit2D m_WallCheck;
    const float k_GroundedRadius = .2f;
    private float m_StoppingDistance = 1.5f;
    private float m_TrackingDistance = 5f;
    private float m_MoveSpeed = 5f;
    private float m_AttackRange = 0.5f;
    private float m_AttackTimer = 0f;
    private float m_ReturnTimer = 0f;
    private float m_PauseTimer = 0f;
    private float m_SlopeDownAngle;
    private float m_OldSlopeDownAngle;
    private float m_SlopeSideAngle;
    private float m_OldSide;
    private float m_NewSide;
    private int m_CurrentHealth;
    private int m_CurrentSide = 0;
    private int builtUpDamage;
    private bool m_FacingRight = false;
    private bool m_Grounded;
    private bool m_combatIdle = true;
    private bool m_IsAttacking = false;
    private bool m_IsAtInitial = true;
    private bool m_CanMove = true;
    private bool m_IsOnSlope;
    private bool m_HasShield = true; //
    private bool m_CantDetect = false;
    private bool m_IsNearLedge;
    
    void Start()
    {
        m_PlayerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();
        m_boxCollider2D = GetComponent<BoxCollider2D>();
        m_CurrentHealth = m_MaxHealth;
        m_InitialPosition = transform.position;
        m_RayLayers = LayerMask.GetMask("Player", "Level");
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
        if (m_Grounded && m_Rigidbody2D.velocity.x == 0.0f && m_IsOnSlope)
        {
            m_circleCollider2D.sharedMaterial = m_FullFrictionMaterial;
        }
        else
        {
            m_circleCollider2D.sharedMaterial = m_NoFrictionMaterial;
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
            Vector2 playerDirection = m_PlayerTransform.position - transform.position;
            if (m_HasShield && ((playerDirection.x > 0 && m_FacingRight) || (playerDirection.x < 0 && !m_FacingRight)))
            {
                m_Shield.SetActive(false);
                m_HasShield = false;
                return;
            }

            m_CurrentHealth -= damage;

            if (m_CurrentHealth <= 0)
            {
                m_Enemy.layer = LayerMask.NameToLayer("Body");
                m_animator.SetTrigger("Death");
                m_boxCollider2D.offset = new Vector2(0, 0.32f);
                m_boxCollider2D.size = new Vector2(1.2f, 0.5f);
                m_Dead = true;
                m_Rigidbody2D.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                m_Rigidbody2D.velocity = Vector2.zero;
                m_Shield.SetActive(false);
            }
            else
            {
                m_animator.SetTrigger("Hurt");
            }
        }
    }

    public void followPlayer()
    {
        Vector3 rayStart = transform.position;
        rayStart.y += 0.5f;
        Vector2 direction = m_PlayerTransform.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, direction, Mathf.Infinity, m_RayLayers);
        direction.y = 0f; // Ignore the y-axis movement
        StartCoroutine(dashIgnore(direction));

        if (!m_Dead && m_Grounded && !GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            m_animator.speed = 1f;
            if (direction.magnitude >= m_TrackingDistance || !hit.collider.CompareTag("Player"))
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                if (!m_IsAtInitial)
                    m_ReturnTimer += Time.fixedDeltaTime;
                if (m_ReturnTimer >= 1f && !m_IsAtInitial)
                {
                    ReturnToInitial();
                }
                else if (m_IsAtInitial)
                {
                    Patrol();
                }
            }
            else if (m_IsNearLedge && ((direction.x > 0 && m_FacingRight) || (direction.x < 0 && !m_FacingRight)))
            {
                m_Rigidbody2D.velocity = Vector2.zero;
            }
            else if (m_IsNearLedge && !m_CantDetect && ((direction.x > 0 && !m_FacingRight) || (direction.x < 0 && m_FacingRight)))
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                Flip();
            }
            else if (m_PlayerTransform != null && direction.magnitude <= m_TrackingDistance && hit.collider.CompareTag("Player") && !m_CantDetect)
            {
                m_ReturnTimer = 0f;
                m_IsAtInitial = false;
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
                if (direction.magnitude <= m_StoppingDistance)
                {
                    m_AttackTimer += Time.fixedDeltaTime;
                    m_Rigidbody2D.velocity = Vector2.zero;
                    if (m_AttackTimer > 0.3f)
                        PlayAttack();
                }
                else
                {
                    m_AttackTimer = 0f;
                    direction = direction.normalized;
                    if (m_IsOnSlope)
                    {
                        Vector3 targetVelocity = new Vector2(-direction.x * m_MoveSpeed * m_SlopeNormalPerp.x, -direction.x * m_MoveSpeed * m_SlopeNormalPerp.y);
                        m_Rigidbody2D.velocity = targetVelocity;
                    }
                    else if (!m_IsOnSlope)
                    {
                        m_Rigidbody2D.velocity = direction * m_MoveSpeed;
                    }
                }
            }
            else if (m_CantDetect)
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
        bool playerDamaged = false;

        if (!m_IsAttacking)
        {
            m_IsAttacking = true;
            m_animator.SetTrigger("Attack");
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(m_AttackPoint.position, m_AttackRange, m_PlayerLayers);
            foreach (Collider2D player in hitEnemies)
            {
                if (playerDamaged == false)
                {
                    Debug.Log("We hit " + player.name);
                    Debug.Log(player.GetType().ToString());
                    player.GetComponent<PlayerCombat>().TakeDamage(m_Damage);
                    playerDamaged = true;
                }
            }

            StartCoroutine(AttackComplete());
        }
    }

    private IEnumerator AttackComplete()
    {
        yield return new WaitForSeconds(1f);
        m_IsAttacking = false;
    }

    private void ReturnToInitial()
    {
        Vector2 direction = m_InitialPosition - transform.position;
        direction.y = 0f; // Ignore the y-axis movement

        if (m_Grounded)
        {
            if (direction.sqrMagnitude < 0.01f) // Check if the magnitude is very small
            {
                m_Rigidbody2D.velocity = Vector2.zero;
                m_ReturnTimer = 0f;
                m_IsAtInitial = true;
                m_CanMove = true;
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
                if (m_IsOnSlope)
                {
                    Vector3 targetVelocity = new Vector2(-direction.x * m_MoveSpeed * m_SlopeNormalPerp.x, -direction.x * m_MoveSpeed * m_SlopeNormalPerp.y);
                    m_Rigidbody2D.velocity = targetVelocity;
                }
                else if (!m_IsOnSlope)
                {
                    m_Rigidbody2D.velocity = direction * m_MoveSpeed;
                }
            }
        }
    }

    private void Patrol() //Points of failure: Ledges, Walls
    {
        Vector2 rightSideDirection = m_RightSide.position - transform.position;
        rightSideDirection.y = 0.0f;
        rightSideDirection = rightSideDirection.normalized;
        Vector2 leftSideDirection = m_LeftSide.position - transform.position;
        leftSideDirection.y = 0.0f;
        leftSideDirection = leftSideDirection.normalized;
        if (m_CurrentSide == 0)
        {
            if (Mathf.Abs(m_RightSide.position.x - transform.position.x) < 0.1f)
            {
                m_CanMove = false;
                m_Rigidbody2D.velocity = Vector2.zero;
                m_PauseTimer += Time.fixedDeltaTime; 
                if (m_PauseTimer >= 1f)
                {
                    m_CurrentSide = 1;
                    m_PauseTimer = 0f;
                    m_CanMove = true;
                }
            }
            else if (m_CanMove)
            {
                if (m_IsOnSlope)
                {
                    Vector3 targetVelocity = new Vector2(-rightSideDirection.x * m_MoveSpeed * m_SlopeNormalPerp.x, -rightSideDirection.x * m_MoveSpeed * m_SlopeNormalPerp.y);
                    m_Rigidbody2D.velocity = targetVelocity;
                }
                else if (!m_IsOnSlope)
                {
                    m_Rigidbody2D.velocity = rightSideDirection * m_MoveSpeed;
                }
            }
            if (rightSideDirection.x > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            else if (rightSideDirection.x < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }
        else if (m_CurrentSide == 1)
        {
            if (Mathf.Abs(m_LeftSide.position.x - transform.position.x) < 0.1f)
            {
                m_CanMove = false;
                m_Rigidbody2D.velocity = Vector2.zero;
                m_PauseTimer += Time.fixedDeltaTime;
                if (m_PauseTimer >= 1f)
                {
                    m_CurrentSide = 0;
                    m_PauseTimer = 0f;
                    m_CanMove = true;
                }
            }
            else if (m_CanMove)
            {
                if (m_IsOnSlope)
                {
                    Vector3 targetVelocity = new Vector2(-leftSideDirection.x * m_MoveSpeed * m_SlopeNormalPerp.x, -leftSideDirection.x * m_MoveSpeed * m_SlopeNormalPerp.y);
                    m_Rigidbody2D.velocity = targetVelocity;
                }
                else if (!m_IsOnSlope)
                {
                    m_Rigidbody2D.velocity = leftSideDirection * m_MoveSpeed;
                }
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            if (leftSideDirection.x > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
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
        RaycastHit2D hitDown = Physics2D.Raycast(checkPos, Vector2.down, m_SlopeCheckDistance, m_WhatIsGround);
        if (hitDown)
        {
            m_SlopeNormalPerp = Vector2.Perpendicular(hitDown.normal).normalized;
            m_SlopeDownAngle = Vector2.Angle(hitDown.normal, Vector2.up);

            if (m_SlopeDownAngle != m_OldSlopeDownAngle)
            {
                m_IsOnSlope = true;
            }

            if ((m_OldSlopeDownAngle < 46 && m_OldSlopeDownAngle > 44) && m_SlopeDownAngle == 0)
            {
                m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, -1f);
            }

            m_OldSlopeDownAngle = m_SlopeDownAngle;


            Debug.DrawRay(hitDown.point, m_SlopeNormalPerp, Color.yellow);
            Debug.DrawRay(hitDown.point, hitDown.normal, Color.yellow);
        }

        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, m_SlopeCheckDistance, m_WhatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, m_SlopeCheckDistance, m_WhatIsGround);

        if (slopeHitFront)
        {
            m_IsOnSlope = true;
            m_SlopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else if (slopeHitBack)
        {
            m_IsOnSlope = true;
            m_SlopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            m_SlopeSideAngle = 0.0f;
            m_IsOnSlope = false;
        }

        if (m_SlopeDownAngle == 0f || !m_Grounded) //Important! without it, will stay forever isOnSlope = true even if you arent on a slope
        {
            m_IsOnSlope = false;
        }
    }

    private IEnumerator dashIgnore(Vector2 side)
    {
        m_NewSide = side.x; 
        if (!m_CantDetect && Mathf.Sign(m_NewSide) != Mathf.Sign(m_OldSide) && GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>().IsDashing())
        {
            m_CantDetect = true;
            yield return new WaitForSeconds(0.5f);
            m_CantDetect = false;
        }
        m_OldSide = m_NewSide;
    }

    private void LedgeDetect()
    {
        RaycastHit2D hitDown = Physics2D.Raycast(m_LedgeCheck.position, Vector2.down, m_WhatIsGround);
        float ledgeDownAngle = Vector2.Angle(hitDown.normal, Vector2.up);
        if (m_FacingRight)
        {
            m_WallCheck = Physics2D.Raycast(m_LedgeCheck.position, Vector2.left, m_EnemyLevelLayers);
        }
        else if (!m_FacingRight)
        {
            m_WallCheck = Physics2D.Raycast(m_LedgeCheck.position, Vector2.right, m_EnemyLevelLayers);
        }
        if (m_Grounded)
        {
            if (hitDown.distance >= 0.1f && ledgeDownAngle == 0 && !m_IsOnSlope && m_WallCheck.collider.gameObject.layer == 3) //Failure points: If it hits another enemies collider, if the stairs lead straight to a ledge
            {
                m_IsNearLedge = true;
            }
            else
            {
                m_IsNearLedge = false;
            }
        }
    }
}
