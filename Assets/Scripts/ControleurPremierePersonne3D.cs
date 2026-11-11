using UnityEngine;
using UnityEngine.Rendering;

// Oblige l'objet a posseder un Rigidbody (comme ControleurRobot3D).
[RequireComponent(typeof(Rigidbody))]

// Controleur a la PREMIERE PERSONNE pour le robot.
//
// Fonctionnement :
// 1. La souris tourne la vue (gauche/droite = le robot, haut/bas = la camera).
// 2. ZQSD / WASD / fleches deplacent le robot DANS LA DIRECTION OU IL REGARDE.
// 3. Shift = courir.
// 4. La camera est placee a la hauteur des yeux du robot a chaque image.
// 5. Echap libere la souris, un clic la reprend.
//
// A l'activation, ce script desactive automatiquement ControleurRobot3D
// (sur le robot) et CameraSuiveuse3D (sur la camera) pour eviter les conflits.
public class ControleurPremierePersonne3D : MonoBehaviour
{
    [Header("Deplacement")]
    [SerializeField] private float vitesse = 4f;
    [SerializeField] private float vitesseCourse = 7f;

    [Header("Vue")]
    [Tooltip("Camera utilisee. Vide = Camera.main.")]
    [SerializeField] private Camera cameraJoueur;
    [Tooltip("Hauteur des yeux par rapport au pivot du robot.")]
    [SerializeField] private float hauteurYeux = 0.7f;
    [SerializeField] private float sensibiliteSouris = 2f;
    [SerializeField] private bool inverserAxeVertical = false;
    [SerializeField, Range(10f, 89f)] private float angleVerticalMax = 80f;
    [Tooltip("Cache le corps du robot pour qu'il ne bloque pas la vue (son ombre reste visible).")]
    [SerializeField] private bool masquerCorps = true;

    private Rigidbody corps;
    private Vector3 direction;   // direction voulue, en coordonnees monde
    private bool course;
    private float lacet;         // rotation gauche/droite (yaw), en degres
    private float tangage;       // rotation haut/bas (pitch), en degres

    private Renderer[] rendus;
    private ShadowCastingMode[] ombresOriginales;

    private void Awake()
    {
        corps = GetComponent<Rigidbody>();
        if (cameraJoueur == null) cameraJoueur = Camera.main;
    }

    private void OnEnable()
    {
        // Couper l'ancien controle a la troisieme personne.
        var ancienControleur = GetComponent<ControleurRobot3D>();
        if (ancienControleur) ancienControleur.enabled = false;
        if (cameraJoueur)
        {
            var suivi = cameraJoueur.GetComponent<CameraSuiveuse3D>();
            if (suivi) suivi.enabled = false;
            // Evite de voir "a travers" les murs de mais quand on les longe.
            cameraJoueur.nearClipPlane = 0.05f;
        }

        // Le robot ne doit jamais basculer.
        corps.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        lacet = transform.eulerAngles.y;
        tangage = 0f;
        VerrouillerSouris(true);
        MasquerCorps(masquerCorps);
    }

    private void OnDisable()
    {
        VerrouillerSouris(false);
        MasquerCorps(false);
    }

    private void Update()
    {
        // Echap : liberer la souris. Clic : la reprendre.
        if (Input.GetKeyDown(KeyCode.Escape)) VerrouillerSouris(false);
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) VerrouillerSouris(true);

        // Regarder avec la souris (seulement si elle est verrouillee).
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            lacet += Input.GetAxis("Mouse X") * sensibiliteSouris;
            float dy = Input.GetAxis("Mouse Y") * sensibiliteSouris;
            tangage += inverserAxeVertical ? dy : -dy;
            tangage = Mathf.Clamp(tangage, -angleVerticalMax, angleVerticalMax);
        }

        // Deplacement relatif a la direction du regard (sans le haut/bas).
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 local = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
        direction = Quaternion.Euler(0f, lacet, 0f) * local;
        course = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void FixedUpdate()
    {
        float v = course ? vitesseCourse : vitesse;

        if (corps.isKinematic)
        {
            corps.MovePosition(corps.position + direction * v * Time.fixedDeltaTime);
        }
        else
        {
            // Avec un Rigidbody dynamique, on pilote la vitesse :
            // le robot glisse le long des murs au lieu de les traverser.
            Vector3 vitesseActuelle = corps.linearVelocity;
            corps.linearVelocity = new Vector3(direction.x * v, vitesseActuelle.y, direction.z * v);
        }

        corps.MoveRotation(Quaternion.Euler(0f, lacet, 0f));
    }

    // La camera est mise a jour apres tout le reste, a chaque image,
    // pour une vue fluide (meme si la physique tourne a une autre frequence).
    private void LateUpdate()
    {
        if (!cameraJoueur) return;
        cameraJoueur.transform.position = transform.position + Vector3.up * hauteurYeux;
        cameraJoueur.transform.rotation = Quaternion.Euler(tangage, lacet, 0f);
    }

    private static void VerrouillerSouris(bool verrouiller)
    {
        Cursor.lockState = verrouiller ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !verrouiller;
    }

    // Le corps reste dans la scene (collisions, ombre) mais n'est plus dessine.
    private void MasquerCorps(bool masquer)
    {
        if (rendus == null)
        {
            rendus = GetComponentsInChildren<Renderer>(true);
            ombresOriginales = new ShadowCastingMode[rendus.Length];
            for (int i = 0; i < rendus.Length; i++) ombresOriginales[i] = rendus[i].shadowCastingMode;
        }
        for (int i = 0; i < rendus.Length; i++)
            if (rendus[i]) rendus[i].shadowCastingMode = masquer ? ShadowCastingMode.ShadowsOnly : ombresOriginales[i];
    }
}
