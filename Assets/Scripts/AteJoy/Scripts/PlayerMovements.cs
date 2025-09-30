using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMovements : MonoBehaviour
{
    public Animator animator;
    
    public float moveSpeed;

    private Rigidbody2D rb;

    private float x;
    private float y;

    private Vector2 input;
    private bool moving;

    public bool canMove = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!canMove)
        {
            input = Vector2.zero;
            Animate(); // Still update animation to reflect idle
            return;
        }

        GetInput();
        Animate();
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            rb.velocity = input * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }

    }

    private void GetInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        input = new Vector2(x, y);
        input.Normalize();
    }

    private void Animate()
    {
        moving = input.magnitude > 0.1f;

        if (moving)
        {
            animator.SetFloat("X", x);
            animator.SetFloat("Y", y);
        }

        animator.SetBool("Moving", moving);
    }
}
