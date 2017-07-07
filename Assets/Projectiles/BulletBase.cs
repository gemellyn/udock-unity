using UnityEngine;
using System.Collections;

public class BulletBase : MonoBehaviour {

    public float lifeTime = 5.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.LookAt(Camera.main.transform.position);
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            GameObject.Destroy(this.gameObject);
        }
	}

    void OnCollisionEnter(Collision collision)
    {
        GameObject.Destroy(this.gameObject);
    }
}
