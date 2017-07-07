using UnityEngine;
using System.Collections;
using System;

//Un cube de l'algo des marching cubes
public class MCube
{
    byte Sommets; ///< Le numero de bit correspond à l'ordre de LorensenCline1987
    ushort EdgeVectorDone; ///< Permet de savoir si on a deja calculé l'interpolation d'un sommet (pour initialiser la première fois) - champ bit
    Vector3[] EdgesVectors; ///< Contient les vecteurs d'edges qu'on interpole sur la surface
    Vector3[] Normals; ///< Les normales au sommet, qu'on calcule si on veut les lisser
    int [] EdgesVectorIndices; ///< Quand on génère la géométrie, stoque l'indice ou l'on place le vertex dans le buffer. Permet de réutiliser cet edge vector quand on génère les facettes d'un cube suivant, plutot que d'en ajouter un nouveau 
    byte Flags; ///< Utilise pour flager des cubes. 0: contraction surface 1: diffusion colorshift


    public float ColorShift; ///< Modifie la couleur de base (par exemple pour la charge des atomes)
    public float Temp; ///< Permet de stoquer un temporaire, par exemple pour un lissage

    public static int[] TianglesPerCode = 
    {
	 -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 1, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 2, 11, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 8, 3, 2, 11, 8, 11, 9, 8, -1, -1, -1, -1, -1, -1,
        3, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 10, 2, 8, 10, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 2, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 10, 2, 1, 9, 10, 9, 8, 10, -1, -1, -1, -1, -1, -1,
        3, 11, 1, 10, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 11, 1, 0, 8, 11, 8, 10, 11, -1, -1, -1, -1, -1, -1,
        3, 9, 0, 3, 10, 9, 10, 11, 9, -1, -1, -1, -1, -1, -1,
        9, 8, 11, 11, 8, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 4, 7, 3, 0, 4, 1, 2, 11, -1, -1, -1, -1, -1, -1,
        9, 2, 11, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1,
        2, 11, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1,
        8, 4, 7, 3, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 4, 7, 10, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1,
        9, 0, 1, 8, 4, 7, 2, 3, 10, -1, -1, -1, -1, -1, -1,
        4, 7, 10, 9, 4, 10, 9, 10, 2, 9, 2, 1, -1, -1, -1,
        3, 11, 1, 3, 10, 11, 7, 8, 4, -1, -1, -1, -1, -1, -1,
        1, 10, 11, 1, 4, 10, 1, 0, 4, 7, 10, 4, -1, -1, -1,
        4, 7, 8, 9, 0, 10, 9, 10, 11, 10, 0, 3, -1, -1, -1,
        4, 7, 10, 4, 10, 9, 9, 10, 11, -1, -1, -1, -1, -1, -1,
        9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 1, 2, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1,
        5, 2, 11, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1,
        2, 11, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1,
        9, 5, 4, 2, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 10, 2, 0, 8, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1,
        0, 5, 4, 0, 1, 5, 2, 3, 10, -1, -1, -1, -1, -1, -1,
        2, 1, 5, 2, 5, 8, 2, 8, 10, 4, 8, 5, -1, -1, -1,
        11, 3, 10, 11, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 0, 8, 1, 8, 11, 1, 8, 10, 11, -1, -1, -1,
        5, 4, 0, 5, 0, 10, 5, 10, 11, 10, 0, 3, -1, -1, -1,
        5, 4, 8, 5, 8, 11, 11, 8, 10, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1,
        0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1,
        1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 9, 5, 7, 11, 1, 2, -1, -1, -1, -1, -1, -1,
        11, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1,
        8, 0, 2, 8, 2, 5, 8, 5, 7, 11, 5, 2, -1, -1, -1,
        2, 11, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1,
        7, 9, 5, 7, 8, 9, 3, 10, 2, -1, -1, -1, -1, -1, -1,
        9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 10, -1, -1, -1,
        2, 3, 10, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1,
        10, 2, 1, 10, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1,
        9, 5, 8, 8, 5, 7, 11, 1, 3, 11, 3, 10, -1, -1, -1,
        5, 7, 0, 5, 0, 9, 7, 10, 0, 1, 0, 11, 10, 11, 0,
        10, 11, 0, 10, 0, 3, 11, 5, 0, 8, 0, 7, 5, 7, 0,
        10, 11, 5, 7, 10, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 5, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 0, 1, 5, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 8, 3, 1, 9, 8, 5, 11, 6, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1,
        9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1,
        5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1,
        2, 3, 10, 11, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 0, 8, 10, 2, 0, 11, 6, 5, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 2, 3, 10, 5, 11, 6, -1, -1, -1, -1, -1, -1,
        5, 11, 6, 1, 9, 2, 9, 10, 2, 9, 8, 10, -1, -1, -1,
        6, 3, 10, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1,
        0, 8, 10, 0, 10, 5, 0, 5, 1, 5, 10, 6, -1, -1, -1,
        3, 10, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1,
        6, 5, 9, 6, 9, 10, 10, 9, 8, -1, -1, -1, -1, -1, -1,
        5, 11, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 3, 0, 4, 7, 3, 6, 5, 11, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 5, 11, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1,
        11, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1,
        6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1,
        1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1,
        8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1,
        7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9,
        3, 10, 2, 7, 8, 4, 11, 6, 5, -1, -1, -1, -1, -1, -1,
        5, 11, 6, 4, 7, 2, 4, 2, 0, 2, 7, 10, -1, -1, -1,
        0, 1, 9, 4, 7, 8, 2, 3, 10, 5, 11, 6, -1, -1, -1,
        9, 2, 1, 9, 10, 2, 9, 4, 10, 7, 10, 4, 5, 11, 6,
        8, 4, 7, 3, 10, 5, 3, 5, 1, 5, 10, 6, -1, -1, -1,
        5, 1, 10, 5, 10, 6, 1, 0, 10, 7, 10, 4, 0, 4, 10,
        0, 5, 9, 0, 6, 5, 0, 3, 6, 10, 6, 3, 8, 4, 7,
        6, 5, 9, 6, 9, 10, 4, 7, 9, 7, 10, 9, -1, -1, -1,
        11, 4, 9, 6, 4, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 11, 6, 4, 9, 11, 0, 8, 3, -1, -1, -1, -1, -1, -1,
        11, 0, 1, 11, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1,
        8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 11, -1, -1, -1,
        1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1,
        0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1,
        11, 4, 9, 11, 6, 4, 10, 2, 3, -1, -1, -1, -1, -1, -1,
        0, 8, 2, 2, 8, 10, 4, 9, 11, 4, 11, 6, -1, -1, -1,
        3, 10, 2, 0, 1, 6, 0, 6, 4, 6, 1, 11, -1, -1, -1,
        6, 4, 1, 6, 1, 11, 4, 8, 1, 2, 1, 10, 8, 10, 1,
        9, 6, 4, 9, 3, 6, 9, 1, 3, 10, 6, 3, -1, -1, -1,
        8, 10, 1, 8, 1, 0, 10, 6, 1, 9, 1, 4, 6, 4, 1,
        3, 10, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1,
        6, 4, 8, 10, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 11, 6, 7, 8, 11, 8, 9, 11, -1, -1, -1, -1, -1, -1,
        0, 7, 3, 0, 11, 7, 0, 9, 11, 6, 7, 11, -1, -1, -1,
        11, 6, 7, 1, 11, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1,
        11, 6, 7, 11, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1,
        1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1,
        2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9,
        7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1,
        7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 10, 11, 6, 8, 11, 8, 9, 8, 6, 7, -1, -1, -1,
        2, 0, 7, 2, 7, 10, 0, 9, 7, 6, 7, 11, 9, 11, 7,
        1, 8, 0, 1, 7, 8, 1, 11, 7, 6, 7, 11, 2, 3, 10,
        10, 2, 1, 10, 1, 7, 11, 6, 1, 6, 7, 1, -1, -1, -1,
        8, 9, 6, 8, 6, 7, 9, 1, 6, 10, 6, 3, 1, 3, 6,
        0, 9, 1, 10, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 8, 0, 7, 0, 6, 3, 10, 0, 10, 6, 0, -1, -1, -1,
        7, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 8, 10, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 1, 9, 10, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 1, 9, 8, 3, 1, 10, 7, 6, -1, -1, -1, -1, -1, -1,
        11, 1, 2, 6, 10, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 3, 0, 8, 6, 10, 7, -1, -1, -1, -1, -1, -1,
        2, 9, 0, 2, 11, 9, 6, 10, 7, -1, -1, -1, -1, -1, -1,
        6, 10, 7, 2, 11, 3, 11, 8, 3, 11, 9, 8, -1, -1, -1,
        7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1,
        2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1,
        1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1,
        11, 7, 6, 11, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1,
        11, 7, 6, 1, 7, 11, 1, 8, 7, 1, 0, 8, -1, -1, -1,
        0, 3, 7, 0, 7, 11, 0, 11, 9, 6, 11, 7, -1, -1, -1,
        7, 6, 11, 7, 11, 8, 8, 11, 9, -1, -1, -1, -1, -1, -1,
        6, 8, 4, 10, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 6, 10, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1,
        8, 6, 10, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1,
        9, 4, 6, 9, 6, 3, 9, 3, 1, 10, 3, 6, -1, -1, -1,
        6, 8, 4, 6, 10, 8, 2, 11, 1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 3, 0, 10, 0, 6, 10, 0, 4, 6, -1, -1, -1,
        4, 10, 8, 4, 6, 10, 0, 2, 9, 2, 11, 9, -1, -1, -1,
        11, 9, 3, 11, 3, 2, 9, 4, 3, 10, 3, 6, 4, 6, 3,
        8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1,
        0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1,
        1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1,
        8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 11, 1, -1, -1, -1,
        11, 1, 0, 11, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1,
        4, 6, 3, 4, 3, 8, 6, 11, 3, 0, 3, 9, 11, 9, 3,
        11, 9, 4, 6, 11, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 7, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 4, 9, 5, 10, 7, 6, -1, -1, -1, -1, -1, -1,
        5, 0, 1, 5, 4, 0, 7, 6, 10, -1, -1, -1, -1, -1, -1,
        10, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1,
        9, 5, 4, 11, 1, 2, 7, 6, 10, -1, -1, -1, -1, -1, -1,
        6, 10, 7, 1, 2, 11, 0, 8, 3, 4, 9, 5, -1, -1, -1,
        7, 6, 10, 5, 4, 11, 4, 2, 11, 4, 0, 2, -1, -1, -1,
        3, 4, 8, 3, 5, 4, 3, 2, 5, 11, 5, 2, 10, 7, 6,
        7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1,
        9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1,
        3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1,
        6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8,
        9, 5, 4, 11, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1,
        1, 6, 11, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4,
        4, 0, 11, 4, 11, 5, 0, 3, 11, 6, 11, 7, 3, 7, 11,
        7, 6, 11, 7, 11, 8, 5, 4, 11, 4, 8, 11, -1, -1, -1,
        6, 9, 5, 6, 10, 9, 10, 8, 9, -1, -1, -1, -1, -1, -1,
        3, 6, 10, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1,
        0, 10, 8, 0, 5, 10, 0, 1, 5, 5, 6, 10, -1, -1, -1,
        6, 10, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 9, 5, 10, 9, 10, 8, 10, 5, 6, -1, -1, -1,
        0, 10, 3, 0, 6, 10, 0, 9, 6, 5, 6, 9, 1, 2, 11,
        10, 8, 5, 10, 5, 6, 8, 0, 5, 11, 5, 2, 0, 2, 5,
        6, 10, 3, 6, 3, 5, 2, 11, 3, 11, 5, 3, -1, -1, -1,
        5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1,
        9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1,
        1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8,
        1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 6, 1, 6, 11, 3, 8, 6, 5, 6, 9, 8, 9, 6,
        11, 1, 0, 11, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1,
        0, 3, 8, 5, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 5, 11, 7, 5, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 5, 11, 10, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1,
        5, 10, 7, 5, 11, 10, 1, 9, 0, -1, -1, -1, -1, -1, -1,
        11, 7, 5, 11, 10, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1,
        10, 1, 2, 10, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 10, -1, -1, -1,
        9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 10, 7, -1, -1, -1,
        7, 5, 2, 7, 2, 10, 5, 9, 2, 3, 2, 8, 9, 8, 2,
        2, 5, 11, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1,
        8, 2, 0, 8, 5, 2, 8, 7, 5, 11, 2, 5, -1, -1, -1,
        9, 0, 1, 5, 11, 3, 5, 3, 7, 3, 11, 2, -1, -1, -1,
        9, 8, 2, 9, 2, 1, 8, 7, 2, 11, 2, 5, 7, 5, 2,
        1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1,
        9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1,
        9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        5, 8, 4, 5, 11, 8, 11, 10, 8, -1, -1, -1, -1, -1, -1,
        5, 0, 4, 5, 10, 0, 5, 11, 10, 10, 3, 0, -1, -1, -1,
        0, 1, 9, 8, 4, 11, 8, 11, 10, 11, 4, 5, -1, -1, -1,
        11, 10, 4, 11, 4, 5, 10, 3, 4, 9, 4, 1, 3, 1, 4,
        2, 5, 1, 2, 8, 5, 2, 10, 8, 4, 5, 8, -1, -1, -1,
        0, 4, 10, 0, 10, 3, 4, 5, 10, 2, 10, 1, 5, 1, 10,
        0, 2, 5, 0, 5, 9, 2, 10, 5, 4, 5, 8, 10, 8, 5,
        9, 4, 5, 2, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 5, 11, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1,
        5, 11, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1,
        3, 11, 2, 3, 5, 11, 3, 8, 5, 4, 5, 8, 0, 1, 9,
        5, 11, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1,
        0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1,
        9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 10, 7, 4, 9, 10, 9, 11, 10, -1, -1, -1, -1, -1, -1,
        0, 8, 3, 4, 9, 7, 9, 10, 7, 9, 11, 10, -1, -1, -1,
        1, 11, 10, 1, 10, 4, 1, 4, 0, 7, 4, 10, -1, -1, -1,
        3, 1, 4, 3, 4, 8, 1, 11, 4, 7, 4, 10, 11, 10, 4,
        4, 10, 7, 9, 10, 4, 9, 2, 10, 9, 1, 2, -1, -1, -1,
        9, 7, 4, 9, 10, 7, 9, 1, 10, 2, 10, 1, 0, 8, 3,
        10, 7, 4, 10, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1,
        10, 7, 4, 10, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1,
        2, 9, 11, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1,
        9, 11, 7, 9, 7, 4, 11, 2, 7, 8, 7, 0, 2, 0, 7,
        3, 7, 11, 3, 11, 2, 7, 4, 11, 1, 11, 0, 4, 0, 11,
        1, 11, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1,
        4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 11, 8, 11, 10, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 0, 9, 3, 9, 10, 10, 9, 11, -1, -1, -1, -1, -1, -1,
        0, 1, 11, 0, 11, 8, 8, 11, 10, -1, -1, -1, -1, -1, -1,
        3, 1, 11, 10, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 1, 10, 9, 9, 10, 8, -1, -1, -1, -1, -1, -1,
        3, 0, 9, 3, 9, 10, 1, 2, 9, 2, 10, 9, -1, -1, -1,
        0, 2, 10, 8, 0, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        3, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 11, 11, 8, 9, -1, -1, -1, -1, -1, -1,
        9, 11, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 11, 0, 1, 8, 1, 11, 8, -1, -1, -1,
        1, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
    };

