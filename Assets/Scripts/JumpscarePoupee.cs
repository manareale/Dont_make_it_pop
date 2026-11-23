using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Jumpscare avec la poupee demoniaque (SKM_DemonDoll).
//
// Quand la joueuse s'approche :
// 1. ses controles sont bloques ;
// 2. la poupee bondit devant la camera, tournee vers elle, eclairee en rouge ;
// 3. cri + tremblement de camera + flashs rouges, la tete de la poupee tressaute ;
// 4. la poupee disparait et la joueuse peut repartir.
//
// A mettre sur l'objet racine de la poupee (menu Outils > Jumpscare > Ajouter aux poupees).
// Aucun asset requis : le cri est genere par code (glissez votre propre son dans "Cri" si vous voulez).
public class JumpscarePoupee : MonoBehaviour
{
    [Header("Declenchement")]
    [Tooltip("Distance (en metres) a laquelle la poupee attaque.")]
    [SerializeField, Min(0.5f)] private float distanceDeclenchement = 2.5f;
    [Tooltip("La poupee disparait apres le jumpscare. Sinon elle revient a sa place et peut recommencer.")]
    [SerializeField] private bool disparaitApres = true;
    [Tooltip("Si elle ne disparait pas : temps avant qu'elle puisse recommencer.")]
    [SerializeField, Min(0f)] private float delaiAvantNouvelleAttaque = 20f;

    [Header("Cachee de loin")]
    [Tooltip("La poupee est invisible tant que la joueuse est loin.")]
    [SerializeField] private bool cacheeDeLoin = true;
    [Tooltip("Distance a laquelle la poupee peut apparaitre.")]
    [SerializeField, Min(1f)] private float distanceRevelation = 8f;
    [Tooltip("Elle n'apparait (et ne disparait) que si la joueuse ne la regarde pas : pas d'apparition sous ses yeux.")]
    [SerializeField] private bool apparaitreHorsDeVue = true;

    [Header("Mise en scene")]
    [Tooltip("Duree du bond vers la camera.")]
    [SerializeField, Min(0.01f)] private float dureeBond = 0.12f;
    [Tooltip("Duree pendant laquelle la poupee reste devant la camera.")]
    [SerializeField, Min(0.1f)] private float dureeFace = 1.3f;
    [Tooltip("Taille du visage a l'ecran (1 = la tete remplit presque l'ecran).")]
    [SerializeField, Range(0.3f, 1.5f)] private float tailleVisage = 0.85f;
    [SerializeField, Range(0f, 1f)] private float forceTremblement = 0.6f;
    [SerializeField] private Color couleurLumiere = new Color(1f, 0.15f, 0.1f);
    [SerializeField] private bool flashsRouges = true;

