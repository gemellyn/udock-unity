using UnityEngine;
using System;
using System.IO;

public class MoleculeData {
	
    public Atom [] atoms; ///< Table des atomes composant la molécule
	public Vector3 barycenter; ///< Le barycentre de la molecule
    String name; ///< Nom de la molecule
    
    protected int maxAtomsLoaded = 0;
    protected bool stopLoadingIfMaxAtomsReached = false;

    public void setMaxAtomLimit(bool isLimited, int limitValue)
    {
        stopLoadingIfMaxAtomsReached = isLimited;
        maxAtomsLoaded = limitValue;
    }

    public virtual void loadFromAsset(String name) { }
    public virtual void loadFromMemory(byte[] tab, String name) { }

    //recupere l'atome le plus haut sur y
    public Atom getHighestAtom()
    {
        Atom highest = atoms[0];
        foreach (Atom a in atoms)
        {
            if (a.position.y > highest.position.y)
                highest = a;
        }
        return highest;
    }

    public void getDimensions(ref float minXres, ref float maxXres, ref float minYres, ref float maxYres, ref float minZres, ref float maxZres)
    {
        getDimensions(0, atoms.Length, ref minXres, ref maxXres, ref minYres, ref maxYres, ref minZres, ref maxZres);
    }
		
    public void getDimensions(int startAtom, int nbAtoms, ref float minXres, ref float maxXres, ref float minYres, ref float maxYres, ref float minZres, ref float maxZres)
    {
	    float minX = atoms[0].position.x;
	    float minY = atoms[0].position.y;
	    float minZ = atoms[0].position.z;
	    float maxX = atoms[0].position.x;
	    float maxY = atoms[0].position.y;
	    float maxZ = atoms[0].position.z;

        for (int i = startAtom; i < startAtom + nbAtoms; i++)
	    {
            if (atoms[i].position.x - atoms[i].radius < minX)
                minX = atoms[i].position.x - atoms[i].radius;
            if (atoms[i].position.y - atoms[i].radius < minY)
                minY = atoms[i].position.y - atoms[i].radius;
            if (atoms[i].position.z - atoms[i].radius < minZ)
                minZ = atoms[i].position.z - atoms[i].radius;

            if (atoms[i].position.x + atoms[i].radius > maxX)
                maxX = atoms[i].position.x + atoms[i].radius;
            if (atoms[i].position.y + atoms[i].radius > maxY)
                maxY = atoms[i].position.y + atoms[i].radius;
            if (atoms[i].position.z + atoms[i].radius > maxZ)
                maxZ = atoms[i].position.z + atoms[i].radius;
	    }			

	    minXres = minX;
	    minYres = minY;
	    minZres = minZ;
	    maxXres = maxX;
	    maxYres = maxY;
	    maxZres = maxZ;
    }

    public void  calcBarycenter()
    {
        barycenter = new Vector3(0,0,0);
        for (int i = 0; i < atoms.Length; i++)
	    {
            barycenter += atoms[i].position;
	    }
        barycenter /= (float)atoms.Length;
    }

    //Multiplie position et radius de tous les atomes
    public void scale(float scale)
    {
        for (int i = 0; i < atoms.Length; i++)
        {
            atoms[i].position *= scale;
            atoms[i].radius *= scale;
        }
        
    }

    
};
