using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Genere un labyrinthe de mais avec l'algorithme Wave Function Collapse.
//
// Utilisation :
// 1. Creer un GameObject vide (ex. "LABYRINTHE") et lui ajouter ce script.
// 2. Dans l'Inspector, cliquer sur "Generer le labyrinthe".
//    (ou cocher "Generer Au Demarrage" pour un nouveau labyrinthe a chaque partie)
// 3. Optionnel : glisser le joueur, la porte de sortie et les batteries
//    dans les listes "Objets..." pour qu'ils soient places automatiquement.
//
// Tout ce qui est genere est range sous un enfant "Labyrinthe_Genere".
// Regenerer remplace l'ancien labyrinthe.
[DisallowMultipleComponent]
public class GenerateurLabyrintheWFC : MonoBehaviour
{
    public const string NomRacine = "Labyrinthe_Genere";

    [Header("Grille")]
    [Tooltip("Nombre de cellules en X.")]
    [SerializeField, Min(2)] private int largeur = 12;
    [Tooltip("Nombre de cellules en Z.")]
    [SerializeField, Min(2)] private int profondeur = 12;
    [Tooltip("Taille d'une cellule en unites Unity (largeur d'un couloir + mur).")]
    [SerializeField, Min(1f)] private float tailleCellule = 4f;

    [Header("Hasard")]
    [Tooltip("Si coche, une nouvelle graine est tiree a chaque generation.")]
    [SerializeField] private bool graineAleatoire = true;
    [Tooltip("Meme graine = meme labyrinthe.")]
    [SerializeField] private int graine = 12345;
    [SerializeField] private bool genererAuDemarrage = false;

    [Header("Poids des tuiles WFC (forme du labyrinthe)")]
    [Tooltip("Impasses. Plus = plus de cachettes pour les batteries.")]
    [SerializeField, Min(0f)] private float poidsCulDeSac = 1f;
    [Tooltip("Couloirs droits. Plus = longs couloirs.")]
    [SerializeField, Min(0f)] private float poidsCouloir = 4f;
    [Tooltip("Virages. Plus = chemins sinueux.")]
    [SerializeField, Min(0f)] private float poidsVirage = 4f;
    [Tooltip("Jonctions en T. Plus = plus de choix (et de boucles).")]
    [SerializeField, Min(0f)] private float poidsJonctionT = 1.5f;
    [Tooltip("Croisements a 4 directions. Plus = labyrinthe plus ouvert.")]
    [SerializeField, Min(0f)] private float poidsCroisement = 0.3f;
    [Tooltip("Cellules pleines (bloc de mais). Laisser a 0 en general.")]
    [SerializeField, Min(0f)] private float poidsFerme = 0f;

    [Header("Murs de mais")]
    [Tooltip("Vos modeles de mais (choisis au hasard). Laisser vide = mais procedural.")]
    [SerializeField] private GameObject[] prefabsMais;
    [Tooltip("Hauteur du mais procedural (ignore pour les prefabs).")]
    [SerializeField, Min(0.5f)] private float hauteurMais = 2.6f;
    [Tooltip("Distance entre deux tiges dans un rang.")]
    [SerializeField, Range(0.2f, 1.5f)] private float espacementTiges = 0.55f;
    [SerializeField, Range(1, 4)] private int rangsParMur = 2;
    [Tooltip("Epaisseur d'un mur de mais.")]
    [SerializeField, Min(0.2f)] private float epaisseurMur = 0.9f;
    [SerializeField, Range(0f, 0.5f)] private float variationTaille = 0.2f;
    [Tooltip("Part des tiges qui portent un epi.")]
    [SerializeField, Range(0f, 1f)] private float probabiliteEpi = 0.5f;
    [SerializeField] private Material materielTige;
    [SerializeField] private Material materielEpi;
    [Tooltip("Un BoxCollider invisible par mur pour bloquer le joueur.")]
    [SerializeField] private bool ajouterColliders = true;

    [Header("Sol")]
    [SerializeField] private bool creerSol = true;
    [SerializeField] private Material materielSol;

    [Header("Objets du jeu (optionnel)")]
    [Tooltip("Places au centre de la cellule d'entree (ex. Robot_Explorateur, Point_Reapparition, Zone_Depart).")]
    [SerializeField] private Transform[] objetsALEntree;
    [Tooltip("Places dans l'ouverture de sortie (ex. Porte_Sortie, Lumiere_Sortie).")]
    [SerializeField] private Transform[] objetsALaSortie;
    [Tooltip("Places dans les impasses, les plus eloignees de l'entree d'abord (ex. les batteries).")]
    [SerializeField] private Transform[] objetsDansCulsDeSac;

