﻿using UnityEngine;
using System.Collections;

[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook : MonoBehaviour {

	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

	float rotationY = 0.0f;

    void Awake()
    {
        
    }

	void Update ()
	{
		if (axes == RotationAxes.MouseXAndY)
		{
            transform.Rotate(Vector3.up,Input.GetAxis("Mouse X") * sensitivityX);
            transform.Rotate(Vector3.right,Input.GetAxis("Mouse Y") * sensitivityY);
		}
		else if (axes == RotationAxes.MouseX)
		{
            transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
		}
		else
		{    
            rotationY = transform.localEulerAngles.x;
            if (rotationY > 180)
                rotationY = rotationY - 360;
            rotationY -= Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
            transform.localEulerAngles = new Vector3(rotationY, transform.localEulerAngles.y, transform.localEulerAngles.z);
		}
	}
	
	void Start ()
	{
		// Make the rigid body not change rotation
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
	}
}

