using UnityEngine;
using System.IO;
using System;
using System.Collections;

[Serializable]
public class MoleculeCreator
{ 
    //Attributs de base
    private MoleculeData molecule;
    private MarchingCubes mCubes;
    private MarchingCubes.MCMesh[] mesh;
    private GameObject[] ballsView;
    private GameObject[] stickView;
  
    //Dimensions
    private Vector3 posMin;
    private Vector3 posMax;

    //Chargement
    public bool loadHetatm = false;
    private Vector3 posCreation = new Vector3(); ///Pour qu'on crée la molecule au bon endroit
    private Vector3 rotCreation = new Vector3(); ///Pour qu'on crée la molecule au bon endroit

    //Paramètres de génération de surface
    public String molRessourceName;
    public bool isURL = false;
    public enum Surfaces { VanDerWaals = 0, SolventAccessible = 1, SolventExcluded = 2 }
    public Surfaces surface = Surfaces.VanDerWaals;
    public float baseTailleCube = 1.0f;
    public float baseProbeBoost = 0.0f;
    public float baseProbeSize = 1.4f;
    public float scale = 10.0f;
    public int nbAtomsPerMesh = 1000;
    public int maxAtomsLoaded = 50000;
    public bool noHydrogen = true;

    //View Balls
    public Transform carbonne;
    public Transform oxygene;
    public Transform hydrogene;
    public Transform azote;
    public Transform unknown;
    
    //View sticks
    public Transform stickPrincipal;
    public Transform stickLateral;
    public Transform stickPrincipalDouble;
    public Transform stickLateralDouble;

    public void getConfigFrom(MoleculeCreator from)
    {
        this.loadHetatm = from.loadHetatm;
        this.molRessourceName = from.molRessourceName;
        this.surface = from.surface;
        this.baseTailleCube = from.baseTailleCube;
        this.baseProbeBoost = from.baseProbeBoost;
        this.baseProbeSize = from.baseProbeSize;
        this.scale = from.scale;
        this.nbAtomsPerMesh = from.nbAtomsPerMesh;
        this.noHydrogen = from.noHydrogen;
        this.maxAtomsLoaded = from.maxAtomsLoaded;

        this.carbonne = from.carbonne;
        this.oxygene = from.oxygene;
        this.hydrogene = from.hydrogene;
        this.azote = from.azote;
        this.unknown = from.unknown;

        this.stickPrincipal = from.stickPrincipal;
        this.stickLateral = from.stickLateral;
        this.stickPrincipalDouble = from.stickPrincipalDouble;
        this.stickLateralDouble = from.stickLateralDouble;
    }


    public int getNbAtoms()
    {
        return molecule.atoms.Length;
    }
    public GameObject[] getBallsView()
    {
        return this.ballsView;
    }

    public GameObject[] getSticksView()
    {
        return this.stickView;
    }

    public Vector3 getBarycenter()
    {
        return molecule.barycenter;
    }

    public Atom getHighestAtom()
    {
        return molecule.getHighestAtom();
    }

    public void setOffsetCreation(Vector3 pos, Vector3 rot)
    {
        posCreation = pos;
        rotCreation = rot;
    }

    private void updateDimensions()
    {
        float minXres=0, maxXres=0, minYres=0, maxYres=0, minZres=0, maxZres=0;
        molecule.getDimensions(ref minXres, ref maxXres, ref minYres, ref maxYres, ref minZres, ref maxZres);
        posMin = new Vector3(minXres, minYres, minZres);
        posMax = new Vector3(maxXres, maxYres, maxZres);
        
    }

    public void getDimensions(ref Vector3 minPos, ref Vector3 maxPos)
    {
        minPos = this.posMin;
        maxPos = this.posMax;
    }

    /*public void getDimensions(ref float minXres, ref float maxXres, ref float minYres, ref float maxYres, ref float minZres, ref float maxZres)
    {
        molecule.getDimensions(ref minXres, ref maxXres, ref minYres, ref maxYres, ref minZres, ref maxZres);
    }*/

    public float getSurfaceOffset()
    {
        return baseProbeSize * scale;
    }

