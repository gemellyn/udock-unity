using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterMotorAllGrav))]

public class FPSInputController : MonoBehaviour {

    private CharacterMotorAllGrav motor;

	// Use this for initialization
	void Start () {
        Cursor.visible = false;
	}

    // Use this for initialization
	void Awake () {
        motor = GetComponent<CharacterMotorAllGrav>();
	
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 directionVector = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
        
        //Debug.Log("Direction globale : " + transform.forward + " locale " + transform.InverseTransformDirection(transform.forward) + " test : " + transform.TransformDirection(Vector3.forward));
	
	    if (directionVector != Vector3.zero) {

            //Debug.Log("Direction  : " + directionVector);

		    // Get the length of the directon vector and then normalize it
		    // Dividing by the length is cheaper than normalizing when we already have the length anyway
		    float directionLength = directionVector.magnitude;
		    directionVector = directionVector / directionLength;
		
		    // Make sure the length is no bigger than 1
		    directionLength = Mathf.Min(1, directionLength);
		
		    // Make the input vector more sensitive towards the extremes and less sensitive in the middle
		    // This makes it easier to control slow speeds when using analog sticks
		    directionLength = directionLength * directionLength;
		
		    // Multiply the normalized direction vector by the modified length
		    directionVector = directionVector * directionLength;
	    }

        //On verifie si veut faire tourner la cam
        motor.setInputBend(Input.GetAxis("Bend"));
	
	    // Apply the direction to the CharacterMotor
	    motor.setInputMoveDir(directionVector);
	    motor.setInputJump(Input.GetButton("Jump"));
	}

 
}
