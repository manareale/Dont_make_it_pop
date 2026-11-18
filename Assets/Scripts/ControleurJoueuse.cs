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

    private void Update()
    {
        // Souris
        if (Input.GetKeyDown(KeyCode.Escape))
            VerrouillerSouris(false);
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            VerrouillerSouris(true);

        // Changer de vue
        if (Input.GetKeyDown(KeyCode.V))
        {
            premierePersonne = !premierePersonne;
            AppliquerVue();
        }

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
                direction.x * vitesseDeplacement, v.y, direction.z * vitesseDeplacement);
        }

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

    private void LateUpdate()
    {
        if (!cameraJoueur) return;

        Quaternion rot = Quaternion.Euler(tangage, lacet, 0f);

        if (premierePersonne)
        {
            cameraJoueur.transform.position = transform.position + Vector3.up * hauteurYeux;
            cameraJoueur.transform.rotation = rot;
        }
        else
        {
            Vector3 pivot = transform.position + Vector3.up * hauteurCible;
            cameraJoueur.transform.position = pivot - rot * Vector3.forward * distanceCamera;
            cameraJoueur.transform.rotation = rot;
        }
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