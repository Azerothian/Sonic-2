﻿using UnityEngine;
using System.Collections;

public class BasePlayerMovement : MonoBehaviour
{

    #region Constants
    private const float FACING_RIGHT = 0f;
    private const float FACING_LEFT = 180f;
    #endregion

    #region Public Variables
    // Player Tuning
    public int LIVES = 3;

    // Physics Section
    public float GRAVITY = 0.21875f;
    public float MAX_FALL_VELOCITY = 16f;
    public float MAX_JUMP_FORCE = 6.5f;
    public float MIN_JUMP_FORCE = 4f;
    public float ACCELERATION = 0.046875f;
    public float FRICTION = 0.046875f;
    public float DECELERATION = 0.5f;
    public float TOP_SPEED = 6f;

    public AudioClip SndJump;
    #endregion

    #region Player States
    //	    ; ===========================================================================
	//		; off_19F6A: Obj01_States:
	//		Obj01_Index:	offsetTable
	//		offsetTableEntry.w Obj01_Init		    ;  0
	//		offsetTableEntry.w Obj01_Control	    ;  2
	//		offsetTableEntry.w Obj01_Hurt		    ;  4
	//		offsetTableEntry.w Obj01_Dead		    ;  6
	//		offsetTableEntry.w Obj01_Gone		    ;  8
	//		offsetTableEntry.w Obj01_Respawning	    ; 10

	private int PlayerState = 0;

	#endregion

	#region Player Modes

	//  ; ===========================================================================
	//	; secondary states under state Obj01_Control
	//	; off_1A0BE:
	//	Obj01_Modes:	offsetTable
	//	offsetTableEntry.w Obj01_MdNormal_Checks	; 0 - not airborne or rolling
	//	offsetTableEntry.w Obj01_MdAir			    ; 2 - airborne
	//	offsetTableEntry.w Obj01_MdRoll			    ; 4 - rolling
	//	offsetTableEntry.w Obj01_MdJump			    ; 6 - jumping
	private int PlayerMode = 0;

	#endregion

    #region Private Variables
    private bool _jump = false;
    private bool _jumping = false;
    private bool _jumpCancel = false;
    private bool _moveLeft = false;
    private bool _moveRight = false;
    private bool _edgeInfront = false;
    private bool _edgeBehind = false;
    private float _edgeDistance = 0f;
    private float _udeltaTime = 0.0f;
    private float _movementRatio = 0.0f;

    // Audio Section
    private AudioSource _audioSource;
    #endregion


	// Private Objects
	private Rect box;
	private LayerMask lmGround;
	private float CurrentSpeed = 0f;
    
	// Ground Sensor Values
	private Vector2 SensorGroundA, SensorGroundB;


	// Checks
	bool grounded = false;
	bool falling = false;

	// variables for raycasting
	private int margin = 1;

	// Velocity and Rotation
	private Vector2 velocity;
	private float YRotation = FACING_RIGHT;

	void Start()
	{
		// Get layer masks by name rather than Int
		lmGround = LayerMask.NameToLayer ("Ground");
	}

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Update at a set rate of 50 Cycles per Second (This keeps compatability with the Megadrive)
    /// </summary>
	void FixedUpdate()
	{



	}

    /// <summary>
    /// Update Every Single Frame
    /// </summary>
    void Update()
    {
        _udeltaTime += (Time.deltaTime - _udeltaTime) * 0.1f;       // Update our delta time between frames to capture frame rate
        _movementRatio = 50f / (1.0f / _udeltaTime);          // Update for our movement ratio calculations (to lock movement to 50 fps)

        if (Input.GetKey(KeyCode.Z))
        {
            Time.timeScale = 0.25f;
        }
        else
        {
            Time.timeScale = 1f;
        }
        // Grab out box collider and make rect object box for easier user
        BoxCollider collider = GetComponent<BoxCollider>();
        box = new Rect(collider.bounds.min.x,
                       collider.bounds.min.y,
                       collider.bounds.size.x,
                       collider.bounds.size.y);
        ApplyGravity();
        Collision();
        CheckJump();
        DMovement();
        UpdateAnimations();
        transform.eulerAngles = new Vector2(0, YRotation);
        transform.Translate(velocity.x, velocity.y, 0f);

    }