    //Les huit sommets d'un cube de marching cubes (en 0,0,0 et de taille 1)
    public static float[] SommetsNormalises = 
    {
	    0.0f,0.0f,0.0f,
	    1.0f,0.0f,0.0f,
	    1.0f,1.0f,0.0f,
	    0.0f,1.0f,0.0f,
	    0.0f,0.0f,1.0f,
	    1.0f,0.0f,1.0f,
	    1.0f,1.0f,1.0f,
	    0.0f,1.0f,1.0f    
    };

    public static float Taille = 1.0f;

    public static bool Interpolation = true;

    public static int [] VoisinFromEdge =
    {
	    //Pour le edge 0
	    0,0,-1,4, //Edge 4 du cube en z-1
	    0,-1,-1,6, 
	    0,-1,0,2, 

	    //Pour le edge 1
	    1,0,0,3,
	    1,0,-1,7, 
	    0,0,-1,5, 

	    //Pour le edge 2
	    0,1,0,0,
	    0,1,-1,4, 
	    0,0,-1,6,

	    //Pour le edge 3
	    -1,0,0,1,
	    -1,0,-1,5, 
	    0,0,-1,7,

	    //Pour le edge 4
	    0,-1,0,6,
	    0,-1,1,2, 
	    0,0,1,0,

	    //Pour le edge 5
	    1,0,0,7,
	    1,0,1,3, 
	    0,0,1,1,

	    //Pour le edge 6
	    0,1,0,4,
	    0,1,1,0, 
	    0,0,1,2,

	    //Pour le edge 7
	    -1,0,0,5,
	    -1,0,1,1, 
	    0,0,1,3,

	    //Pour le edge 8
	    -1,0,0,9,
	    -1,-1,0,11, 
	    0,-1,0,10,

	    //Pour le edge 9
	     1,0,0,8,
	     1,-1,0,10, 
	    0,-1,0,11,

	    //Pour le edge 10
	     0,1,0,8,
	     -1,1,0,9, 
	    -1,0,0,11,

	    //Pour le edge 11
	     1,0,0,10,
	     0,1,0,9, 
	     1,1,0,8
    };

