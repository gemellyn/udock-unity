using UnityEngine;
using System.Collections;

public class SoundUpdater : MonoBehaviour {

    Grainer grainer = null;
    AudioLowPassFilter lpFilter = null;
    CharacterMotorAllGrav motor = null;
    public float zStart = 0.0f;
    public float zEnd = 1000.0f;
    public float vMax = 300.0f;

    public void setZRange(float zStart, float zEnd)
    {
        this.zStart = zStart;
        this.zEnd = zEnd;
    }


	// Use this for initialization
	void Start () {
        grainer = transform.GetComponent<Grainer>();
        motor = transform.GetComponent<CharacterMotorAllGrav>();
        lpFilter = transform.GetComponent<AudioLowPassFilter>();
	}
	
	// Update is called once per frame
	void Update () {
        grainer.SetOffset((int)((grainer.SourceClip.samples / (zEnd - 400 - zStart)) * (transform.position.z - zStart)));

        if (motor.getTimeFly() > 0.5f)
            lpFilter.cutoffFrequency = System.Math.Max(1000.0f / motor.getTimeFly()-0.4f,100.0f);
        else
            lpFilter.cutoffFrequency = System.Math.Min(22000.0f,lpFilter.cutoffFrequency+Time.deltaTime * 7000.0f);
        //grainer.setLPCutoff(1.0f - (motor.getSpeed().magnitude / vMax));
        //Debug.Log(motor.getSpeed().magnitude);
	} 
}