    [Header("Son")]
    [Tooltip("Votre propre cri. Vide = un cri genere par code.")]
    [SerializeField] private AudioClip cri;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("Degats (optionnel)")]
    [Tooltip("Vies perdues par la joueuse (0 = juste la peur).")]
    [SerializeField, Min(0)] private int viesPerdues = 0;

    [Header("Joueuse")]
    [Tooltip("Vide = l'objet avec le tag Player.")]
    [SerializeField] private Transform joueuse;

    private Camera cam;
    private ControleurJoueuse controleur;
    private Rigidbody corpsJoueuse;
    private Transform os_tete;
    private Renderer rendu_tete;
    private Renderer[] rendus;
    private Vector3 faceLocale = Vector3.forward;   // direction du visage, dans le repere de la poupee
    private Vector3 centreTeteLocal;                 // centre de la tete, dans le repere de la poupee
    private float hauteurTete;
    private Vector3 positionDepart;
    private Quaternion rotationDepart;
    private bool enCours;
    private float prochaineAttaque;

    private static AudioClip criGenere;

    private void Awake()
    {
        positionDepart = transform.position;
        rotationDepart = transform.rotation;
        rendus = GetComponentsInChildren<Renderer>(true);

        os_tete = TrouverOs("head");
        foreach (var r in rendus) if (r.name == "SK_Head") rendu_tete = r;

        // Mesure de la tete une fois pour toutes (la poupee est encore immobile).
        Vector3 centre = rendu_tete ? rendu_tete.bounds.center : (os_tete ? os_tete.position : transform.position + Vector3.up * transform.lossyScale.y);
        centreTeteLocal = transform.InverseTransformPoint(centre);
        hauteurTete = rendu_tete ? rendu_tete.bounds.size.y : 0.35f * transform.lossyScale.y;

        // Le visage regarde dans la direction des orteils (pied -> ball).
        Transform piedG = TrouverOs("foot_l"), piedD = TrouverOs("foot_r");
        Transform ortG = TrouverOs("ball_l"), ortD = TrouverOs("ball_r");
        if (piedG && piedD && ortG && ortD)
        {
            Vector3 avant = (ortG.position + ortD.position - piedG.position - piedD.position) * 0.5f;
            avant.y = 0f;
            if (avant.sqrMagnitude > 1e-6f) faceLocale = Quaternion.Inverse(transform.rotation) * avant.normalized;
        }

        // Boite qui entoure toute la poupee (pour savoir si la camera la voit).
        boiteMonde = new Bounds(transform.position, Vector3.zero);
        bool premiere = true;
        foreach (var r in rendus)
        {
            if (premiere) { boiteMonde = r.bounds; premiere = false; } else boiteMonde.Encapsulate(r.bounds);
        }

        if (cacheeDeLoin) Afficher(false);
    }

    // ------------------------------------------------------------------
    // Cachee de loin

    private Bounds boiteMonde;
    private bool affichee = true;
    private readonly Plane[] plans = new Plane[6];

    private void Afficher(bool oui)
    {
        affichee = oui;
        foreach (var r in rendus) if (r) r.enabled = oui;
    }

    // Vrai si la camera voit la poupee : dans le champ de vision ET pas cachee derriere un mur.
    private bool VueParLaCamera()
    {
        Camera c = Camera.main;
        if (!c) return false;
        GeometryUtility.CalculateFrustumPlanes(c, plans);
        if (!GeometryUtility.TestPlanesAABB(plans, boiteMonde)) return false;

        // Teste le haut et le milieu de la poupee : si les deux sont caches par le mais, elle n'est pas vue.
        Vector3 oeil = c.transform.position;
        Vector3 haut = new Vector3(boiteMonde.center.x, boiteMonde.max.y - boiteMonde.size.y * 0.1f, boiteMonde.center.z);
        return LigneDegagee(oeil, haut) || LigneDegagee(oeil, boiteMonde.center);
    }

    private bool LigneDegagee(Vector3 de, Vector3 a)
    {
        if (!Physics.Linecast(de, a, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
        Transform t = hit.transform;
        // Le corps de la joueuse (en 3e personne) ne cache pas la poupee.
        return t.IsChildOf(transform) || (joueuse && t.IsChildOf(joueuse));
    }

    private void GererVisibilite()
    {
        if (!cacheeDeLoin || enCours) return;
        Vector3 ecart = joueuse.position - transform.position; ecart.y = 0f;
        float d = ecart.magnitude;

        // Petite marge pour ne pas clignoter a la limite.
        bool voulue = affichee ? d <= distanceRevelation + 2f : d <= distanceRevelation;
        if (voulue == affichee) return;
        if (apparaitreHorsDeVue && VueParLaCamera()) return; // attendre qu'elle ne regarde plus
        Afficher(voulue);
    }

    private void Start()
    {
        if (!joueuse)
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j) joueuse = j.transform;
        }
        if (joueuse)
        {
            controleur = joueuse.GetComponentInParent<ControleurJoueuse>();
            corpsJoueuse = joueuse.GetComponentInParent<Rigidbody>();
        }
    }

    private Transform TrouverOs(string nom)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == nom) return t;
        return null;
    }

    private void Update()
    {
        if (!enCours && joueuse && enabled) GererVisibilite();
        if (enCours || !joueuse || Time.time < prochaineAttaque) return;
        var sante = joueuse.GetComponentInParent<SanteJoueuse>();
        if (sante && sante.EstMorte) return;

        Vector3 ecart = joueuse.position - transform.position; ecart.y = 0f;
        if (ecart.magnitude <= distanceDeclenchement) StartCoroutine(Jumpscare());
    }

    // Pour declencher depuis un autre script (ex. une porte qui s'ouvre).
    public void Declencher()
    {
        if (!enCours) StartCoroutine(Jumpscare());
    }

    // ------------------------------------------------------------------
    private IEnumerator Jumpscare()
    {
        enCours = true;
        cam = Camera.main;
        if (!cam) { enCours = false; yield break; }

        // Si elle etait encore cachee, elle apparait d'un coup (dans le bond).
        Afficher(true);

        // 1. Bloquer la joueuse.
        if (controleur) controleur.Bloquee = true;
        if (corpsJoueuse && !corpsJoueuse.isKinematic) corpsJoueuse.linearVelocity = Vector3.zero;

        Vector3 camPos = cam.transform.position;
        Quaternion camRot = cam.transform.rotation;

        // 2. Ou placer la poupee : son visage juste devant la camera, tourne vers elle.
        Vector3 avant = camRot * Vector3.forward; avant.y = 0f;
        if (avant.sqrMagnitude < 1e-4f) avant = cam.transform.up; // camera a la verticale
        avant.Normalize();

        Quaternion rotFinale = RotationFaceA(-avant);
        Vector3 posDebut = transform.position;
        Quaternion rotDebut = transform.rotation;

        // Taille de la tete -> distance pour qu'elle remplisse l'ecran.
        float champ = 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float distance = Mathf.Max(cam.nearClipPlane + 0.1f, hauteurTete / (tailleVisage * champ));
        Vector3 cibleVisage = camPos + avant * distance;

        // Position de la racine telle que le centre de la tete arrive sur cibleVisage.
        transform.SetPositionAndRotation(posDebut, rotFinale);
        Vector3 decalageTete = CentreTete() - transform.position;
        Vector3 posFinale = cibleVisage - decalageTete;
        transform.SetPositionAndRotation(posDebut, rotDebut);

        // Lumiere rouge sur le visage + son + interface.
        Light lumiere = CreerLumiere();
        JouerCri();
        var flash = flashsRouges ? new EcranFlash() : null;

        // 3. Le bond (tres rapide), la camera se tourne vers le visage.
        float t = 0f;
        while (t < dureeBond)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dureeBond);
            transform.SetPositionAndRotation(Vector3.LerpUnclamped(posDebut, posFinale, k), Quaternion.Slerp(rotDebut, rotFinale, k));
            ViserVisage(camPos, 0f);
            yield return null;
        }
        transform.SetPositionAndRotation(posFinale, rotFinale);
        flash?.FlashBlanc();

        if (viesPerdues > 0)
        {
            var sante = joueuse.GetComponentInParent<SanteJoueuse>();
            if (sante) sante.RecevoirDegats(viesPerdues, transform.position);
        }

        // 4. Face a face : tremblement, tete qui tressaute, lumiere qui clignote, flashs.
        Quaternion teteRepos = os_tete ? os_tete.localRotation : Quaternion.identity;
        t = 0f;
        while (t < dureeFace)
        {
            t += Time.deltaTime;
            float force = forceTremblement * (1f - t / dureeFace * 0.6f);
            ViserVisage(camPos, force);

            if (os_tete && Random.value < 0.35f)
                os_tete.localRotation = teteRepos * Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-18f, 18f), Random.Range(-10f, 10f));
            if (lumiere) lumiere.intensity = Random.value < 0.2f ? 0.3f : Random.Range(2.5f, 5f);
            flash?.MettreAJour(t);
            yield return null;
        }
        if (os_tete) os_tete.localRotation = teteRepos;

        // 5. Fin : la poupee disparait (ou retourne a sa place).
        if (lumiere) Destroy(lumiere.gameObject);
        flash?.Detruire();
        cam.transform.SetPositionAndRotation(camPos, camRot);

        if (disparaitApres)
        {
            Afficher(false);
        }
        else
        {
            transform.SetPositionAndRotation(positionDepart, rotationDepart);
            prochaineAttaque = Time.time + delaiAvantNouvelleAttaque;
            if (cacheeDeLoin) Afficher(false); // elle reapparaitra plus tard, hors de vue
        }

        if (controleur) controleur.Bloquee = false;
        enCours = false;
        if (disparaitApres) enabled = false;
    }

    private Vector3 CentreTete() => transform.TransformPoint(centreTeteLocal);

    // Rotation (autour de Y) qui tourne le visage de la poupee vers 'direction'.
    private Quaternion RotationFaceA(Vector3 direction)
    {
        Vector3 faceActuelle = transform.rotation * faceLocale; faceActuelle.y = 0f;
        float angle = Vector3.SignedAngle(faceActuelle, direction, Vector3.up);
        return Quaternion.AngleAxis(angle, Vector3.up) * transform.rotation;
    }

    private void ViserVisage(Vector3 camPos, float tremblement)
    {
        Vector3 secousse = Random.insideUnitSphere * 0.06f * tremblement;
        cam.transform.position = camPos + secousse;
        Vector3 vers = CentreTete() - cam.transform.position;
        if (vers.sqrMagnitude > 1e-4f)
            cam.transform.rotation = Quaternion.LookRotation(vers) * Quaternion.Euler(Random.insideUnitSphere * 4f * tremblement);
    }

    private Light CreerLumiere()
    {
        var go = new GameObject("Lumiere_Jumpscare");
        go.transform.position = cam.transform.position + Vector3.up * 0.3f;
        go.transform.SetParent(cam.transform, true);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = couleurLumiere;
        l.range = 4f;
        l.intensity = 4f;
        l.shadows = LightShadows.None;
        return l;
    }

    private void JouerCri()
    {
        AudioClip clip = cri ? cri : (criGenere ? criGenere : (criGenere = GenererCri()));
        var go = new GameObject("Cri_Jumpscare");
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f; // son "dans la tete", pas dans l'espace
        source.Play();
        Destroy(go, clip.length + 0.2f);
    }

    // Cri synthetise : hurlement aigu qui descend + grincement + souffle + coup sourd au debut.
    private static AudioClip GenererCri()
    {
        const int frequence = 44100;
        const float duree = 1.6f;
        int n = (int)(frequence * duree);
        var donnees = new float[n];
        var hasard = new System.Random(666);
        float phase1 = 0f, phase2 = 0f, phase3 = 0f, bruitFiltre = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / frequence;
            float p = t / duree;

            // Hurlement : frequence qui descend avec un vibrato rapide et instable.
            float vibrato = Mathf.Sin(t * 2f * Mathf.PI * 31f) * 70f + Mathf.Sin(t * 2f * Mathf.PI * 7.3f) * 40f;
            float f1 = Mathf.Lerp(1150f, 520f, p * p) + vibrato;
            float f2 = f1 * 1.498f + 13f;                   // quinte desaccordee = son plus "malade"
            float f3 = f1 * 0.5f - 7f;
            phase1 += f1 / frequence; phase2 += f2 / frequence; phase3 += f3 / frequence;
            float hurlement = DentDeScie(phase1) * 0.5f + DentDeScie(phase2) * 0.3f + Mathf.Sin(phase3 * 2f * Mathf.PI) * 0.3f;

            // Souffle / grincement (bruit filtre).
            float bruit = (float)(hasard.NextDouble() * 2.0 - 1.0);
            bruitFiltre += (bruit - bruitFiltre) * 0.35f;
            float souffle = bruitFiltre * 0.45f;

            // Coup sourd au tout debut.
            float coup = Mathf.Sin(t * 2f * Mathf.PI * 48f) * Mathf.Exp(-t * 9f) * 1.2f;

            // Enveloppe : attaque instantanee, fin en fondu.
            float env = Mathf.Clamp01(t / 0.008f) * (p < 0.7f ? 1f : 1f - (p - 0.7f) / 0.3f);
            float s = (hurlement + souffle) * env + coup;
            donnees[i] = (float)System.Math.Tanh(s * 1.8f) * 0.9f; // saturation
        }
        var clip = AudioClip.Create("Cri_Poupee", n, 1, frequence, false);
        clip.SetData(donnees, 0);
        return clip;
    }

    private static float DentDeScie(float phase) => 2f * (phase - Mathf.Floor(phase)) - 1f;

    // ------------------------------------------------------------------
    // Flashs rouges / noirs plein ecran.
    private class EcranFlash
    {
        private readonly GameObject racine;
        private readonly Image image;

        public EcranFlash()
        {
            racine = new GameObject("INTERFACE_JUMPSCARE", typeof(Canvas));
            var canvas = racine.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var go = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(racine.transform, false);
            image = go.GetComponent<Image>();
            image.raycastTarget = false;
            var r = image.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        // Flash blanc quand la poupee arrive devant la camera.
        public void FlashBlanc() { image.color = new Color(1f, 1f, 1f, 0.85f); }

        public void MettreAJour(float t)
        {
            if (t < 0.06f) return; // garder le flash blanc une fraction de seconde
            float r = Random.value;
            if (r < 0.15f) image.color = new Color(0.6f, 0f, 0f, 0.55f);       // rouge
            else if (r < 0.22f) image.color = new Color(0f, 0f, 0f, 0.85f);    // noir
            else image.color = new Color(0.5f, 0f, 0f, Mathf.Max(0f, 0.25f - t * 0.15f)); // voile rouge qui s'efface
        }

        public void Detruire()
        {
            if (racine) Object.Destroy(racine);
        }
    }
}
