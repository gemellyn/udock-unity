using UnityEngine;
using System.Collections;

public class CrossHair : MonoBehaviour {

    public Texture2D texCross;
    private Rect position;

    void OnGUI()
    {
        position = new Rect((Screen.width - texCross.width) / 2, (Screen.height - texCross.height) / 2, texCross.width, texCross.height);
        GUI.DrawTexture(position, texCross);
    }
}
