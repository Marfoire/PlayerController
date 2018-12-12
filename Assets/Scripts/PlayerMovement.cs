using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    //horizontal movement variables
    public float storedLastInput;
    public float MaxSpeed;
    public float AccelerationTime;
    public float DecelerationTime;
    public float totalVelocity;
    public float deltaVelocity;
    public float prevVelocity;

    //jump variables
    public float gravityValue;
    public float jumpHeight;
    public float jumpTime;
    public float jumpVelocity;
    public float terminalVelocity;
    public bool grounded;

    //reference variables
    public Rigidbody2D rb;

    //the raycast for the grouned checks
    RaycastHit2D hit;

    public BoxCollider2D bc;

    public bool clingingToWall;
    public float clingBufferStart;

    public bool jumpInputted;
    public bool leftInputted;
    public bool rightInputted;

    // Use this for initialization
    void Start()
    {
        //get references
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();

        //use motion equations to get gravity and jump velocity
        gravityValue = (terminalVelocity/0.3333f);
        jumpVelocity = -(gravityValue * jumpTime);
    }

    //method for jumping
    void JumpCall()
    {
        //if the player is grounded and they press jump
        if (jumpInputted == true && grounded == true)
        {
            jumpInputted = false;
            //apply the initial jump velocity
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + jumpVelocity);
            //rb.MovePosition(new Vector2(rb.position.x, rb.position.y + jumpVelocity));
        }
        if (jumpInputted == true && clingingToWall == true)
        {
            jumpInputted = false;

            if((totalVelocity * Input.GetAxis("Horizontal") > 0 && leftInputted != true) || (totalVelocity * Input.GetAxis("Horizontal") < 0 && rightInputted != true))
            {
                totalVelocity = -totalVelocity * 6;
                prevVelocity = -prevVelocity * 6;
            }
            else if((totalVelocity * Input.GetAxis("Horizontal") > 0 && leftInputted == true) || (totalVelocity * Input.GetAxis("Horizontal") < 0 && rightInputted == true))
            {
                storedLastInput = -storedLastInput;
            }
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + jumpVelocity);
        }
    }

    //recycled from shipmotor
    public void HorizontalMovement()
    {
        //if the either of the input axis are receiving RAW input, start running some acceleration calculations (GetAxisRaw only returns 0,1,-1 based on what input is pressed)
        if (leftInputted == true || rightInputted == true)
        {          

            storedLastInput = Input.GetAxisRaw("Horizontal");//store the input values in a vector2 for deceleration

            //If the acceleration time is not 0
            if (AccelerationTime != 0)
            {
                deltaVelocity = MaxSpeed / AccelerationTime * Time.deltaTime;//find acceleration delta velocity with maxSpeed divided by acceleration time
                totalVelocity = prevVelocity + deltaVelocity;//add the delta velocity to the previous velocity to get the current total velocity                
            }
            else //if the acceleration time is 0, don't worry about doing acceleration calculations
            {
                totalVelocity = MaxSpeed;//set the velocity to the max speed
            }
            
            //totalStart++; //this is for testing
        }

        //if there is no input being detected and the velocity isn't 0
        else if (leftInputted == false && rightInputted == false && totalVelocity != 0)
        {
            //if deceleration time is not 0
            if (DecelerationTime != 0)
            {
                deltaVelocity = MaxSpeed / DecelerationTime * Time.deltaTime;//find deceleration delta velocity with max speed divided by deceleration time 
            }
            else//if the deceleration is 0
            {
                deltaVelocity = prevVelocity;//don't worry about any deceleration calculations
            }

            totalVelocity = prevVelocity - deltaVelocity;//subtract the delta velocity from the previous velocity to get the current velocity value
            //totalEnd++; //this is for testing
        }

        //run a check to see if the velocity is greater than the speed cap
        if (totalVelocity > MaxSpeed)
        {
            totalVelocity = MaxSpeed;//correct the velocity back to the max speed
            //print("Acceleration time in frames: " + totalStart);
        }

        //if total velocity rolls over to be negative when decelerating
        if (totalVelocity < 0)
        {
            storedLastInput = 0;//reset the input to 0
            totalVelocity = 0;//set the total velocity back to 0 to fix that
            //print("Deceleration time in frames: " + totalEnd);
        }

        //call the translate command for the x axis using vector3.right (storedLastInput provides input vector2 values that can never be 0,0)
        //rb.velocity = new Vector2((storedLastInput * displacement), rb.velocity.y);

        //set the previous velocity to the current total velocity for the next loop around with this function
        prevVelocity = totalVelocity;
    }

    //checkGrounded does some raycasting and position correcting for the purpose of keeping thje player grounded
    void CheckGrounded()
    {
        //create a ray that's just below the player's character
        hit = Physics2D.Raycast(new Vector2(bc.offset.x + 0.1f + bc.bounds.center.x - (bc.bounds.extents.x), rb.position.y - (bc.bounds.extents.y + 0.1f)), Vector2.right, bc.bounds.extents.x * 2 - 0.1f);

        Debug.DrawRay(new Vector2(bc.offset.x + 0.1f + bc.bounds.center.x - (bc.bounds.extents.x), rb.position.y - (bc.bounds.extents.y + 0.1f)), Vector2.right * (bc.bounds.extents.x * 2 - 0.1f), Color.white);

        if (hit.collider != null)
        {//if the ray collider with something

            if (hit.collider.bounds.center.y + hit.collider.bounds.extents.y - 30 < rb.position.y - bc.bounds.extents.y && grounded != true && rb.velocity.y < -500)//if the ray is really far inside the stage piece it collided with and the velocity y is really high
            {
                rb.position = new Vector3(rb.position.x, hit.collider.bounds.center.y + hit.collider.bounds.extents.y + bc.bounds.extents.y);//correct the player position to be just on top of that stage piece
            }


            if (hit.collider.bounds.center.y + hit.collider.bounds.extents.y - 1 < rb.position.y - bc.bounds.extents.y && hit.collider.gameObject.tag == "Stage")//if the ray is colliding with the topside of the stage piece it connected with
            {
                grounded = true;//set grounded to true

            }


        }
        else//if there are no collisions
        {
            grounded = false;//set grounded to false
        }
    }

    void CheckWallCling()
    {
        if(grounded == false && rb.velocity.x == 0 && (totalVelocity * storedLastInput == 180 || totalVelocity * storedLastInput == -180))
        {
            clingingToWall = true;
            rb.velocity = new Vector2(0, rb.velocity.y - (gravityValue * Time.deltaTime));
            clingBufferStart = Time.time;
        }
        else if(clingBufferStart + 0.2 < Time.time)
        {
            clingingToWall = false;
        }
    }

    private void Update()
    {       
        if (Input.GetButtonDown("Jump") == true)
        {
            jumpInputted = true;
        }

        if (Input.GetKey("left") == true || Input.GetKey("a"))
        {
            leftInputted = true;
        }

        if (Input.GetKey("right") == true || Input.GetKey("d"))
        {
            rightInputted = true;
        }

    }

    void FixedUpdate () {

        CheckGrounded();

        //call horizontal movement
        HorizontalMovement();

        CheckWallCling();

        //check the jump
        JumpCall();

        //apply gravity to shovel knight
        rb.velocity = new Vector2((totalVelocity * storedLastInput), rb.velocity.y + (gravityValue * Time.deltaTime));        

        //if the player is not grounded
        if (grounded == false)
        {
            //clamp the velocity between the terminal velocity and the intial velocity
            Mathf.Clamp(rb.velocity.y, terminalVelocity, jumpVelocity);
        }

        leftInputted = false;
        rightInputted = false;
    }
}