    public int getNbMeshs()
    {
        return (molecule.atoms.Length / nbAtomsPerMesh) + ((molecule.atoms.Length % nbAtomsPerMesh) > 0 ? 1 : 0);
    }
    
    public IEnumerator load()
    {
        molecule = new MoleculeDataPdb(loadHetatm);
        molecule.setMaxAtomLimit(true, maxAtomsLoaded);
        
        //Chargement de la molécule
        if (!isURL)
        {
            Debug.Log("Loading pdb " + molRessourceName + " from assets");
            molecule.loadFromAsset(molRessourceName);
        }
        else
        {
            Debug.Log("Loading pdb " + molRessourceName + " from www");
            WWW www = new WWW(molRessourceName);
            yield return www;
            molecule.loadFromMemory(www.bytes, molRessourceName);
        }

        molecule.scale(scale);
        molecule.calcBarycenter();
        updateDimensions();
        Debug.Log("Barycentre " + molecule.barycenter);
    }

    public MarchingCubes.MCMesh[] makeMesh()
    {
        int nbMesh = getNbMeshs();
        this.mesh = new MarchingCubes.MCMesh[nbMesh];
        int startAtom = 0;
        for (int i = 0; i < nbMesh; i++)
        {
            int endAtom = startAtom + nbAtomsPerMesh;
            startAtom = setIndexToResiduStart(startAtom);
            endAtom = setIndexToResiduStart(endAtom);

            this.mesh[i] = makeMesh(startAtom, endAtom - startAtom);

            startAtom = endAtom;
        }

        return this.mesh;
    }

