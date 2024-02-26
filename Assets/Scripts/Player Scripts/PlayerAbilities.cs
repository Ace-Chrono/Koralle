using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private Slider timeSlowMeter;
    [SerializeField] private Slider timeStopMeter;
    [SerializeField] private GameObject grapple;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float grappleForce;
    [SerializeField] private int timeStopDuration;
    [SerializeField] private int grappleAmount;
    [SerializeField] private int pickupDistance;
    [SerializeField] private TextMeshProUGUI grappleAmountText;
    [SerializeField] private ItemData grappleData;
    private Rigidbody2D rigidBody; 
    private float timeSlowMeterValue = 1;
    private float timeStopMeterValue = 10;
    private GameObject[] grapples;
    private GameObject closestGrapple = null;
    private GameObject item = null;
    private bool timeStopped = false;

    private void Start()
    {
        //kunaiAmountText.text = "Kunai: " + kunaiAmount.ToString();
        item = GameObject.FindGameObjectWithTag("Item");
    }

    private void FixedUpdate()
    {
        timeStopMeterValue += Time.deltaTime;
        if (timeStopMeterValue > 10) 
        {
            timeStopMeterValue = 10f;
        }
        timeSlowMeter.value = timeSlowMeterValue;
        timeStopMeter.value = timeStopMeterValue;
        rigidBody = GetComponent<Rigidbody2D>();
        grapples = GameObject.FindGameObjectsWithTag("Grapple");
        float distance = Mathf.Infinity;
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        foreach (GameObject grapple in grapples)
        {
            Vector3 diff = grapple.transform.position - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closestGrapple = grapple;
                distance = curDistance;
            }
        }
    }

    public void ChangeTimeMeter(int type, float amount)
    {
        if (type == 0)
        {
            timeSlowMeterValue -= amount;
            if (timeSlowMeterValue < 0)
                timeSlowMeterValue = 0;
        }
        else if (type == 1)
        {
            timeSlowMeterValue += amount;
            if (timeSlowMeterValue > 1)
                timeSlowMeterValue = 1;
        }
    }

    public float ReturnTimeMeter()
    {
        return timeSlowMeterValue;
    }

    public void StopTime()
    {
        if (timeStopMeterValue == 10)
        {
            timeStopMeterValue = 0;
            StartCoroutine(StopTimeRoutine());
        }
        
    }

    IEnumerator StopTimeRoutine()
    {
        timeStopped = true;
        yield return new WaitForSeconds(timeStopDuration);
        timeStopped = false;
    }

    public bool TimeStopped()
    {
        return timeStopped;
    }

    public void Teleport()
    {
        if (grapples == null)
            return;
        else
        {
            Vector2 target = closestGrapple.transform.position;
            target = new Vector2(target.x, target.y - 0.6f); 
            RaycastHit2D hitDown = Physics2D.Raycast(closestGrapple.transform.position, Vector2.down, 1.8f, whatIsGround);
            RaycastHit2D hitUp = Physics2D.Raycast(closestGrapple.transform.position, Vector2.up, 1.8f, whatIsGround);
            RaycastHit2D hitLeft = Physics2D.Raycast(closestGrapple.transform.position, Vector2.left, 0.9f, whatIsGround);
            RaycastHit2D hitRight = Physics2D.Raycast(closestGrapple.transform.position, Vector2.right, 0.9f, whatIsGround);
            if (hitDown)
            {
                if (hitUp)
                {
                    return;
                }
                target.y += 0.6f;
            }
            if (hitUp)
            {
                if (hitDown)
                {
                    return;
                }
                target.y -= 0.6f;
            }
            if (hitLeft)
            {
                if (hitRight)
                {
                    return;
                }
                target.x += 0.3f;
            }
            if (hitRight)
            {
                if (hitLeft)
                {
                    return;
                }
                target.x -= 0.3f;
            }
            rigidBody.velocity = Vector3.zero;
            rigidBody.position = target; 
        }
        
    }

    public void Grapple()
    {
        Vector3 start = rigidBody.transform.position;
        start.y += 1f;
        Vector3 direction = closestGrapple.transform.position - start; 
        RaycastHit2D hit = Physics2D.Raycast(start, direction, direction.magnitude, whatIsGround);
        if (grapples == null || hit)
        {
            return;
        }
        else if (closestGrapple.GetComponent<Grapple>().CantDamage() && (closestGrapple.transform.position - GetComponent<Transform>().position).sqrMagnitude < 300)
        {
            rigidBody.velocity = Vector2.zero;
            Vector3 grappleDirection = (closestGrapple.transform.position - transform.position).normalized;
            rigidBody.AddForce(grappleDirection * grappleForce / Time.timeScale);
        }
    }

    public void ThrowGrapple()
    {
        if (InventoryManager.Instance.Get(grappleData) != null)
        {
            Vector3 spawnPoint = attackPoint.position;
            RaycastHit2D hitDown = Physics2D.Raycast(spawnPoint, Vector2.down, 0.592f, whatIsGround);
            RaycastHit2D hitUp = Physics2D.Raycast(spawnPoint, Vector2.up, 0.592f, whatIsGround);
            RaycastHit2D hitLeft = Physics2D.Raycast(spawnPoint, Vector2.left, 0.592f, whatIsGround);
            RaycastHit2D hitRight = Physics2D.Raycast(spawnPoint, Vector2.right, 0.592f, whatIsGround);
            RaycastHit2D hitWall = Physics2D.Raycast(spawnPoint, spawnPoint - new Vector3(transform.position.x, transform.position.y + 0.6f), 1f, whatIsGround);
            if (hitDown || hitUp || hitLeft || hitRight || hitWall)
            {
                return;
            }
            if (grappleAmount > 0)
            {
                Instantiate(grapple, spawnPoint, Quaternion.identity);
                grappleAmount--;
                grappleAmountText.text = "Grapples: " + grappleAmount;
            }
        }
    }

    public void PickUpItem()
    {
        Vector3 distance = item.GetComponent<Rigidbody2D>().transform.position - rigidBody.transform.position;
        if (distance.magnitude < pickupDistance)
        {
            item.GetComponent<ItemController>().OnHandlePickupItem();
            grappleAmountText.text = "Grapples: " + grappleAmount.ToString();
        }
    }
}
