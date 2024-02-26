using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShieldBehavior : MonoBehaviour
{
    public EnemyShield enemy;

    private void FixedUpdate()
    {
        enemy.followPlayer();
    }
}
