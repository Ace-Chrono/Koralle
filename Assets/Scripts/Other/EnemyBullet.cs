using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private GameObject player;
    private Rigidbody2D rb;
    private Vector3 direction; 
    public float force;
    private bool playerDamaged = false;
    private bool enemyDamaged = false;
    private bool isEnemies = true;
    public int damage = 100;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");

        direction = player.transform.position - transform.position;
    }

    private void FixedUpdate()
    {
        if (GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            rb.velocity = Vector2.zero;
        }
        else if (isEnemies)
        {
            rb.velocity = new Vector2(direction.x, direction.y + 1.2f).normalized * force;
            float rot = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
            if (direction.x < 0)
            {
                rot -= 20;
            }
            if (direction.x > 0)
            {
                rot += 20;
            }

            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
        else
        {
            rb.velocity = new Vector2(-direction.x, -(direction.y + 1.2f)).normalized * force;
            float rot = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
            if (direction.x < 0)
            {
                rot -= 20;
            }
            if (direction.x > 0)
            {
                rot += 20;
            }
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject collider = collision.gameObject;
        if (!playerDamaged && collision.gameObject.CompareTag("Player") && isEnemies)
        {
            collider.GetComponent<PlayerCombat>().TakeDamage(damage);
            Destroy(gameObject);
            playerDamaged=true;
        }
        else if (!enemyDamaged && collision.gameObject.CompareTag("Enemy") && !isEnemies)
        {
            collider.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(gameObject);
            enemyDamaged=true;
        }
        else if (!enemyDamaged && collision.gameObject.CompareTag("Ranged Enemy") && !isEnemies)
        {
            collider.GetComponent<EnemyRanged>().TakeDamage(damage);
            Destroy(gameObject);
            enemyDamaged = true;
        }
        else if (!enemyDamaged && collision.gameObject.CompareTag("Shield Enemy") && !isEnemies)
        {
            collider.GetComponent<EnemyShield>().TakeDamage(damage);
            Destroy(gameObject);
            enemyDamaged = true;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public void parryBullet()
    {
        gameObject.layer = 9;

        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        /*For deflecting to mouse
        Vector3 mouseDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        rb.velocity = new Vector2(mouseDirection.x, mouseDirection.y).normalized * force;
        float rot = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rot);
        */

        isEnemies = false;
    }
}
