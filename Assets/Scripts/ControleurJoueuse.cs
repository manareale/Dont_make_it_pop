using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class ControleurJoueuse : MonoBehaviour
{
    [Header("Déplacement")]
    [SerializeField] private float vitesseMarche = 2.5f;
    [SerializeField] private float vitesseCourse = 5f;
    [SerializeField] private float vitesseRotation = 10f;

    [Header("Valeurs envoyées au Blend Tree (= Thresholds)")]
    [SerializeField] private float valeurIdle = 0f;
    [SerializeField] private float valeurMarche = 0.5f;
    [SerializeField] private float valeurCourse = 1f;

    [Header("Caméra")]
    [Tooltip("Vide = Camera.main")]
    [SerializeField] private Camera cameraJoueur;
    [SerializeField] private bool premierePersonne = false;
    [SerializeField] private float sensibiliteSouris = 2f;
    [SerializeField, Range(10f, 89f)] private float angleVerticalMax = 70f;

    [Header("Troisième personne")]
    [SerializeField] private float distanceCamera = 4f;
    [SerializeField] private float hauteurCible = 1.5f;
    [Tooltip("La caméra s'arrête devant les murs au lieu de passer à travers.")]
    [SerializeField] private bool cameraEviteMurs = true;
    [SerializeField] private float rayonCamera = 0.25f;

    [Header("Bouton de vue")]
    [Tooltip("Affiche un bouton à l'écran pour passer de la 1re à la 3e personne (la touche V marche aussi).")]
    [SerializeField] private bool afficherBoutonVue = true;
    [SerializeField] private KeyCode toucheVue = KeyCode.V;

    [Header("Première personne")]
    [SerializeField] private float hauteurYeux = 1.6f;

    [Header("Collisions (murs de maïs)")]
    [Tooltip("Rayon de la capsule de collision du personnage.")]
    [SerializeField] private float rayonCorps = 0.3f;
    [Tooltip("Hauteur de la capsule. 0 = calculée à partir du modèle.")]
    [SerializeField] private float hauteurCorps = 0f;

    private Animator animateur;
    private Rigidbody corps;
    private Renderer[] rendus;
    private ShadowCastingMode[] ombresOriginales;

    private Vector3 direction;
    private float vitesseDeplacement;
    private float vitesseAnimation;
    private float lacet;
    private float tangage = 15f;

    private static readonly int ParametreVitesse = Animator.StringToHash("Vitesse");

    private void Awake()
    {
        animateur = GetComponentInChildren<Animator>();
        corps = GetComponent<Rigidbody>();
        if (cameraJoueur == null) cameraJoueur = Camera.main;

        corps.constraints |= RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;
        corps.interpolation = RigidbodyInterpolation.Interpolate;
        ConfigurerCollisions();

        if (animateur == null)
            Debug.LogError("Aucun Animator trouvé !");
        else
            animateur.applyRootMotion = false; // c'est le script qui déplace

        rendus = GetComponentsInChildren<Renderer>(true);
        ombresOriginales = new ShadowCastingMode[rendus.Length];
        for (int i = 0; i < rendus.Length; i++)
            ombresOriginales[i] = rendus[i].shadowCastingMode;
    }

    private void OnEnable()
    {
        if (cameraJoueur)
            cameraJoueur.nearClipPlane = 0.05f;

        lacet = transform.eulerAngles.y;
        VerrouillerSouris(true);
        AppliquerVue();
    }

    private void OnDisable()
    {
        VerrouillerSouris(false);
        MasquerCorps(false);
    }

    // Quand c'est vrai (ex. pendant un jumpscare) : la joueuse ne bouge plus
    // et la caméra n'est plus contrôlée par ce script.
    public bool Bloquee { get; set; }

    private void Update()
    {
        if (Bloquee)
        {
            direction = Vector3.zero;
            vitesseDeplacement = 0f;
            if (animateur) animateur.SetFloat(ParametreVitesse, valeurIdle, 0.1f, Time.deltaTime);
            return;
        }

        // Souris (un clic sur un bouton de l'interface ne reprend pas la souris :
        // sinon le curseur saute au centre et le bouton ne recevrait jamais le clic).
        if (Input.GetKeyDown(KeyCode.Escape))
            VerrouillerSouris(false);
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && !SourisSurInterface())
            VerrouillerSouris(true);

        // Changer de vue
        if (Input.GetKeyDown(toucheVue))
            ChangerVue();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            lacet += Input.GetAxis("Mouse X") * sensibiliteSouris;
            tangage -= Input.GetAxis("Mouse Y") * sensibiliteSouris;
            tangage = Mathf.Clamp(tangage, -angleVerticalMax, angleVerticalMax);
        }

        // Direction relative à la caméra
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 local = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
        direction = Quaternion.Euler(0f, lacet, 0f) * local;

        bool seDeplace = direction.sqrMagnitude > 0.01f;
        bool court = seDeplace &&
                     (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (!seDeplace)
        {
            vitesseAnimation = valeurIdle;
            vitesseDeplacement = 0f;
        }
        else if (court)
        {
            vitesseAnimation = valeurCourse;
            vitesseDeplacement = vitesseCourse;
        }
        else
        {
            vitesseAnimation = valeurMarche;
            vitesseDeplacement = vitesseMarche;
        }

        if (animateur)
            animateur.SetFloat(ParametreVitesse, vitesseAnimation, 0.15f, Time.deltaTime);
    }

    private void FixedUpdate()
    {
        // Déplacement
        if (corps.isKinematic)
        {
            corps.MovePosition(corps.position + direction * vitesseDeplacement * Time.fixedDeltaTime);
        }
        else
        {
            Vector3 v = corps.linearVelocity;
            corps.linearVelocity = new Vector3(
                direction.x * vitesseDeplacement + repousse.x, v.y, direction.z * vitesseDeplacement + repousse.z);
        }

        // La poussee (coup du clown) s'arrete progressivement.
        repousse = Vector3.MoveTowards(repousse, Vector3.zero, 20f * Time.fixedDeltaTime);

        // Rotation
        if (premierePersonne)
        {
            corps.MoveRotation(Quaternion.Euler(0f, lacet, 0f));
        }
        else if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion cible = Quaternion.LookRotation(direction, Vector3.up);
            corps.MoveRotation(Quaternion.Slerp(
                corps.rotation, cible, vitesseRotation * Time.fixedDeltaTime));
        }
    }

    // Pousse la joueuse (ex. coup d'un ennemi). 'vitesse' en m/s, horizontale.
    private Vector3 repousse;
    public void Repousser(Vector3 vitesse)
    {
        repousse = new Vector3(vitesse.x, 0f, vitesse.z);
    }

    private void LateUpdate()
    {
        if (!cameraJoueur || Bloquee) return;

        Quaternion rot = Quaternion.Euler(tangage, lacet, 0f);

        if (premierePersonne)
        {
            cameraJoueur.transform.position = transform.position + Vector3.up * hauteurYeux;
            cameraJoueur.transform.rotation = rot;
        }
        else
        {
            Vector3 pivot = transform.position + Vector3.up * hauteurCible;
            Vector3 arriere = rot * Vector3.back;
            float voulue = cameraEviteMurs ? DistanceLibre(pivot, arriere, distanceCamera) : distanceCamera;
            // Se rapproche tout de suite d'un mur, s'en eloigne en douceur.
            distanceActuelle = voulue < distanceActuelle
                ? voulue
                : Mathf.MoveTowards(distanceActuelle, voulue, 6f * Time.deltaTime);
            cameraJoueur.transform.position = pivot + arriere * distanceActuelle;
            cameraJoueur.transform.rotation = rot;
        }
    }

    // ------------------------------------------------------------------
    // Changement de vue (touche V ou bouton à l'écran)

    public bool PremierePersonne => premierePersonne;
    public event System.Action<bool> VueChangee;   // true = première personne

    public void ChangerVue() => DefinirVue(!premierePersonne);

    public void DefinirVue(bool enPremierePersonne)
    {
        premierePersonne = enPremierePersonne;
        distanceActuelle = 0.5f; // en 3e personne, la caméra recule depuis la tête
        AppliquerVue();
        VueChangee?.Invoke(premierePersonne);
    }

    // Reprend le contrôle de la souris (après un clic sur le bouton).
    public void ReprendreSouris() => VerrouillerSouris(true);

    private float distanceActuelle;
    private readonly RaycastHit[] touchesCamera = new RaycastHit[16];

    // Distance jusqu'au premier obstacle derrière la joueuse (en ignorant son propre corps et les triggers).
    private float DistanceLibre(Vector3 pivot, Vector3 direction, float max)
    {
        int n = Physics.SphereCastNonAlloc(pivot, rayonCamera, direction, touchesCamera, max, ~0, QueryTriggerInteraction.Ignore);
        float libre = max;
        for (int i = 0; i < n; i++)
        {
            RaycastHit t = touchesCamera[i];
            if (t.collider.transform.IsChildOf(transform)) continue;   // la joueuse elle-même
            if (t.collider.attachedRigidbody && !t.collider.attachedRigidbody.isKinematic) continue; // objets qui bougent
            if (t.distance <= 0f) continue;                             // déjà dedans au départ
            libre = Mathf.Min(libre, t.distance);
        }
        return Mathf.Max(0.3f, libre - 0.1f);
    }

    private static bool SourisSurInterface()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }

    private void Start()
    {
        distanceActuelle = distanceCamera;
        if (afficherBoutonVue && FindFirstObjectByType<BoutonChangerVue>() == null)
            gameObject.AddComponent<BoutonChangerVue>();
    }

    private void AppliquerVue()
    {
        MasquerCorps(premierePersonne);
    }

    // Pour que la joueuse soit bloquée par les murs :
    // 1. Le Rigidbody ne doit PAS être kinematic. Un Rigidbody kinematic déplacé
    //    avec MovePosition traverse tout : la physique ne le repousse jamais.
    // 2. Il faut un vrai collider. Le MeshCollider ajouté sur le modèle Mixamo
    //    n'a aucun maillage (le maillage est sur un enfant) : il ne touche rien.
    //    On le remplace par une capsule à la taille du personnage.
    // 3. Friction nulle : la joueuse glisse le long des murs au lieu d'y coller.
    private void ConfigurerCollisions()
    {
        if (corps.isKinematic)
        {
            Debug.LogWarning("[Joueuse] Le Rigidbody était 'Is Kinematic' : elle traversait les murs. " +
                             "Désactivé automatiquement (décochez-le aussi dans l'Inspector).", this);
            corps.isKinematic = false;
        }
        corps.useGravity = true;
        corps.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Désactiver les MeshCollider vides ou non convexes (inutilisables sur un corps qui bouge).
        foreach (MeshCollider mc in GetComponents<MeshCollider>())
        {
            if (mc.sharedMesh == null || !mc.convex)
            {
                mc.enabled = false;
                Debug.LogWarning("[Joueuse] MeshCollider sans maillage désactivé, remplacé par une capsule. " +
                                 "Vous pouvez le supprimer de l'Inspector.", this);
            }
        }

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = gameObject.AddComponent<CapsuleCollider>();
            float h = hauteurCorps > 0f ? hauteurCorps : HauteurDuModele();
            capsule.direction = 1; // axe Y
            capsule.height = h;
            capsule.radius = Mathf.Min(rayonCorps, h * 0.5f);
            capsule.center = new Vector3(0f, h * 0.5f, 0f); // pivot Mixamo = aux pieds
        }
        capsule.isTrigger = false;

        var glisse = new PhysicsMaterial("Joueuse_SansFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0f,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        capsule.sharedMaterial = glisse;
    }

    // Hauteur du personnage d'après ses maillages (environ 1,7 m pour un modèle Mixamo).
    private float HauteurDuModele()
    {
        Bounds b = default;
        bool trouve = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!trouve) { b = r.bounds; trouve = true; } else b.Encapsulate(r.bounds);
        }
        float echelle = Mathf.Max(0.0001f, transform.lossyScale.y);
        float h = trouve ? (b.max.y - transform.position.y) / echelle : 1.7f;
        return Mathf.Clamp(h, 1f, 2.5f);
    }

    private static void VerrouillerSouris(bool verrouiller)
    {
        Cursor.lockState = verrouiller ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !verrouiller;
    }

    private void MasquerCorps(bool masquer)
    {
        if (rendus == null) return;
        for (int i = 0; i < rendus.Length; i++)
            if (rendus[i])
                rendus[i].shadowCastingMode = masquer ? ShadowCastingMode.ShadowsOnly
                                                      : ombresOriginales[i];
    }
}