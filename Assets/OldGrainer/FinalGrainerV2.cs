/*
	Creation :  05 / 2014
	Author : Maxime GILLET
*/

using UnityEngine;
using System.Collections;

/*
* Class description:
*
*
*
*
*
*
*
*/

[RequireComponent(typeof(AudioSource))]
public class FinalGrainerV2 : MonoBehaviour {

	/*Hidden public variables called in Editor*/
	public AudioClip clip;				//AudioClip source from which we get the grains
	public int offset = 204800;			//Offset cursor positioning the delta and where the grains will be taken from (counted in samples)
	public int delta = 10240;			//Range in which we can chose the grains (counted in samples)
	public int samplesInOneGrain = 256;	//Number of samples in one grain
	public float covering;				//Covering/overlap between the grains
	
	private float[] Grain;				//Grain data extracted from the source clip
	private float[][] AudioBuffer;		//Size of all audio float[] should be multiples of 2
	private float[] AudioBufferLeft;	//Intermediate buffer used to split data and apply filters and envelopes on each waveform.
	private float[] AudioBufferRight;	//Intermediate buffer used to split data and apply filters and envelopes on each waveform.
	private int bufferSize;				//size of the unity buffer passed in OnAudioFilterRead();
	private AudioClip bufferClip;
	private int writePointer;
	private float[] save;				//Data saved to keep continuity between buffers
	private float[] FadeIN;				//FadeIN curve data	
	private float[] FadeOUT;			//FadeOUT curve data
	public int FadeFunction;			//IDN of the envelope function chosen in inspector
	
	/*Hidden public variables called in Editor*/
	public int LPcutoff ;				//Low-pass filter Cut-off frequency
	public float LPQ ;					//Low-pass Quality factor
	public int HPcutoff ;				//High-pass filter Cut-off frequency 
	public float HPQ ;					//High-pass quality factor
	public int BPcutoff;				//Band-pass Cut-off frequency
	public float BPQ;					//Band-pass quality factor
	public bool toggleBP;				//toggle to switch to Band-pass filter
	public  int totalSamples;			//Total samples in the source clip
	public float g;						//Gain applied at OnAudioFilterRead()
	
	
	private int First;					//Flag to initiate the write pointer at 0
	private int counter;				//Number of different grains saved to keep continuity (used for debug)
	private int lastRank;				//lastRank used to know where to start when we fill a buffer with grains
	private int queueLength;			//Number of buffers in the queue
	private int nextBufferToRead = 0;   //le prochain buffer qui doit etre lu par le DSP
    private int nextBufferToGenerate = 0;//le prochain buffer qui doit etre genere dans le update
    private int nbBuffersReady = 0;		//Combien de buffers sont generes
	//debug:
	public bool toggleView;
	Texture2D bufferView;
	Texture2D env;
	Texture2D sum;


    public void setOffet(int offset)
    {
        if (offset <= delta / 2)
            offset = delta / 2 + 1;

        offset = offset % totalSamples;

        this.offset = offset;

        //Debug.Log(offset);

    }

    public void setLPCutoff(float cutoff)
    {
        if (cutoff < 10)
            cutoff = 10;

        if (cutoff > 1.0f)
            cutoff = 1.0f;

        cutoff = cutoff * 5000.0f;

        this.LPcutoff = (int)cutoff;

        //Debug.Log(offset);

    }


	void Awake(){
	
		GetComponent<AudioSource>().Stop();
	}
	
