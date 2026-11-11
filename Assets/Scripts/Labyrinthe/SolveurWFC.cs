using System;
using System.Collections.Generic;

// Solveur "Wave Function Collapse" (WFC) pour un labyrinthe en grille.
//
// Chaque cellule de la grille recoit une TUILE. Une tuile est decrite
// par ses 4 cotes (Nord, Est, Sud, Ouest) : ouvert (passage) ou ferme (mur).
// Cela donne 16 tuiles possibles (masque de 4 bits).
//
// Regle d'adjacence : le cote commun de deux cellules voisines doit etre
// identique des deux cotes (ouvert/ouvert ou ferme/ferme).
//
// Algorithme :
// 1. Chaque cellule commence avec toutes les tuiles possibles.
// 2. On choisit la cellule avec la plus petite entropie (le moins de choix).
// 3. On l'effondre (collapse) : on tire une tuile au hasard selon les poids.
// 4. On propage les contraintes aux voisins.
// 5. On recommence jusqu'a ce que tout soit decide.
//
// Le WFC seul ne garantit pas que tout le labyrinthe soit relie.
// Une etape de RACCORDEMENT ouvre ensuite le minimum de murs pour que
// chaque cellule soit accessible, puis on place l'entree et la sortie.
//
// Ce fichier n'utilise pas UnityEngine : il peut etre teste hors de Unity.
public class SolveurWFC
{
    // Directions : 0 = Nord (+Z), 1 = Est (+X), 2 = Sud (-Z), 3 = Ouest (-X)
    public const int Nord = 0, Est = 1, Sud = 2, Ouest = 3;
    public static readonly int[] DX = { 0, 1, 0, -1 };
    public static readonly int[] DZ = { 1, 0, -1, 0 };

    public const int NombreTuiles = 16;
    private const int ToutesLesTuiles = (1 << NombreTuiles) - 1;

    public static int Oppose(int dir) => (dir + 2) % 4;
    public static bool EstOuvert(int tuile, int dir) => (tuile & (1 << dir)) != 0;
    public static int NombreOuvertures(int tuile)
    {
        int n = 0;
        for (int d = 0; d < 4; d++) if (EstOuvert(tuile, d)) n++;
        return n;
    }

    // compatibles[tuile, dir] = ensemble (bits) des tuiles autorisees
    // chez le voisin situe dans la direction dir.
    private static readonly int[,] compatibles = CalculerCompatibles();

    private static int[,] CalculerCompatibles()
    {
        var c = new int[NombreTuiles, 4];
        for (int a = 0; a < NombreTuiles; a++)
            for (int d = 0; d < 4; d++)
                for (int b = 0; b < NombreTuiles; b++)
                    if (EstOuvert(a, d) == EstOuvert(b, Oppose(d)))
                        c[a, d] |= 1 << b;
        return c;
    }

    private readonly int largeur, profondeur;
    private readonly double[] poids;
    private readonly Random hasard;
    private int[] possibles; // une liste de tuiles (16 bits) par cellule

    // poidsTuiles : 16 valeurs (une par masque). Un poids de 0 interdit la tuile.
    public SolveurWFC(int largeur, int profondeur, double[] poidsTuiles, int graine)
    {
        if (largeur < 2 || profondeur < 2) throw new ArgumentException("Grille trop petite (minimum 2 x 2).");
        if (poidsTuiles == null || poidsTuiles.Length != NombreTuiles) throw new ArgumentException("Il faut 16 poids.");
        this.largeur = largeur;
        this.profondeur = profondeur;
        poids = poidsTuiles;
        hasard = new Random(graine);
    }

    private int Index(int x, int z) => z * largeur + x;
    private bool DansGrille(int x, int z) => x >= 0 && z >= 0 && x < largeur && z < profondeur;