    // Dernier plan genere (non sauvegarde avec la scene).
    public PlanLabyrinthe Plan { get; private set; }
    public float TailleCellule => tailleCellule;
    public Transform Racine => transform.Find(NomRacine);
    public IEnumerable<Transform> ObjetsDeplaces
    {
        get
        {
            foreach (var liste in new[] { objetsALEntree, objetsALaSortie, objetsDansCulsDeSac })
                if (liste != null) foreach (var o in liste) if (o) yield return o;
        }
    }

    private const int CellulesParBloc = 4; // regroupement des maillages de mais

    private void Start()
    {
        if (genererAuDemarrage) Generer();
    }

    // ------------------------------------------------------------------
    [ContextMenu("Generer le labyrinthe")]
    public void Generer()
    {
        if (graineAleatoire) graine = Random.Range(int.MinValue, int.MaxValue);

        double[] poids = PlanLabyrinthe.PoidsParFamille(poidsCulDeSac, poidsCouloir, poidsVirage, poidsJonctionT, poidsCroisement, poidsFerme);
        Plan = PlanLabyrinthe.Generer(largeur, profondeur, poids, graine);

        Effacer();
        var racine = new GameObject(NomRacine).transform;
        racine.SetParent(transform, false);

        PreparerMateriaux();
        if (creerSol) ConstruireSol(racine);
        ConstruireMurs(racine);
        PlacerObjets();

        Debug.Log($"[Labyrinthe WFC] {largeur}x{profondeur}, graine {graine} : chemin de {Plan.LongueurChemin} cellules, " +
                  $"{Plan.CulsDeSac.Count} impasses, {Plan.MursOuvertsPourRaccorder} murs ouverts pour tout relier.\n{Plan.EnTexte()}", this);
    }

    [ContextMenu("Effacer le labyrinthe")]
    public void Effacer()
    {
        Transform ancien;
        while ((ancien = Racine) != null)
        {
            // Liberer les maillages combines (ce ne sont pas des assets).
            foreach (var mf in ancien.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh && mf.sharedMesh.name == "Mais_Combine") DetruireObjet(mf.sharedMesh);
            ancien.name = NomRacine + "_ancien";
            ancien.SetParent(null);
            DetruireObjet(ancien.gameObject);
        }
    }

    private static void DetruireObjet(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    // ------------------------------------------------------------------
    // Coordonnees

    // Centre d'une cellule, en coordonnees locales (labyrinthe centre sur l'objet).
    public Vector3 CentreCellule(int x, int z)
    {
        return new Vector3((x - (largeur - 1) * 0.5f) * tailleCellule, 0f, (z - (profondeur - 1) * 0.5f) * tailleCellule);
    }

    public Vector3 CentreCelluleMonde(int x, int z) => transform.TransformPoint(CentreCellule(x, z));

    // ------------------------------------------------------------------
    // Construction

    private void PreparerMateriaux()
    {
        if (!materielTige) materielTige = CreerMateriau("M_Mais_Tige", new Color(0.40f, 0.58f, 0.18f));
        if (!materielEpi) materielEpi = CreerMateriau("M_Mais_Epi", new Color(0.95f, 0.78f, 0.28f));
    }

    // Materiau de secours (l'outil d'Editor en cree de vrais assets a la place).
    public static Material CreerMateriau(string nom, Color couleur)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        var m = new Material(s) { name = nom, color = couleur };
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        return m;
    }

    private void ConstruireSol(Transform racine)
    {
        var sol = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sol.name = "Sol_Labyrinthe";
        sol.transform.SetParent(racine, false);
        float marge = epaisseurMur + 2f;
        sol.transform.localScale = new Vector3(largeur * tailleCellule + marge, 0.2f, profondeur * tailleCellule + marge + tailleCellule * 2f);
        sol.transform.localPosition = new Vector3(0, -0.1f, 0);
        if (materielSol) sol.GetComponent<Renderer>().sharedMaterial = materielSol;
    }

    private struct Mur
    {
        public Vector3 centre;   // local
        public bool selonX;      // true = mur oriente selon l'axe X
        public int bloc;         // index du bloc de maillage
    }

