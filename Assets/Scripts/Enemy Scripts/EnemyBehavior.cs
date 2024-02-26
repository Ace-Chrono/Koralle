using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    public Enemy enemy;

    private void FixedUpdate()
    {
        enemy.followPlayer(); 
    }
}