	void OnGUI()
	{
        //GUILayout.Label ("Grounded: " + grounded + ", Falling: " + falling + ", Jumping: " + _jumping + ", Front Edge: " + FrontEdge + ", Back Edge: " + BackEdge);
		GUILayout.Label ("Location: " + transform.position.ToString());
		GUILayout.Label ("Velocity: " + velocity.ToString ());
        GUILayout.Label("Current Speed: " + Mathf.Abs(CurrentSpeed).ToString());
		GUILayout.Label ("Sensor A: " + SensorGroundA.ToString() + ", Sensor B: " + SensorGroundB.ToString ());
		GUILayout.Label ("Edge Detect Distance: " + _edgeDistance.ToString());
	}

    /// <summary>
    /// Check For Jumps
    /// </summary>
    void CheckJump()
    {
        /// When you release the jump button in the air after jumping, the computer checks to see if Sonic is moving upward (i.e. Y speed is negative). If he is, then it checks to see if Y speed is less than -4 (e.g. -5 is "less" than -4). If it is, then Y speed is set to -4. In this way, you can cut your jump short at any time, just by releasing the jump button. If you release the button in the very next step after jumping, Sonic makes the shortest possible jump.

        if (Input.GetKeyDown(KeyCode.A) && grounded)
        {
            _jumping = true;
            velocity = new Vector2(velocity.x, MAX_JUMP_FORCE);
            _audioSource.PlayOneShot(SndJump);
            _jump = false;
        }

        if (Input.GetKeyUp(KeyCode.A) && !grounded)
        {
            if (velocity.y > MIN_JUMP_FORCE)
                velocity = new Vector2(velocity.x, MIN_JUMP_FORCE);
            _jumpCancel = false;
        }
    }

