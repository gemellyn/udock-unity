using UnityEngine;
using System;

public class Atom
{

	public Vector3 position;
    public String name;
    public String element;
    public String residueName;
    public int residueId;
    public float radius;
    public float charge;
    public float chargeLissee;
    public float surface;
	public Type udockId;
    public Residue residueUdockId;
    public byte numberInResidue; ///Numero de l'atome dans le residu, permet de savoir à qui il est lié 

    public Atom()
    {

    }

    public enum Residue : int
    {
        Alanine = 0, //Pour chaque residu
        Arginine,
        Asparagine,
        AcideAspartique,
        Cysteine,
        AcideGlutamique,
        Glutamine,
        Glycine,
        Histidine,
        Isoleucine,
        Leucine,
        Lysine,
        Methionine,
        Phenylalanine,
        Proline,
        Pyrrolysine,
        Selenocysteine,
        Serine,
        Threonine,
        Tryptophane,
        Tyrosine,
        Valine,
        NB_RESIDUE
    };

    static String[] ResiduesStr = {
        "ALA",    
		"ARG",    
		"ASN",   
		"ASP",  
		"CYS",    
		"GLU",    
		"GLN",    
		"GLY",   
		"HIS",   
		"ILE",  
		"LEU",    
		"LYS",    
		"MET",  
		"PHE",    
		"PRO",    
		"PYL",      
		"SEC",      
		"SER",
        "THR",
        "TRP",
        "TYR",
        "VAL",
    };


    public static int[][] ResidueStructure = new int[][] 
    {
        new int[] {1,4,1}, //de 1 vers 4, chaine simple            //"ALA",    
        new int[] {1,4,1,4,5,1,5,6,1,6,7,1,7,8,1,8,9,1,8,10,2},    //"ARG",    
		new int[] {1,4,1,4,5,1,5,6,1,5,7,1},                       //"ASN",   
        new int[] {1,4,1,4,5,1,5,6,0,5,7,0}, //de 1 vers 4, chaine simple            //"ASP",  
        new int[] {1,4,1,4,5,1},                                   //"CYS",    
        new int[] {1,4,1,4,5,1,5,6,1,6,7,0,6,8,0}, //de 1 vers 4, chaine simple            //"GLU",    
        new int[] {1,4,1,4,5,1,5,6,1,6,7,2,6,8,1}, //de 1 vers 4, chaine simple            //"GLN",    
        new int[] {}, //de 1 vers 4, chaine simple            //"GLY",   
        new int[] {1,4,1,4,5,1,5,6,0,5,7,0,6,8,0,7,9,0,8,9,0}, //de 1 vers 4, chaine simple            //"HIS",   
        new int[] {1,4,1,4,5,1,4,6,1,5,7,1}, //de 1 vers 4, chaine simple            //"ILE",  
        new int[] {1,4,1,4,5,1,5,6,1,5,7,1}, //de 1 vers 4, chaine simple            //"LEU",    
        new int[] {1,4,1,4,5,1,5,6,1,6,7,1,7,8,1}, //de 1 vers 4, chaine simple            //"LYS",    
        new int[] {1,4,1,4,5,1,5,6,1,6,7,1}, //de 1 vers 4, chaine simple            //"MET",  
        new int[] {1,4,1,4,5,1,5,6,0,5,7,0,6,8,0,7,9,0,8,10,0,9,10,0}, //de 1 vers 4, chaine simple            //"PHE",    
        new int[] {1,4,1,4,5,1,5,6,1,6,0,1},                       //"PRO",    
        new int[] {1,4,1}, //de 1 vers 4, chaine simple            //"PYL",    
        new int[] {1,4,1}, //de 1 vers 4, chaine simple            //"SEC",    
        new int[] {1,4,1,4,5,1},                                   //"SER",
        new int[] {1,4,1,4,5,1,4,6,1}, //de 1 vers 4, chaine simple            //"THR",
        new int[] {1,4,1,4,5,1,5,6,0,5,7,0,6,8,0,8,9,0,7,9,0,7,10,0,9,11,0,10,12,0,11,13,0,12,13,0}, //"TRP",
        new int[] {1,4,1,4,5,1,5,6,0,5,7,0,6,8,0,7,9,0,8,10,0,9,10,0,10,11,1}, //de 1 vers 4, chaine simple            //"TYR",
        new int[] {1,4,1,4,5,1,4,6,1} //de 1 vers 4, chaine simple             //"VAL",
    };
    