	// Use this for initialization
	void Start () {
		
		//Values/Parameters initialization
		queueLength = 2;
		offset = 204800;	
		delta = 10240;
		covering = 0.3f;
		First = 0;
		g = 1f;
		HPQ = LPQ = BPQ = 1;
		LPcutoff = 5000;
		lastRank=0;
		
		//Specifying the original clip
        clip = Resources.Load("snd_background_1") as AudioClip;
		totalSamples = clip.samples;
		
		/*debug*/
		bufferView = new Texture2D(500, 100);
		AudioBuffer = new float[queueLength][];
		
		//Setting DSP parameters
		AudioSettings.outputSampleRate = clip.frequency;
		
		//General Settings
		if(clip.channels == 2){
			AudioSettings.speakerMode = AudioSpeakerMode.Stereo;
			bufferSize = 2048;
			AudioBufferLeft = new float [bufferSize/2 +samplesInOneGrain];
			AudioBufferRight = new float [bufferSize/2 +samplesInOneGrain];
			Grain = new float[2*samplesInOneGrain];
			// Memory allocation:
			for(int i = 0; i < queueLength; i++){
				AudioBuffer[i] = new float[bufferSize+2*samplesInOneGrain];
			}
			save = new float[2*samplesInOneGrain];
		}
		else{
			AudioSettings.speakerMode = AudioSpeakerMode.Mono;
			bufferSize = 1024;
			Grain = new float[samplesInOneGrain];
			
			// Memory allocation:
			for(int i = 0; i < queueLength; i++){
				AudioBuffer[i] = new float[bufferSize+2*samplesInOneGrain];
			}
			save = new float[samplesInOneGrain];
		}
		
		ResetEnvelopes(covering);
		
		//si on veut passer par un clip pour looper les samples je pense qu'il faut forcer à 0 début et fin
		//il faut synchroniser l'update par rapport au temps du clip... sinon les données peuvent être changées pendant la lecture.
		// bufferClip = AudioClip.Create("grainerOUT", 1024, 2 , 44100, false, false);
		// audio.clip=bufferClip;
		

		GetComponent<AudioSource>().Play();
		
	}

	void UpdateBuffer(int q){
	
		ResetEnvelopes(covering);
		Destroy(bufferView);
		bufferView = new Texture2D(500, 100);
		for(int i = 0; i < AudioBuffer[q].Length; i++){
			bufferView.SetPixel(500-500*2*samplesInOneGrain/AudioBuffer[q].Length ,100*i/AudioBuffer[q].Length,Color.red);
		}
		for(int i = 0; i < AudioBuffer[q].Length; i++){
			bufferView.SetPixel(500*2*samplesInOneGrain/AudioBuffer[q].Length ,100*i/AudioBuffer[q].Length,Color.red);
		}
		// clean buffer
		for(int i = 0; i < AudioBuffer[q].Length; i++){
			bufferView.SetPixel((int) (500 * i /AudioBuffer[q].Length ),(int)( 50*5* ( AudioBuffer[q][i]*g+1f)), Color.blue);
			AudioBuffer[q][i] = 0;
		}
		bufferView.Apply();
		
		Destroy(env);
		env = new Texture2D(200,100);
		for(int i = 0; i < FadeIN.Length;i++){
			env.SetPixel( (int)(100*i/FadeIN.Length), (int)( 100*FadeIN[i] ),Color.black);
		}
		for(int i = 0; i < FadeIN.Length;i++){
			env.SetPixel( (int)(100+100*i/FadeOUT.Length), (int)( 100*FadeOUT[i]),Color.black );
		}
		env.Apply();
		
		Destroy(sum);
		sum = new Texture2D(200,100);
		for(int i = 0; i < FadeIN.Length;i++){
			sum.SetPixel( (int)(200*i/FadeIN.Length), (int)((FadeIN[i] + FadeOUT[i])*50 ),Color.black);
		}
		sum.Apply();
		
		//clean grain
		for(int i = 0; i < Grain.Length; i++){
			
			Grain[i] = 0;
		}
		//Fill the buffer with grains
		if(AudioSettings.speakerMode == AudioSpeakerMode.Stereo)FillStereoBuffer();
		else FillMonoBuffer(q);
		
		//Toggle to Band pass filter
		if( toggleBP){
		
			LPQ = HPQ = BPQ;
			//setting window (default width : 100)
			LPcutoff= BPcutoff +50;
			HPcutoff= BPcutoff -50;
		}
		//Apply Low pass filter
		if( LPcutoff < 5000){
			if(AudioSettings.speakerMode == AudioSpeakerMode.Stereo){
				LPF(AudioBufferLeft);
				LPF(AudioBufferRight);
			}
			else LPF(AudioBuffer[q]);
			
		}
		//Apply High pass filter
		if (HPcutoff > 1){
			if(AudioSettings.speakerMode == AudioSpeakerMode.Stereo){
				HPF(AudioBufferLeft);
				HPF(AudioBufferRight);
			}
			else HPF(AudioBuffer[q]);
		}
	
	}
	
