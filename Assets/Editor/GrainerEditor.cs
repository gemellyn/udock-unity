/*
	Creation :  06 / 2014
	Author : Maxime GILLET
*/

using UnityEngine;
using System.Collections;
using UnityEditor;

/**
**	Used in the unity environment to test and play with the parameters of the Grainer at runtime.
**/

[CustomEditor(typeof(Grainer))]
public class GrainerEditor : Editor{

	enum Function { Log , Gauss , Tanh , Tanh_x_Sin , Arctan};
	
	public override void OnInspectorGUI(){
	
		Grainer myScript = (Grainer)target;
		myScript.SourceClip = (AudioClip) EditorGUILayout.ObjectField("Source Clip",myScript.SourceClip, typeof(AudioClip), false);
		myScript.Offset = EditorGUILayout.IntSlider("Offset:", myScript.Offset , (int)myScript.Delta/2+1 , myScript.SourceClip.samples - myScript.Delta,GUILayout.MaxWidth(500),GUILayout.ExpandWidth(false));
		myScript.Delta = EditorGUILayout.IntSlider("Delta:", myScript.Delta , 1 , myScript.SourceClip.samples);		
		myScript.samplesInOneGrain =  EditorGUILayout.IntSlider("samplesInOneGrain:",myScript.samplesInOneGrain,myScript.MinSamplesInOneGrain,myScript.MaxSamplesInOneGrain);
		myScript.Covering = EditorGUILayout.Slider("covering:", myScript.Covering , 0.01f , 0.9f);
		myScript.FadeFunction = (int)(Function)EditorGUILayout.EnumPopup("Fade Function :",(Function)myScript.FadeFunction);
		myScript.toggleView = GUILayout.Toggle(myScript.toggleView, "View GUI");
		

	}
	
}