    public enum Type : int {
	    C_3 = 0, //Pour chaque atome, du plus court au plus long
	    C_2,
	    C_AR,
	    C_CAT,
	    N_3,
	    N_2,
	    N_4,
	    N_AR,
	    N_AM,
	    N_PL3,
	    O_3,
	    O_2,
	    O_CO2,
	    S_3,
	    P_3,
	    F,
	    H,
	    LI,
	    NB_ATOM_UDOCK_ID
    };

    static String[] TypesStr = {
        "C.3",    //C_3
		"C.2",    //C_2,
		"C.ar",   //C_AR,
		"C.cat",  //C_CAT,
		"N.3",    //N_3,
		"N.2",    //N_2,
		"N.4",    //N_4,
		"N.ar",   //N_AR,
		"N.am",   //N_AM,
		"N.pl3",  //N_PL3,
		"O.3",    //O_3,
		"O.2",     //O_2,
		"O.co2",   //O_CO2,
		"S.3",     //S_3,
		"P.3",     //P_3,
		"F",       //F,
		"H",       //H,
		"Li"       //LI,
    };

    static float[] AtomsRadius = {
         1.908f,    //C_3
	     1.908f,    //C_2,
	     1.908f,    //C_AR,
	     1.908f,    //C_CAT,
	     1.875f,    //N_3,
	     1.824f,    //N_2,
	     1.824f,    //N_4,
	     1.824f,    //N_AR,
	     1.824f,    //N_AM,
	     1.824f,    //N_PL3,
	     1.721f,    //O_3,
	     1.6612f,    //O_2,
	     1.6612f,    //O_CO2,
	     2.0f,       //S_3,
	     2.1f,       //P_3,
	     1.75f,      //F,
	     1.4870f,    //H,
	     1.137f      //LI,
    };

    /*static double[] AtomsEpsilonsSquared = {
         Math.Sqrt(0.1094f*0.1094f),    //C_3
	     Math.Sqrt(0.0860f*0.0860f),    //C_2,
	     Math.Sqrt(0.0860f*0.0860f),    //C_AR,
	     Math.Sqrt(1.908f*1.908f),    //C_CAT,
	     Math.Sqrt(0.17f*0.17f),    //N_3,
	     Math.Sqrt(0.17f*0.17f),    //N_2,
	     Math.Sqrt(0.17f*0.17f),    //N_4,
	     Math.Sqrt(0.17f*0.17f),    //N_AR,
	     Math.Sqrt(0.17f*0.17f),    //N_AM,
	     Math.Sqrt(0.17f*0.17f),    //N_PL3,
	     Math.Sqrt(0.2104f*0.2104f),    //O_3,
	     Math.Sqrt(0.21f*0.21f),    //O_2,
	     Math.Sqrt(0.21f*0.21f),    //O_CO2,
	     Math.Sqrt(0.25f*0.25f),       //S_3,
	     Math.Sqrt(0.25f*0.25f),       //P_3,
	     Math.Sqrt(0.061f*0.061f),      //F,
	     Math.Sqrt(0.0157f*0.0157f),    //H,
	     Math.Sqrt(0.0183f*0.0183f)  //LI,
    };*/

    protected Type findUdockId() {
		for(int i=0;i<(int)Type.NB_ATOM_UDOCK_ID;i++)
		{
			String sousChaine = TypesStr[i].Substring(0, Math.Min(TypesStr[i].Length,this.element.Length));
			if (sousChaine.CompareTo(this.element) == 0)
				return (Type)i;
		}

        Debug.LogError("Unknown atom " + this.element); 
		return 0;
	}

    protected Residue findResidueUdockId()
    {
        for (int i = 0; i < (int)Residue.NB_RESIDUE; i++)
        {
			String sousChaine = ResiduesStr[i].Substring(0, Math.Min(ResiduesStr[i].Length, this.residueName.Length));
            if (sousChaine.CompareTo(this.residueName) == 0)
                return (Residue)i;
        }

        Debug.LogError("Unknown residue " + this.residueName);
        return 0;
    }

	public void update()
	{
		this.udockId = findUdockId();
        this.residueUdockId = findResidueUdockId();
		this.radius = AtomsRadius[(int)this.udockId];
	}

    public void setPosition(Vector3 pos)
    {
        this.position = pos;
    }

    public void setCharge(float charge)
    {
        this.charge = charge;
    }

    public void setName(String name)
    {
        this.name = name;
    }

    public void setElement(String element)
    {
        this.element = element;
    }

    public void setResidueId(int id)
    {
        this.residueId = id;
    }

    public void setResidueName(String Residue)
    {
        this.residueName = Residue;
    }

    public void setNumInResidue(byte num)
    {
        this.numberInResidue = num;
    }
};


