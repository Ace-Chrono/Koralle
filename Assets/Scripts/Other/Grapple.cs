using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapple : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private int damage;
    private Rigidbody2D rigidBody;
    private Vector3 direction;
    private bool cantDamage = false;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;

        float rot = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
        rot -= 90;
        transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    private void FixedUpdate()
    {
        if (GameObject.FindWithTag("Player").GetComponent<PlayerAbilities>().TimeStopped())
        {
            rigidBody.velocity = Vector3.zero;
        }
        else
        {
            rigidBody.velocity = new Vector2(direction.x, direction.y).normalized * force;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject collider = collision.gameObject;
        Debug.Log(collider.tag);
        if (collider.CompareTag("Level"))
        {
            rigidBody.velocity = Vector3.zero;
            rigidBody.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
            cantDamage = true;
            gameObject.layer = LayerMask.NameToLayer("Dead Kunai");
        }
        else if (!cantDamage && collision.gameObject.CompareTag("Enemy"))
        {
            collider.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(gameObject);
            cantDamage = true;
        }
        else if (!cantDamage && collision.gameObject.CompareTag("Ranged Enemy"))
        {
            collider.GetComponent<EnemyRanged>().TakeDamage(damage);
            Destroy(gameObject);
            cantDamage = true;
        }
        else if (!cantDamage && collision.gameObject.CompareTag("Shield Enemy"))
        {
            collider.GetComponent<EnemyShield>().TakeDamage(damage);
            Destroy(gameObject);
            cantDamage = true;
        }
        else
        {
            return;
        }
    }

    public bool CantDamage()
    { return cantDamage; }

}