    private MarchingCubes.MCMesh makeMesh(int startAtom, int nbAtoms)
    {
        if (molecule == null)
        {
            Debug.LogError("You did not load the molecule : cannot make mesh");
            return null;
        }
        //On corrige nbAtoms
        nbAtoms = Math.Min(nbAtoms, molecule.atoms.Length - startAtom);

        Debug.Log("Making molecule geometry for " + nbAtoms + " atoms.");

        //Aply scale to algo
        float tailleCube = baseTailleCube *  scale;
        float probeBoost = baseProbeBoost * scale;
        float probeSize = baseProbeSize * scale;
        float margin = 1.0f * scale;

        //Init marching cubes
        mCubes = new MarchingCubes();
        float minX = 0, maxX = 0, minY = 0, maxY = 0, minZ = 0, maxZ = 0;
        molecule.getDimensions(startAtom,nbAtoms,ref minX, ref maxX, ref minY, ref maxY, ref minZ, ref maxZ);
        mCubes.setOrigin(new Vector3(minX - margin - probeBoost - probeSize, minY - margin - probeBoost - probeSize, minZ - margin - probeBoost - probeSize));
        mCubes.create(maxX - minX + (margin * 2.0f) + (probeBoost * 2.0f) + (probeBoost * 2.0f),
            maxY - minY + (margin * 2.0f) + (probeBoost * 2.0f) + (probeSize * 2.0f),
            maxZ - minZ + (margin * 2.0f) + (probeBoost * 2.0f) + (probeSize * 2.0f), tailleCube, true);

	    switch (surface)
        {
            case Surfaces.VanDerWaals:
                Debug.Log("Making molecule geometry for van der waals only");

		        //On crée la van der waals
		        mCubes.setInterpolation(true);

                Debug.Log("Making van der walls for " + nbAtoms + " atomes");
                for (int i = startAtom; i < nbAtoms + startAtom; i++)
			        mCubes.valideSphere(molecule.atoms[i].position,molecule.atoms[i].radius,molecule.atoms[i].charge);
		    
		        Debug.Log("Van der Walls ok");
		        mCubes.setLissageNormales(true);
                break;

            case Surfaces.SolventAccessible:
                Debug.Log("Making molecule geometry for SAS");

		        //On crée la SAS
		        mCubes.setInterpolation(true);

                Debug.Log("Making SAS for " + nbAtoms + " atomes");
                for (int i = startAtom; i < nbAtoms + startAtom; i++)
                    mCubes.valideSphere(molecule.atoms[i].position, molecule.atoms[i].radius + (probeSize + probeBoost), 0);
		
		        Debug.Log("SAS ok");
		        mCubes.setLissageNormales(true);
                break;

            case Surfaces.SolventExcluded:
		        Debug.Log("Making molecule geometry for SES");

		        if(probeBoost != 0)
			        Debug.Log("Using probe boost: " + probeBoost);

		        //STEP 1
		        //On crée la SAS sans interpolation, on marque juste les cubes
		        mCubes.setInterpolation(false); //Donc sans interpolation, pas besoin de recalculer les edges.
                Debug.Log("Making SAS for " + nbAtoms + " atomes");

                for (int i = startAtom; i < nbAtoms + startAtom; i++)
                    mCubes.valideSphere(molecule.atoms[i].position, molecule.atoms[i].radius + (probeSize + probeBoost), 0);
                
		        Debug.Log("Cubes ok");
		    
		        //STEP 2
		        //On reduit la SAS sur un diamètre PROBE_SIZE
		        Debug.Log("Contracting SAS to SES");
			    mCubes.setInterpolation(true);
		   	    mCubes.contractSurface(probeSize + probeBoost);
		   
		        //STEP 3
		        //On calcule tout bien la partie van der walls de la SES
			    mCubes.setInterpolation(true);
                Debug.Log("Making van des Walls part for " + nbAtoms + " atomes");

                for (int i = startAtom; i < nbAtoms + startAtom; i++)
			        mCubes.valideSphere(molecule.atoms[i].position,molecule.atoms[i].radius,molecule.atoms[i].charge);
			    
                Debug.Log("Van der Walls done");
		
		        //On précalcule nous meme les normales
			    mCubes.setLissageNormales(true);	

                break;
	    }


         /*Debug.Log("Calculating Electrostatics");

		//STEP 4
		//On calcule la charge des cubes
		NYVert3Df pos;
		NYVert3Df normal;
		float charge;
		uint8 code;
		for (int x=0;x<_MCubes->NbX;x++)
			for (int y=0;y<_MCubes->NbY;y++)
				for (int z=0;z<_MCubes->NbZ;z++)
				{
					code = _MCubes->getCubeCode(x,y,z);
					if(code !=0 && code != 255)
					{
						//_MCubes->getCubeCenter(pos,x,y,z);
						//Debug.Log(("Pos1 :" + pos.toStr()).c_str());
						_MCubes->getCubeBarycentreCoords(pos,x,y,z);
						//Debug.Log(("Pos2 :" + pos.toStr()).c_str());
						
						_MCubes->getCubeFaceNormal(normal,x,y,z);
						charge = getCharge(pos + (normal * PROBE_SIZE));
						_MCubes->setCubeColorShift(charge,x,y,z);
					}
					//Debug.Log("---");
					
				}
		//_MCubes->diffuseColorShift();
		Debug.Log("Electrostatics done");*/

        //On cree le mesh
        MarchingCubes.MCMesh mesh = new MarchingCubes.MCMesh();
        mCubes.makeGeometryFaces(molecule.barycenter * -1, ref mesh);
        return mesh;

    }

    private int setIndexToResiduStart(int index)
    {
        if (index == 0)
            return 0;

        if (index >= molecule.atoms.Length)
            index = molecule.atoms.Length - 1;

        while (molecule.atoms[index - 1] == molecule.atoms[index])
        {
            index--;
            if (index == 0)
                return 0;
        }

        return index;
    }

    public IEnumerator makeBallsView(float scale = 1.0f)
    {
        int nbMesh = getNbMeshs();
        this.ballsView = new GameObject[nbMesh];
        int startAtom = 0;
        for (int i = 0; i < nbMesh; i++)
        {
            int endAtom = startAtom + nbAtomsPerMesh;
            startAtom = setIndexToResiduStart(startAtom);
            endAtom = setIndexToResiduStart(endAtom);

            this.ballsView[i] = makeBallsView(startAtom, endAtom - startAtom, scale);
            this.ballsView[i].transform.position = posCreation;
            this.ballsView[i].transform.localEulerAngles = rotCreation;
            yield return null;

            startAtom = endAtom;
        }

        yield return null;
    }