    // Lance le WFC. Renvoie null en cas de contradiction (aucune tuile possible).
    public int[,] Resoudre()
    {
        possibles = new int[largeur * profondeur];

        int autorisees = 0;
        for (int t = 0; t < NombreTuiles; t++) if (poids[t] > 0) autorisees |= 1 << t;
        if (autorisees == 0) return null;

        var pile = new Stack<int>();
        for (int z = 0; z < profondeur; z++)
            for (int x = 0; x < largeur; x++)
            {
                int masque = autorisees;
                // Bordure : aucun passage vers l'exterieur (l'entree et la sortie sont ajoutees apres).
                for (int d = 0; d < 4; d++)
                    if (!DansGrille(x + DX[d], z + DZ[d]))
                        masque &= ToutesSansOuverture(d);
                possibles[Index(x, z)] = masque;
                if (masque == 0) return null;
                pile.Push(Index(x, z));
            }
        if (!Propager(pile)) return null;

        while (true)
        {
            int cellule = CelluleMinEntropie();
            if (cellule < 0) break; // tout est decide
            possibles[cellule] = 1 << TirerTuile(possibles[cellule]);
            pile.Push(cellule);
            if (!Propager(pile)) return null;
        }

        var resultat = new int[largeur, profondeur];
        for (int z = 0; z < profondeur; z++)
            for (int x = 0; x < largeur; x++)
                resultat[x, z] = UniqueTuile(possibles[Index(x, z)]);
        return resultat;
    }

    private static int ToutesSansOuverture(int dir)
    {
        int m = 0;
        for (int t = 0; t < NombreTuiles; t++) if (!EstOuvert(t, dir)) m |= 1 << t;
        return m;
    }

