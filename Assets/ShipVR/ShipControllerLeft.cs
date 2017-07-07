using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipControllerLeft : MonoBehaviour {

	// Use this for initialization
	void Start () {
        
       // Camera.main.farClipPlane = Camera.main.farClipPlane * 2;

    }

    // Update is called once per frame
    void Update () {
		
	}

    private SteamVR_TrackedController _controller;
    
    private void OnEnable()
    {
        _controller = GetComponent<SteamVR_TrackedController>();
        _controller.TriggerClicked += HandleTriggerClicked;
    }

    private void HandleTriggerClicked(object sender, ClickedEventArgs e)
    {
        //Debug.Log("left");
        GameObject.Find("Player").GetComponent<Rigidbody>().AddForce(transform.forward * 100,ForceMode.Impulse);
    }
}
