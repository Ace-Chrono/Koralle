using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private LayerMask m_EnemyLayers;
    [SerializeField] private LayerMask m_BulletLayers;
    [SerializeField] private GameObject m_Player;
    [SerializeField] private Transform m_AttackPoint;
    [SerializeField] private int m_MaxHealth = 100;
    [SerializeField] private int m_Damage = 100;
    [SerializeField] private bool m_Dead = false;
    [SerializeField] private GameObject RestartCanvas;

    private bool m_IsAttacking = false;
    private float m_AttackRange = 0.5f;
    private int m_CurrentHealth;
    private Color originalColor;
    private Animator m_animator;
    private Animator m_AttackAnimator;
    private Rigidbody2D m_Rigidbody2D;
    private BoxCollider2D m_boxCollider2D;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        RestartCanvas.GetComponent<Canvas>().enabled = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        m_AttackAnimator = m_AttackPoint.GetComponent<Animator>();
        m_animator = GetComponent<Animator>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_boxCollider2D = GetComponent<BoxCollider2D>();
        m_CurrentHealth = m_MaxHealth;
    }

    private void Update()
    {
        FollowCursor();
    }

    private void FollowCursor()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Convert mouse position to world coordinates
        Vector3 playerPosition = m_Player.transform.position;
        playerPosition.y += 1f;

        Vector3 direction = mousePosition - playerPosition; // Calculate direction from player to mouse position
        direction.z = 0f; // Ensure the z-component is zero to keep it in 2D space

        Vector3 targetPosition = playerPosition + direction.normalized * 1f; // Calculate the target position with the desired distance

        m_AttackPoint.position = targetPosition;
        m_AttackPoint.up = direction.normalized;
    }

    public void PlayAttack()
    {
        bool enemyDamaged = false;

        if (!m_IsAttacking)
        {
            m_IsAttacking = true;
            m_AttackAnimator.SetTrigger("Attack");

            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(m_AttackPoint.position, m_AttackRange, m_EnemyLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemyDamaged == false)
                {
                    if (enemy.tag == "Ranged Enemy")
                        enemy.GetComponent<EnemyRanged>().TakeDamage(m_Damage);
                    else if (enemy.tag == "Enemy")
                        enemy.GetComponent<Enemy>().TakeDamage(m_Damage);
                    else if (enemy.tag == "Shield Enemy")
                        enemy.GetComponent<EnemyShield>().TakeDamage(m_Damage);
                    enemyDamaged = true;
                }
            }
            Collider2D[] hitProjectiles = Physics2D.OverlapCircleAll(m_AttackPoint.position, m_AttackRange, m_BulletLayers);
            foreach (Collider2D projectile in hitProjectiles)
            {
                projectile.GetComponent<EnemyBullet>().parryBullet();
            }

            StartCoroutine(AttackDelay());
        }
    }

    private IEnumerator AttackDelay()
    {
        yield return new WaitForSeconds(0.5f);
        m_IsAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        m_CurrentHealth -= damage;

        if (m_CurrentHealth <= 0)
        {
            m_Player.layer = LayerMask.NameToLayer("Body");
            m_animator.SetTrigger("Death");
            m_boxCollider2D.offset = new Vector2(0, 0.32f);
            m_boxCollider2D.size = new Vector2(1.2f, 0.5f);
            m_Dead = true;
            m_Rigidbody2D.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            m_Rigidbody2D.velocity = Vector2.zero;
            RestartCanvas.GetComponent<Canvas>().enabled = true;
        }
        else
        {
            //m_animator.SetTrigger("Hurt");
            StartCoroutine(DamageColor());
        }
    }

    private IEnumerator DamageColor()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        spriteRenderer.color = originalColor;
    }

    public bool Dead()
    {
        return m_Dead;
    }
}