    private GameObject makeBallsView(int startAtom, int nbAtoms, float scale)
    {
        if (molecule == null)
        {
            Debug.LogError("You did not load the molecule : cannot make balls view");
            return null;
        }

        //On corrige nbAtoms
        nbAtoms = Math.Min(nbAtoms, molecule.atoms.Length - startAtom); 

        //On crée la balls view
        GameObject ballsView = new GameObject();
        ballsView.transform.position = new Vector3(0, 0, 0);
        int residueId = -1;
        GameObject currentResidue = null;
        for (int i = startAtom; i < nbAtoms + startAtom; i++)
        {
            if (molecule.atoms[i].udockId == Atom.Type.H && noHydrogen)
                continue;

            //Si c'est un nouveau résidu
            if (molecule.atoms[i].residueId != residueId)
            {
                currentResidue = new GameObject();
                currentResidue.transform.position = molecule.atoms[i].position;
                currentResidue.transform.parent = ballsView.transform;
                residueId = molecule.atoms[i].residueId;
            }

            Transform sphere = null;

            if (molecule.atoms[i].udockId == Atom.Type.C_2 ||
                molecule.atoms[i].udockId == Atom.Type.C_3 ||
                molecule.atoms[i].udockId == Atom.Type.C_AR ||
                molecule.atoms[i].udockId == Atom.Type.C_CAT)
                sphere = GameObject.Instantiate(carbonne) as Transform;

            if (molecule.atoms[i].udockId == Atom.Type.O_2 ||
                molecule.atoms[i].udockId == Atom.Type.O_3 ||
                molecule.atoms[i].udockId == Atom.Type.O_CO2)
                sphere = GameObject.Instantiate(oxygene) as Transform;

            if (molecule.atoms[i].udockId == Atom.Type.H)
                sphere = GameObject.Instantiate(hydrogene) as Transform;

            if (molecule.atoms[i].udockId == Atom.Type.N_2 ||
                molecule.atoms[i].udockId == Atom.Type.N_3 ||
                molecule.atoms[i].udockId == Atom.Type.N_4 ||
                molecule.atoms[i].udockId == Atom.Type.N_AM ||
                molecule.atoms[i].udockId == Atom.Type.N_AR ||
                molecule.atoms[i].udockId == Atom.Type.N_PL3)
                sphere = GameObject.Instantiate(azote) as Transform;

            if (sphere == null)
                sphere = GameObject.Instantiate(unknown) as Transform;

            if (sphere)
            {
                sphere.gameObject.layer = 8;
                sphere.localScale = new Vector3(molecule.atoms[i].radius * scale, molecule.atoms[i].radius * scale, molecule.atoms[i].radius * scale);
                sphere.position = molecule.atoms[i].position - molecule.barycenter;
                sphere.parent = currentResidue.transform;
                sphere.GetComponent<AtomObject>().atom = molecule.atoms[i];
            }
        }
        ballsView.SetActive(true);
        return ballsView;
    }

    public IEnumerator makeSticksView()
    {
        int nbMesh = getNbMeshs();
        this.stickView = new GameObject[nbMesh];
        int startAtom = 0;
        for (int i = 0; i < nbMesh; i++)
        {
            int endAtom = startAtom + nbAtomsPerMesh;
            startAtom = setIndexToResiduStart(startAtom);
            endAtom = setIndexToResiduStart(endAtom);

            this.stickView[i] = makeSticksView(startAtom, endAtom - startAtom);
            this.stickView[i].transform.position = posCreation;
            this.stickView[i].transform.localEulerAngles = rotCreation;
            yield return null;

            startAtom = endAtom;
        }

        yield return null;
    }

