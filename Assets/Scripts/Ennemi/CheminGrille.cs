using System;
using System.Collections.Generic;

// Recherche de chemin A* sur une grille (sans UnityEngine : testable hors de Unity).
//
// Chaque case a :
// - libre[i]   : vrai si on peut s'y tenir debout
// - hauteur[i] : hauteur du sol dans cette case
// Deux cases voisines sont reliees si elles sont libres et si la difference
// de hauteur est petite (une marche). Les diagonales ne coupent pas les coins.
public class CheminGrille
{
    public readonly int LargeurX, LargeurZ;
    private readonly bool[] libre;
    private readonly float[] hauteur;
    private readonly float marcheMax;

    // Tableaux reutilises d'une recherche a l'autre (pas d'allocation).
    private readonly float[] cout;
    private readonly int[] parent;
    private readonly int[] visite;     // numero de recherche ou la case a ete ouverte
    private readonly int[] fermee;     // numero de recherche ou la case a ete fermee
    private int recherche;
    private readonly Tas tas = new Tas();

    private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };
    private const float Diagonale = 1.41421356f;

    public CheminGrille(int largeurX, int largeurZ, bool[] libre, float[] hauteur, float marcheMax)
    {
        LargeurX = largeurX; LargeurZ = largeurZ;
        this.libre = libre; this.hauteur = hauteur; this.marcheMax = marcheMax;
        int n = largeurX * largeurZ;
        cout = new float[n]; parent = new int[n]; visite = new int[n]; fermee = new int[n];
    }

    public int Index(int x, int z) => z * LargeurX + x;
    public bool DansGrille(int x, int z) => x >= 0 && z >= 0 && x < LargeurX && z < LargeurZ;
    public bool Libre(int x, int z) => DansGrille(x, z) && libre[Index(x, z)];
    public float Hauteur(int x, int z) => hauteur[Index(x, z)];

    private bool Relie(int a, int b) => libre[a] && libre[b] && Math.Abs(hauteur[a] - hauteur[b]) <= marcheMax;

    // Case libre la plus proche (recherche en anneaux), ou false si rien dans le rayon.
    public bool CaseLibreProche(int x, int z, int rayonMax, out int rx, out int rz)
    {
        rx = x; rz = z;
        if (Libre(x, z)) return true;
        for (int r = 1; r <= rayonMax; r++)
        {
            float meilleure = float.MaxValue; bool trouve = false;
            for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dz) != r) continue; // seulement le bord de l'anneau
                    if (!Libre(x + dx, z + dz)) continue;
                    float d = dx * dx + dz * dz;
                    if (d < meilleure) { meilleure = d; rx = x + dx; rz = z + dz; trouve = true; }
                }
            if (trouve) return true;
        }
        return false;
    }

    // A* entre deux cases. Remplit 'cases' (indices, depart -> arrivee). Renvoie false si aucun chemin.
    public bool Chercher(int departX, int departZ, int arriveeX, int arriveeZ, List<int> cases, int casesMax = 200000)
    {
        cases.Clear();
        if (!Libre(departX, departZ) || !Libre(arriveeX, arriveeZ)) return false;
        int depart = Index(departX, departZ), arrivee = Index(arriveeX, arriveeZ);

        recherche++;
        if (recherche == int.MaxValue) { Array.Clear(visite, 0, visite.Length); Array.Clear(fermee, 0, fermee.Length); recherche = 1; }
        tas.Vider();
        cout[depart] = 0; parent[depart] = -1; visite[depart] = recherche;
        tas.Ajouter(depart, Heuristique(departX, departZ, arriveeX, arriveeZ));

        int traitees = 0;
        while (tas.Nombre > 0)
        {
            int i = tas.Retirer();
            if (fermee[i] == recherche) continue;
            fermee[i] = recherche;
            if (i == arrivee)
            {
                for (int c = arrivee; c != -1; c = parent[c]) cases.Add(c);
                cases.Reverse();
                return true;
            }
            if (++traitees > casesMax) return false;

            int x = i % LargeurX, z = i / LargeurX;
            for (int d = 0; d < 8; d++)
            {
                int nx = x + DX[d], nz = z + DZ[d];
                if (!DansGrille(nx, nz)) continue;
                int j = Index(nx, nz);
                if (fermee[j] == recherche || !Relie(i, j)) continue;
                if (d >= 4 && (!Relie(i, Index(nx, z)) || !Relie(i, Index(x, nz)))) continue; // pas de coin coupe
                float nouveau = cout[i] + (d >= 4 ? Diagonale : 1f);
                if (visite[j] == recherche && nouveau >= cout[j]) continue;
                visite[j] = recherche; cout[j] = nouveau; parent[j] = i;
                tas.Ajouter(j, nouveau + Heuristique(nx, nz, arriveeX, arriveeZ));
            }
        }
        return false;
    }

    // Distance "octile" (exacte pour 8 directions sans obstacle).
    private static float Heuristique(int ax, int az, int bx, int bz)
    {
        int dx = Math.Abs(ax - bx), dz = Math.Abs(az - bz);
        return Math.Max(dx, dz) + (Diagonale - 1f) * Math.Min(dx, dz);
    }

    // Vrai si on peut aller en ligne droite d'une case a l'autre (sert a lisser le chemin).
    public bool LigneLibre(int ax, int az, int bx, int bz)
    {
        int dx = bx - ax, dz = bz - az;
        int pas = Math.Max(Math.Abs(dx), Math.Abs(dz)) * 2;
        if (pas == 0) return Libre(ax, az);
        int precedente = Index(ax, az);
        for (int k = 1; k <= pas; k++)
        {
            float t = (float)k / pas;
            // On teste les 4 cases autour du point pour garder une marge avec les murs.
            float fx = ax + dx * t, fz = az + dz * t;
            int x0 = (int)Math.Floor(fx), z0 = (int)Math.Floor(fz);
            int x1 = (int)Math.Ceiling(fx), z1 = (int)Math.Ceiling(fz);
            if (!Libre(x0, z0) || !Libre(x1, z0) || !Libre(x0, z1) || !Libre(x1, z1)) return false;
            int ici = Index((int)Math.Round(fx), (int)Math.Round(fz));
            if (Math.Abs(hauteur[ici] - hauteur[precedente]) > marcheMax) return false;
            precedente = ici;
        }
        return true;
    }

    // Garde seulement les cases ou le chemin doit tourner.
    public void Lisser(List<int> cases)
    {
        if (cases.Count < 3) return;
        var garde = new List<int> { cases[0] };
        int ancre = 0;
        while (ancre < cases.Count - 1)
        {
            int loin = ancre + 1;
            for (int k = cases.Count - 1; k > ancre + 1; k--)
            {
                int a = cases[ancre], b = cases[k];
                if (LigneLibre(a % LargeurX, a / LargeurX, b % LargeurX, b / LargeurX)) { loin = k; break; }
            }
            garde.Add(cases[loin]);
            ancre = loin;
        }
        cases.Clear();
        cases.AddRange(garde);
    }

    // File de priorite (tas binaire min).
    private class Tas
    {
        private readonly List<int> elements = new List<int>();
        private readonly List<float> priorites = new List<float>();
        public int Nombre => elements.Count;
        public void Vider() { elements.Clear(); priorites.Clear(); }

        public void Ajouter(int e, float p)
        {
            elements.Add(e); priorites.Add(p);
            int i = elements.Count - 1;
            while (i > 0)
            {
                int pa = (i - 1) / 2;
                if (priorites[pa] <= priorites[i]) break;
                Echanger(i, pa); i = pa;
            }
        }

        public int Retirer()
        {
            int premier = elements[0];
            int dernier = elements.Count - 1;
            elements[0] = elements[dernier]; priorites[0] = priorites[dernier];
            elements.RemoveAt(dernier); priorites.RemoveAt(dernier);
            int i = 0;
            while (true)
            {
                int g = 2 * i + 1, d = g + 1, m = i;
                if (g < elements.Count && priorites[g] < priorites[m]) m = g;
                if (d < elements.Count && priorites[d] < priorites[m]) m = d;
                if (m == i) break;
                Echanger(i, m); i = m;
            }
            return premier;
        }

        private void Echanger(int a, int b)
        {
            int e = elements[a]; elements[a] = elements[b]; elements[b] = e;
            float p = priorites[a]; priorites[a] = priorites[b]; priorites[b] = p;
        }
    }
}