    private List<Mur> ListerMurs()
    {
        var murs = new List<Mur>();
        int blocsX = (largeur + CellulesParBloc - 1) / CellulesParBloc;
        float demi = tailleCellule * 0.5f;
        for (int z = 0; z < profondeur; z++)
            for (int x = 0; x < largeur; x++)
            {
                int tuile = Plan.Tuiles[x, z];
                Vector3 c = CentreCellule(x, z);
                int bloc = (z / CellulesParBloc) * blocsX + x / CellulesParBloc;
                // Chaque mur interieur est partage : on ne pose que Sud et Ouest,
                // plus Nord et Est sur la derniere rangee / colonne.
                if (!SolveurWFC.EstOuvert(tuile, SolveurWFC.Sud)) murs.Add(new Mur { centre = c + new Vector3(0, 0, -demi), selonX = true, bloc = bloc });
                if (!SolveurWFC.EstOuvert(tuile, SolveurWFC.Ouest)) murs.Add(new Mur { centre = c + new Vector3(-demi, 0, 0), selonX = false, bloc = bloc });
                if (z == profondeur - 1 && !SolveurWFC.EstOuvert(tuile, SolveurWFC.Nord)) murs.Add(new Mur { centre = c + new Vector3(0, 0, demi), selonX = true, bloc = bloc });
                if (x == largeur - 1 && !SolveurWFC.EstOuvert(tuile, SolveurWFC.Est)) murs.Add(new Mur { centre = c + new Vector3(demi, 0, 0), selonX = false, bloc = bloc });
            }
        return murs;
    }

    private void ConstruireMurs(Transform racine)
    {
        var hasard = new System.Random(graine);
        List<Mur> murs = ListerMurs();
        var parentMais = new GameObject("Mais").transform;
        parentMais.SetParent(racine, false);

        // Le mur deborde de la moitie de son epaisseur a chaque bout pour boucher les coins.
        float longueur = tailleCellule + epaisseurMur;
        int tigesParRang = Mathf.Max(1, Mathf.RoundToInt(longueur / espacementTiges));
        bool procedural = prefabsMais == null || System.Array.TrueForAll(prefabsMais, p => p == null);

        var tigesParBloc = new Dictionary<int, List<Matrix4x4>>();
        var episParBloc = new Dictionary<int, List<Matrix4x4>>();

        foreach (Mur mur in murs)
        {
            Quaternion orientation = mur.selonX ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
            for (int r = 0; r < rangsParMur; r++)
            {
                float travers = -epaisseurMur * 0.5f + epaisseurMur * (r + 0.5f) / rangsParMur;
                for (int k = 0; k < tigesParRang; k++)
                {
                    float le_long = -longueur * 0.5f + longueur * (k + 0.5f) / tigesParRang;
                    le_long += Aleatoire(hasard, -0.3f, 0.3f) * espacementTiges;
                    float decalage = travers + Aleatoire(hasard, -0.25f, 0.25f) * epaisseurMur / rangsParMur;
                    Vector3 pos = mur.centre + orientation * new Vector3(le_long, 0, decalage);
                    Quaternion rot = Quaternion.Euler(Aleatoire(hasard, -6f, 6f), Aleatoire(hasard, 0f, 360f), Aleatoire(hasard, -6f, 6f));
                    float echelle = 1f + Aleatoire(hasard, -variationTaille, variationTaille);

                    if (procedural)
                    {
                        var m = Matrix4x4.TRS(pos, rot, Vector3.one * echelle);
                        Ajouter(tigesParBloc, mur.bloc, m);
                        if (hasard.NextDouble() < probabiliteEpi) Ajouter(episParBloc, mur.bloc, m);
                    }
                    else
                    {
                        GameObject prefab = prefabsMais[hasard.Next(prefabsMais.Length)];
                        if (!prefab) prefab = System.Array.Find(prefabsMais, p => p != null);
                        GameObject tige = Instantiate(prefab, parentMais);
                        tige.transform.localPosition = pos;
                        tige.transform.localRotation = rot;
                        tige.transform.localScale = prefab.transform.localScale * echelle;
                    }
                }
            }
        }

        if (procedural) ConstruireMaillagesCombines(parentMais, tigesParBloc, episParBloc);
        if (ajouterColliders) ConstruireColliders(racine, murs, longueur);
    }

    private static void Ajouter(Dictionary<int, List<Matrix4x4>> dico, int cle, Matrix4x4 m)
    {
        if (!dico.TryGetValue(cle, out var liste)) dico[cle] = liste = new List<Matrix4x4>();
        liste.Add(m);
    }

    private static float Aleatoire(System.Random r, float min, float max) => (float)(min + r.NextDouble() * (max - min));

