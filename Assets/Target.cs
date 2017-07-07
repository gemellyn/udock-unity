using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour {

    private Transform player;

	// Use this for initialization
	void Start () {
        player = GameObject.Find("Player").transform;
            
	}
	
	// Update is called once per frame
	void Update () {
            
	}

    public void go(Vector3 direction)
    {
        RaycastHit hit;
        //int layerMask = 0;//0x01 << 8;
        if (Physics.Raycast(transform.position, direction, out hit, 10000.0f))
        {
            transform.position = hit.point + hit.normal * (transform.localScale.y/1.8f);
        }
        else
            Destroy(this.gameObject);
    }

    void OnTriggerEnter(Collider col)
    {
        Debug.Log(col.gameObject.transform +  " and " + player);
        this.GetComponent<Renderer>().enabled = false;
        this.GetComponent<Collider>().enabled = false;
        if (col.gameObject.transform == player || col.gameObject.GetComponent<BulletBase>() != null)
        {
            player.GetComponent<ComboManager>().gotTarget();
            this.GetComponent<AudioSource>().Play();
        }
        
    }
}