	void DMovement()
	{
        if (Input.GetKey(KeyCode.LeftArrow))
        {

            // Turn Sprite Left
            YRotation = FACING_LEFT;
			
			if (CurrentSpeed > 0f) {
				CurrentSpeed = CurrentSpeed - DECELERATION;
			} else {
				if (CurrentSpeed > -TOP_SPEED)
				{
					CurrentSpeed = CurrentSpeed - ACCELERATION;
				}else{
					CurrentSpeed = -TOP_SPEED;
				}
			}

        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {

            // Turn Sprite Right
            YRotation = FACING_RIGHT;
			
			if (CurrentSpeed < 0f) {
                CurrentSpeed = CurrentSpeed + DECELERATION;
			} else {
				if (CurrentSpeed < TOP_SPEED)
				{
					CurrentSpeed = CurrentSpeed + ACCELERATION;
				}else{
					CurrentSpeed = TOP_SPEED;
				}
			}
			
		} else {
			CurrentSpeed = CurrentSpeed - (Mathf.Min(Mathf.Abs(CurrentSpeed), FRICTION)*Mathf.Sign(CurrentSpeed));
		}

        

		velocity = new Vector2 (Mathf.Abs(CurrentSpeed), velocity.y);		
		
	}

	void UpdateAnimations()
	{
		Animator anim = GetComponent<Animator> ();
		anim.SetFloat ("Speed", Mathf.Abs(CurrentSpeed));
		anim.SetBool ("Jumping", _jumping);
        anim.SetFloat("EdgeDistance", _edgeDistance);
        anim.SetBool("EdgeInfront", _edgeInfront);
        anim.SetBool("EdgeBehind", _edgeBehind);
	}

    void ApplyGravity()
    {
        // Not grounded apply gravity
        if (!grounded)
        {
            velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - GRAVITY, -MAX_FALL_VELOCITY));
        }
        // Were falling
        if (velocity.y < 0)
        {
            falling = true;
        }
    }

	void Collision()
	{
		Vector3 vGaStart, vGbStart;

		if (grounded || falling) {

            // Check if we are jumping, if so change the width of the Sensors
            // Also check the roataion and swap the A and B sensors depending on what way we are facing
            switch(_jumping)
            {
            case true:
                    vGaStart = new Vector3(box.center.x + 7,box.center.y,transform.position.z);
                    vGbStart = new Vector3(box.center.x - 7,box.center.y,transform.position.z);
                break;
            default:
					vGaStart = new Vector3(box.center.x + 9,box.center.y,transform.position.z);
					vGbStart = new Vector3(box.center.x - 9,box.center.y,transform.position.z);
                break;
            }

            RaycastHit hGa, hGb;

            // TODO: Fix this
            // This may need to be revised - This is what was causing the bouncing issue, but dividing the velocity has helped
            float distance = (box.height / 2) +(grounded? margin: Mathf.Abs (velocity.y)/1.5f);


			// No were not connected, no yet anyway
			bool bGaConnected = false;
			bool bGbConnected = false;

			// Make the ray vectors
			Ray rGa = new Ray(vGaStart,Vector3.down);
			Ray rGb = new Ray(vGbStart,Vector3.down);

			// Debug this shiz
			Debug.DrawRay(vGaStart,Vector3.down * distance,Color.green,2f);
			Debug.DrawRay(vGbStart,Vector3.down * distance,Color.green,2f);

			bGaConnected = Physics.Raycast(rGa, out hGa, distance,1 << lmGround); // Shoot out Ray A and set layer mask to ground
			bGbConnected = Physics.Raycast(rGb, out hGb, distance,1 << lmGround); // Shoot out Ray B and set layer mask to ground

			// If anything collides were on the floor
			if (bGaConnected || bGbConnected)
			{
           
                _jumping = false;
				grounded = true;
				falling = false;
			
				// Store out the sensor values
				if (hGa.collider != null) {SensorGroundA = hGa.point;}
				if (hGb.collider != null) {SensorGroundB = hGb.point;}

                // Fixes a weird bug where a and b although the same height seem to give different distances
                if (hGa.distance > hGb.distance)
                {
                    transform.Translate(Vector3.down * (hGa.distance - box.height / 2)); // Places the transform on the ground

                }
                else if (hGa.distance < hGb.distance)
                {
                    transform.Translate(Vector3.down * (hGb.distance - box.height / 2)); // Places the transform on the ground
                }
                velocity = new Vector2(velocity.x, 0);


			}
            else
            {
                grounded = false;
            }

			// Edge detection for the Balancing Animations
			if (!bGaConnected && bGbConnected && YRotation == FACING_RIGHT && !_jumping)         // We are facing right and edge is infront of us
			{
                _edgeInfront = true;
                _edgeBehind = false;
				_edgeDistance = Mathf.Abs(((SensorGroundA.x - hGb.point.x) / 2));
			}
            else if (!bGaConnected && bGbConnected && YRotation == FACING_LEFT && !_jumping)    // We are facing right and the ledge is behind us
            {
                _edgeInfront = false;
                _edgeBehind = true;
                _edgeDistance = Mathf.Abs(((SensorGroundA.x - hGb.point.x) / 2));
            }
            else if (bGaConnected && !bGbConnected && YRotation == FACING_LEFT && !_jumping)   // We are facing left and the ledge is infront of us
            {
                _edgeInfront = true;
                _edgeBehind = false;
                _edgeDistance = Mathf.Abs(((SensorGroundB.x - hGa.point.x) / 2));
            }
            else if (bGaConnected && !bGbConnected && YRotation == FACING_RIGHT && !_jumping)  // We are facing right and the ledge is behind us
            {
                _edgeInfront = false;
                _edgeBehind = true;
                _edgeDistance = Mathf.Abs(((SensorGroundB.x - hGa.point.x) / 2));
            }
            else
            {
                _edgeInfront = false;
                _edgeBehind = false;
            }
		}
	}

}