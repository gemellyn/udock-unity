using UnityEngine;
using System.Collections.Generic;

public class MyLog : MonoBehaviour
{
    private List<string> log = new List<string>();
    private string toShow;
    public float elapsedSinceLastLog = 0;
    public GUIStyle guiStyle;

    void OnEnable()
    {
        Application.RegisterLogCallback(HandleLog);
    }
    void OnDisable()
    {
        Application.RegisterLogCallback(null);
    }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        log.Add(logString);

        toShow = "";
        for (int i = log.Count - 1; i >= System.Math.Max(log.Count - 5,0); i--)
            toShow = log[i] +"\n"+ toShow;

        elapsedSinceLastLog = 0;

    }

    void OnGUI()
    {
        elapsedSinceLastLog += Time.deltaTime;
        if (elapsedSinceLastLog < 5.0f)
            GUI.Label(new Rect(20, 20, Screen.width - 20, Screen.height - 20), toShow, guiStyle);
    }
}