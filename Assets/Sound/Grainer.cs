/*
	Creation :  06 / 2014
	Author : Maxime GILLET
*/
using UnityEngine;
using System.Collections;

/*******************
* Class description:
*
*
*
*
*
*
*
**/

[RequireComponent(typeof(AudioSource))]
public class Grainer : MonoBehaviour {


	public AudioClip SourceClip;			//The Audio CLip source from which we pick the grains
	public int Offset;						//Position where to pick the grains
	public int Delta;						//Delta randomisation rate from Offset
	public int samplesInOneGrain = 2048;	//Number of Samples in one Grain
	public int QueueLength = 30;			//Length of the intern buffer queue (pre-DSP)
	public float Covering = 0.1f;			//Overlap between grains
	public int FadeFunction = 0;			//IDN of the envelope function chosen in inspector
	public bool toggleView = false;			//Display the internBuffer view
	public int MaxSamplesInOneGrain = 10000;		//Maximum Samples in One Grain (default is 1% of the total number of samples in the SourceClip)
	public int MinSamplesInOneGrain = 256;		//Minimum Samples in One Grain (default is 256)
	
	int sampleRate;							//DSP Sample Rate (44100, 48000 ...)
	float[] GrainData;						//Temporary stored data of the grain
	float[] internBuffer;					//Circular Buffer Data (pre-DSP)
	int internBufferSize;					//The size in samples of the intern Circular Buffer (pre-DSP)
	int DSPBufferSize;						//The size of the DSP Buffer (minimum size of the Circular Buffer)
	int ptReadBuffer = 0;					//Read pointer in the Circular Buffer
	int ptWriteBuffer = 0;					//Write pointer in the Circular Buffer
	int ptReadGrain = 0;					//Read pointer in the Grain
	AudioClip internBufferClip;				//Output Clip used to send Data to DSP

	private float[] FadeIN;					//FadeIN curve data	
	private float[] FadeOUT;				//FadeOUT curve data
	
	//debug
	Texture2D internBufferView;
	
	//Setter of the offset
	public void SetOffset(int offset){
		if(offset < this.Delta/2)
			this.Offset = this.Delta/2;
		else if(offset > SourceClip.samples - this.Delta/2)
			this.Offset = SourceClip.samples - this.Delta/2;
		else
			this.Offset = offset;
	
	}
	
	//Setter of the Delta
	public void SetDelta(int delta){
		if(delta > SourceClip.samples)
			this.Delta = SourceClip.samples;
		else if(delta < 1)
			this.Delta = 1;
		else
			this.Delta = delta;
	
	}
	//Setter of the covering between grains
	public void SetCovering(float cover){
		if(cover > 0.9f)
			this.Covering = 0.9f;
		else if(cover < 0.01f)
			this.Covering = 0.01f;
		else
			this.Covering = cover;
	
	}
	
	//Called on awake
	void Awake(){
	
		GetComponent<AudioSource>().Stop();
	}
	
	//Called on creation , used for the initiation
    void Start() {
		InitGrainer();
		
    }
	
	//Set Default parameters
	void InitGrainer(){
	
		DSPBufferSize = 2048;
		//Offset =(int) SourceClip.samples/10;
		//Delta = (int) SourceClip.samples/50;
		internBufferSize = DSPBufferSize*QueueLength;
		internBuffer = new float[internBufferSize];
		//samplesInOneGrain = 5000;
		MaxSamplesInOneGrain = (int)SourceClip.samples/10;
		MinSamplesInOneGrain = 256;
		GrainData = new float[MaxSamplesInOneGrain];

		AudioSettings.outputSampleRate = SourceClip.frequency;
		if(SourceClip.channels == 1)
			AudioSettings.speakerMode = AudioSpeakerMode.Mono;
		else AudioSettings.speakerMode = AudioSpeakerMode.Stereo;
		sampleRate = SourceClip.frequency;
		
        internBufferClip = AudioClip.Create("internBuffer", internBufferSize, SourceClip.channels, sampleRate, false, true, OnAudioRead);
		
        GetComponent<AudioSource>().clip = internBufferClip;
        GetComponent<AudioSource>().Play();
		GetComponent<AudioSource>().loop = true;
		GetComponent<AudioSource>().volume = 1f;
		
		ResetEnvelopes(Covering);
	
	}
	
	//Called once per frame
	void Update(){
		//Checking Boundaries
		SetCovering(Covering);
		SetOffset(Offset);
		SetDelta(Delta);
		
		//Displaying the texture and drawing the pixels is putting fps down.
		Destroy(internBufferView);
		if(toggleView){
			internBufferView = new Texture2D(800,100);
			
			for( int i = 0; i < internBufferSize; i++){
			
				internBufferView.SetPixel((int) (800 * i/internBufferSize),(int) (50*5f*(internBuffer[i]+1f)),new Color (255,165,0));
			}
			for( int i = 0; i < 100; i++){
			
				internBufferView.SetPixel( 800*ptReadBuffer/internBufferSize,i,Color.blue);
			}
			internBufferView.Apply();
			
			for( int i = 0; i < 100; i++){
			
				internBufferView.SetPixel( 800*ptWriteBuffer/internBufferSize,i,Color.red);
			}
			internBufferView.Apply();	
		}
	
		ResetEnvelopes(Covering);
		FillInternBuffer(samplesInOneGrain);

		
	}
	