    public MCube()
    {
        EdgesVectors = null;
        EdgeVectorDone = 0;
        Normals = null;
        Flags = 0;
        ColorShift = 0;
        Temp = 0;
        Sommets = 0;
    }

    public byte getCode()
    {
        return Sommets;
    }

    public void razSommets()
    {
        Sommets = 0;
    }

    //Permet de calculer des valeurs par defaut pour les edges, au cas ou on en calcul pas nous meme (ce qui ne doit pas arriver normalement)
    //place le edge vector au milieu des sommets
    public void calcBasicEdgesVectors()
    {
        EdgesVectors[0] = new Vector3(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2]) + new Vector3(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5]);
        EdgesVectors[1] = new Vector3(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5]) + new Vector3(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8]);
        EdgesVectors[2] = new Vector3(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8]) + new Vector3(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11]);
        EdgesVectors[3] = new Vector3(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11]) + new Vector3(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2]);
        EdgesVectors[4] = new Vector3(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14]) + new Vector3(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17]);
        EdgesVectors[5] = new Vector3(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17]) + new Vector3(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20]);
        EdgesVectors[6] = new Vector3(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20]) + new Vector3(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23]);
        EdgesVectors[7] = new Vector3(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23]) + new Vector3(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14]);
        EdgesVectors[8] = new Vector3(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14]) + new Vector3(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2]);
        EdgesVectors[9] = new Vector3(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17]) + new Vector3(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5]);
        EdgesVectors[10] = new Vector3(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11]) + new Vector3(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23]);
        EdgesVectors[11] = new Vector3(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8]) + new Vector3(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20]);
        for (int i = 0; i < 12; i++)
            EdgesVectors[i] /= 2.0f;
    }

    //A n'utiliser qu'avant de créer des cubes...
    public static void updateTaille(float taille)
    {
        for (int i = 0; i < 8 * 3; i++)
        {
            SommetsNormalises[i] /= Taille;
            SommetsNormalises[i] *= taille;
        }
        Taille = taille;
    }

    //A n'utiliser qu'avant de créer des cubes...
    public static void SetInterpolation(bool interpolation)
    {
        Interpolation = interpolation;
    }

    //Num entre 0 et 7
    public void setSommet(int num, bool val)
    {
        byte tmp = 0x01;
        tmp <<= num;

        if (val)
            Sommets |= tmp;
        else
            Sommets &= (byte)~tmp;
    }

    //Num entre 0 et 7
    public bool getSommet(int num)
    {
        byte tmp = 0x01;
        tmp <<= num;
        return ((Sommets & tmp) != 0) ? true : false;
    }

    //Num entre 0 et 7
    public void setFlag(int num, bool val)
    {
        byte tmp = 0x01;
        tmp <<= num;

        if (val)
            Flags |= tmp;
        else
            Flags &= (byte)~tmp;
    }

    //Num entre 0 et 7
    public bool getFlag(int num)
    {
        byte tmp = 0x01;
        tmp <<= num;
        return ((Flags & tmp) != 0) ? true : false;
    }


    //Num entre 0 et 11
    public void setEdgeVectorDone(int num)
    {
        ushort tmp = 0x01;
        tmp <<= num;
        EdgeVectorDone |= tmp;
    }

    public bool isEdgeVectorDone(int num)
    {
        ushort tmp = 0x01;
        tmp <<= num;
        return ((EdgeVectorDone & tmp) != 0) ? true : false;
    }

    //Intersection sphère et segment [p1,p2]
    //Attention, part du principe que le segment coupe la sphère en un seul endroit !!!!
    public bool interDroiteSphere(float p1x, float p1y, float p1z, float p2x, float p2y, float p2z, float pcx, float pcy, float pcz, float rayon, ref Vector3 inter)
    {
        float a = (p2x - p1x) * (p2x - p1x) + (p2y - p1y) * (p2y - p1y) + (p2z - p1z) * (p2z - p1z);
        float b = 2 * ((p2x - p1x) * (p1x - pcx) + (p2y - p1y) * (p1y - pcy) + (p2z - p1z) * (p1z - pcz));
        float c = pcx * pcx + pcy * pcy + pcz * pcz + p1x * p1x + p1y * p1y + p1z * p1z - 2 * (pcx * p1x + pcy * p1y + pcz * p1z) - rayon * rayon;

        float delta = b * b - 4 * a * c;
        if (delta > 0)
        {
            float racDelta = (float)Math.Sqrt(delta);

            float u = (-b - racDelta) / (2 * a);
            inter.x = p1x + u * (p2x - p1x);
            inter.y = p1y + u * (p2y - p1y);
            inter.z = p1z + u * (p2z - p1z);

            return true;
        }

        if (delta == 0)
        {
            float u = (-b) / (2 * a);
            inter.x = p1x + u * (p2x - p1x);
            inter.y = p1y + u * (p2y - p1y);
            inter.z = p1z + u * (p2z - p1z);
        }


        return false;
    }



    //Num entre 0 et 7 et recalcule les edges en fonction des params d'un sphere
    //N'appeler la fonction avec l'interpolation d'activée que si on l'a déja appelée une fois
    //pour la meme sphère sans l'interpolation (car on ne traitera les edges que pour les cubes de valeur != 255 et != 0)
    public void setSommetSphere(int num, bool val, Vector3 centre, float rayon, float colorShift)
    {
        //On valide le bit correpondant
        byte tmp = 0x01;
        tmp <<= num;
        if (val)
            Sommets |= tmp;
        else
            Sommets &= (byte)~tmp;

        //Couleur
        if (val)
            ColorShift = colorShift;

        //On nettoie au fur et a mesure (face recouverte)
        if (EdgesVectors != null && (Sommets == 255 || Sommets == 0))
            EdgesVectors = null;

        //Si on replace les sommets le long des edges
        if (Interpolation && Sommets != 255 && Sommets != 0)
        {
            //Si on a pas encore créé le tableau, on le crée et on l'initialise
            //Creation de mémoire souvent, mais impossible de l'avoir pour tous les cubes, pas la place
            if (EdgesVectors == null)
            {
                EdgesVectors = new Vector3[12];
                calcBasicEdgesVectors();
            }

            //Le centre est exprimé en fonction du coin (0,0,0) du cube. Si le sommet n'est pas le (0,0,0) on décale la sphère en conséquence
            centre += new Vector3(SommetsNormalises[3 * num], SommetsNormalises[3 * num + 1], SommetsNormalises[3 * num + 2]);

            Vector3 inter = new Vector3();
            Vector3 sommet = new Vector3();

            //Pour le sommet qu'on traite, on va vérifier les 3 edges
            //On calcule l'intersection avec la sphère, en partant du point au bout de l'edge, vers le point qu'on traite (ordre des parames interDroiteSphere pour trouver la bonne intersection)
            //Ensuite, si intersection, on vérifie qu'elle est au dessus de l'intersection calculée précédement (que la surface recouvre celle qu'on avait dejà)
            //Si on en avait pas deja (isEdgeVectorDone) on set l'intersection de toute facon;
            switch (num)
            {
                case 0: ///012
                    sommet = new Vector3(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2]);
                    if (interDroiteSphere(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5], SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[0]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(0)) { EdgesVectors[0] = inter; setEdgeVectorDone(0); }
                    if (interDroiteSphere(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11], SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[3]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(3)) { EdgesVectors[3] = inter; setEdgeVectorDone(3); }
                    if (interDroiteSphere(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14], SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[8]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(8)) { EdgesVectors[8] = inter; setEdgeVectorDone(8); }
                    break;
                case 1: //345
                    sommet = new Vector3(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5]);
                    if (interDroiteSphere(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2], SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[0]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(0)) { EdgesVectors[0] = inter; setEdgeVectorDone(0); }
                    if (interDroiteSphere(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8], SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[1]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(1)) { EdgesVectors[1] = inter; setEdgeVectorDone(1); }
                    if (interDroiteSphere(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17], SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[9]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(9)) { EdgesVectors[9] = inter; setEdgeVectorDone(9); }
                    break;
                case 2: //678
                    sommet = new Vector3(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8]);
                    if (interDroiteSphere(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5], SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[1]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(1)) { EdgesVectors[1] = inter; setEdgeVectorDone(1); }
                    if (interDroiteSphere(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11], SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[2]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(2)) { EdgesVectors[2] = inter; setEdgeVectorDone(2); }
                    if (interDroiteSphere(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20], SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[11]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(11)) { EdgesVectors[11] = inter; setEdgeVectorDone(11); }
                    break;
                case 3: //9 10 11
                    sommet = new Vector3(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11]);
                    if (interDroiteSphere(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8], SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[2]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(2)) { EdgesVectors[2] = inter; setEdgeVectorDone(2); }
                    if (interDroiteSphere(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2], SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[3]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(3)) { EdgesVectors[3] = inter; setEdgeVectorDone(3); }
                    if (interDroiteSphere(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23], SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[10]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(10)) { EdgesVectors[10] = inter; setEdgeVectorDone(10); }
                    break;
                case 4: //12 13 14
                    sommet = new Vector3(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14]);
                    if (interDroiteSphere(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17], SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[4]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(4)) { EdgesVectors[4] = inter; setEdgeVectorDone(4); }
                    if (interDroiteSphere(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23], SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[7]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(7)) { EdgesVectors[7] = inter; setEdgeVectorDone(7); }
                    if (interDroiteSphere(SommetsNormalises[0], SommetsNormalises[1], SommetsNormalises[2], SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[8]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(8)) { EdgesVectors[8] = inter; setEdgeVectorDone(8); }
                    break;
                case 5: //15 16 17
                    sommet = new Vector3(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17]);
                    if (interDroiteSphere(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14], SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[4]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(4)) { EdgesVectors[4] = inter; setEdgeVectorDone(4); }
                    if (interDroiteSphere(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20], SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[5]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(5)) { EdgesVectors[5] = inter; setEdgeVectorDone(5); }
                    if (interDroiteSphere(SommetsNormalises[3], SommetsNormalises[4], SommetsNormalises[5], SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[9]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(9)) { EdgesVectors[9] = inter; setEdgeVectorDone(9); }
                    break;
                case 6: //18 19 20
                    sommet = new Vector3(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20]);
                    if (interDroiteSphere(SommetsNormalises[15], SommetsNormalises[16], SommetsNormalises[17], SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[5]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(5)) { EdgesVectors[5] = inter; setEdgeVectorDone(5); }
                    if (interDroiteSphere(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23], SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20], centre.x, centre.y, centre.z, rayon, ref  inter))
                        if ((sommet - EdgesVectors[6]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(6)) { EdgesVectors[6] = inter; setEdgeVectorDone(6); }
                    if (interDroiteSphere(SommetsNormalises[6], SommetsNormalises[7], SommetsNormalises[8], SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[11]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(11)) { EdgesVectors[11] = inter; setEdgeVectorDone(11); }
                    break;
                case 7: //21 22 23
                    sommet = new Vector3(SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23]);
                    if (interDroiteSphere(SommetsNormalises[18], SommetsNormalises[19], SommetsNormalises[20], SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23], centre.x, centre.y, centre.z, rayon, ref  inter))
                        if ((sommet - EdgesVectors[6]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(6)) { EdgesVectors[6] = inter; setEdgeVectorDone(6); }
                    if (interDroiteSphere(SommetsNormalises[12], SommetsNormalises[13], SommetsNormalises[14], SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[7]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(7)) { EdgesVectors[7] = inter; setEdgeVectorDone(7); }
                    if (interDroiteSphere(SommetsNormalises[9], SommetsNormalises[10], SommetsNormalises[11], SommetsNormalises[21], SommetsNormalises[22], SommetsNormalises[23], centre.x, centre.y, centre.z, rayon, ref inter))
                        if ((sommet - EdgesVectors[10]).magnitude < (sommet - inter).magnitude || !isEdgeVectorDone(10)) { EdgesVectors[10] = inter; setEdgeVectorDone(10); }
                    break;
            }
        }
    }



    //Permet de récupérer un vertex d'un triangle
    //il y'en a 12 possibles (un par edge)
    //num : 0-11
    //Le cube se considère avec son sommet 1 en 0,0,0 et de taille 1
    public void getEdgeVertice(ref Vector3 vert, int num)
    {
        //Si pas d'interpolation deja calculée, on calcule celle au milieu
        if (EdgesVectors == null)
        {
            //Les deux sommets à interpoler
            int interpol0 = 0;
            int interpol1 = 0;

            switch (num)
            {
                case 0: interpol0 = 0; interpol1 = 1; break;
                case 1: interpol0 = 1; interpol1 = 2; break;
                case 2: interpol0 = 2; interpol1 = 3; break;
                case 3: interpol0 = 3; interpol1 = 0; break;
                case 4: interpol0 = 4; interpol1 = 5; break;
                case 5: interpol0 = 5; interpol1 = 6; break;
                case 6: interpol0 = 6; interpol1 = 7; break;
                case 7: interpol0 = 7; interpol1 = 4; break;
                case 8: interpol0 = 0; interpol1 = 4; break;
                case 9: interpol0 = 1; interpol1 = 5; break;
                case 10: interpol0 = 3; interpol1 = 7; break;
                case 11: interpol0 = 2; interpol1 = 6; break;
            }

            //On interpole
            Vector3 sommet0 = new Vector3(SommetsNormalises[interpol0 * 3], SommetsNormalises[interpol0 * 3 + 1], SommetsNormalises[interpol0 * 3 + 2]);
            Vector3 sommet1 = new Vector3(SommetsNormalises[interpol1 * 3], SommetsNormalises[interpol1 * 3 + 1], SommetsNormalises[interpol1 * 3 + 2]);
            vert = sommet0 + sommet1;
            vert /= 2.0f;
        }
        else
        {
            //Sinon on va recup le deja calcule
            vert = EdgesVectors[num];
        }
    }

    //On note l'indice ou l'on a place le sommet dans le buffer de géométrie, quand on la génère. Comme ca on pourra le réutiliser
    //quand on génèrera la géométrie d'un cube voisin
    public void setEdgeVerticeIndice(int num, int indice)
    {
        if (EdgesVectorIndices == null)
        {
            EdgesVectorIndices = new int[12];
            for (int i = 0; i < 12; i++)
                EdgesVectorIndices[i] = -1;
        }
        EdgesVectorIndices[num] = indice;
    }

    //On recup l'indice ou un edge vector a été placé pour le réutilise. On utilise cette fonction quand on génère les facettes d'un autre cube, pour réutiliser les sommets
    //deja mis dans le buffer de géometrie
    public int getEdgeVerticeIndice(int num)
    {
        if (EdgesVectorIndices == null)
        {
            return -1;
        }
        return EdgesVectorIndices[num];
    }

    //Retourne le nombre de triangles en fonction du code du cube
    public int getNbTriangles()
    {
        int i = 0;
        while (TianglesPerCode[Sommets * 15 + i] != -1 && i < 15)
            i++;
        return i / 3;
    }

    //A calculer une fois le cube traite !
    public void calcNormals()
    {
        byte code = Sommets;

        //si c'est pas un cube qui output des faces on quitte
        if (code == 255 || code == 0)
            return;

        //Si pas deja fait, on alloue le buffer de normales
        if (Normals == null)
            Normals = new Vector3[12];

        //Offset dans le buffer des faces
        int offset = code * 15;

        //On calcule les normales de chaque face
        int i = 0;

        //On fait face par face
        Vector3[] vertice = new Vector3[3];
        int[] numVertices = new int[3];
        while (TianglesPerCode[offset + i] != -1 && i < 15)
        {
            //On chope une face
            for (int j = 0; j < 3; j++)
            {
                //On chope le numéro de vertice
                int vertNum = TianglesPerCode[offset + i + j];

                ///On le stoque
                numVertices[j] = vertNum;

                //On en récup le point correspondant, dans l'espace d'un cube placé à l'origine
                getEdgeVertice(ref vertice[j], vertNum);
            }

            //On calcule la normale
            Vector3 v1 = vertice[1] - vertice[0];
            Vector3 v2 = vertice[2] - vertice[0];
            Vector3 normal = Vector3.Cross(v2, v1);
                        
            normal.Normalize();

            //On attribue la normale calculée au bon edge
            for (int j = 0; j < 3; j++)
                Normals[numVertices[j]] = normal;

            //Face suivante
            i += 3;
        }
    }

	//A calculer une fois le cube traite !
	//Moyennise les rois normales de chaque face du cube
	public void meanNormals()
	{
		byte code = Sommets;
		
		//si c'est pas un cube qui output des faces on quitte
		if (code == 255 || code == 0)
			return;
		
		//Si pas de normales, fini
		if (Normals == null)
			return;
		
		//Offset dans le buffer des faces
		int offset = code * 15;
		
		//On calcule les normales de chaque face
		int i = 0;
		
		//On fait face par face
		//Vector3[] vertice = new Vector3[3];
		int[] numVertices = new int[3];
		Vector3 sommeNormale = new Vector3();
		while (TianglesPerCode[offset + i] != -1 && i < 15)
		{
			//On chope une face
			for (int j = 0; j < 3; j++)
			{
				//On chope le numéro de vertice
				int vertNum = TianglesPerCode[offset + i + j];

				///On le stoque
				numVertices[j] = vertNum;
				
				//On somme la normale
				sommeNormale += Normals[numVertices[j]];
			}
			
			//On renormalise
			sommeNormale.Normalize();
			
			//On attribue la normale calculée au bon edge
			for (int j = 0; j < 3; j++)
				Normals[numVertices[j]] = sommeNormale;
			
			//Face suivante
			i += 3;
		}
	}

    public void getEdgeNormal(ref Vector3 normal, int num)
    {
        if (Normals != null)
            normal = Normals[num];
    }

    public void setEdgeNormal(ref Vector3 normal, int num)
    {
        if (Normals != null)
            Normals[num] = normal;
    }

    //Pour savoir en gros ou se trouve la partie vide de la molecule
    public void getGlobalNormal(ref Vector3 normal)
    {
        if (Normals != null)
        {
            normal = new Vector3();
            for (int i = 0; i < 12; i++)
                normal += Normals[i];
            normal.Normalize();
        }
        
    }

    public void getFaceCenter(ref Vector3 barycentre)
    {
        byte code = Sommets;

        //si c'est pas un cube qui output des faces on quitte
        if (code == 255 || code == 0)
            return;

        //Offset dans le buffer des faces
        int offset = code * 15;

        //On calcule le barycentre des faces
        int i = 0;

        //On fait face par face
        int nbPoints = 0;
        Vector3 vertice = new Vector3();
        barycentre = new Vector3();
        while (TianglesPerCode[offset + i] != -1 && i < 15)
        {
            //On chope le numéro de vertice
            int vertNum = TianglesPerCode[offset + i];

            //On en récup le point correspondant, dans l'espace d'un cube placé à l'origine
            getEdgeVertice(ref vertice, vertNum);

            //Log::log(Log::ENGINE_INFO,("Vertice :" + vertice.toStr()).c_str());

            barycentre += vertice;
            nbPoints++;

            //Face suivante
            i++;
        }

        //On calcule le barycentre
        barycentre /= (float)nbPoints;

        //Log::log(Log::ENGINE_INFO,("barycentre :" + barycentre.toStr() + " nb points :" + toString(nbPoints)).c_str());


    }

}