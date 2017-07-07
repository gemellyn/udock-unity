/*
	Creation :  06 / 2014
	Author : Maxime GILLET
*/

using UnityEngine;
using System.Collections;
using UnityEditor;



[CustomEditor(typeof(FinalGrainerV2))]
public class FinalGrainerV2Editor : Editor{

	enum Function { Log , Gauss , Tanh , Tanh_x_Sin , Arctan};
	
	//simplement cela pour supprimer le gui de OnAudioFilterRead qui fait droper à mort les fps
	public override void OnInspectorGUI(){
		FinalGrainerV2 myScript = (FinalGrainerV2)target;
		myScript.offset = EditorGUILayout.IntSlider("offset:", myScript.offset , (int)myScript.delta/2+1 , myScript.totalSamples - myScript.delta,GUILayout.MaxWidth(500),GUILayout.ExpandWidth(false));
		myScript.delta = EditorGUILayout.IntSlider("delta:", myScript.delta , 1 , (int)Mathf.Round(myScript.totalSamples/10));
		myScript.toggleBP = GUILayout.Toggle(myScript.toggleBP,"Band pass mode",GUILayout.Width(150));
		if( ! myScript.toggleBP){
			myScript.LPcutoff =  EditorGUILayout.IntSlider("LPcutoff:",myScript.LPcutoff,1,5000);
			GUILayout.Label("quality :"+myScript.LPQ);
			myScript.LPQ =  GUILayout.HorizontalSlider(myScript.LPQ,0.01f,1f);
			myScript.HPcutoff =  EditorGUILayout.IntSlider("HPcutoff:",myScript.HPcutoff,1,5000);
			GUILayout.Label("quality :"+myScript.HPQ);
			myScript.HPQ =  GUILayout.HorizontalSlider(myScript.HPQ,0.01f,1f);
		}
		else{
		
			myScript.BPcutoff =  EditorGUILayout.IntSlider("BPcutoff:",myScript.BPcutoff,52,4949);
			myScript.BPQ = GUILayout.HorizontalSlider(myScript.BPQ,0.01f,1f);
		}
		myScript.covering = EditorGUILayout.Slider("covering:", myScript.covering , 0.01f , 0.5f);
		myScript.FadeFunction = (int)(Function)EditorGUILayout.EnumPopup("Fade Function :",(Function)myScript.FadeFunction);
		myScript.g = EditorGUILayout.Slider("Volume:", myScript.g , 0.1f , 3f);
		myScript.toggleView = GUILayout.Toggle(myScript.toggleView, "View GUI");

	}
	
}
