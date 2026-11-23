using UnityEngine;

// Champ de force qui empeche la joueuse de tomber du bord du sol.
//
// - 4 murs invisibles (BoxCollider) poses sur les bords du sol, un peu a l'interieur.
// - Un bouclier bleu en hexagones apparait quand la joueuse s'approche d'un bord,
//   et flashe quand elle le touche.
// - Filet de securite : si malgre tout la joueuse tombe sous le sol, elle est
//   remise a son dernier endroit sur (et perd des PV si vous le voulez).
//
// Utilisation : un GameObject avec ce script. "Sol" = le collider du sol
// (vide = cherche "Sol_Labyrinthe", sinon le sol sous la joueuse).
public class ChampDeForce : MonoBehaviour
{
    [Header("Zone")]
    [Tooltip("Le sol a entourer. Vide = automatique.")]
    [SerializeField] private Collider sol;
    [Tooltip("Distance entre le bord du sol et le champ de force.")]
    [SerializeField, Min(0f)] private float marge = 0.75f;
    [SerializeField, Min(1f)] private float hauteur = 8f;
    [SerializeField, Min(0.2f)] private float epaisseur = 2f;

    [Header("Apparence")]
    [SerializeField] private bool visible = true;
    [SerializeField] private Color couleur = new Color(0.25f, 0.75f, 1f, 1f);
    [Tooltip("Hauteur du bouclier visible.")]
    [SerializeField, Min(0.5f)] private float hauteurVisuelle = 3.5f;
    [Tooltip("Le bouclier apparait quand la joueuse est a moins de cette distance.")]
    [SerializeField, Min(0.5f)] private float distanceApparition = 5f;
    [SerializeField, Range(0f, 1f)] private float opaciteMax = 0.55f;
    [Tooltip("Taille d'un hexagone, en metres.")]
    [SerializeField, Min(0.2f)] private float tailleMotif = 1.2f;

    [Header("Filet de securite (chute)")]
    [Tooltip("Si la joueuse passe sous le sol de plus de cette distance, elle est remise sur le sol.")]
    [SerializeField, Min(0.5f)] private float profondeurChute = 4f;
    [Tooltip("Vies perdues en cas de chute (0 = aucune).")]
    [SerializeField, Min(0)] private int viesPerduesChute = 1;

    [Header("Joueuse")]
    [Tooltip("Vide = l'objet avec le tag Player.")]
    [SerializeField] private Transform joueuse;

    private Bounds zone;
    private float hautDuSol;
    private Rigidbody corpsJoueuse;
    private Vector3 derniereBonnePosition;
    private float rayonJoueuse = 0.3f;

    // 0 = Nord (+Z), 1 = Est (+X), 2 = Sud (-Z), 3 = Ouest (-X)
    private readonly Renderer[] boucliers = new Renderer[4];
    private readonly Material[] materiaux = new Material[4];
    private readonly float[] flash = new float[4];
    private bool pret;

