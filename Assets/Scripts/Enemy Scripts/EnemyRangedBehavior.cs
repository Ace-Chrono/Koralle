using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRangedBehavior : MonoBehaviour
{
    public EnemyRanged enemy;

    private void FixedUpdate()
    {
        enemy.followPlayer();
    }
}
