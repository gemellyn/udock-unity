using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

/**
  * Remarque générale
  * pour reflechir, le repère pour un cube et pour l'hypercube est le suivant :
  * le sommet 1 (selon LorensenCline87) est en (0,0,0)
  * Meme repère que LorensenCline87 :
  * x par vers la droite
  * y vers le haut
  * z part dans la profondeur
  * donc repere main gauche 
  * on note qu'ensuite, le repère de visualisation n'est pas forcément le meme
  */

/**
  * Fonctionnement :
  * On peut générer les cubes de deux facon : en interpolant ou pas la position finale des sommets des triangles le long des cotés.
  *
  * Si on interpole pas, on utilise peu de mémoire car on calcule la position des sommet à la fin, en les placant au milieu des cotés
  * L'interpolation est plus longue : on calcule l'intersection entre l'edge et la sphère à chaque fois qu'on traite un sommet du cube,
  * et si cette intersection est un peu plus loin du sommet traité que la précédente (recouvre la précédente puisque le sommet traité
  * est forcément dans la sphère) alors on la garde.

  * Pour generer une SES (solvent excluded surface - celle que montre pymol et co)
  * - generer la surface solvent accessible : ajouter les sphère avec le rayon (r_i + PROBE_SIZE). Ne pas utiliser l'interpolation, c'est juste pour marquer les cubes
  * - supprimer tous les cubes qui sont à PROBE_SIZE distance des cubes de surface de la SAS (methode contractSurface) qui propage aux sommets
  * - lisser la SES generant la surface de van der Walls : celle avec juste les atomes, donc en ajoutant des sphères de rayon r_i mais en ne modifiant
      que les cubes qui ne sont pas a 255
  */

public class MarchingCubes {
    
    /*private static float COLOR_BASE_R = 255.0f/255.0f;
    private static float COLOR_BASE_V = 255.0f/255.0f;
    private static float COLOR_BASE_B = 255.0f/255.0f;

    private static float COLOR_MAX_NEG_R = 255.0f/255.0f;
    private static float COLOR_MAX_NEG_V = 0.0f/255.0f;
    private static float COLOR_MAX_NEG_B = 0.0f/255.0f;

    private static float COLOR_MAX_POS_R = 0.0f/255.0f;
    private static float COLOR_MAX_POS_V = 0.0f/255.0f;
    private static float COLOR_MAX_POS_B = 255.0f/255.0f;*/

