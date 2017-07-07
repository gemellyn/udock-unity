using UnityEngine;
using System.Collections;

public class CharacterMotorAllGrav : MonoBehaviour {

    public bool noGravWhenGrounded = true;
    public bool snapUpToNormal = true;
    public float baseGravity = 10.0f;
    public float acceleration = 20.0f;
    public float damping = 0.5f;
    public float jumpForce = 100.0f;
    public float moveForce = 2.0f;
    public float rotateSpeed = 30.0f;
    public const float timeGrounded = 0.2f;
    private float timerGrounded = timeGrounded;
    public float dashForce = 150.0f;
    public const float timeDashEffect = 0.5f;
    private float timerDashEffect = timeDashEffect;
    public const float timeDelayDash = 0.5f;
    private float timerDelayDash = timeDelayDash;
    private float baseDashBlurAmount = 0.0f;

    private float timerFly = 0.0f;
   

    private float gravity = 10.0f;
    private Vector3 velocity = Vector3.zero;
    //private bool isJumping = false;
    private bool isGrounded = false;
    private Vector3 groundNormal = Vector3.up;
    //private bool hasJumped = false;

    private Vector3 inputMoveDirection = Vector3.zero;
    private bool inputJump = false;
    private float inputBend = 0.0f;
    //private bool canRotate = true;
    //private bool updateGrav = false;
    private bool canJump = true;
    private bool canDash = false;
    private int nbDash = 0;
    private int nbJump = 0;

    public Vector3 getSpeed()
    {
        return velocity;
    }
    

    void OnControllerColliderHit (ControllerColliderHit hit) {
        groundNormal = hit.normal;

        Vector3 trNormal = transform.InverseTransformDirection(groundNormal);
        if (trNormal.y > 0.01)
        {
            isGrounded = true;
            timerGrounded = timeGrounded;
            //updateGrav = false;
        }

    }

	// Use this for initialization
	void Start () {
        //baseDashBlurAmount = Camera.main.GetComponent<MotionBlur>().blurAmount;
	}

    public float getTimeFly()
    {
        return timerFly;
    }

    public void setInputMoveDir(Vector3 dir)
    {
        inputMoveDirection = dir;
    }

    public void setInputJump(bool jmp)
    {
        inputJump = jmp;
    }

    public void setInputBend(float bend)
    {
        inputBend = bend;
    }

    Vector3 getInputMovement()
    {
        Vector3 inputJumpForce = new Vector3();

        if (isGrounded)
        {
            canDash = false;
            nbDash = 1;
            nbJump = 1;
        }

        //Dash
        if (inputJump && canDash && nbDash > 0 && !isGrounded && timerDelayDash <= 0)
        {
            inputJumpForce += transform.forward * dashForce;
            nbDash--;
            timerDashEffect = timeDashEffect;
        }

        if (inputJump && isGrounded && canJump && nbJump > 0)
        {
            inputJumpForce += transform.up * jumpForce;
            nbJump--;
            isGrounded = false;
            //hasJumped = true;
            //updateGrav = true;
            canJump = false;
        }

        if (inputJump == false)
        {
            if (!isGrounded && nbDash > 0)
            {
                canDash = true;
            }
            canJump = true;
        }

        

        return inputMoveDirection * moveForce + inputJumpForce;
    }
	

	// Update is called once per frame
	void FixedUpdate () {

        //on applique rotation de la cam en fonction du bend
        /*if (inputBend != 0)
        {
            if (canRotate)
            {
                canRotate = false;
                transform.Rotate(Vector3.forward, -inputBend * 90);

            }

        }
        else
            canRotate = true;*/

        Vector3 newVelocity = velocity;

        //Debug.Log("update ------------------------ ");

        //Debug.Log("base velocity : " + newVelocity);

        //On verifie si on est grounded
        timerGrounded -= Time.fixedDeltaTime;
        if (timerGrounded <= 0.0f)
            isGrounded = false;

        if (timerDelayDash > 0)
            timerDelayDash -= Time.fixedDeltaTime;

        if (timerDashEffect >= 0)
        {
            timerDashEffect -= Time.fixedDeltaTime;
            //Camera.main.GetComponent<MotionBlur>().enabled = true;
            //Camera.main.GetComponent<MotionBlur>().blurAmount = (timerDashEffect / timeDashEffect) * baseDashBlurAmount;
        }
        //else
            //Camera.main.GetComponent<MotionBlur>().enabled = false;




        if (!isGrounded)
        {
            transform.Rotate(Vector3.forward, -inputBend * rotateSpeed * Time.fixedDeltaTime);
            transform.LookAt(transform.position + Camera.main.transform.forward, Camera.main.transform.up);
            Camera.main.transform.LookAt(transform.position + transform.forward, transform.up);
            gravity = baseGravity;
            //Debug.Log("new gravity ! " + (transform.up*-1));
        }
        else
        {
            if (snapUpToNormal)
            {

            }
        }

        

        

        /*if (inputBend < 0)
        {
            //transform.Rotate(Vector3.forward, -inputBend * rotateSpeed * Time.fixedDeltaTime);
            transform.LookAt(transform.position + Camera.main.transform.forward, Camera.main.transform.up);
            Camera.main.transform.LookAt(transform.position + transform.forward, transform.up);
        }*/

       

        //float verticalMovement = Mathf.Abs(Vector3.Dot(transform.up,velocity.normalized));
        //Debug.Log(verticalMovement + " : " + isGrounded);

        if (Input.GetButton("Fire2"))
        {
            gravity = 0;
            newVelocity *= 0.98f;
        }
        else
        {
            //On applique les frottements : uniquement sur le sens de deplacement
            //On change donc de repère: on passe la velocite dans le repere du perso
            Vector3 trVelocity = transform.InverseTransformDirection(newVelocity);
            //Debug.Log("Local vel : " + trVelocity + " glob vel : " + newVelocity);
            //On damp sur x et y uniquement pour garder le mouvement vertical
            trVelocity.x *= damping;
            trVelocity.z *= damping;
            newVelocity = transform.TransformDirection(trVelocity);
        }

        

       // Debug.Log("damped velocity : " + newVelocity);

        //Debug.Log("Local vel damped : " + trVelocity + " glob vel damped : " + newVelocity);

       // newVelocity *= damping;
        

        //On applique la gravite
        if(!(noGravWhenGrounded && isGrounded))
            newVelocity += gravity * (transform.up * -1) * Time.fixedDeltaTime;

        //Debug.Log(transform.up);

        //On applique les controles
        Vector3 inputMovement = getInputMovement();

        if (inputMovement != Vector3.zero && !Input.GetButton("Fire2"))
        {
            //Debug.Log("inputMovement  : " + inputMovement + " (puiss: " + inputMovement.magnitude + ", old velocity : " + newVelocity + ")");
            newVelocity += inputMovement * acceleration * Time.fixedDeltaTime;
        }   

        //Debug.Log("after input velocity : " + newVelocity);

        
        
        //On se déplace
        Vector3 lastPosition = transform.position;
        Vector3 movement = newVelocity * Time.fixedDeltaTime;
        GetComponent<CharacterController>().Move(movement);
        Vector3 actualVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        velocity = actualVelocity;
                
        //hasJumped = false;

        //Update timer fly
        timerFly += Time.deltaTime;
        if (isGrounded)
            timerFly = 0.0f;
	
	}
}
