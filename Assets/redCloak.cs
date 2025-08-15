using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class redCloak : MonoBehaviour   
{

   
    [SerializeField] private Animator animator;


    public float movespeed;
    float x, y;
    Rigidbody2D rb;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        x = Input.GetAxisRaw("Horizontal") * movespeed;
        y = Input.GetAxisRaw("Vertical") * movespeed;
        rb.velocity = new Vector2 (x, y);


        if (x != 0)
        {
            animator.SetBool("isrunning", true);
        }
        else 
        {

            animator.SetBool("isrunning", false);

        }



        if (x != 0) 
        {
            rb.AddForce (new Vector2 (x * movespeed, 0f));
        }

        if (x > 0) 
        {
            gameObject.transform.localScale = new Vector3(1, 1, 1);
        }

        else if (x < 0)
        {
            gameObject.transform.localScale = new Vector3(-1, 1, 1);
        }



        if (Input.GetMouseButtonDown(0)) 
        {
            animator.SetBool("isattacking", true);
        }

    }

    public void endAttack() 
    {
        animator.SetBool("isattacking", false);
    }

    
    
}
    