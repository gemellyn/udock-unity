using UnityEngine;
using System;
using System.IO;

public class AtomPdb : Atom
{

    public int serialNumber;
    
    public AtomPdb() : base()
    {
        serialNumber = 0;
    }

    public void setSerialNumber(int serial)
    {
        this.serialNumber = serial;
    }

    void saveToFile(StreamWriter fs, int numberInFile)
    {

        int chainIdentifier = 'A' + numberInFile;
        //ATOM 12345 nnnn rrr issss     xxxxxxxx yyyyyyyy zzzzzzzz  
        fs.WriteLine("ATOM  % 5d %c%c%c%c %c%c%c %c% 4d    % 8.3f% 8.3f% 8.3f\n", serialNumber,
                        name[0],
                        name[1] == 0 ? ' ' : name[1],
                        name[2] == 0 ? ' ' : name[2],
                        name[3] == 0 ? ' ' : name[3],
                        residueName[0],
                        residueName[1] == 0 ? ' ' : residueName[1],
                        residueName[2] == 0 ? ' ' : residueName[2],
                        chainIdentifier,
                        residueId,
                        position.x, position.y, position.z);
    }
};
