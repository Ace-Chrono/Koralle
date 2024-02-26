using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

	[SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerAbilities abilities;

	int m_HorizontalMove = 0;
	bool m_Jump = false;
    bool m_Crouch = false;

    // Update is called once per frame
    void Update()
	{
        if (!combat.Dead())
		{
            if (Input.GetMouseButton(1) && abilities.ReturnTimeMeter() > 0)
            {
                Time.timeScale = 0.7f;
                Time.fixedDeltaTime = 0.02F * Time.timeScale;
            }
            else if (abilities.ReturnTimeMeter() == 0 || !Input.GetMouseButton(1))
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02F;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                abilities.PickUpItem();
            }

            /*if (Input.GetKeyDown(KeyCode.Q))
            {
                abilities.StopTime();
            }*/

            if (Input.GetKeyDown(KeyCode.F))
            {
                abilities.ThrowGrapple();
            }

            /*if (Input.GetKeyDown(KeyCode.G)) Teleportation doesn't really fit the game tbh
            {
                abilities.Teleport();
            }*/

            if (Input.GetKeyDown(KeyCode.G))
            {
                abilities.Grapple();
            }

            if (movement.IsDashing() || movement.IsWallJumping())
            {
                return;
            }

            if (Input.GetAxisRaw("Horizontal") > 0)
            {
                m_HorizontalMove = 1;
            }
            else if (Input.GetAxisRaw("Horizontal") < 0)
            {
                m_HorizontalMove = -1;
            }
            else 
            { 
                m_HorizontalMove = 0; 
            }

            if (Input.GetButtonDown("Jump") && movement.IsOnWall() && !movement.IsOnGround() && !movement.ReachedWallJumpLimit())
            {
                movement.WallJump();
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && movement.CanDash() && !movement.ReturnUnderCeiling())
            {
                movement.Dash();
            }

            if (Input.GetButtonDown("Jump"))
            {
                m_Jump = true;
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                m_Crouch = true;
            }
            else
            {
                m_Crouch = false; 
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                combat.PlayAttack();
            }
        }
        else if (combat.Dead())
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02F;
        }
    }

	void FixedUpdate()
	{
        if (!combat.Dead())
        {
            if (Input.GetMouseButton(1))
            {
                abilities.ChangeTimeMeter(0, Time.deltaTime);
            }
            else
            {
                abilities.ChangeTimeMeter(1, Time.deltaTime / 4);
            }
        }

        if (movement.IsDashing() || movement.IsWallJumping())
		{
			return;
		}

		// Move our character
		movement.Move(m_HorizontalMove, m_Jump, m_Crouch);
		m_Jump = false;
    }
} 
