using UnityEngine;
using System.Collections;

public class Shooter : MonoBehaviour {

    public Transform bullet;

    private bool canShoot = true;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetButton("Fire1") && canShoot)
        {
            Transform shot = GameObject.Instantiate(bullet) as Transform;
            shot.position = Camera.main.transform.position + Camera.main.transform.forward * 10;
            shot.GetComponent<Rigidbody>().velocity = Camera.main.transform.forward * 200;
            canShoot = false;
        }

        if (!Input.GetButton("Fire1"))
            canShoot = true;
	
	}
}
