using UnityEngine;
using System.Collections;

public class ResidueSelector : MonoBehaviour {

    public Transform moleculeManager;
    public GUISkin skin; 

    private Transform lastSelected;


	// Use this for initialization
	void Start () {
        lastSelected = null;
	}
	
	// Update is called once per frame
	void Update () {
        RaycastHit hit;
        //On raycast sur le layer 8 ou se trouvent les molécules ballsview
        int layerMask = 0x01 << 8;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1000.0f, layerMask))
        {
            if(lastSelected)
                moleculeManager.GetComponent<MoleculeManager>().showResidueFromAtom(lastSelected,false);

            moleculeManager.GetComponent<MoleculeManager>().showResidueFromAtom(hit.transform, true);

            lastSelected = hit.transform;
		}
        else
            if (lastSelected)
            {
                moleculeManager.GetComponent<MoleculeManager>().showResidueFromAtom(lastSelected, false);
                lastSelected = null;
            }
        
	
	}

    void OnGUI()
    {
        

        //GUI.enabled = true;
        //Vector3 pos = Camera.main.WorldToScreenPoint(lastSelected.position);
        //GUI.Label(new Rect(pos.x, Screen.height - pos.y, 150, 130), "Hello");
        if (lastSelected)
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(lastSelected.position);
            GUI.skin = skin;
            GUI.Label(new Rect(pos.x - 50, Screen.height - pos.y - 50, 100, 100), lastSelected.GetComponent<AtomObject>().atom.residueName + " " + lastSelected.GetComponent<AtomObject>().atom.residueId);
        }
    }
}