    /*
     * Attention ! ca plante sur les HETATM !!! normal structure a la con... donc si on fait les sticks, faut pas charger les hetatm
     * */
    private GameObject makeSticksView(int startAtom, int nbAtoms)
    {
        if (molecule == null)
        {
            Debug.LogError("You did not load the molecule : cannot make balls view");
            return null;
        }

        //On corrige nbAtoms
        nbAtoms = Math.Min(nbAtoms, molecule.atoms.Length - startAtom);

        GameObject linkTop = new GameObject();
        linkTop.transform.position = new Vector3(0, 0, 0);

        Atom previousTerminal = null;
        
        for (int i = startAtom; i < nbAtoms + startAtom; )
        {
            GameObject linkResidu = new GameObject();
            linkResidu.transform.parent = linkTop.transform;
            int residueStart = i; 
            int nbAtomResidue = 0;

            //Debug.Log("Chaine principale res " + molecule.atoms[residueStart].residueId + "(" + molecule.atoms[residueStart].residueName + ")");

            if (previousTerminal != null)
            {
                //Debug.Log("Link au precedent a partir de  " + previousTerminal.name);
                makeStick(previousTerminal, molecule.atoms[i + 0], linkResidu.transform, true, false,new Vector3(1, 0, 0));
            }

            //On fait la chaine de base
            makeStick(molecule.atoms[i + 0], molecule.atoms[i + 1], linkResidu.transform, true, false,new Vector3(1, 0, 0));
            makeStick(molecule.atoms[i + 1], molecule.atoms[i + 2], linkResidu.transform, true, false,new Vector3(1, 0, 0));
            Vector3 dirPrev = molecule.atoms[i+1].position - molecule.atoms[i+2].position;
            Vector3 dirNext = molecule.atoms[i+3].position - molecule.atoms[i+2].position;
            Vector3 normale = Vector3.Cross(dirPrev, dirNext);
            normale = Vector3.Cross(normale, dirNext);
            makeStick(molecule.atoms[i + 2], molecule.atoms[i + 3], linkResidu.transform, true, true, normale);
            previousTerminal = molecule.atoms[i + 2];

            //Debug.Log("Chaine laterale res " + molecule.atoms[residueStart].residueId);

            nbAtomResidue += 4;

            if (i + 4 < nbAtoms + startAtom)
            {
                if (molecule.atoms[i + 4].residueId == molecule.atoms[i + 3].residueId)
                {
                    //Debut de la chaine latérale
                    //Debug.Log("Debut chaine laterale 1-4 " + molecule.atoms[residueStart].residueId);
					makeStick(molecule.atoms[i + 1], molecule.atoms[i + 4], linkResidu.transform, false,false,new Vector3(1, 0, 0));
                    nbAtomResidue++;

                    bool chaineParticuliere = false;

                    //Chaine laterale particuliere : 
					if (true)//molecule.atoms[residueStart].residueUdockId != Atom.Residue.Histidine)
                    {	
                        chaineParticuliere = true;
                        //on va lier les atomes j aux atomes j+1. L'indice j est déja lié, au step précédent ou quand on fait le début plus haut
                        for (int j = i + 4,k=3; j < (nbAtoms + startAtom)-1;j++, k+=3)
                        {
                            if (molecule.atoms[j+1].residueId != molecule.atoms[residueStart].residueId)
                                break;

							if(k < Atom.ResidueStructure[(int)(molecule.atoms[residueStart].residueUdockId)].Length)
							{
                            	int atomFromOffset = Atom.ResidueStructure[(int)(molecule.atoms[residueStart].residueUdockId)][k];
                            	int atomToOffset = Atom.ResidueStructure[(int)(molecule.atoms[residueStart].residueUdockId)][k + 1];
                                bool lienDouble = Atom.ResidueStructure[(int)(molecule.atoms[residueStart].residueUdockId)][k + 2] != 1;

                                //calcul de la normale
                                normale = new Vector3(1, 0, 0);
                                if (lienDouble)
                                {
                                    dirPrev = molecule.atoms[j - 1].position - molecule.atoms[j].position;
                                    dirNext = molecule.atoms[j + 1].position - molecule.atoms[j].position;
                                    normale = Vector3.Cross(dirPrev, dirNext);
                                    normale = Vector3.Cross(normale, dirNext);
                                }

                                makeStick(molecule.atoms[residueStart + atomFromOffset], molecule.atoms[residueStart + atomToOffset], linkResidu.transform, false, lienDouble, normale);
							}

                            nbAtomResidue++;
                        }
                    }

                    if(!chaineParticuliere)
                    {
                        //reste de la chaine latérale
					    for (int j = i + 5; j < (nbAtoms + startAtom); j++)
                        {
						    if(molecule.atoms[j].residueId != molecule.atoms[residueStart].residueId)
							    break;

                            makeStick(molecule.atoms[j], molecule.atoms[j-1], linkResidu.transform, false, false,new Vector3(1, 0, 0));
                            nbAtomResidue++;
                        }
                    }
                }
            }

            //Debug.Log(nbAtomResidue+" atoms in residue " + molecule.atoms[residueStart].residueId);
            i += nbAtomResidue;              
        }

        return linkTop;
    }

