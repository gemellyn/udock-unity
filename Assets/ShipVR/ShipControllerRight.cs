using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipControllerRight : MonoBehaviour {

	// Use this for initialization
	void Start () {
        
        Camera.main.farClipPlane = Camera.main.farClipPlane * 10;

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
        GameObject.Find("Player").GetComponent<Rigidbody>().velocity = GameObject.Find("Player").GetComponent<Rigidbody>().velocity / 4.0f;
        GameObject.Find("Player").GetComponent<Rigidbody>().angularVelocity = GameObject.Find("Player").GetComponent<Rigidbody>().angularVelocity / 4.0f;
        //Debug.Log("right");
    }
}
