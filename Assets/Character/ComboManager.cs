using UnityEngine;
using System.Collections;

public class ComboManager : MonoBehaviour {

    public float[] comboDuration = { 32.0f, 20.0f, 16.0f, 12.0f, 8.0f, 6.0f ,4.0f,2.0f,1.0f,0.5f,0.25f};
    public int[] comboMultiplier = { 1,2,4,8,16,32,64,128,256,512,1024 };
    private int currentCombo = 0;
    private float currentComboDuration = 0.0f;
    private int currentScore = 0;
    public GUIStyle guiStyle;
    
    
    // Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (currentCombo > 0)
        {
            currentComboDuration -= Time.deltaTime;
            if (currentComboDuration <= 0)
            {
                currentCombo = System.Math.Max(currentCombo - 1, 0);
                currentComboDuration = comboDuration[currentCombo];
            }
        }
	}

    public void OnGUI()
    {
        if (currentCombo > 0)
        {
            GUI.Box(new Rect(10, 10, (Screen.width - 10.0f) * (currentComboDuration / comboDuration[currentCombo]), 20), GUIContent.none);
        }

        GUI.Label(new Rect(Screen.width -200, 10, 190, 50), "Mult : " + comboMultiplier[currentCombo], guiStyle);
        GUI.Label(new Rect(Screen.width -200, 50, 190, 50), "Score : "+currentScore, guiStyle);
    }

    public void gotTarget()
    {
        currentScore += comboMultiplier[currentCombo];
        currentCombo = System.Math.Min(comboDuration.Length - 1, currentCombo+1);
        currentComboDuration = comboDuration[currentCombo];

        
    }
}
