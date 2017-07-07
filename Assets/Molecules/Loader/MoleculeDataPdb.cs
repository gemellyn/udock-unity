using UnityEngine;
using System;
using System.IO;

public class MoleculeDataPdb : MoleculeData
{
    public MoleculeDataPdb( bool loadHetatm)
        : base()
    {
        this.loadHetatm = loadHetatm;
    }
     
    private Boolean loadHetatm = false;
    private String debutLigneAtom = "ATOM";
    private String debutLigneHetatom = "HETATM";

    override public void loadFromAsset(String name)
    {
        TextAsset fileAsset = Resources.Load(name) as TextAsset;

        if (fileAsset == null)
        {
            Debug.LogError("Asset no found file : " + name);
            return;
        }

        loadFromMemory(fileAsset.bytes,name);
    }

    /*
     * Chargement d'un PDB
     */
    override public void loadFromMemory(byte [] data, String name)
	{

        StreamReader fe = null;
        MemoryStream stream = new MemoryStream(data);
        fe = new StreamReader(stream);
        

        if (fe == null)
        {
            Debug.LogError("Unable to open molecule from " + name);
            return;
        }
        

        //Pour le moment, on récupère tous les atomes, juste ca.
        
        int nbAtoms = 0;
        while (!fe.EndOfStream)
        {
            String line = fe.ReadLine();
            if (line.Substring(0, 4).CompareTo(debutLigneAtom) == 0 || (line.Substring(0, 6).CompareTo(debutLigneHetatom) == 0 && loadHetatm))
                if (line.Substring(17, 3).Trim() != "HOH")
                    nbAtoms++;
        }

        Debug.Log("Il y a <color=green>" + nbAtoms + "</color> atomes dans le fichier <color=green>" + name + "</color>");

        if (stopLoadingIfMaxAtomsReached)
            Debug.Log("Limite demandee de " + maxAtomsLoaded);

        //On crée le tableau des atomes
        this.atoms = new AtomPdb[nbAtoms];
        for (int i = 0; i < this.atoms.Length; i++)
        {
            this.atoms[i] = new AtomPdb();
        }


        //On les charge
        fe.Close();
        stream = new MemoryStream(data);
        fe = new StreamReader(stream);
        
        if (fe == null)
        {
            Debug.LogError("Unable to re-open molecule file ?? : " + name);
            return;
        }

        //on les charge
        int currentAtom = 0;
        byte numInResidue = 0;
        int currentResidueId = -1;
        bool stopLoading = false;
        while (!fe.EndOfStream && !stopLoading)
        {
            String line = fe.ReadLine();
            if (line.Substring(0, 4).CompareTo(debutLigneAtom) == 0 || (line.Substring(0, 6).CompareTo(debutLigneHetatom) == 0 && loadHetatm))
            {
                if (line.Substring(17, 3).Trim() != "HOH")
                {

                    AtomPdb a = (AtomPdb)(this.atoms[currentAtom]);
                    try
                    {

                        a.setSerialNumber(int.Parse(line.Substring(6, 5)));
                        a.setName(line.Substring(12, 4).Trim());
                        //a.setAltLoc(line.Substring(17, 1));
                        a.setResidueName(line.Substring(17, 3).Trim());
                        //a.setChainId(line.Substring(21,1));
                        a.setResidueId(int.Parse(line.Substring(22, 4)));
                        
                        if (a.residueId != currentResidueId)
                        {
                            numInResidue = 0;
                            currentResidueId = a.residueId;
                            if (stopLoadingIfMaxAtomsReached && currentAtom > maxAtomsLoaded)
                                stopLoading = true;
                        }
                        a.setNumInResidue(numInResidue);



                        //a.setResidueInsertionCode(line.Substring(26, 1));
                        a.setPosition(new Vector3(float.Parse(line.Substring(30, 8)), float.Parse(line.Substring(38, 8)), float.Parse(line.Substring(46, 8))));
                        //a.setOccupancy(double.Parse(line.Substring(54, 5));
                        // a.setTempFactor(double.Parse(line.Substring(60, 5));
                        a.setElement(line.Substring(76, 2).Trim());
                        a.setCharge(float.Parse(line.Substring(78, 2)));

                        
                        
                    }
                    catch (FormatException e)
                    {
                        //Debug.LogException(e);
                    }
                    finally
                    {
                        a.update();
                        currentAtom++;
                        numInResidue++;
                    }
                }

            }
        }

        fe.Close();

        //Tres pas optimisé mais pas le temps : si on a limité la taille
        if (stopLoadingIfMaxAtomsReached && currentAtom < atoms.Length)
        {
            Debug.Log("On applique la limite de taile : " + currentAtom);
            Atom [] newAtoms = new Atom[currentAtom];
            for (int i = 0; i < newAtoms.Length; i++)
            {
                newAtoms[i] = atoms[i];
            }
            atoms = newAtoms;
        }

        Debug.Log("Molecule pdb loaded");
    }



    /*
     * Sauvegarde en PDB
     */
    protected static void writeEntete(StreamWriter fs, String molName, int nbAtoms, int nbBonds, String molType, String chargeType)
    {

    }

    protected static void writeJunction(StreamWriter fs, AtomPdb lastAtom, int numberInFile)
    {
        int chainIdentifier = 'A' + numberInFile;
        fs.WriteLine("TER   % 5d      %c%c%c %c% 4d\n", lastAtom.serialNumber + 1,
            lastAtom.residueName[0],
            lastAtom.residueName[1] == 0 ? ' ' : lastAtom.residueName[1],
            lastAtom.residueName[2] == 0 ? ' ' : lastAtom.residueName[2],
            chainIdentifier,
            lastAtom.residueId);
    }


};