    public class MCMesh
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public int[] triangles;
    }

    public MCube [] Cubes;
	public float TailleCube;
	public int nbX;
	public int nbY;
	public int nbZ;
	public int nbCubes;
	public Vector3 Origin; //< on décale pour le caler autour de l'objet a modéliser
	
	Vector3 [] _Vertices; //< Tableau temporaire pour stoquer les vertices
    Vector3 [] _Normales; //< Tableau temporaire pour stoquer les normales
    int [] _Indices; //< Tableau temporaire pour stoquer les normales
	int _NbVertices; //< Nombre de vertices dans le tableau
	bool _PrecomputeNormals; //<Si on doit calculer les normales pour qu'elles soient lissées
	bool _NormalsPrecomputed; //< Si on a effectivement calculé les normales
    bool _ReuseEdgeVertices; //< Si on ne veut pas dupliquer les sommets dans le vertex buffer

    //int _VertexFloatsCountVBO; //< Nombre de flottants par somment dans le VBO

    public MarchingCubes()
	{
		Cubes = null;
		_Vertices = null;
		TailleCube = 0;
		nbCubes = 0;
		Origin = new Vector3();
		_PrecomputeNormals = false;
		_NormalsPrecomputed = false;
        _ReuseEdgeVertices = true;
	}

	//Si on demande les précalcul des normales,
	//elles seront calculées et lissées (mais allocation mémoire)
	//sinon calculées à la volée au moment du stockage des facettes (mais pas lissées)
    public void setLissageNormales(bool bLissage)
	{
		_PrecomputeNormals = bLissage;
	}

    public void setInterpolation(bool bInterpolation)
	{
		MCube.SetInterpolation(bInterpolation);
	}

    public void setOrigin(Vector3 origin)
	{
		Origin = origin;
	}

    public void create(float sizeX, float sizeY, float sizeZ, float sizeCube, bool interpolation)
	{
		TailleCube = sizeCube;
		this.nbX = (int)(sizeX / sizeCube) + 1;
		this.nbY = (int)(sizeY / sizeCube) + 1;
		this.nbZ = (int)(sizeZ / sizeCube) + 1;
		nbCubes = nbX * nbY * nbZ;

		MCube.updateTaille(TailleCube);
		MCube.SetInterpolation(interpolation);

		Cubes = new MCube[nbCubes];
		if(Cubes == null)
		{
			Debug.LogError("Unable to create cubes, no more memory");
			return;
		}

        //creation des cubes
        for (int i = 0; i < nbCubes; i++)
        {
            Cubes[i] = new MCube();
        }

		/*float taille = (float) Marshal.SizeOf(typeof(MCube))*nbCubes;
		taille /= 1024.0f*1024.0f;
		Debug.Log("Marching cubes : allocating " + 
									taille + 
									"Mo for " + 
									nbCubes + 
									" cubes");*/

        Debug.Log("Marching cubes : allocating " + nbCubes + " cubes");
	}

    //Ne detruit pas les données de géometrie, juste la structure des cubes
    public void destroyCubes()
	{
		Cubes = null;
	}


	//Permet de contracter la surface sur un rayon donné : 
	//On parcourt tous les cubes de surface et on supprime tous les cubes dans un hypercube donné autour de ce cube
	//Pas très précis mais rapide, permet de passer de SAS a SES
	public void contractSurface(float radius)
	{
		//On flag tous les sommets de la surface
		//en effet, on va generer de la surface au fur et a mesure, donc impossible de trouver la surface a contracter des qu'on genrere un peu de la contractée sauf par flag
		byte numFlag = 0;
		for(int z = 0; z < nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = 0; y < nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = 0; x < nbX; x++)
				{
					int indice = x + offsetY + offsetZ;
					int code = Cubes[indice].getCode();
						
					//On flag les cubes de surface comme a traiter
					if(code != 255 && code != 0)
						Cubes[indice].setFlag(numFlag,true);
				}
			}
		}

			
		//_NbThreads = 5;
		/*int stepX = (nbX / _NbThreads);// - radius/TailleCube - 2;

		HANDLE * threadHandles = new HANDLE[_NbThreads];
		PARAMS_THREAD_CONTRACT * params = new PARAMS_THREAD_CONTRACT[_NbThreads];	

		//_cprintf("x: %d\n",stepX);
			
		for(int i=0;i<_NbThreads;i++)
		{
			params[i].MCubes = this;
			params[i].Radius = radius;
			params[i].XStart = (i*nbX) / _NbThreads;// + (radius/(2*TailleCube)) + 1;
			params[i].YStart = 0;
			params[i].ZStart = 0;
			params[i].nbX = stepX;
			params[i].nbY = nbY;
			params[i].nbZ = nbZ;
			threadHandles[i] = (HANDLE)_beginthreadex (null,0,threadContractSurface,params + i,0,null);
		}

		//On attend la fin des threads
		DWORD resWait = WaitForMultipleObjects(_NbThreads,threadHandles,TRUE,INFINITE);
		if(resWait == WAIT_FAILED)
			Debug.LogError(("Error " + toString(GetLastError()) + " waiting for threads when contracting surface").c_str());
				
		Debug.Log("End of surface contration threads");*/

		//On finit les oubliés
		contractSurface(radius,0,nbX,0,nbY,0,nbZ);						
	}



	/*
	* Procédure de contraction de la surface, lancée en thread
	*/
	/*static  unsigned int __stdcall threadContractSurface(void * contractParams)
	{
		PARAMS_THREAD_CONTRACT * params = (PARAMS_THREAD_CONTRACT*) contractParams;
		params->MCubes->contractSurface(params->Radius,params->XStart,params->nbX,params->YStart,params->nbY,params->ZStart,params->nbZ);
		return 0;
	}*/

	//on contracte la surface sur un sous ensemble de l'hypercube, utile pour paralleliser
	void contractSurface(float radius,int startx, int nbX, int starty, int nbY, int startz, int nbZ)
	{
		byte numFlag = 0;

		//On va parcourir un hypercube autour de chaque cube de surface
		int rayonHyperCube = (int)(radius/TailleCube)+1;
		float magDist = radius * radius;

		Vector3 posCubeCentral;
		for(int z = startz; z < startz+nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = starty; y < starty+nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = startx; x < startx+nbX; x++)
				{
					int indice = x + offsetY + offsetZ;

					//Si cube a traiter
					if(Cubes[indice].getFlag(numFlag))
					{
						//On garde la position de ce cube
						posCubeCentral.x = (float)x + 0.5f;
						posCubeCentral.y = (float)y + 0.5f;
						posCubeCentral.z = (float)z + 0.5f;
						posCubeCentral *= TailleCube;

						//On parcourt un hypercube autour du cube
						int xstart = x - rayonHyperCube - 2;
						int ystart = y - rayonHyperCube - 2;
						int zstart = z - rayonHyperCube - 2;
						int nbcubes = rayonHyperCube*2;
						int xfin = xstart + nbcubes + 2;
						int yfin = ystart + nbcubes + 2;
						int zfin = zstart + nbcubes + 2;

						if(xstart < 0) xstart = 0;
						if(ystart < 0) ystart = 0;
						if(zstart < 0) zstart = 0;
						if(xfin >= nbX) xfin = nbX-1;
						if(yfin >= nbY) yfin = nbY-1;
						if(zfin >= nbZ) zfin = nbZ-1;

						//On efface tout les cubes pleins de l'hyper cube qui sont a une bonne distance
						Vector3 posSommet = new Vector3();
						int indiceLocal = 0;
						//int codeLocal =0;
						for(int z2 = zstart; z2 <= zfin; z2++)
						{
							for(int y2 = ystart; y2 <= yfin; y2++)
							{
								for(int x2 = xstart; x2 <= xfin; x2++)		
								{
									indiceLocal = x2+y2*nbX+z2*(nbX*nbY);
									if(Cubes[indiceLocal].getFlag(numFlag) == false)
									{
										posSommet.x = (float)x2;
										posSommet.y = (float)y2;
										posSommet.z = (float)z2;
										posSommet *= TailleCube;
										posSommet -= posCubeCentral;
										//On efface tout ce qui se trouve a portée
										if(posSommet.magnitude <= magDist)							
											valideSommet(false,x2,y2,z2);
											//valideSommetSphere(false,x2,y2,z2,posCubeCentral,radius,0); //Si on veut interpoler, mais deja ca coute cher...
									}
								}
							}
						}

						//On efface les sommets du cube de surface qu'on vient de traiter
						valideSommet(false,x,y,z);
						valideSommet(false,x+1,y,z);
						valideSommet(false,x+1,y+1,z);
						valideSommet(false,x,y+1,z);
						valideSommet(false,x,y,z+1);
						valideSommet(false,x+1,y,z+1);
						valideSommet(false,x+1,y+1,z+1);
						valideSommet(false,x,y+1,z+1);
							
						Cubes[indice].setFlag(numFlag,false);
					}
				}
			}
		}
	}


	//On donne la valeur d'un sommet, il la répercute aux cubes correspondants (huit cubes qui partagent le meme sommet)
	void valideSommetSphere(bool value, int x, int y, int z, ref Vector3 centre, float rayon, float colorShift)
	{
		//Chaque sommet touche potentiellement 8 cubes
		int xprev = x-1;
		int yprev = y-1;
		int zprev = z-1;

		//On fait les 8 cas
		if(z<nbZ)
		{
			if(x<nbX && y<nbY)
				Cubes[x     + y     * nbX + z * (nbX*nbY)].setSommetSphere(0,value,centre,rayon,colorShift);
			if(xprev>=0 && y<nbY)
				Cubes[xprev + y     * nbX + z * (nbX*nbY)].setSommetSphere(1,value,centre,rayon,colorShift);
			if(xprev>=0 && yprev>=0)
				Cubes[xprev + yprev * nbX + z * (nbX*nbY)].setSommetSphere(2,value,centre,rayon,colorShift);
			if(x<nbX && yprev>=0)
				Cubes[x     + yprev * nbX + z * (nbX*nbY)].setSommetSphere(3,value,centre,rayon,colorShift);
		}

		if(zprev>=0)
		{
			if(x<nbX && y<nbY)
				Cubes[x     + y     * nbX + zprev * (nbX*nbY)].setSommetSphere(4,value,centre,rayon,colorShift);
			if(xprev>=0 && y<nbY)
				Cubes[xprev + y     * nbX + zprev * (nbX*nbY)].setSommetSphere(5,value,centre,rayon,colorShift);
			if(xprev>=0 && yprev>=0)
				Cubes[xprev + yprev * nbX + zprev * (nbX*nbY)].setSommetSphere(6,value,centre,rayon,colorShift);
			if(x<nbX && yprev>=0)
				Cubes[x     + yprev * nbX + zprev * (nbX*nbY)].setSommetSphere(7,value,centre,rayon,colorShift);
		}
	}

	//On donne la valeur d'un sommet, il la répercute aux cubes correspondants (huit cubes qui partagent le meme sommet)
	void valideSommet(bool value, int x, int y, int z)
	{
		//Chaque sommet touche potentiellement 8 cubes
		int xprev = x-1;
		int yprev = y-1;
		int zprev = z-1;

		
		//On fait les 8 cas
		if(z<nbZ)
		{
			if(x<nbX && y<nbY)
				Cubes[x     + y     * nbX + z * (nbX*nbY)].setSommet(0,value);
			if(xprev>=0 && y<nbY)
				Cubes[xprev + y     * nbX + z * (nbX*nbY)].setSommet(1,value);
			if(xprev>=0 && yprev>=0)
				Cubes[xprev + yprev * nbX + z * (nbX*nbY)].setSommet(2,value);
			if(x<nbX && yprev>=0)
				Cubes[x     + yprev * nbX + z * (nbX*nbY)].setSommet(3,value);
		}

		if(zprev>=0)
		{
			if(x<nbX && y<nbY)
				Cubes[x     + y     * nbX + zprev * (nbX*nbY)].setSommet(4,value);
			if(xprev>=0 && y<nbY)
				Cubes[xprev + y     * nbX + zprev * (nbX*nbY)].setSommet(5,value);
			if(xprev>=0 && yprev>=0)
				Cubes[xprev + yprev * nbX + zprev * (nbX*nbY)].setSommet(6,value);
			if(x<nbX && yprev>=0)
				Cubes[x     + yprev * nbX + zprev * (nbX*nbY)].setSommet(7,value);
		}
	}

	//On verifie si le sommet a la meme valeur dans tous les cubes
	void checkSommet(int x, int y, int z)
	{
		//Chaque sommet touche potentiellement 8 cubes
		int xprev = x-1;
		int yprev = y-1;
		int zprev = z-1;

		bool [] values = new bool[8];

		for(int i=0;i<8;i++)
			values[i] = false;

		//On fait les 8 cas
		if(z<nbZ)
		{
			if(x<nbX && y<nbY)
				values[0] = Cubes[x     + y     * nbX + z * (nbX*nbY)].getSommet(0);
			if(xprev>=0 && y<nbY)
				values[1] = Cubes[xprev + y     * nbX + z * (nbX*nbY)].getSommet(1);
			if(xprev>=0 && yprev>=0)
				values[2] = Cubes[xprev + yprev * nbX + z * (nbX*nbY)].getSommet(2);
			if(x<nbX && yprev>=0)
				values[3] = Cubes[x     + yprev * nbX + z * (nbX*nbY)].getSommet(3);
		}

		if(zprev>=0)
		{
			if(x<nbX && y<nbY)
				values[4] = Cubes[x     + y     * nbX + zprev * (nbX*nbY)].getSommet(4);
			if(xprev>=0 && y<nbY)
				values[5] = Cubes[xprev + y     * nbX + zprev * (nbX*nbY)].getSommet(5);
			if(xprev>=0 && yprev>=0)
				values[6] = Cubes[xprev + yprev * nbX + zprev * (nbX*nbY)].getSommet(6);
			if(x<nbX && yprev>=0)
				values[7] = Cubes[x     + yprev * nbX + zprev * (nbX*nbY)].getSommet(7);
		}

		bool val = values[0];
		for(int i=1;i<8;i++)
			if(values[i] != val)
			{
				Debug.LogError("Marching cubes : vertex " + 
									i + 
									" has not coherent values");
			}

	}



	//Valide tous les sommets comme 'inside' dans un rayon donné
	public void valideSphere(Vector3 centreCercle, float rayon, float colorShift)
	{
		//Changement de repere passage dans le repère des marching cubes
		centreCercle -= Origin;

		if(centreCercle.x < 0.0f || centreCercle.x > TailleCube * nbX)
			Debug.LogError("Error 1 in valideSphere");
		if(centreCercle.y < 0.0f || centreCercle.y > TailleCube * nbY)
			Debug.LogError("Error 2 in valideSphere");
		if(centreCercle.z < 0.0f || centreCercle.z > TailleCube * nbZ)
			Debug.LogError("Error 3 in valideSphere");

		//On parcourt un hypercube autour du centre
		int xstart = (int)((centreCercle.x - rayon)/TailleCube);
		int ystart = (int)((centreCercle.y - rayon)/TailleCube);
		int zstart = (int)((centreCercle.z - rayon)/TailleCube);
		int nbcubes = (int)(((rayon*2.0f)/TailleCube))+1;
		int xfin = xstart + nbcubes -1;
		int yfin = ystart + nbcubes -1;
		int zfin = zstart + nbcubes -1;

		if(xstart < 0) xstart = 0;
		if(ystart < 0) ystart = 0;
		if(zstart < 0) zstart = 0;
		if(xfin >= nbX) xfin = nbX-1;
		if(yfin >= nbY) yfin = nbY-1;
		if(zfin >= nbZ) zfin = nbZ-1;
			
		double magRayon = rayon*rayon;
		Vector3 posSommet = new Vector3();

		//On fait un premier parcours sans interpolation
		bool interpolation = MCube.Interpolation;
		MCube.SetInterpolation(false);
		for(int z = zstart; z <= zfin; z++)
		{
			for(int y = ystart; y <= yfin; y++)
			{
				for(int x = xstart; x <= xfin; x++)		
				{
					//On teste si le sommet est dans la sphère
					posSommet.x = (float)x;
					posSommet.y = (float)y;
					posSommet.z = (float)z;
					posSommet *= TailleCube;
					posSommet = centreCercle - posSommet;
					double magDist = posSommet.sqrMagnitude;
					if(magDist <= magRayon)
					{
						valideSommetSphere(true,x,y,z,ref posSommet,rayon,colorShift);
					}	
				}
			}
		}

		//On fait un second parcours avec interpolation
		if(interpolation)
		{
			MCube.SetInterpolation(interpolation);
			for(int z = zstart; z <= zfin; z++)
			{
				for(int y = ystart; y <= yfin; y++)
				{
					for(int x = xstart; x <= xfin; x++)		
					{
						//On teste si le sommet est dans la sphère
						posSommet.x = (float)x;
						posSommet.y = (float)y;
						posSommet.z = (float)z;
						posSommet *= TailleCube;
						posSommet = centreCercle - posSommet;
                        double magDist = posSommet.sqrMagnitude;
						if(magDist <= magRayon)
						{
							valideSommetSphere(true,x,y,z,ref posSommet,rayon,colorShift);
						}	
					}
				}
			}
		}
	}

	//Retourne le nombre de facettes qu genere cette sphere, étant donne l'état des cubes en ce moment
	public int getNbFacettesSphere(Vector3 centreCercle, float rayon)
	{
		//On va compter le nombres de cubes qui ont une facette
		int nbFacettes = 0;

		//Changement de repere passage dans le repère des marching cubes
		centreCercle -= Origin;

		if(centreCercle.x < 0.0f || centreCercle.x > TailleCube * nbX)
			Debug.LogError("Error 1 in getNbFacettesSphere");
		if(centreCercle.y < 0.0f || centreCercle.y > TailleCube * nbY)
			Debug.LogError("Error 2 in getNbFacettesSphere");
		if(centreCercle.z < 0.0f || centreCercle.z > TailleCube * nbZ)
			Debug.LogError("Error 3 in getNbFacettesSphere");

		//On parcourt un hypercube autour du centre
		int xstart = (int)((centreCercle.x - rayon)/TailleCube);
		int ystart = (int)((centreCercle.y - rayon)/TailleCube);
		int zstart = (int)((centreCercle.z - rayon)/TailleCube);
		int nbcubes = (int)(((rayon*2.0f)/TailleCube))+1;
		int xfin = xstart + nbcubes -1;
		int yfin = ystart + nbcubes -1;
		int zfin = zstart + nbcubes -1;

		if(xstart < 0) xstart = 0;
		if(ystart < 0) ystart = 0;
		if(zstart < 0) zstart = 0;
		if(xfin >= nbX) xfin = nbX-1;
		if(yfin >= nbY) yfin = nbY-1;
		if(zfin >= nbZ) zfin = nbZ-1;

		double magRayon = rayon*rayon;
		Vector3 posCube = new Vector3();

		for(int z = zstart; z <= zfin; z++)
		{
			for(int y = ystart; y <= yfin; y++)
			{
				for(int x = xstart; x <= xfin; x++)		
				{
					//On teste si le cube est proche de la sphere
					posCube.x = (float)x + 0.5f;
					posCube.y = (float)y + 0.5f;
					posCube.z = (float)z + 0.5f;
					posCube *= TailleCube;
					posCube = centreCercle - posCube;
					posCube -= new Vector3(TailleCube,TailleCube,TailleCube); //On se laisse un peu de marge
					double magDist = posCube.magnitude;
						
					//Si le cube est assez proche de la sphere
					if(magDist <= magRayon)
					{
						byte code = Cubes[x     + y * nbX + z * (nbX*nbY)].getCode();
						if(code != 255 && code != 0)
							nbFacettes++;
					}
				}
			}
		}

		return nbFacettes;
	}

	public void diffuseColorShift()
	{
		for(int z = 0; z < nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = 0; y < nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = 0; x < nbX; x++)
				{
					int indice = x + offsetY + offsetZ;

					//if(Cubes[indice].ColorShift != 0.0f)
						//continue;
						
					//Pour tous les cubes qui l'entourent
					int zDeb = Math.Max(z-2,0);
					int zFin = Math.Min(z+2,nbZ-2);
					int yDeb = Math.Max(y-2,0);
					int yFin = Math.Min(y+2,nbY-2);
					int xDeb = Math.Max(x-2,0);
					int xFin = Math.Min(x+2,nbX-2);

					float newColorShift = 0.0f;
					float nbAdd = 0.0f;
						
					for(int z2 = zDeb; z2 <= zFin; z2++)
					{
						int offsetZ2 = z2 * (nbX*nbY);
						for(int y2 = yDeb; y2 <= yFin; y2++)
						{
							int offsetY2 = y2 * nbX;
							for(int x2 = xDeb; x2 <= xFin; x2++)
							{
								int indice2 = x2 + offsetY2 + offsetZ2;
									
								newColorShift += Cubes[indice2].ColorShift;
								nbAdd++;
									
							}
						}
					}

					newColorShift /= nbAdd;
					Cubes[indice].Temp = newColorShift;
				}
			}
		}

		//On affecte les nouvelles valeurs
		float minColorShift = Cubes[0].Temp;
		float maxColorShift = Cubes[0].Temp;
		for(int i = 0; i < nbCubes; i++)
		{
			Cubes[i].ColorShift = Cubes[i].Temp;
			if(Cubes[i].ColorShift < minColorShift)
				minColorShift = Cubes[i].ColorShift;
			if(Cubes[i].ColorShift > maxColorShift)
				maxColorShift = Cubes[i].ColorShift;
		}
		Debug.Log("Energy max : "+maxColorShift+", energy min : "+minColorShift);
	}


	//Il faut avoir calculé les normales avant de faire ca
	//CALCUL FAUX : on compte une charge par cube donc varie en fonction du nombe de cubes par atomes....
	public void coulombSimplifyColorShift(float solventSize, ref float pcent, float pcentWidth)
	{
		Debug.Log("Simplify color shift");
			
		//On en a besoin pour le cote de la face
		//Si on calcule les normales, on les lisse forcément (sinon a la volée si pas lissée, économise mémoire)
		setLissageNormales(true);
		computeNormals();
			
		int sizeConvolution = (int)(5.0f / TailleCube)+1;
			 
		float pcentStep = pcentWidth / (float)nbCubes;

		for(int z = 0; z < nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = 0; y < nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = 0; x < nbX; x++)
				{
					pcent += pcentStep;
						
					int indice = x + offsetY + offsetZ;

					byte code = Cubes[indice].getCode();
					if(code == 255 || code == 0)
						continue;						

					//On recup la normale globale du cube en cours
					Vector3 normaleGlobale = new Vector3();
					Cubes[indice].getGlobalNormal(ref normaleGlobale);
					normaleGlobale *= solventSize;

					//On place un point a solventSize angstrom de la surface
					Vector3 vbase = new Vector3((float)x,(float)y,(float)z)*TailleCube + normaleGlobale; //Origine du repère pas origine world, mais on travaille en relatif
						
					//Pour tous les cubes qui l'entourent
					int zDeb = Math.Max(z-sizeConvolution,0);
					int zFin = Math.Min(z+sizeConvolution,nbZ-1);
					int yDeb = Math.Max(y-sizeConvolution,0);
					int yFin = Math.Min(y+sizeConvolution,nbY-1);
					int xDeb = Math.Max(x-sizeConvolution,0);
					int xFin = Math.Min(x+sizeConvolution,nbX-1);

					float sumColorShift = 0.0f;
					float coeffSum = 0.0f;

					for(int z2 = zDeb; z2 <= zFin; z2++)
					{
						int offsetZ2 = z2 * (nbX*nbY);
						for(int y2 = yDeb; y2 <= yFin; y2++)
						{
							int offsetY2 = y2 * nbX;
							for(int x2 = xDeb; x2 <= xFin; x2++)
							{
								int indice2 = x2 + offsetY2 + offsetZ2;

								//Si c'est un cube de surface
								code = Cubes[indice2].getCode();
								if(/*code != 255 &&*/ code != 0)
								{
									//Distance de la référence à ce cube
                                    float dist = (vbase - (new Vector3((float)x2, (float)y2, (float)z2) * TailleCube)).sqrMagnitude;
									coeffSum += 1.0f/dist;
									coeffSum++;
									sumColorShift += Cubes[indice2].ColorShift;//distCarre;
								}
							}
						}
					}

					sumColorShift /= coeffSum;
					Cubes[indice].Temp = sumColorShift;

				}
			}
		}


		//On affecte les nouvelles valeurs
		float minColorShift = Cubes[0].Temp;
		float maxColorShift = Cubes[0].Temp;
		for(int i = 0; i < nbCubes; i++)
		{
			Cubes[i].ColorShift = Cubes[i].Temp;
			if(Cubes[i].ColorShift < minColorShift)
				minColorShift = Cubes[i].ColorShift;
			if(Cubes[i].ColorShift > maxColorShift)
				maxColorShift = Cubes[i].ColorShift;
		}
		Debug.Log("Energy max : "+maxColorShift+", energy min : "+minColorShift);

	}

	void getCubeBarycentreCoords(ref Vector3 point, int x, int y, int z)
	{
		Cubes[z*(nbX*nbY)+y*nbX+x].getFaceCenter(ref point);
		point.x += x;
		point.y += y;
		point.z += z;
		point *= TailleCube;
		point += Origin;
	}

	void getCubeCenter(ref Vector3 point, int x, int y, int z)
	{
		point.x = x*TailleCube + TailleCube/2.0f;
		point.y = y*TailleCube + TailleCube/2.0f;
		point.z = z*TailleCube + TailleCube/2.0f;
		point += Origin;
	}

	byte getCubeCode(int x, int y, int z)
	{
		return Cubes[z*(nbX*nbY)+y*nbX+x].getCode();
	}

	void setCubeColorShift(float shift, int x, int y, int z)
	{
		Cubes[z*(nbX*nbY)+y*nbX+x].ColorShift = shift;
	}

	void getCubeFaceNormal(ref Vector3 normal,  int x, int y, int z)
	{
		Cubes[z*(nbX*nbY)+y*nbX+x].getGlobalNormal(ref normal);
	}

	//Permet de précalculer des normales lissées (sinon on les calcule dans le make geometry)
	private void lissageNormales(int nbPasses)
	{
		Debug.Log("Lissage des normales");

		//On parcourt tous les cubes
		for(int z = 0; z < nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = 0; y < nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = 0; x < nbX; x++)
				{
					int indice = x + offsetY + offsetZ;
					Cubes[indice].calcNormals();
				}
			}
		}

		//On a les normales calculées
		//On va les moyenner
		//On parcourt tous les cubes
		Vector3 somme = new Vector3(); //Pour stoquer la somme
		Vector3 normal = new Vector3(); //Pour requp la normale
        Vector3 sommetBase = new Vector3(); //le sommet dont on calcule la normale (un edgeVector)
        //Vector3 sommetVoisin = new Vector3(); //le sommet voisins dont on recup la normale (un edgeVector)
        for (int passe = 0; passe < nbPasses; passe++)
        {
			//On moyenne les normales sur chaque face de chaque cube
			//On parcourt tous les cubes
			for(int z = 0; z < nbZ; z++)
			{
				int offsetZ = z * (nbX*nbY);
				for(int y = 0; y < nbY; y++)
				{
					int offsetY = y * nbX;
					for(int x = 0; x < nbX; x++)
					{
						int indice = x + offsetY + offsetZ;
						Cubes[indice].meanNormals();
					}
				}
			}

            for (int z = 0; z < nbZ; z++)
            {
                int offsetZ = z * (nbX * nbY);
                for (int y = 0; y < nbY; y++)
                {
                    int offsetY = y * nbX;
                    for (int x = 0; x < nbX; x++)
                    {
                        int indice = x + offsetY + offsetZ;

                        byte code = Cubes[indice].getCode();

                        //Si c'est un cube de surface
                        if (code != 255 && code != 0)
                        {
							//On fait tous les edges : on somme avec les voisins (et on leur affecte)
                            for (int i = 0; i < 12; i++)
                            {
                                Cubes[indice].getEdgeNormal(ref somme, i);
                                Cubes[indice].getEdgeVertice(ref sommetBase, i);
                                //float sommeDist = 0.0f;
                                
                                //On somme les 3 voisins
                                for (int j = 0; j < 3; j++)
                                {
                                    int xAdd = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 0] + x;
                                    int yAdd = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 1] + y;
                                    int zAdd = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 2] + z;
                                    int vert = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 3];

                                    if (xAdd < 0 || xAdd >= nbX || yAdd < 0 || yAdd >= nbY || zAdd < 0 || zAdd >= nbZ)
                                        continue;

                                    int indiceAdd = xAdd + yAdd * nbX + zAdd * (nbX * nbY);

                                    Cubes[indiceAdd].getEdgeNormal(ref normal, vert);
                                    //Cubes[indiceAdd].getEdgeVertice(ref sommetVoisin, vert);

                                    //On calcule la distance entre les sommets, pour pondérer (on tient plus en compte une normale proche que lointaine)
                                    //float dist = (sommetVoisin - sommetBase).magnitude;


									somme += normal;// / dist;
                                    //sommeDist += dist; 
                                }

								//somme *= sommeDist;
                                somme.Normalize();

                                Cubes[indice].setEdgeNormal(ref somme, i);

                                //On affecte aux 3 voisins
                                /*for (int j = 0; j < 3; j++)
                                {
                                    int xAdd = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 0] + x;
                                    int yAdd = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 1] + y;
                                    int zAdd = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 2] + z;
                                    int vert = MCube.VoisinFromEdge[i * (3 * 4) + (j * 4) + 3];

                                    if (xAdd < 0 || xAdd >= nbX || yAdd < 0 || yAdd >= nbY || zAdd < 0 || zAdd >= nbZ)
                                        continue;

                                    int indiceAdd = xAdd + yAdd * nbX + zAdd * (nbX * nbY);

                                    Cubes[indiceAdd].setEdgeNormal(ref somme, vert);
                                }*/
                            }
                        }
                    }
                }
            }


        }

        _NormalsPrecomputed = true;

        //DEBUG
        for (int z = 0; z < nbZ; z++)
        {
            int offsetZ = z * (nbX * nbY);
            for (int y = 0; y < nbY; y++)
            {
                int offsetY = y * nbX;
                for (int x = 0; x < nbX; x++)
                {
                    int indice = x + offsetY + offsetZ;

                    byte code = Cubes[indice].getCode();

                    //Si c'est un cube de surface
                    if (code != 255 && code != 0)
                    {
                        Vector3 norm = new Vector3();
                        for (int i = 0; i < 12; i++)
                        {
                            Cubes[indice].getEdgeNormal(ref norm, i);                            
                        }
                        return;
 
                    }
                }
            }
        }


		
	}

	

	/**
		* A n'appeler que si la géométrie a été généree
		*/
	/*public void saveToBinFile(const char * file)
	{
		FILE * fs = fopen(file,"wb");
		if(fs == null)
		{
			Debug.LogError(("Cannot save molecule to binary file "+toString(file)).c_str());
		}
		else
		{
			unsigned char * pt = (unsigned char*)_Vertices;
			for(unsigned long i=0;i<_NbVertices*_VertexFloatsCountVBO*sizeof(float);i++)
			{
				fprintf(fs,"%c",pt[i]);
			}
			fclose(fs);
		}
	}*/

	/**
		* A n'appeler que si la géométrie a été généree
		*/
	/*public void saveToObjFile(const char * file)
	{			
		FILE * fs = fopen(file,"wb");
		if(fs == null)
		{
			Debug.LogError(("Cannot save molecule to obj file "+toString(file)).c_str());
		}
		else
		{
			//On sort les vertex
			for(unsigned long i=0;i<_NbVertices*3;i+=3)
			{
				fprintf(fs,"v %f %f %f\n",_Vertices[i],_Vertices[i+2],_Vertices[i+1]); //SENS POUR CULLING UNITY
			}

			fprintf(fs,"\n");

			//On sort les faces
			for(unsigned long i=1;i<=_NbVertices;i+=3)
			{
				fprintf(fs,"f %d %d %d\n",i,i+1,i+2);
			}
			fclose(fs);

			Debug.Log(("Saved to obj file "+toString(file)).c_str());
		}
	}*/

	public void computeNormals()
	{
        if (_PrecomputeNormals && !_NormalsPrecomputed)
            lissageNormales(20);


	}

    public void makeGeometryFaces(Vector3 translateOrigin, ref MCMesh mesh)
    {
        //On se recalcule les normales si on est en mode interpolation et donc si les edges sont deja calcules
        computeNormals();

        //Init des constantes pour le rendu
        //_VertexFloatsCountVBO = 4 + 3 + 3; //Taille d'un vertex en nombre de floats dans le VBO

        //On calcule le nombre de triangles qu'on va devoir stoquer
        int nbTriangles = 0;
        for (int i = 0; i < nbCubes; i++)
            nbTriangles += Cubes[i].getNbTriangles();

		Debug.Log(nbTriangles+" triangles in MarchingCube");

        //D'ou le nombre de sommets
        _NbVertices = nbTriangles * 3;

        //On fera un tableau type C4F_V3F
        _Vertices = new Vector3[_NbVertices];
        _Normales = new Vector3[_NbVertices];
        _Indices = new int[_NbVertices];

        if (_Vertices == null)
        {
            Debug.LogError("Memory allocation failed for _Vertices of MarchingCubes");
            return;
        }

        //On construit le tableau de vertices
        int nbIndices = 0;
        int nbVertices = 0;

        //On prepare les couleurs
        //Color colorMaxPos = new Color(COLOR_MAX_POS_R, COLOR_MAX_POS_V, COLOR_MAX_POS_B, 1.0f);
        //Color colorMaxNeg = new Color(COLOR_MAX_NEG_R, COLOR_MAX_NEG_V, COLOR_MAX_NEG_B, 1.0f);
        //Color colorBase = new Color(COLOR_BASE_R, COLOR_BASE_V, COLOR_BASE_B, 1.0f);

        //On parcourt tous les cubes
        for (int z = 0; z < nbZ; z++)
        {
            int offsetZ = z * (nbX * nbY);
            for (int y = 0; y < nbY; y++)
            {
                int offsetY = y * nbX;
                for (int x = 0; x < nbX; x++)
                {
                    int indice = x + offsetY + offsetZ;

                    byte code = Cubes[indice].getCode();
                    int offset = code * 15;

                    //On ajoute les triangles
                    int i = 0;

                    //On fait face par face
                    Vector3[] vertice = new Vector3[3];
                    for (int k = 0; k < 3; k++)
                        vertice[k] = new Vector3();
                    Vector3[] normals = new Vector3[3];
                    for (int k = 0; k < 3; k++)
                        normals[k] = new Vector3();

                    while (MCube.TianglesPerCode[offset + i] != -1 && i < 15)
                    {
                        //On chope une face
                        for (int j = 0; j < 3; j++)
                        {
                            //On chope le numéro d'edge vertice
                            int vertNum = MCube.TianglesPerCode[offset + i + j];

                            //On en récup le point correspondant, dans l'espace d'un cube placé à l'origine
                            Cubes[indice].getEdgeVertice(ref vertice[j], vertNum);

                            //On place le vertice dans le bon espace (on le translate au niveau du cube concerne)
                            vertice[j].x += x * TailleCube;
                            vertice[j].y += y * TailleCube;
                            vertice[j].z += z * TailleCube;

                            vertice[j] += translateOrigin + Origin;

                            //Si normale précalculée on la chope
                            if (_NormalsPrecomputed)
                            {
                                Cubes[indice].getEdgeNormal(ref normals[j], vertNum);
                                normals[j].Normalize(); //On les a pas re normalisées lors du lissage
                            }
                        }


                        //Si pas calculées, on la calcule a la volee
                        if (!_NormalsPrecomputed)
                        {
                            //On calcule la normale
                            Vector3 v1 = vertice[1] - vertice[0];
                            Vector3 v2 = vertice[2] - vertice[0];
                            Vector3 normal = Vector3.Cross(v2, v1);
                            normal.Normalize();

                            //Ca sera la meme pour les trois
                            for (int j = 0; j < 3; j++)
                                normals[j] = normal;
                        }


                        //On ajoute la face
						//Le bon sens pour GL_CULLFACES
                        for (int j = 2; j >= 0; j--) 
                        {
                            //On chope le numéro de vertice
                            int vertNum = MCube.TianglesPerCode[offset + i + j];

                            //On verifie si un voisin l'a pas deja mis en buffer
                            int indiceBufferGeom = -1;

                            if (_ReuseEdgeVertices)
                            {
                                //Pour les trois voisins 
                                for (int k = 0; k < 3; k++)
                                {
                                    int xVoisin = MCube.VoisinFromEdge[vertNum * (3 * 4) + (k * 4) + 0] + x;
                                    int yVoisin = MCube.VoisinFromEdge[vertNum * (3 * 4) + (k * 4) + 1] + y;
                                    int zVoisin = MCube.VoisinFromEdge[vertNum * (3 * 4) + (k * 4) + 2] + z;
                                    int vert    = MCube.VoisinFromEdge[vertNum * (3 * 4) + (k * 4) + 3];

                                    if (xVoisin < 0 || xVoisin >= nbX || yVoisin < 0 || yVoisin >= nbY || zVoisin < 0 || zVoisin >= nbZ)
                                        continue;

                                    int cubeVoisinIndice = xVoisin + yVoisin * nbX + zVoisin * (nbX * nbY);

                                    int edgeVectorIndice = Cubes[cubeVoisinIndice].getEdgeVerticeIndice(vert);

                                    if (edgeVectorIndice >= 0)
                                    {
                                        indiceBufferGeom = edgeVectorIndice; 
                                        break; 
                                    }
                                }
                            }

                            //Si il n'existe pas deja
                            if (indiceBufferGeom < 0 || !_ReuseEdgeVertices)
                            {
                                
                                _Normales[nbVertices] = normals[j];
                                _Vertices[nbVertices] = vertice[j];
                                _Indices[nbIndices] = nbVertices;

                                //On note l'indice ou l'on a stoqué le vertice
                                Cubes[indice].setEdgeVerticeIndice(vertNum, nbVertices);

                                //On avance
                                nbVertices++;
                                nbIndices++;
                            }
                            else
                            {
                                _Indices[nbIndices] = indiceBufferGeom; 
                                nbIndices++;
                            }


                            /*if (Cubes[indice].ColorShift > 0)
                            {
                                NYColor res = colorBase.interpolate(colorMaxPos, Cubes[indice].ColorShift);
                                *ptVertices = res.R; ptVertices++;
                                *ptVertices = res.V; ptVertices++;
                                *ptVertices = res.B; ptVertices++;
                            }

                            if (Cubes[indice].ColorShift < 0)
                            {
                                NYColor res = colorBase.interpolate(colorMaxNeg, -Cubes[indice].ColorShift);
                                *ptVertices = res.R; ptVertices++;
                                *ptVertices = res.V; ptVertices++;
                                *ptVertices = res.B; ptVertices++;
                            }

                            if (Cubes[indice].ColorShift == 0)
                            {
                                *ptVertices = COLOR_BASE_R; ptVertices++;
                                *ptVertices = COLOR_BASE_V; ptVertices++;
                                *ptVertices = COLOR_BASE_B; ptVertices++;
                            }

                            *ptVertices = 1.0f; ptVertices++;*/

                            
                        }

                        i += 3;
                    }
                }
            }

        }

        Debug.Log("Molecule mesh has " + nbVertices + " vertices and " + (nbIndices/3) + " triangles");
        
        Vector3 [] GoodVertices = new Vector3[nbVertices];
        for (int i = 0; i < nbVertices; i++)
            GoodVertices[i] = _Vertices[i];
        Vector3[] GoodNormals = new Vector3[nbVertices];
        for (int i = 0; i < nbVertices; i++)
            GoodNormals[i] = _Normales[i];
        
        mesh.vertices = GoodVertices;
        mesh.normals = GoodNormals;
        mesh.triangles = _Indices;
    }

  	/*public void makeVerticesOnlyBuffer(NYVert3Df & translateOrigin)
	{
		//On calcule le nombre de triangles qu'on va devoir stoquer
		int nbTriangles = 0;
		for(int i=0;i<NbCubes;i++)
			nbTriangles += Cubes[i].getNbTriangles();

		//D'ou le nombre de sommets
		_NbVertices = nbTriangles * 3;

		//On fait un tableau type V3F seulement
		_Vertices = new float[3 * _NbVertices];
		if(_Vertices == null)
		{
			Debug.LogError("Memory allocation failed for _Vertices of MarchingCubes");
			return;
		}

		//On construit le tableau de vertices
		float * ptVertices = _Vertices;

		//On parcourt tous les cubes
		for(int z = 0; z < nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = 0; y < nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = 0; x < nbX; x++)
				{
					int indice = x + offsetY + offsetZ;

					uint8 code = Cubes[indice].getCode();
					int offset = code*15;

					//On ajoute les triangles
					int i=0;
						
					//On fait face par face
					NYVert3Df vertice[3];
					while(MCube.TianglesPerCode[offset + i] != -1 && i < 15)
					{							
						//On chope une face
						for(int j=0;j<3;j++)
						{
							//On chope le numéro de vertice
							int vertNum = MCube.TianglesPerCode[offset + i + j];

							//On en récup le point correspondant, dans l'espace d'un cube placé à l'origine
							Cubes[indice].getEdgeVertice(vertice[j],vertNum);

							//On place le vertice dans le bon espace (on le translate au niveau du cube concerne)
							vertice[j].X += (x * TailleCube);
							vertice[j].Y += (y * TailleCube);
							vertice[j].Z += (z * TailleCube);
						}

						//On ajoute la face
						for(int j=0;j<3;j++)
						{
							*ptVertices = vertice[j].X + Origin.X + translateOrigin.X; ptVertices++;
							*ptVertices = vertice[j].Y + Origin.Y + translateOrigin.Y; ptVertices++;
							*ptVertices = vertice[j].Z + Origin.Z + translateOrigin.Z; ptVertices++;
						}
	
						i+=3;
					}
				}
			}
		}

		//On detruit pas pour pouvoir ensuite sauver dans un fichier si on veut
		//On detruit les buffers
		//SAFEDELETE(_Vertices);
	}

	public void destroyTempGeometry(void)
	{
		SAFEDELETE_TAB(_Vertices);
	}


	//Fait une geom de debug, avec plein de points
	public void makeGeometry(void)
	{
		//Init des constantes pour le rendu
		_VertexFloatsCountVBO = 4+3; //Taille d'un vertex en nombre de floats dans le VBO
			
		//On convertit les points en géometrie affichable
		int nbPoints = NbCubes * 8;

		//On fera un tableau type C4F_V3F
		_Vertices = new float[_VertexFloatsCountVBO * nbPoints];
		_NbVertices = nbPoints;
		if(_Vertices == null)
		{
			Debug.LogError("Memory allocation failed for _Vertices of MarchingCubes");
			return;
		}

		//On construit le tableau de vertices
		float * ptVertices = _Vertices;

		//On parcourt tous les cubes
		for(int z = 0; z < nbZ; z++)
		{
			int offsetZ = z * (nbX*nbY);
			for(int y = 0; y < nbY; y++)
			{
				int offsetY = y * nbX;
				for(int x = 0; x < nbX; x++)
				{
					int indice = x + offsetY + offsetZ;

					//point 1
					*ptVertices = Cubes[indice].getSommet(0) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(0) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(0) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = x*TailleCube; ptVertices++;
					*ptVertices = y*TailleCube; ptVertices++;
					*ptVertices = z*TailleCube; ptVertices++;

					//point 2
					*ptVertices = Cubes[indice].getSommet(1) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(1) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(1) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = (x+0.9f)*TailleCube; ptVertices++;
					*ptVertices = y*TailleCube; ptVertices++;
					*ptVertices = z*TailleCube; ptVertices++;

					//point 3
					*ptVertices = Cubes[indice].getSommet(2) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(2) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(2) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = (x+0.9f)*TailleCube; ptVertices++;
					*ptVertices = (y+0.9f)*TailleCube; ptVertices++;
					*ptVertices = z*TailleCube; ptVertices++;

					//point 4
					*ptVertices = Cubes[indice].getSommet(3) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(3) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(3) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = x*TailleCube; ptVertices++;
					*ptVertices = (y+0.9f)*TailleCube; ptVertices++;
					*ptVertices = z*TailleCube; ptVertices++;

					//point 5
					*ptVertices = Cubes[indice].getSommet(4) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(4) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(4) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = x*TailleCube; ptVertices++;
					*ptVertices = y*TailleCube; ptVertices++;
					*ptVertices = (z+0.9f)*TailleCube; ptVertices++;

					//point 6
					*ptVertices = Cubes[indice].getSommet(5) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(5) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(5) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = (x+0.9f)*TailleCube; ptVertices++;
					*ptVertices = y*TailleCube; ptVertices++;
					*ptVertices = (z+0.9f)*TailleCube; ptVertices++;

					//point 7
					*ptVertices = Cubes[indice].getSommet(6) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(6) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(6) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = (x+0.9f)*TailleCube; ptVertices++;
					*ptVertices = (y+0.9f)*TailleCube; ptVertices++;
					*ptVertices = (z+0.9f)*TailleCube; ptVertices++;

					//point 8
					*ptVertices = Cubes[indice].getSommet(7) ? 0.0f : 1.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(7) ? 1.0f : 0.0f; ptVertices++;
					*ptVertices = Cubes[indice].getSommet(7) ? 0.0f : 0.0f; ptVertices++;
					*ptVertices = 1.0f; ptVertices++;

					*ptVertices = x*TailleCube; ptVertices++;
					*ptVertices = (y+0.9f)*TailleCube; ptVertices++;
					*ptVertices = (z+0.9f)*TailleCube; ptVertices++;
				}
			}
		}

		//On cree les VBO
		//On détruit si existe
		if(_BufGeom != 0)
			glDeleteBuffers(1, &_BufGeom);
		_BufGeom = 0;
			
		//Generation des buffers
		glGenBuffers(1, &_BufGeom);
			
		//On met les vertices
		glBindBuffer(GL_ARRAY_BUFFER, _BufGeom);

		glBufferData(GL_ARRAY_BUFFER, nbPoints*_VertexFloatsCountVBO*sizeof(float), _Vertices, GL_STREAM_DRAW); 

		NYRenderer::checkGlError("glBufferData(GL_ARRAY_BUFFER, _NbAtomes*_AtomVertexArraySize*sizeof(float), _Vertices, GL_STREAM_DRAW);");

		//On debind
		glBindBuffer(GL_ARRAY_BUFFER, 0);
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);

		//On detruit les buffers
		//SAFEDELETE(_Vertices);
	}*/

	/*void render(void)
	{
		glPushMatrix();
		glTranslatef(Origin.X,Origin.Y,Origin.Z);

		glEnable(GL_COLOR_MATERIAL);
		glEnable(GL_LIGHTING);

		glBindBuffer(GL_ARRAY_BUFFER, _BufGeom);

		// activation des tableaux de sommets 
		glEnableClientState(GL_COLOR_ARRAY);
		glEnableClientState(GL_VERTEX_ARRAY);

		glColorPointer(4, GL_FLOAT,_VertexFloatsCountVBO * sizeof(float), BUFFER_OFFSET(0));

		NYRenderer::checkGlError("glColorPointer(4, GL_FLOAT,_VertexFloatsCountVBO*sizeof(float), BUFFER_OFFSET(0));");

		glVertexPointer(3, GL_FLOAT,_VertexFloatsCountVBO * sizeof(float),BUFFER_OFFSET(4*sizeof(float))); 

		NYRenderer::checkGlError("glVertexPointer(3, GL_FLOAT,_VertexFloatsCountVBO*sizeof(float),BUFFER_OFFSET(7*sizeof(float))); ");

		glDrawArrays(GL_POINTS,0,_NbVertices);
		NYRenderer::checkGlError("glDrawArrays(GL_POINTS,0,_NbVertices);");

		glBindBuffer(GL_ARRAY_BUFFER, 0);

		glDisableClientState(GL_COLOR_ARRAY);
		glDisableClientState(GL_VERTEX_ARRAY);

		glDisable(GL_COLOR_MATERIAL);
		glDisable(GL_LIGHTING);
		glPopMatrix();
	}

	void renderFaces(void)
	{
		glPushMatrix();
		glTranslatef(Origin.X,Origin.Y,Origin.Z);

		glEnable(GL_COLOR_MATERIAL);
		glEnable(GL_LIGHTING);

		glBindBuffer(GL_ARRAY_BUFFER, _BufGeom);

		//activation des tableaux de sommets 
		glEnableClientState(GL_COLOR_ARRAY);
		glEnableClientState(GL_NORMAL_ARRAY);
		glEnableClientState(GL_VERTEX_ARRAY);

		glColorPointer(4, GL_FLOAT,_VertexFloatsCountVBO * sizeof(float), BUFFER_OFFSET(0));

		NYRenderer::checkGlError("glColorPointer(4, GL_FLOAT,_VertexFloatsCountVBO*sizeof(float), BUFFER_OFFSET(0));");

		glNormalPointer( GL_FLOAT,_VertexFloatsCountVBO * sizeof(float),BUFFER_OFFSET(4*sizeof(float)));

		NYRenderer::checkGlError("glNormalPointer( GL_FLOAT,_VertexFloatsCountVBO * sizeof(float),BUFFER_OFFSET(4*sizeof(float)));");

		glVertexPointer(3, GL_FLOAT,_VertexFloatsCountVBO * sizeof(float),BUFFER_OFFSET(7*sizeof(float))); 

		NYRenderer::checkGlError("glVertexPointer(3, GL_FLOAT,_VertexFloatsCountVBO*sizeof(float),BUFFER_OFFSET(7*sizeof(float))); ");

		glDrawArrays(GL_TRIANGLES,0,_NbVertices);
		NYRenderer::checkGlError("glDrawArrays(GL_POINTS,0,_NbVertices);");

		glDisable(GL_COLOR_MATERIAL);
		glDisable(GL_LIGHTING);
		glPopMatrix();
	}*/

}