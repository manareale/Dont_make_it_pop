using System.Collections.Generic;
using UnityEngine;

// Carte de navigation construite en "sondant" la scene avec la physique.
//
// Pour chaque case de la grille :
// 1. un rayon vertical trouve le sol (Terrain, Sol, etc.) ;
// 2. une capsule de la taille du clown verifie qu'il n'y a pas de mur au-dessus.
// Les murs de mais (BoxCollider), les murs poses a la main et les obstacles
// deviennent donc des cases bloquees, sans rien configurer.
//
// Pas besoin du module AI / NavMesh de Unity.
// Les objets avec un Rigidbody (joueur, clown, batteries...) et les triggers sont ignores.
public class GrilleNavigation
{
    public readonly CheminGrille Grille;
    public readonly Vector3 Origine;   // coin (x min, z min) de la grille
    public readonly float TailleCase;
    public readonly int SceneHandle;

    private readonly List<int> tampon = new List<int>();

    private GrilleNavigation(CheminGrille grille, Vector3 origine, float taille, int scene)
    {
        Grille = grille; Origine = origine; TailleCase = taille; SceneHandle = scene;
    }

    // ------------------------------------------------------------------
    // Construction

    public static GrilleNavigation Construire(Bounds zone, float tailleCase, float rayon, float hauteurPersonnage,
                                              float marcheMax, LayerMask masque, int sceneHandle)
    {
        Physics.SyncTransforms();
        int nx = Mathf.Max(1, Mathf.CeilToInt(zone.size.x / tailleCase));
        int nz = Mathf.Max(1, Mathf.CeilToInt(zone.size.z / tailleCase));
        var libre = new bool[nx * nz];
        var hauteur = new float[nx * nz];

        var touches = new RaycastHit[16];
        var chevauche = new Collider[16];
        float hautRayon = zone.max.y + 1f;
        float longueurRayon = zone.size.y + 2f;

        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                int i = z * nx + x;
                var haut = new Vector3(zone.min.x + (x + 0.5f) * tailleCase, hautRayon, zone.min.z + (z + 0.5f) * tailleCase);
                int n = Physics.RaycastNonAlloc(haut, Vector3.down, touches, longueurRayon, masque, QueryTriggerInteraction.Ignore);
                System.Array.Sort(touches, 0, n, ComparateurDistance.Instance);

                // On garde la surface la plus haute ou le clown tient debout.
                // (Le dessus d'un mur sera choisi s'il n'y a pas de place en dessous,
                //  mais il est trop haut pour etre relie au sol : jamais atteint.)
                for (int k = 0; k < n; k++)
                {
                    RaycastHit t = touches[k];
                    if (t.collider.attachedRigidbody != null) continue;
                    if (t.normal.y < 0.6f) continue; // trop en pente
                    if (!EspaceLibre(t.point, rayon, hauteurPersonnage, marcheMax, masque, chevauche)) continue;
                    libre[i] = true;
                    hauteur[i] = t.point.y;
                    break;
                }
            }

        var origine = new Vector3(zone.min.x, 0f, zone.min.z);
        return new GrilleNavigation(new CheminGrille(nx, nz, libre, hauteur, marcheMax), origine, tailleCase, sceneHandle);
    }

    private static bool EspaceLibre(Vector3 sol, float rayon, float hauteur, float marche, LayerMask masque, Collider[] tampon)
    {
        Vector3 bas = sol + Vector3.up * (rayon + marche);
        Vector3 haut = sol + Vector3.up * Mathf.Max(rayon + marche, hauteur - rayon);
        int n = Physics.OverlapCapsuleNonAlloc(bas, haut, rayon, tampon, masque, QueryTriggerInteraction.Ignore);
        for (int k = 0; k < n; k++)
            if (tampon[k].attachedRigidbody == null) return false;
        return true;
    }

    private class ComparateurDistance : IComparer<RaycastHit>
    {
        public static readonly ComparateurDistance Instance = new ComparateurDistance();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

    // Zone a couvrir : tous les colliders fixes (sauf Terrain et triggers), avec une marge.
    public static Bounds ZoneAutomatique(Vector3 repli)
    {
        bool trouve = false;
        var zone = new Bounds(repli, Vector3.zero);
        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (!c.enabled || c.isTrigger || c.attachedRigidbody != null || c is TerrainCollider) continue;
            Bounds b = c.bounds;
            if (b.size.x > 1000f || b.size.z > 1000f) continue;
            if (!trouve) { zone = b; trouve = true; } else zone.Encapsulate(b);
        }
        if (!trouve) zone = new Bounds(repli, new Vector3(100f, 10f, 100f));
        zone.Encapsulate(repli);
        zone.Expand(new Vector3(4f, 4f, 4f));
        return zone;
    }

    // ------------------------------------------------------------------
    // Utilisation

    public bool VersCase(Vector3 p, out int x, out int z)
    {
        x = Mathf.FloorToInt((p.x - Origine.x) / TailleCase);
        z = Mathf.FloorToInt((p.z - Origine.z) / TailleCase);
        return Grille.DansGrille(x, z);
    }

    public Vector3 CentreCase(int x, int z)
    {
        return new Vector3(Origine.x + (x + 0.5f) * TailleCase, Grille.Hauteur(x, z), Origine.z + (z + 0.5f) * TailleCase);
    }

    // Chemin lisse (points du monde) de 'depart' a 'arrivee'. Le premier point est apres le depart.
    public bool TrouverChemin(Vector3 depart, Vector3 arrivee, List<Vector3> chemin)
    {
        chemin.Clear();
        if (!VersCase(depart, out int ax, out int az) || !VersCase(arrivee, out int bx, out int bz)) return false;
        if (!Grille.CaseLibreProche(ax, az, 6, out ax, out az)) return false;
        if (!Grille.CaseLibreProche(bx, bz, 6, out bx, out bz)) return false;
        if (!Grille.Chercher(ax, az, bx, bz, tampon)) return false;
        Grille.Lisser(tampon);
        for (int k = 1; k < tampon.Count; k++)
        {
            int c = tampon[k];
            chemin.Add(CentreCase(c % Grille.LargeurX, c / Grille.LargeurX));
        }
        return true;
    }

    public void DessinerGizmos(float y)
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        var taille = new Vector3(TailleCase * 0.9f, 0.05f, TailleCase * 0.9f);
        for (int z = 0; z < Grille.LargeurZ; z++)
            for (int x = 0; x < Grille.LargeurX; x++)
                if (!Grille.Libre(x, z))
                    Gizmos.DrawCube(new Vector3(Origine.x + (x + 0.5f) * TailleCase, y, Origine.z + (z + 0.5f) * TailleCase), taille);
    }
}