	// Update is called once per frame
	void Update () {
		
		if(delta <0 ) delta = 0;
		if(covering > 0.5f ) covering = 0.5f;
		if(covering <=0 ) covering = 0.01f;

	   while (nbBuffersReady < queueLength)
        {
            // Debug.Log("Generating buffer " + nextBufferToGenerate);
            
          	UpdateBuffer(nextBufferToGenerate);
			saveAndPrepareForDSP(nextBufferToGenerate);

            //prepare next buffer target
            nextBufferToGenerate = (nextBufferToGenerate + 1) % queueLength;

            nbBuffersReady++;
        }
		
		// bufferClip.SetData(AudioBuffer[nextBufferToRead],0);
	}
	
	/** OnAudioFilterRead is called everytime a chunk of audio is routed through the filter 
	// (this happens frequently, every ~20ms depending on the samplerate and platform). 
	// The audio data is an array of floats ranging from [-1.0f;1.0f] and contains audio from 
	// the previous filter in the chain or the AudioClip on the AudioSource. If this is the first 
	// filter in the chain and a clip isn't attached to the audio source this filter will be 'played'. 
	// That way you can use the filter as the audio clip, procedurally generating audio. **/
	void OnAudioFilterRead(float[] data, int channels){
		
		if (nbBuffersReady > 0)
        {

            //Send buffer to DSP queue
            for (int i = 0; i < data.Length; i++)
                data[i] += AudioBuffer[nextBufferToRead][i]*g;

           //prepare next buffer target
            nextBufferToRead = (nextBufferToRead + 1) % queueLength;

            nbBuffersReady--;
           
        }
	
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
					FadeIN[j] = Mathf.Log( 1+ 1.7f*j/Mathf.Round(samplesInOneGrain * covering));
					break;
				case 1:
					//Gaussian
					FadeIN[j] =  Mathf.Exp( -(2.3f*j/Mathf.Round(samplesInOneGrain * covering) -2.3f)* (2.3f*j/Mathf.Round(samplesInOneGrain * covering)-2.3f) );
					break;
				case 2:
					//Hyperbolic tan
					FadeIN[j] =(float)(System.Math.Tanh(5*j/Mathf.Round(samplesInOneGrain * covering)-2.4)+1f)/2f; 
					break;
				case 3:
					//sin*tanh
					FadeIN[j] =(float)System.Math.Tanh(j/Mathf.Round(samplesInOneGrain * covering))*Mathf.Sin(1.8f*j/Mathf.Round(samplesInOneGrain * covering))*1.34f;
					break;
				case 4:
					FadeIN[j]= Mathf.Atan(20f*j/Mathf.Round(samplesInOneGrain * covering)-10f)/3f+0.5f;
					break;
			}
		}
			
		for (int j =0; j< FadeOUT.Length;j++){
			switch (FadeFunction){
				case 0:
					//Log
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * covering)-j-1] = Mathf.Log( 1+ 1.7f*j/Mathf.Round(samplesInOneGrain * covering));
					break;
				case 1:
					//Gaussian
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * covering)-j-1] = Mathf.Exp( -(2.3f*j/Mathf.Round(samplesInOneGrain * covering) -2.3f)* (2.3f*j/Mathf.Round(samplesInOneGrain * covering)-2.3f) );
					break;
				case 2:
					//Hyperbolic tan
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * covering)-j-1] = (float)(System.Math.Tanh(5*j/Mathf.Round(samplesInOneGrain * covering)-2.4)+1f)/2f;
					break;
				case 3:
					//sin*tanh
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * covering)-j-1] =(float)System.Math.Tanh(j/Mathf.Round(samplesInOneGrain * covering))*Mathf.Sin(1.8f*j/Mathf.Round(samplesInOneGrain * covering))*1.34f;
					break;
				case 4:
					//Arctan
					FadeOUT[(int)Mathf.Round(samplesInOneGrain * covering)-j-1] = Mathf.Atan(20f*j/Mathf.Round(samplesInOneGrain * covering)-10f)/3f+0.5f;
					break;
			}
		}
	
	}
	
	//reset the size of the grains
	void ResetGrains(int size){
	
		//...
		
	}
	
	//Fill mono buffer with random grains from the clip
	void FillMonoBuffer(int q){
	
		int iterator = 0;	
		bool looping = false;
		int WriteOffset = (samplesInOneGrain - lastRank -(int)Mathf.Round(samplesInOneGrain * covering))*First;
		
		while( (WriteOffset+iterator * (samplesInOneGrain - (int)Mathf.Round(samplesInOneGrain * covering))) < (AudioBuffer[q].Length-samplesInOneGrain)){
			looping = false;
			clip.GetData(Grain,offset + (int) Mathf.Round(Random.Range(-delta/2,+delta/2)));
			//Applying envelop on the extracted grain
			for(int i=0;i < (int)Mathf.Round(samplesInOneGrain * covering);i++){
				Grain[i] = Grain[i]*FadeIN[i] ;
			}		

			for(int i=Grain.Length - (int)Mathf.Round(samplesInOneGrain * covering);i < Grain.Length;i++){
				Grain[i] = Grain[i]*FadeOUT[i-Grain.Length + (int)Mathf.Round(samplesInOneGrain * covering)];
			}
			//set the pointer
			writePointer = WriteOffset+ iterator * (samplesInOneGrain -(int)Mathf.Round(samplesInOneGrain * covering) );
			if(writePointer < 0)writePointer=0;
			//the write pointer can be negative if the covering change a lot from one loop to another, we decide to constraint to 0 :
			//some sound quality might be lost if there is a big variation of covering between updates.
			for(int i = 0; i < samplesInOneGrain ; i++){
				if(writePointer +i < AudioBuffer[q].Length -samplesInOneGrain){
					AudioBuffer[q][writePointer +i] += Grain[i];
				}
				else{
					AudioBuffer[q][writePointer +i] += Grain[i];
					if(looping == false){
						lastRank = i;
						First = 1;
						counter++;
						looping = true;
					}
				}

			}
			iterator ++;
		}
	
	
	}
	
	//Fill stereo buffer with random grains from the clip
	void FillStereoBuffer(){
	
		int iterator = 0;	
		bool looping = false;
		int WriteOffset = (samplesInOneGrain - lastRank -(int)Mathf.Round(samplesInOneGrain * covering))*First;
		for(int i =0 ; i < AudioBufferLeft.Length;i++){
			AudioBufferLeft[i]=0;
			AudioBufferRight[i]=0;
		}
		
		while( WriteOffset+iterator * (samplesInOneGrain - (int)Mathf.Round(samplesInOneGrain * covering)) < (AudioBufferLeft.Length-samplesInOneGrain)){
			looping = false;
			clip.GetData(Grain,offset + (int) Mathf.Round(Random.Range(-delta/2,+delta/2)));
			//Applying envelope on the extracted grain
			
			/*
			*	Fade IN
			*/
			
			//Left Stereo Grain
			for(int i=0;i < (int)Mathf.Round(samplesInOneGrain * covering);i++){
				Grain[2*i] = Grain[2*i]*FadeIN[i] ;
			}	
			//Right Stereo Grain			
			for(int i=0;i < (int)Mathf.Round(samplesInOneGrain * covering);i++){
				Grain[2*i+1] = Grain[2*i+1]*FadeIN[i] ;
			}
			
			/*
			*	Fade OUT
			*/
			
			//Left Stereo Grain
			for(int i=0;i < (int)Mathf.Round(samplesInOneGrain * covering);i++){
				Grain[Grain.Length - 2*(int)Mathf.Round(samplesInOneGrain * covering)+2*i] = Grain[Grain.Length - 2*(int)Mathf.Round(samplesInOneGrain * covering)+2*i]*FadeOUT[i];
			}
			//Right Stereo Grain
			for(int i=0;i < (int)Mathf.Round(samplesInOneGrain * covering);i++){
				Grain[Grain.Length - 2*(int)Mathf.Round(samplesInOneGrain * covering)+2*i+1] = Grain[Grain.Length - 2*(int)Mathf.Round(samplesInOneGrain * covering)+2*i+1]*FadeOUT[i];
			}
			
			/*
			*	Queueing Grains
			*/
			
			//set the pointer
			writePointer = WriteOffset+ iterator * (samplesInOneGrain -(int)Mathf.Round(samplesInOneGrain * covering) );
			if(writePointer < 0)writePointer=0;
			//the write pointer could be negative if the covering change a lot from one loop to another, we decide to constraint the minimum offset 0 :
			//some sound quality/integrity will be potentially lost if there is a big variation of covering between updates.
			
			//Left stereo
			for(int i = 0; i < samplesInOneGrain ; i++){
				if(writePointer +i < AudioBufferLeft.Length -samplesInOneGrain){
					AudioBufferLeft[writePointer +i] += Grain[2*i];
					AudioBufferRight[writePointer +i] += Grain[2*i+1];

				}
				else{
					AudioBufferLeft[writePointer +i] += Grain[2*i];
					AudioBufferRight[writePointer +i] += Grain[2*i+1];
					if(looping == false){
						lastRank = i;
						First = 1;
						counter++;
						looping = true;
					}
				}

			}
			iterator ++;
		}
	
	}
	
	//Low pass filter
	void LPF(float[] buf){
		// filter parameters
		float O = 2.0f * Mathf.PI * LPcutoff / clip.frequency; 
		float C = LPQ / O;
		float L = 1 / LPQ / O;
		float V = 0f, I=0f,T;
		for( int s = 0; s < buf.Length ; s++){
			T = (I - V) / C;
			I += (buf[s] * O - V) / L;
			V += T;
			buf[s] =  V / O;
			//boundaries checking (to protect ears)
			if(buf[s] > 1f){
				//Debug.Log("WARNING : high positive value !");
				buf[s] = 1f;
			}
			if(buf[s] < -1f){
				//Debug.Log("WARNING : high negative value !");
				buf[s] = -1f;
			}
			
		}
	
	}
	
	//High pass filter
	void HPF(float[] buf){
	// filter parameters
		float O = 2.0f * Mathf.PI * HPcutoff / clip.frequency;
		float C = HPQ / O;
		float L = 1 / HPQ / O;	
		float V = 0f; 
		float I=0f;
		float T;
		for( int s = 0; s < buf.Length ; s++){
			T = (buf[s] * O - V) ;
			V += (I + T) / C;
			I += T / L;
			buf[s] = buf[s] - V / O;
			//boundaries checking (to protect ears)
			if(buf[s] > 1f){
				//Debug.Log("WARNING : high positive value !");
				buf[s] = 1f;
			}
			if(buf[s] < -1f){
				//Debug.Log("WARNING : high negative value !");
				buf[s] = -1f;
			}
			
		}
	
	
	}
	
	//Save extra grain data for next turn to keep continuity in reading and order data for the DSP (stereo or mono)
	void saveAndPrepareForDSP(int q){
	
		if(AudioSettings.speakerMode == AudioSpeakerMode.Stereo){
			//ordering data for stereo before passing it to unity DSP buffer
			for( int i = 0; i < AudioBufferLeft.Length; i++){
			
				AudioBuffer[q][2*i] = AudioBufferLeft[i];
			}
			for( int i = 0; i < AudioBufferRight.Length; i++){
			
				AudioBuffer[q][2*i+1] = AudioBufferRight[i];
			}
		}
		
		//Apply last saved extra data
		
		for(int i =0 ; i < save.Length ; i++){
		
			AudioBuffer[q][i] += save[i];

		}
		
		//Update new extra data for next turn 
		for( int i = bufferSize ; i < AudioBuffer[q].Length;i++){
		
			save[i-bufferSize] = AudioBuffer[q][i];

		}

	}
	void OnGUI(){
		if(toggleView){
			if(bufferView)GUILayout.Label(bufferView);	
			if(GUILayout.Button("Play"))GetComponent<AudioSource>().Play();
			if(GUILayout.Button("Stop"))GetComponent<AudioSource>().Stop();
			if(env)GUILayout.Label(env);
			if(sum)GUILayout.Label(sum);
		}
	}
	
	//Safe exit or unity crashes
	void OnApplicationQuit() {
		GetComponent<AudioSource>().Stop();
	}
}
