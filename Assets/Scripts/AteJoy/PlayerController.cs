using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public float moveSpeed;

    public Rigidbody2D rgbd2d;
    Vector2 movement;

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.x >= 0) {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }

    }

    private void FixedUpdate() {
        rgbd2d.MovePosition(rgbd2d.position + movement.normalized * moveSpeed * Time.deltaTime);
    }
}