    private void ConstruireMaillagesCombines(Transform parent, Dictionary<int, List<Matrix4x4>> tiges, Dictionary<int, List<Matrix4x4>> epis)
    {
        Mesh modeleTige = MaisProcedural.CreerTige(hauteurMais);
        Mesh modeleEpi = MaisProcedural.CreerEpi(hauteurMais);

        foreach (var paire in tiges)
        {
            var parties = new List<CombineInstance>();
            var materiaux = new List<Material>();
            Mesh mTiges = Combiner(modeleTige, paire.Value);
            parties.Add(new CombineInstance { mesh = mTiges });
            materiaux.Add(materielTige);
            Mesh mEpis = null;
            if (epis.TryGetValue(paire.Key, out var listeEpis) && listeEpis.Count > 0)
            {
                mEpis = Combiner(modeleEpi, listeEpis);
                parties.Add(new CombineInstance { mesh = mEpis });
                materiaux.Add(materielEpi);
            }

            // Un seul maillage par bloc, avec un sous-maillage par materiau.
            var final = new Mesh { name = "Mais_Combine", indexFormat = IndexFormat.UInt32 };
            final.CombineMeshes(parties.ToArray(), false, false);
            DetruireObjet(mTiges);
            if (mEpis) DetruireObjet(mEpis);

            var go = new GameObject("Mais_Bloc_" + paire.Key);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = final;
            var rendu = go.AddComponent<MeshRenderer>();
            rendu.sharedMaterials = materiaux.ToArray();
        }

        DetruireObjet(modeleTige);
        DetruireObjet(modeleEpi);
    }

    private static Mesh Combiner(Mesh modele, List<Matrix4x4> matrices)
    {
        var instances = new CombineInstance[matrices.Count];
        for (int i = 0; i < matrices.Count; i++) instances[i] = new CombineInstance { mesh = modele, transform = matrices[i] };
        var m = new Mesh { indexFormat = IndexFormat.UInt32 };
        m.CombineMeshes(instances, true, true);
        return m;
    }

    private void ConstruireColliders(Transform racine, List<Mur> murs, float longueur)
    {
        var parent = new GameObject("Colliders_Murs").transform;
        parent.SetParent(racine, false);
        float h = Mathf.Max(hauteurMais, 2f);
        foreach (Mur mur in murs)
        {
            var go = new GameObject("Mur_Mais");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = mur.centre + Vector3.up * h * 0.5f;
            var box = go.AddComponent<BoxCollider>();
            box.size = mur.selonX ? new Vector3(longueur, h, epaisseurMur) : new Vector3(epaisseurMur, h, longueur);
        }
    }

    // ------------------------------------------------------------------
    // Placement des objets du jeu (on garde leur hauteur actuelle).

    private void PlacerObjets()
    {
        Vector3 entree = CentreCelluleMonde(Plan.EntreeX, 0);
        Vector3 sortie = transform.TransformPoint(CentreCellule(Plan.SortieX, profondeur - 1) + new Vector3(0, 0, tailleCellule * 0.5f));

        if (objetsALEntree != null)
            foreach (var o in objetsALEntree) Placer(o, entree, true);
        if (objetsALaSortie != null)
            foreach (var o in objetsALaSortie) Placer(o, sortie, true);
        if (objetsDansCulsDeSac != null)
        {
            int i = 0;
            foreach (var o in objetsDansCulsDeSac)
            {
                if (!o) continue;
                if (i >= Plan.CulsDeSac.Count)
                {
                    Debug.LogWarning($"[Labyrinthe WFC] Pas assez d'impasses pour {o.name}. Augmentez le poids des culs-de-sac.", this);
                    continue;
                }
                var cell = Plan.CulsDeSac[i++];
                Placer(o, CentreCelluleMonde(cell.x, cell.z), false);
            }
        }
    }

    // On aligne le CENTRE VISIBLE de l'objet (ses Renderers) sur la cible,
    // pas son pivot : ainsi un parent vide dont les enfants sont decales
    // (comme Porte_Sortie, dont la porte est a z = 8) tombe au bon endroit.
    private void Placer(Transform objet, Vector3 positionSol, bool orienterVersLeNord)
    {
        if (!objet) return;
        if (orienterVersLeNord) objet.rotation = transform.rotation;

        Vector3 decalage = Vector3.zero;
        var rendus = objet.GetComponentsInChildren<Renderer>(false);
        if (rendus.Length > 0)
        {
            Bounds b = rendus[0].bounds;
            for (int i = 1; i < rendus.Length; i++) b.Encapsulate(rendus[i].bounds);
            decalage = b.center - objet.position;
        }
        objet.position = new Vector3(positionSol.x - decalage.x, objet.position.y, positionSol.z - decalage.z);
        var rb = objet.GetComponent<Rigidbody>();
        if (rb) { rb.position = objet.position; rb.rotation = objet.rotation; }
    }

    // ------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.9f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(0, hauteurMais * 0.5f, 0), new Vector3(largeur * tailleCellule, hauteurMais, profondeur * tailleCellule));
        if (Plan == null || Plan.Largeur != largeur || Plan.Profondeur != profondeur) return;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(CentreCellule(Plan.EntreeX, 0) + Vector3.up, 0.4f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(CentreCellule(Plan.SortieX, profondeur - 1) + Vector3.up, 0.4f);
    }
}
