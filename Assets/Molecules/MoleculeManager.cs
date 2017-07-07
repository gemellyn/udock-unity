using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MoleculeManager : MonoBehaviour {

    public bool useWeb = false;
    public MoleculeCreator moleculeCreatorDeBase; //On reprendra ses params pour charger les autres molecules
    public Transform moleculeObjectPrefab;
    public Transform player;
    public Transform sun;
    public Transform endPlane;
    public Material materialSuperVision; 
    public Material materialBase;

    private MoleculeCreator currentMolCreator = null;
    private List<MoleculeCreator> moleculeCreators = new List<MoleculeCreator>();
    private List<Transform> moleculeObjs = new List<Transform>();
    private List<Transform> moleculePartObjs = new List<Transform>();
    private Transform lastAddedMoleculeObj = null;

    //Pour la creation de la surface en assynchrone
    private bool isMolCreated = false;
    private MoleculeCreator assyncMolCreated = null;
    private MarchingCubes.MCMesh[] assyncMeshTab = null;

    //Pour decider du chargement d'une nouvelle molecule
    private bool loadingNewMol = false;
    private bool loadingNewMol_phase1 = false;
    private bool loadingNewMol_phase2 = false;
    private float currentMaxZ = 100;

    public GUIStyle guiStyle;

    //Targets
    public Transform Target;
    public float targetDensityPerAxis = 0.001f;



	// Use this for initialization
	void Start () {

        sun.position = new Vector3(0, 0, 1000);
        
	}

    private void molCreationAssync()
    {
        assyncMeshTab = assyncMolCreated.makeMesh();
        isMolCreated = true;
    }

  
    String getRandomPdbURL()
    {
        String id = PDBGet.getRandomPdbId();
        String url = "http://www.rcsb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId="+id;
        return url;
    }

    String getRandomPdbLocal()
    {
        int id = UnityEngine.Random.Range(1, 4);
        String url = "PDB/" + id;
        return url;
    }

    IEnumerator loadRandomMol(bool randFromWeb)
    {
        //Chargement d'une molecule
        MoleculeCreator molCreator = new MoleculeCreator();
        molCreator.getConfigFrom(moleculeCreatorDeBase);
        currentMolCreator = molCreator;

        if (randFromWeb)
        {
            molCreator.molRessourceName = getRandomPdbURL();
            molCreator.isURL = true;
        }
        else
        {
            molCreator.molRessourceName = getRandomPdbLocal();
            molCreator.isURL = false;
        }

        yield return StartCoroutine(molCreator.load());
        yield return new WaitForSeconds(1.0f);

        loadingNewMol_phase1 = false;
        loadingNewMol_phase2 = true;

        //On calcule la nouvelle position de la molécule
        Vector3 posMin = new Vector3();
        Vector3 posMax = new Vector3();
        molCreator.getDimensions(ref posMin, ref posMax);

        Debug.Log("Mol dimensions min " + posMin + " max " + posMax);

        //On trouve son max de longueur
        Vector3 widthMol = posMax - posMin;
        Vector3 rotationMol = new Vector3();
        float zWidth = posMax.z - posMin.z;
        float zOffset = -posMin.z + molCreator.getBarycenter().z;

        if (widthMol.x > widthMol.y && widthMol.x > widthMol.z)
        {
            rotationMol = new Vector3(0, -90, 0);
            zWidth = posMax.x - posMin.x;
            zOffset = -posMin.x + molCreator.getBarycenter().x ;
        }
        if (widthMol.y > widthMol.x && widthMol.y > widthMol.z)
        {
            rotationMol = new Vector3(90, 0, 0);
            zWidth = posMax.y - posMin.y;
            zOffset = -posMin.y + molCreator.getBarycenter().y ;
        }
        if (widthMol.z > widthMol.x && widthMol.z > widthMol.y)
        {
            rotationMol = new Vector3(0, 0, 0);
            zWidth = posMax.z - posMin.z;
            zOffset = -posMin.z + molCreator.getBarycenter().z;
        }

        Debug.Log("ZOFFSET " + zOffset);
        Vector3 offsetCreation = new Vector3(0, 0, currentMaxZ+zOffset);

        //Change le son
        //player.GetComponent<SoundUpdater>().setZRange(currentMaxZ, currentMaxZ + zWidth);

        //On place le parent de la molécule
        GameObject parent = new GameObject();
        parent.transform.position = offsetCreation;
        
        //On dit a la mol ou se placer pour qu'elle load au bon endroit
        molCreator.setOffsetCreation(offsetCreation, rotationMol);
        
        isMolCreated = false;
        assyncMolCreated = molCreator;
        System.Threading.Thread m_Thread = new System.Threading.Thread(this.molCreationAssync);
        m_Thread.Start();
        while (!isMolCreated)
            yield return null;
        MarchingCubes.MCMesh[] meshTab = assyncMeshTab;
        yield return new WaitForSeconds(1.0f);

        
        yield return StartCoroutine(molCreator.makeBallsView(0.3f));
        GameObject[] ballsViews = molCreator.getBallsView();

        yield return StartCoroutine(molCreator.makeSticksView());
        GameObject[] sticksViews = molCreator.getSticksView();

        Debug.Log("Molecule made of " + meshTab.Length + " meshes");
        
        for (int i = 0; i < meshTab.Length; i++)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = meshTab[i].vertices;
            mesh.normals = meshTab[i].normals;
            mesh.triangles = meshTab[i].triangles;
            Transform moleculePart = GameObject.Instantiate(moleculeObjectPrefab) as Transform;
            moleculePart.GetComponent<MeshFilter>().mesh = mesh;
            moleculePart.GetComponent<MeshCollider>().sharedMesh = mesh;
            moleculePart.GetComponent<MoleculeObject>().ballsView = ballsViews[i];
            moleculePart.position = offsetCreation;
            moleculePart.localEulerAngles = rotationMol;
            ballsViews[i].transform.parent = moleculePart;
            moleculePart.GetComponent<MoleculeObject>().sticksView = sticksViews[i];
            sticksViews[i].transform.parent = moleculePart;
            moleculePart.parent = parent.transform;
            moleculePartObjs.Add(moleculePart);

            

            yield return new WaitForSeconds(1.0f);
        }

        //On stoque le parent, qui représente le game object de la molécule, en aggrégant tout ses bout
        moleculeObjs.Add(parent.transform);

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform moleculePart = parent.transform.GetChild(i);
            createTargetsFromBBox(moleculePart.position - moleculePart.GetComponent<Collider>().bounds.extents,
                                  moleculePart.position + moleculePart.GetComponent<Collider>().bounds.extents,
                                  moleculePart);
        }

        

        

        //on bouge la molecule
        //parent.transform.position = new Vector3(0, 0, currentMaxZ + zOffset);
        //parent.transform.localEulerAngles = rotationMol;
        currentMaxZ += zWidth;

        Debug.Log("Max z is now " + currentMaxZ);

        sun.position = new Vector3(0, 0, currentMaxZ + 1000);
        endPlane.position = new Vector3(0, 0, currentMaxZ -500);
        
        
        /*//Le xmax de la dernière molécule
        float maxZLast = 0;
        Transform lastMol = lastAddedMoleculeObj;
        if (lastMol)
        {
            for (int i = 0; i < lastMol.childCount; i++)
            {
                float z = lastMol.GetChild(i).position.z + lastMol.GetChild(i).collider.bounds.extents.z;
                if (z > maxZLast)
                    maxZLast = z;
            }
        }

        //Le minX de notre molécule
        float minZNew = 100000;
        float maxZNew = 0;
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            float zStart = parent.transform.GetChild(i).position.z - parent.transform.GetChild(i).collider.bounds.extents.z;
            float zEnd = parent.transform.GetChild(i).position.z + parent.transform.GetChild(i).collider.bounds.extents.z;
            if (zStart < minZNew)
                minZNew = zStart;
            if (zEnd > maxZNew)
                maxZNew = zEnd;
        }*/

        //currentMaxZ = maxZLast + maxZNew;

        //Debug.Log(currentMaxZ);

        

        lastAddedMoleculeObj = parent.transform;

        //parent.transform.position = new Vector3(0, 0, maxZLast - minZNew + 1000);

        loadingNewMol = false;
        loadingNewMol_phase1 = false;
        loadingNewMol_phase2 = false;

       

        yield return null;
    }

    public void showBallsView(bool show)
    {
        for (int i = 0; i < moleculePartObjs.Count; i++)
        {
            moleculePartObjs[i].GetComponent<MoleculeObject>().ballsView.SetActive(show);
            moleculePartObjs[i].GetComponent<MoleculeObject>().sticksView.SetActive(show);
            moleculePartObjs[i].GetComponent<Renderer>().material = show ? materialSuperVision : materialBase;
        }
    }

    void OnGUI()
    {
        //GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
        //centeredStyle.alignment = TextAnchor.UpperCenter;
        if (loadingNewMol_phase1)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2 - 25, 100, 50), "Loading new Molecule", guiStyle);
        }

        if (loadingNewMol_phase2)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2 - 25, 100, 50), "Creating molecule of " + currentMolCreator.getNbAtoms() + " atoms", guiStyle);
        }

    }

    // Update is called once per frame
    void Update()
    {
        //Si trois molecules chargées, on supprime la premiere
        if (moleculeObjs.Count >= 3)
        {
            Transform parent = moleculeObjs[0];

            for(int i=0;i<moleculePartObjs.Count;)
                if (moleculePartObjs[i].parent == parent)
                {
                    Destroy(moleculePartObjs[i].gameObject);
                    moleculePartObjs.Remove(moleculePartObjs[i]);
                }
                else
                {
                    i++;
                }


            moleculeObjs.Remove(parent);
            Destroy(parent.gameObject);
        }

        if (player.position.z >= currentMaxZ - 500 && !loadingNewMol)
        {
            loadingNewMol = true;
            loadingNewMol_phase1 = true;
            StartCoroutine("loadRandomMol",useWeb);
        }

        if (Input.GetButton("Fire2"))
        {
            showBallsView(true);
        }
        else
        {
            showBallsView(false);
        }

        if (Input.GetButton("Restart"))
            Application.LoadLevel(Application.loadedLevel);

    }

    public void showResidueFromAtom(Transform atom, bool show)
    {
        Transform parent = atom.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).GetComponent<AtomSelector>().setSelected(show);    
        }
    }
    
    //Etoiles
    public void createTargetsFromBBox(Vector3 min, Vector3 max, Transform parent)
    {
        //Plan du haut
        float surf = (max.x - min.x) * (max.z - min.z);
        for (int i = 0; i < targetDensityPerAxis * surf; i++)
        {
            float x = UnityEngine.Random.Range(min.x,max.x);
            float z = UnityEngine.Random.Range(min.z,max.z);
            Transform target =  GameObject.Instantiate(Target) as Transform;
            target.parent = parent;
            target.position = new Vector3(x,max.y,z);
            target.GetComponent<Target>().go(new Vector3(0, -100, 0));
                
        }

        //Plan du bas
        surf = (max.x - min.x) * (max.z - min.z);
        for (int i = 0; i < targetDensityPerAxis * surf; i++)
        {
            float x = UnityEngine.Random.Range(min.x, max.x);
            float z = UnityEngine.Random.Range(min.z, max.z);
            Transform target = GameObject.Instantiate(Target) as Transform;
            target.parent = parent;
            target.position = new Vector3(x, min.y, z);
            target.GetComponent<Target>().go(new Vector3(0, 100, 0));

        }


        //Plan de droite
        surf = (max.y - min.y) * (max.z - min.z);
        for (int i = 0; i < targetDensityPerAxis * surf; i++)
        {
            float y = UnityEngine.Random.Range(min.y, max.y);
            float z = UnityEngine.Random.Range(min.z, max.z);
            Transform target = GameObject.Instantiate(Target) as Transform;
            target.parent = parent;
            target.position = new Vector3(max.x, y, z);
            target.GetComponent<Target>().go(new Vector3(-100, 0, 0));

        }

        //Plan de gauche
        surf = (max.y - min.y) * (max.z - min.z);
        for (int i = 0; i < targetDensityPerAxis * surf; i++)
        {
            float y = UnityEngine.Random.Range(min.y, max.y);
            float z = UnityEngine.Random.Range(min.z, max.z);
            Transform target = GameObject.Instantiate(Target) as Transform;
            target.parent = parent;
            target.position = new Vector3(min.x, y, z);
            target.GetComponent<Target>().go(new Vector3(100, 0, 0));

        }

        //Plan de devant
        surf = (max.x - min.x) * (max.y - min.y);
        for (int i = 0; i < targetDensityPerAxis * surf; i++)
        {
            float x = UnityEngine.Random.Range(min.x, max.x);
            float y = UnityEngine.Random.Range(min.y, max.y);
            Transform target = GameObject.Instantiate(Target) as Transform;
            target.parent = parent;
            target.position = new Vector3(x, y, max.z);
            target.GetComponent<Target>().go(new Vector3(0, 0, -100));

        }

        //Plan de derriere
        surf = (max.x - min.x) * (max.y - min.y);
        for (int i = 0; i < targetDensityPerAxis * surf; i++)
        {
            float x = UnityEngine.Random.Range(min.x, max.x);
            float y = UnityEngine.Random.Range(min.y, max.y);
            Transform target = GameObject.Instantiate(Target) as Transform;
            target.parent = parent;
            target.position = new Vector3(x, y, min.z);
            target.GetComponent<Target>().go(new Vector3(0, 0, 100));

        }
    }
}