    private bool Propager(Stack<int> pile)
    {
        while (pile.Count > 0)
        {
            int i = pile.Pop();
            int x = i % largeur, z = i / largeur;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], nz = z + DZ[d];
                if (!DansGrille(nx, nz)) continue;

                // Union des tuiles que le voisin peut encore avoir.
                int permis = 0;
                int ici = possibles[i];
                for (int t = 0; t < NombreTuiles; t++)
                    if ((ici & (1 << t)) != 0) permis |= compatibles[t, d];

                int j = Index(nx, nz);
                int avant = possibles[j];
                int apres = avant & permis;
                if (apres == avant) continue;
                if (apres == 0) return false; // contradiction
                possibles[j] = apres;
                pile.Push(j);
            }
        }
        return true;
    }

    // Entropie de Shannon ponderee + un petit bruit pour departager les egalites.
    // L'entropie ne depend que de l'ensemble de tuiles (16 bits) : on la met en cache.
    private double[] cacheEntropie;

    private double Entropie(int m)
    {
        if (cacheEntropie == null)
        {
            cacheEntropie = new double[1 << NombreTuiles];
            for (int k = 0; k < cacheEntropie.Length; k++) cacheEntropie[k] = double.NaN;
        }
        double e = cacheEntropie[m];
        if (!double.IsNaN(e)) return e;
        double somme = 0, sommeLog = 0;
        for (int t = 0; t < NombreTuiles; t++)
            if ((m & (1 << t)) != 0) { somme += poids[t]; sommeLog += poids[t] * Math.Log(poids[t]); }
        e = Math.Log(somme) - sommeLog / somme;
        cacheEntropie[m] = e;
        return e;
    }

    private int CelluleMinEntropie()
    {
        int meilleure = -1;
        double min = double.MaxValue;
        for (int i = 0; i < possibles.Length; i++)
        {
            int m = possibles[i];
            if ((m & (m - 1)) == 0) continue; // une seule tuile : deja decidee
            double entropie = Entropie(m) + hasard.NextDouble() * 1e-6;
            if (entropie < min) { min = entropie; meilleure = i; }
        }
        return meilleure;
    }

    private int TirerTuile(int masque)
    {
        double total = 0;
        for (int t = 0; t < NombreTuiles; t++) if ((masque & (1 << t)) != 0) total += poids[t];
        double r = hasard.NextDouble() * total;
        int derniere = -1;
        for (int t = 0; t < NombreTuiles; t++)
        {
            if ((masque & (1 << t)) == 0) continue;
            derniere = t;
            r -= poids[t];
            if (r <= 0) return t;
        }
        return derniere;
    }

    private static int UniqueTuile(int masque)
    {
        for (int t = 0; t < NombreTuiles; t++) if (masque == 1 << t) return t;
        throw new InvalidOperationException("Cellule non decidee.");
    }

    // ------------------------------------------------------------------
    // Raccordement : ouvre des murs entre zones isolees (Kruskal sur les
    // murs interieurs, dans un ordre aleatoire) jusqu'a ce que tout soit relie.
    // Renvoie le nombre de murs ouverts.
    public static int Raccorder(int[,] grille, Random hasard)
    {
        int lx = grille.GetLength(0), lz = grille.GetLength(1);
        var parent = new int[lx * lz];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;
        Func<int, int> Trouver = a => { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; };

        // Regrouper les cellules deja reliees par des passages.
        for (int z = 0; z < lz; z++)
            for (int x = 0; x < lx; x++)
            {
                if (x + 1 < lx && EstOuvert(grille[x, z], Est)) parent[Trouver(z * lx + x)] = Trouver(z * lx + x + 1);
                if (z + 1 < lz && EstOuvert(grille[x, z], Nord)) parent[Trouver(z * lx + x)] = Trouver((z + 1) * lx + x);
            }

        // Liste des murs interieurs (cellule, direction Est ou Nord).
        var murs = new List<(int x, int z, int d)>();
        for (int z = 0; z < lz; z++)
            for (int x = 0; x < lx; x++)
            {
                if (x + 1 < lx && !EstOuvert(grille[x, z], Est)) murs.Add((x, z, Est));
                if (z + 1 < lz && !EstOuvert(grille[x, z], Nord)) murs.Add((x, z, Nord));
            }
        for (int i = murs.Count - 1; i > 0; i--) { int k = hasard.Next(i + 1); var tmp = murs[i]; murs[i] = murs[k]; murs[k] = tmp; }

        int ouverts = 0;
        foreach (var mur in murs)
        {
            int x = mur.x, z = mur.z, d = mur.d;
            int nx = x + DX[d], nz = z + DZ[d];
            int a = Trouver(z * lx + x), b = Trouver(nz * lx + nx);
            if (a == b) continue;
            parent[a] = b;
            Ouvrir(grille, x, z, d);
            ouverts++;
        }
        return ouverts;
    }

    // Ouvre le passage entre (x,z) et son voisin dans la direction d (des deux cotes).
    public static void Ouvrir(int[,] grille, int x, int z, int d)
    {
        grille[x, z] |= 1 << d;
        int nx = x + DX[d], nz = z + DZ[d];
        if (nx >= 0 && nz >= 0 && nx < grille.GetLength(0) && nz < grille.GetLength(1))
            grille[nx, nz] |= 1 << Oppose(d);
    }

    // Distances (en nombre de cellules) depuis une cellule, en suivant les passages.
    public static int[,] Distances(int[,] grille, int departX, int departZ)
    {
        int lx = grille.GetLength(0), lz = grille.GetLength(1);
        var dist = new int[lx, lz];
        for (int z = 0; z < lz; z++) for (int x = 0; x < lx; x++) dist[x, z] = -1;
        var file = new Queue<(int, int)>();
        dist[departX, departZ] = 0;
        file.Enqueue((departX, departZ));
        while (file.Count > 0)
        {
            var (x, z) = file.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                if (!EstOuvert(grille[x, z], d)) continue;
                int nx = x + DX[d], nz = z + DZ[d];
                if (nx < 0 || nz < 0 || nx >= lx || nz >= lz || dist[nx, nz] >= 0) continue;
                dist[nx, nz] = dist[x, z] + 1;
                file.Enqueue((nx, nz));
            }
        }
        return dist;
    }
}

// Resultat complet : grille + entree (bord Sud) + sortie (bord Nord, la plus eloignee).
public class PlanLabyrinthe
{
    public int[,] Tuiles;
    public int EntreeX, SortieX;      // entree en z = 0, sortie en z = profondeur - 1
    public int LongueurChemin;         // nombre de cellules entre l'entree et la sortie
    public List<(int x, int z)> CulsDeSac = new List<(int, int)>(); // tries du plus loin au plus proche de l'entree
    public int Tentatives;
    public int MursOuvertsPourRaccorder;

