using UnityEngine;
using System.Collections;

public class AtomSelector : MonoBehaviour {

    public Material baseMaterial;
    public Material selectedMaterial;

    public void setSelected(bool selected)
    {
        GetComponent<Renderer>().material = selected ? selectedMaterial : baseMaterial;
    }

}