    private Transform makeStick(Atom atomFrom, Atom atomTo, Transform parent, bool principale, bool lienDouble, Vector3 normale)
    {
        if ((atomFrom.udockId == Atom.Type.H || atomTo.udockId == Atom.Type.H) && noHydrogen)
            return null;
        
        //Debug.Log("Link " + atomFrom.name + " -> " + atomTo.name + " (" + atomFrom.residueId + "-" + atomTo.residueId + ")");

        Transform link = null;
        if (principale)
        {
            if (!lienDouble)
                link = GameObject.Instantiate(stickPrincipal) as Transform;
            else
                link = GameObject.Instantiate(stickPrincipalDouble) as Transform;
        }
        else
        {
            if (!lienDouble)
                link = GameObject.Instantiate(stickLateral) as Transform;
            else
                link = GameObject.Instantiate(stickLateralDouble) as Transform;
        }

        

        link.position = ((atomFrom.position + atomTo.position) / 2.0f) - molecule.barycenter;
        link.parent = parent;
        link.localScale = new Vector3(link.localScale.x, link.localScale.y, (atomFrom.position - atomTo.position).magnitude);
        link.LookAt(atomTo.position - molecule.barycenter, normale);
        //link.renderer.enabled = true;
        return link;
    }

    /*
    private GameObject makeSticksView(int startAtom, int nbAtoms)
    {
        if (molecule == null)
        {
            Debug.LogError("You did not load the molecule : cannot make balls view");
            return null;
        }

        //On corrige nbAtoms
        nbAtoms = Math.Min(nbAtoms, molecule.atoms.Length - startAtom);

        GameObject linkTop = new GameObject();
        linkTop.transform.position = new Vector3(0, 0, 0);
        GameObject linkResidu = new GameObject();
        linkResidu.transform.parent = linkTop.transform;

        int residueStart = startAtom;
        for (int i = startAtom; i < nbAtoms + startAtom; i++)
        {
            //Si on passe a un nouveau residu
            if (molecule.atoms[i].residueId != molecule.atoms[residueStart].residueId)
            {
                linkResidu = new GameObject();
                linkResidu.transform.parent = linkTop.transform;
                residueStart = i;
            }

            if (molecule.atoms[i].udockId == Atom.Type.H && noHydrogen)
                continue;

            for (int j = residueStart; molecule.atoms[j].residueId == molecule.atoms[residueStart].residueId; j++)
            {
                if (molecule.atoms[j].udockId == Atom.Type.H && noHydrogen)
                    continue;

                if (i == j)
                    continue;

                //Debug.Log((molecule.atoms[i].position - molecule.atoms[j].position).magnitude + " " + (molecule.atoms[i].radius + molecule.atoms[j].radius));
                if ((molecule.atoms[i].position - molecule.atoms[j].position).magnitude <= molecule.atoms[i].radius + molecule.atoms[j].radius)
                {
                    Transform link = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    link.position = ((molecule.atoms[i].position + molecule.atoms[j].position) / 2.0f) - molecule.barycenter;
                    link.parent = linkResidu.transform;
                    link.localScale = new Vector3(1, 1, (molecule.atoms[i].position - molecule.atoms[j].position).magnitude);
                    link.LookAt(molecule.atoms[j].position - molecule.barycenter);
                }

            }

        }

        return linkTop;
    }
    */
    
}