    public int Largeur => Tuiles.GetLength(0);
    public int Profondeur => Tuiles.GetLength(1);

    // Genere un plan complet. Reessaie le WFC en cas de contradiction.
    public static PlanLabyrinthe Generer(int largeur, int profondeur, double[] poidsTuiles, int graine, int tentativesMax = 50)
    {
        for (int essai = 0; essai < tentativesMax; essai++)
        {
            var solveur = new SolveurWFC(largeur, profondeur, poidsTuiles, graine + essai * 7919);
            int[,] grille = solveur.Resoudre();
            if (grille == null) continue;

            var hasard = new Random(graine ^ 0x5bd1e995);
            var plan = new PlanLabyrinthe { Tuiles = grille, Tentatives = essai + 1 };
            plan.MursOuvertsPourRaccorder = SolveurWFC.Raccorder(grille, hasard);

            // Entree au milieu du bord Sud, sortie sur le bord Nord la plus eloignee.
            plan.EntreeX = largeur / 2;
            SolveurWFC.Ouvrir(grille, plan.EntreeX, 0, SolveurWFC.Sud);
            int[,] dist = SolveurWFC.Distances(grille, plan.EntreeX, 0);
            int meilleur = -1;
            for (int x = 0; x < largeur; x++)
                if (dist[x, profondeur - 1] > meilleur) { meilleur = dist[x, profondeur - 1]; plan.SortieX = x; }
            SolveurWFC.Ouvrir(grille, plan.SortieX, profondeur - 1, SolveurWFC.Nord);
            plan.LongueurChemin = meilleur + 1;

            for (int z = 0; z < profondeur; z++)
                for (int x = 0; x < largeur; x++)
                    if (SolveurWFC.NombreOuvertures(grille[x, z]) == 1) plan.CulsDeSac.Add((x, z));
            plan.CulsDeSac.Sort((a, b) => dist[b.x, b.z].CompareTo(dist[a.x, a.z]));
            return plan;
        }
        throw new InvalidOperationException("Le WFC n'a pas trouve de solution. Verifiez les poids des tuiles.");
    }

    // Poids des 16 tuiles a partir des 6 familles (cul-de-sac, couloir, virage, T, croisement, ferme).
    public static double[] PoidsParFamille(double culDeSac, double couloir, double virage, double jonctionT, double croisement, double ferme)
    {
        var p = new double[SolveurWFC.NombreTuiles];
        for (int t = 0; t < SolveurWFC.NombreTuiles; t++)
        {
            switch (SolveurWFC.NombreOuvertures(t))
            {
                case 0: p[t] = ferme; break;
                case 1: p[t] = culDeSac; break;
                case 2: p[t] = (t == 0b0101 || t == 0b1010) ? couloir : virage; break;
                case 3: p[t] = jonctionT; break;
                case 4: p[t] = croisement; break;
            }
            if (p[t] < 0) p[t] = 0;
        }
        return p;
    }

    // Representation texte (pratique pour deboguer dans la console).
    public string EnTexte()
    {
        var sb = new System.Text.StringBuilder();
        for (int z = Profondeur - 1; z >= 0; z--)
        {
            for (int x = 0; x < Largeur; x++) sb.Append(SolveurWFC.EstOuvert(Tuiles[x, z], SolveurWFC.Nord) ? "+  " : "+--");
            sb.Append("+\n");
            for (int x = 0; x < Largeur; x++) sb.Append(SolveurWFC.EstOuvert(Tuiles[x, z], SolveurWFC.Ouest) ? "   " : "|  ");
            sb.Append(SolveurWFC.EstOuvert(Tuiles[Largeur - 1, z], SolveurWFC.Est) ? " \n" : "|\n");
        }
        for (int x = 0; x < Largeur; x++) sb.Append(SolveurWFC.EstOuvert(Tuiles[x, 0], SolveurWFC.Sud) ? "+  " : "+--");
        sb.Append("+\n");
        return sb.ToString();
    }
}