	//Fill the intern (pre-DSP) Circular Buffer with grains
	void FillInternBuffer(int samples){

        while (ptWriteBuffer + ptReadGrain < ptReadBuffer)
		{
			while(ptReadGrain < samples)
			{
				if(ptWriteBuffer+ptReadGrain >= ptReadBuffer)
					break;
				else{
					internBuffer[ (ptWriteBuffer+ptReadGrain)%internBufferSize] += GrainData[ptReadGrain];
					ptReadGrain ++;
				}

			}
			
			if(ptReadGrain >= samples)
			{
				ptReadGrain = 0;
				SourceClip.GetData(GrainData, Offset + (int) Mathf.Floor(Random.Range(-Delta/2,+Delta/2)));
				
				ApplyEnvelope(samples);
				ptWriteBuffer = ptWriteBuffer + samples - FadeOUT.Length;
			}
			
			if( ptWriteBuffer >= internBufferSize  && ptReadBuffer >= internBufferSize){
			
				ptWriteBuffer = ptWriteBuffer%internBufferSize;
				ptReadBuffer = ptReadBuffer%internBufferSize;
			}
		
		}	
	}
	
	//Apply envelope regarding the number of channels in the script, related to how the data is stored with GetData() for multiple channels.
	void ApplyEnvelope(int samples){
		/*
		for(int step = 0; step < SourceClip.channels; step++){
		
			for(int i = step; i < SourceClip.channels*FadeIN.Length; i+=SourceClip.channels){
				
				GrainData[i] = GrainData[i]*FadeIN[i/SourceClip.channels];
			
			}
			for(int i = step; i < SourceClip.channels*FadeOUT.Length; i+=SourceClip.channels){
				
				GrainData[samples - SourceClip.channels*FadeOUT.Length + i] = GrainData[samples - SourceClip.channels*FadeOUT.Length + i]*FadeOUT[i/SourceClip.channels];
			}
		}*/
		
		for(int i = 0; i < FadeIN.Length; i++){
				
			GrainData[i] = GrainData[i]*FadeIN[i];
			
		}
		for(int i = 0; i < FadeOUT.Length; i++){
				
			GrainData[samples - FadeOUT.Length + i] = GrainData[samples - FadeOUT.Length + i]*FadeOUT[i];
		}
	}
	
	//Called every time a chunk of data is read in the output clip
    void OnAudioRead(float[] data) {
		// Debug.Log(data.Length);
		int count = 0;
        while (count < data.Length) {
            data[count] = internBuffer[ (ptReadBuffer+count)%internBufferSize];
			internBuffer[ (ptReadBuffer+count)%internBufferSize] = 0;
            count++;
        }
		ptReadBuffer = ptReadBuffer + data.Length ;
    }
	
	//Reset the Size of the envelopes
	void ResetEnvelopes(float ratio){
	
		FadeIN = new float[(int)Mathf.Round(samplesInOneGrain * ratio)];
		FadeOUT = new float[(int)Mathf.Round(samplesInOneGrain * ratio)];

		//envelope generation
		for (int j =0; j< FadeIN.Length;j++){
			switch (FadeFunction){
				case 0:
					//Log
					FadeIN[j] = Mathf.Log( 1+ 1.7f*j/Mathf.Round(samplesInOneGrain * ratio));
					break;
				case 1:
					//Gaussian
					FadeIN[j] =  Mathf.Exp( -(2.3f*j/Mathf.Round(samplesInOneGrain * ratio) -2.3f)* (2.3f*j/Mathf.Round(samplesInOneGrain * ratio)-2.3f) );
					break;
				case 2:
					//Hyperbolic tan
					FadeIN[j] =(float)(System.Math.Tanh(5*j/Mathf.Round(samplesInOneGrain * ratio)-2.4)+1f)/2f; 
					break;
				case 3:
					//sin*tanh
					FadeIN[j] =(float)System.Math.Tanh(j/Mathf.Round(samplesInOneGrain * ratio))*Mathf.Sin(1.8f*j/Mathf.Round(samplesInOneGrain * ratio))*1.34f;
					break;
				case 4:
					FadeIN[j]= Mathf.Atan(20f*j/Mathf.Round(samplesInOneGrain * ratio)-10f)/3f+0.5f;
					break;
			}
		}
			
		for (int j =0; j< FadeOUT.Length;j++){
			switch (FadeFunction){
				case 0:
					//Log
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * ratio)-j-1] = Mathf.Log( 1+ 1.7f*j/Mathf.Round(samplesInOneGrain * ratio));
					break;
				case 1:
					//Gaussian
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * ratio)-j-1] = Mathf.Exp( -(2.3f*j/Mathf.Round(samplesInOneGrain * ratio) -2.3f)* (2.3f*j/Mathf.Round(samplesInOneGrain * ratio)-2.3f) );
					break;
				case 2:
					//Hyperbolic tan
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * ratio)-j-1] = (float)(System.Math.Tanh(5*j/Mathf.Round(samplesInOneGrain * ratio)-2.4)+1f)/2f;
					break;
				case 3:
					//sin*tanh
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * ratio)-j-1] =(float)System.Math.Tanh(j/Mathf.Round(samplesInOneGrain * ratio))*Mathf.Sin(1.8f*j/Mathf.Round(samplesInOneGrain * ratio))*1.34f;
					break;
				case 4:
					//Arctan
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * ratio)-j-1] = Mathf.Atan(20f*j/Mathf.Round(samplesInOneGrain * ratio)-10f)/3f+0.5f;
					break;
			}
		}
	
	}
	
	void OnGUI(){
		if(toggleView){
			if(GUILayout.Button("Play"))GetComponent<AudioSource>().Play();
			if(GUILayout.Button("Stop"))GetComponent<AudioSource>().Stop();
			if(internBufferView)GUILayout.Label(internBufferView);
		}
	
	}
	
	//Safe exit
	void OnApplicationQuit() {
		GetComponent<AudioSource>().Stop();
	}
	
}