    private void Awake()
    {
        if (!joueuse)
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j) joueuse = j.transform;
        }
        if (joueuse)
        {
            corpsJoueuse = joueuse.GetComponent<Rigidbody>();
            var capsule = joueuse.GetComponent<CapsuleCollider>();
            if (capsule) rayonJoueuse = capsule.radius * Mathf.Max(joueuse.lossyScale.x, joueuse.lossyScale.z);
            derniereBonnePosition = joueuse.position;
        }

        if (!sol) sol = TrouverSol();
        if (!sol)
        {
            Debug.LogWarning("[Champ de force] Aucun sol trouve. Glissez le collider du sol dans le champ 'Sol'.", this);
            return;
        }

        zone = sol.bounds;
        hautDuSol = zone.max.y;
        Construire();
        pret = true;
    }

    private Collider TrouverSol()
    {
        var parNom = GameObject.Find("Sol_Labyrinthe");
        if (parNom && parNom.TryGetComponent(out Collider c)) return c;
        if (joueuse && Physics.Raycast(joueuse.position + Vector3.up, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider;
        return null;
    }

    // ------------------------------------------------------------------
    private void Construire()
    {
        var murs = new GameObject("Murs_ChampDeForce").transform;
        murs.SetParent(transform, false);

        float minX = zone.min.x + marge, maxX = zone.max.x - marge;
        float minZ = zone.min.z + marge, maxZ = zone.max.z - marge;
        float largeurX = maxX - minX, largeurZ = maxZ - minZ;
        float y = hautDuSol + hauteur * 0.5f;

        // Les murs depassent aux coins (epaisseur * 2) pour ne laisser aucun trou.
        Mur(murs, "Nord", new Vector3((minX + maxX) * 0.5f, y, maxZ + epaisseur * 0.5f), new Vector3(largeurX + epaisseur * 2f, hauteur, epaisseur));
        Mur(murs, "Est", new Vector3(maxX + epaisseur * 0.5f, y, (minZ + maxZ) * 0.5f), new Vector3(epaisseur, hauteur, largeurZ + epaisseur * 2f));
        Mur(murs, "Sud", new Vector3((minX + maxX) * 0.5f, y, minZ - epaisseur * 0.5f), new Vector3(largeurX + epaisseur * 2f, hauteur, epaisseur));
        Mur(murs, "Ouest", new Vector3(minX - epaisseur * 0.5f, y, (minZ + maxZ) * 0.5f), new Vector3(epaisseur, hauteur, largeurZ + epaisseur * 2f));

        if (!visible) return;
        Texture2D motif = CreerMotifHexagones();
        float yv = hautDuSol + hauteurVisuelle * 0.5f;
        // Les boucliers sont poses sur la face INTERIEURE des murs, tournes vers la joueuse.
        boucliers[0] = Bouclier(murs, "Nord", new Vector3((minX + maxX) * 0.5f, yv, maxZ), Quaternion.Euler(0, 0, 0), largeurX, motif, 0);
        boucliers[1] = Bouclier(murs, "Est", new Vector3(maxX, yv, (minZ + maxZ) * 0.5f), Quaternion.Euler(0, 90, 0), largeurZ, motif, 1);
        boucliers[2] = Bouclier(murs, "Sud", new Vector3((minX + maxX) * 0.5f, yv, minZ), Quaternion.Euler(0, 180, 0), largeurX, motif, 2);
        boucliers[3] = Bouclier(murs, "Ouest", new Vector3(minX, yv, (minZ + maxZ) * 0.5f), Quaternion.Euler(0, 270, 0), largeurZ, motif, 3);
    }

    private static void Mur(Transform parent, string nom, Vector3 centre, Vector3 taille)
    {
        var go = new GameObject("Mur_" + nom);
        go.transform.SetParent(parent, false);
        go.transform.position = centre;
        go.AddComponent<BoxCollider>().size = taille;
    }

    private Renderer Bouclier(Transform parent, string nom, Vector3 centre, Quaternion rotation, float largeur, Texture2D motif, int i)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Bouclier_" + nom;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(centre, rotation);
        go.transform.localScale = new Vector3(largeur, hauteurVisuelle, 1f);

        Shader s = Shader.Find("Sprites/Default");
        var mat = new Material(s) { name = "M_ChampDeForce_" + nom, mainTexture = motif };
        mat.mainTextureScale = new Vector2(largeur / tailleMotif, hauteurVisuelle / (tailleMotif * 1.732f)); // une tuile = 1 x racine(3) motifs
        mat.renderQueue = 3100;
        materiaux[i] = mat;

        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.enabled = false;
        return r;
    }

    // Grille d'hexagones (contours lumineux) qui se repete sans couture.
    private static Texture2D CreerMotifHexagones()
    {
        const int l = 128, h = 222; // proportions d'un pavage hexagonal (h = l * racine(3))
        var tex = new Texture2D(l, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Motif_ChampDeForce" };
        var pixels = new Color32[l * h];
        // Centres des hexagones : un reseau ou chaque centre a 6 voisins a la meme distance.
        // Les cellules autour de ces centres (la zone la plus proche de chacun) sont des hexagones.
        var centres = new System.Collections.Generic.List<Vector2>();
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 2; i++)
            {
                centres.Add(new Vector2(i * l, j * h));
                centres.Add(new Vector2(i * l + l * 0.5f, j * h + h * 0.5f));
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < l; x++)
            {
                // Distance aux deux centres les plus proches.
                float meilleur = float.MaxValue, second = float.MaxValue;
                foreach (var c in centres)
                {
                    float dx = x + 0.5f - c.x, dy = y + 0.5f - c.y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < meilleur) { second = meilleur; meilleur = d; } else if (d < second) second = d;
                }
                float bord = second - meilleur;                       // 0 sur la frontiere entre deux hexagones
                float ligne = Mathf.Clamp01(1f - bord / 5f);           // contour
                float lueur = Mathf.Clamp01(1f - bord / 22f) * 0.35f;  // halo
                float a = Mathf.Max(ligne, lueur) + 0.06f;             // leger voile partout
                pixels[y * l + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply(true);
        return tex;
    }

    // ------------------------------------------------------------------
    private void Update()
    {
        if (!pret || !joueuse) return;
        Vector3 p = joueuse.position;

        if (visible)
        {
            float minX = zone.min.x + marge, maxX = zone.max.x - marge;
            float minZ = zone.min.z + marge, maxZ = zone.max.z - marge;
            float[] distances = { maxZ - p.z, maxX - p.x, p.z - minZ, p.x - minX };
            float decalage = Time.time * 0.05f;
            for (int i = 0; i < 4; i++)
            {
                if (!boucliers[i]) continue;
                float d = distances[i] - rayonJoueuse;
                if (d < 0.15f) flash[i] = 1f;                                   // contact
                flash[i] = Mathf.MoveTowards(flash[i], 0f, Time.deltaTime * 2.5f);
                float proche = Mathf.Clamp01(1f - d / distanceApparition);
                float pulsation = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f + i);
                float a = Mathf.Max(proche * proche * opaciteMax * pulsation, flash[i]);
                boucliers[i].enabled = a > 0.01f;
                if (!boucliers[i].enabled) continue;
                materiaux[i].color = new Color(couleur.r, couleur.g, couleur.b, a);
                materiaux[i].mainTextureOffset = new Vector2(decalage, decalage * 0.6f);
            }
        }

        // Filet de securite.
        if (p.y < hautDuSol - profondeurChute) RattraperChute();
        else if (Mathf.Abs(p.y - hautDuSol) < 0.5f && DansLaZone(p)) derniereBonnePosition = p;
    }

    private bool DansLaZone(Vector3 p)
    {
        return p.x > zone.min.x + marge && p.x < zone.max.x - marge && p.z > zone.min.z + marge && p.z < zone.max.z - marge;
    }

    private void RattraperChute()
    {
        Vector3 cible = derniereBonnePosition + Vector3.up * 0.2f;
        if (corpsJoueuse)
        {
            corpsJoueuse.position = cible;
            if (!corpsJoueuse.isKinematic) { corpsJoueuse.linearVelocity = Vector3.zero; corpsJoueuse.angularVelocity = Vector3.zero; }
        }
        joueuse.position = cible;
        Physics.SyncTransforms();

        if (viesPerduesChute > 0 && joueuse.TryGetComponent(out SanteJoueuse sante)) sante.RecevoirDegats(viesPerduesChute);
        Debug.Log("[Champ de force] La joueuse est tombee : remise sur le sol.", this);
    }

    private void OnDestroy()
    {
        foreach (var m in materiaux) if (m) { if (m.mainTexture) Destroy(m.mainTexture); Destroy(m); }
    }

    private void OnDrawGizmosSelected()
    {
        Collider c = sol;
        if (!c) { var go = GameObject.Find("Sol_Labyrinthe"); if (go) c = go.GetComponent<Collider>(); }
        if (!c) return;
        Bounds b = c.bounds;
        Gizmos.color = new Color(0.25f, 0.75f, 1f, 0.8f);
        var centre = new Vector3(b.center.x, b.max.y + hauteur * 0.5f, b.center.z);
        Gizmos.DrawWireCube(centre, new Vector3(b.size.x - marge * 2f, hauteur, b.size.z - marge * 2f));
    }
}
