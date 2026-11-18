using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Ennemi qui poursuit le joueur dans le labyrinthe et anime le clown
// avec le Blend Tree "Vitesse" (0 = Idle, 0.5 = Walk, 1 = Run).
//
// Comportement :
// - Repos  (Idle) : le joueur est loin et le clown ne sait pas ou il est.
// - Marche (Walk) : le clown ENTEND le joueur (assez proche, meme derriere le mais)
//                   ou va voir le dernier endroit ou il l'a vu.
// - Course (Run)  : le clown VOIT le joueur (devant lui, sans mur entre eux).
// - Attrape       : le joueur est renvoye au point de reapparition.
//
// Le chemin est calcule sur une grille qui evite les murs (voir GrilleNavigation).
[RequireComponent(typeof(Animator))]
public class ClownPoursuivant : MonoBehaviour
{
    public enum Etat { Repos, Marche, Course }

    [Header("Cible")]
    [Tooltip("Le joueur. Vide = l'objet avec le tag Player.")]
    [SerializeField] private Transform cible;
    [Tooltip("Ou renvoyer le joueur quand il est attrape. Vide = l'objet Point_Reapparition.")]
    [SerializeField] private Transform pointReapparition;

    [Header("Vitesses (m/s)")]
    [SerializeField] private float vitesseMarche = 1.6f;
    [SerializeField] private float vitesseCourse = 4.2f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float vitesseRotation = 8f;

    [Header("Perception")]
    [Tooltip("Distance a laquelle le clown peut VOIR le joueur (il court).")]
    [SerializeField] private float distanceVision = 18f;
    [Tooltip("Angle total du champ de vision, en degres.")]
    [SerializeField, Range(30f, 360f)] private float angleVision = 140f;
    [Tooltip("Distance a laquelle le clown ENTEND le joueur a travers les murs (il marche vers lui). 0 = jamais.")]
    [SerializeField] private float distanceEcoute = 10f;
    [SerializeField] private float hauteurYeux = 1.5f;
    [Tooltip("Ce qui bloque la vue et le passage.")]
    [SerializeField] private LayerMask obstacles = ~0;

    [Header("Attraper")]
    [SerializeField] private float distanceAttrape = 1.1f;
    [Tooltip("Pause du clown apres avoir attrape le joueur.")]
    [SerializeField] private float pauseApresAttrape = 2f;
    [Tooltip("Le clown retourne a sa position de depart apres avoir attrape le joueur.")]
    [SerializeField] private bool retourAuDepart = true;
    [Tooltip("Flash rouge + fleche de direction a l'ecran quand le joueur est attrape.")]
    [SerializeField] private bool afficherIndicateurDegats = true;
    [Tooltip("Appele quand le joueur est attrape (son, ecran de defaite...).")]
    public UnityEvent quandAttrape;

    [Header("Navigation")]
    [Tooltip("Taille d'une case de la grille. Plus petit = plus precis mais plus lent a construire.")]
    [SerializeField, Range(0.25f, 1.5f)] private float tailleCase = 0.5f;
    [SerializeField] private float rayonClown = 0.3f;
    [SerializeField] private float hauteurClown = 1.8f;
    [Tooltip("Optionnel : un collider qui delimite la zone de navigation. Vide = automatique.")]
    [SerializeField] private Collider limiteZone;
    [SerializeField] private float intervalleChemin = 0.25f;
    [SerializeField] private bool afficherGrille = false;

    [Header("Animation")]
    [SerializeField] private float lissageAnimation = 0.1f;

    // Grille partagee par tous les clowns de la scene.
    private static GrilleNavigation grillePartagee;

    public Etat EtatActuel { get; private set; } = Etat.Repos;

    private Animator animateur;
    private Rigidbody corps;
    private Rigidbody corpsCible;
    private Collider colliderCible;
    private GrilleNavigation grille;

    private readonly List<Vector3> chemin = new List<Vector3>();
    private int prochainPoint;
    private float vitesseActuelle;
    private float vitesseVoulue;
    private float prochainCalcul;
    private float pauseJusqua;
    private bool aUneDestination;
    private Vector3 derniereVue;
    private Vector3 positionDepart;
    private Quaternion rotationDepart;

    private static readonly int ParametreVitesse = Animator.StringToHash("Vitesse");

    private void Awake()
    {
        animateur = GetComponent<Animator>();
        animateur.applyRootMotion = false; // c'est ce script qui deplace le clown
        corps = GetComponent<Rigidbody>();
        if (corps)
        {
            corps.isKinematic = true;
            corps.useGravity = false;
            corps.interpolation = RigidbodyInterpolation.Interpolate;
        }
        positionDepart = transform.position;
        rotationDepart = transform.rotation;
    }

    private IEnumerator Start()
    {
        if (!cible)
        {
            GameObject joueur = GameObject.FindGameObjectWithTag("Player");
            if (joueur) cible = joueur.transform;
        }
        if (!pointReapparition)
        {
            GameObject point = GameObject.Find("Point_Reapparition");
            if (point) pointReapparition = point.transform;
        }
        if (cible)
        {
            corpsCible = cible.GetComponentInParent<Rigidbody>();
            colliderCible = cible.GetComponentInChildren<Collider>();
        }
        else Debug.LogWarning("[Clown] Aucun joueur trouve (tag Player).", this);

        // Attendre une image : le labyrinthe peut etre genere dans un autre Start().
        yield return null;
        ObtenirGrille();
    }

    private void ObtenirGrille()
    {
        int scene = gameObject.scene.handle;
        if (grillePartagee == null || grillePartagee.SceneHandle != scene || !Mathf.Approximately(grillePartagee.TailleCase, tailleCase))
        {
            Bounds zone = limiteZone ? limiteZone.bounds : GrilleNavigation.ZoneAutomatique(transform.position);
            float debut = Time.realtimeSinceStartup;
            grillePartagee = GrilleNavigation.Construire(zone, tailleCase, rayonClown, hauteurClown, 0.45f, obstacles, scene);
            Debug.Log($"[Clown] Grille de navigation {grillePartagee.Grille.LargeurX} x {grillePartagee.Grille.LargeurZ} " +
                      $"construite en {(Time.realtimeSinceStartup - debut) * 1000f:F0} ms.", this);
        }
        grille = grillePartagee;
    }

    // A appeler si les murs changent pendant la partie (ex. labyrinthe regenere).
    public static void ReconstruireGrille() => grillePartagee = null;

    // ------------------------------------------------------------------
    private void Update()
    {
        if (grille == null || !cible) { ChangerVitesseAnimation(); return; }

        if (Time.time < pauseJusqua)
        {
            vitesseVoulue = 0f;
            ChangerVitesseAnimation();
            return;
        }

        Vector3 ici = Position;
        Vector3 joueur = cible.position;
        Vector3 ecart = joueur - ici; ecart.y = 0f;
        float distance = ecart.magnitude;

        // Attrape ?
        if (distance <= distanceAttrape && Mathf.Abs(joueur.y - ici.y) < 2.5f)
        {
            Attraper();
            ChangerVitesseAnimation();
            return;
        }

        if (Time.time >= prochainCalcul)
        {
            prochainCalcul = Time.time + intervalleChemin;
            Decider(ici, distance);
        }

        ChangerVitesseAnimation();
    }

    private void Decider(Vector3 ici, float distance)
    {
        if (VoitLeJoueur(ici, distance))
        {
            EtatActuel = Etat.Course;
            derniereVue = cible.position;
            aUneDestination = true;
            AllerVers(ici, cible.position);
        }
        else if (distance <= distanceEcoute)
        {
            EtatActuel = Etat.Marche;
            derniereVue = cible.position;
            aUneDestination = true;
            AllerVers(ici, cible.position);
        }
        else if (aUneDestination)
        {
            // Va voir le dernier endroit connu, puis abandonne.
            Vector3 d = derniereVue - ici; d.y = 0f;
            if (d.magnitude < 0.8f || !AllerVers(ici, derniereVue)) aUneDestination = false;
            EtatActuel = aUneDestination ? Etat.Marche : Etat.Repos;
        }
        else
        {
            EtatActuel = Etat.Repos;
        }

        if (EtatActuel == Etat.Repos) chemin.Clear();
        vitesseVoulue = EtatActuel == Etat.Course ? vitesseCourse : EtatActuel == Etat.Marche ? vitesseMarche : 0f;
    }

    private bool AllerVers(Vector3 depart, Vector3 destination)
    {
        if (!grille.TrouverChemin(depart, destination, chemin)) { chemin.Clear(); return false; }
        prochainPoint = 0;
        return true;
    }

    private bool VoitLeJoueur(Vector3 ici, float distance)
    {
        if (distance > distanceVision) return false;
        Vector3 yeux = ici + Vector3.up * hauteurYeux;
        Vector3 point = colliderCible ? colliderCible.bounds.center : cible.position + Vector3.up;
        Vector3 vers = point - yeux;
        Vector3 plat = new Vector3(vers.x, 0f, vers.z);
        if (plat.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, plat) > angleVision * 0.5f) return false;

        if (Physics.Linecast(yeux, point, out RaycastHit touche, obstacles, QueryTriggerInteraction.Ignore))
        {
            Transform t = touche.transform;
            if (t != cible && !t.IsChildOf(cible) && !t.IsChildOf(transform)) return false;
        }
        return true;
    }

    private void Attraper()
    {
        // Avant la teleportation : la fleche doit montrer ou etait le clown.
        if (afficherIndicateurDegats) IndicateurDegats.Signaler(Position);
        quandAttrape?.Invoke();

        if (pointReapparition)
        {
            if (corpsCible)
            {
                corpsCible.position = pointReapparition.position;
                if (!corpsCible.isKinematic)
                {
                    corpsCible.linearVelocity = Vector3.zero;
                    corpsCible.angularVelocity = Vector3.zero;
                }
            }
            cible.position = pointReapparition.position;
        }

        if (retourAuDepart) Teleporter(positionDepart, rotationDepart);
        chemin.Clear();
        aUneDestination = false;
        EtatActuel = Etat.Repos;
        vitesseVoulue = 0f;
        vitesseActuelle = 0f;
        pauseJusqua = Time.time + pauseApresAttrape;
    }

    // ------------------------------------------------------------------
    // Deplacement

    private Vector3 Position => corps ? corps.position : transform.position;

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool aUnChemin = prochainPoint < chemin.Count;
        float voulue = aUnChemin && Time.time >= pauseJusqua ? vitesseVoulue : 0f;
        vitesseActuelle = Mathf.MoveTowards(vitesseActuelle, voulue, acceleration * dt);
        if (!aUnChemin || vitesseActuelle <= 0.001f) return;

        Vector3 ici = Position;
        Vector3 cibleChemin = chemin[prochainPoint];
        Vector3 vers = cibleChemin - ici; vers.y = 0f;

        // Point atteint : passer au suivant.
        while (vers.magnitude < 0.25f)
        {
            prochainPoint++;
            if (prochainPoint >= chemin.Count) return;
            cibleChemin = chemin[prochainPoint];
            vers = cibleChemin - ici; vers.y = 0f;
        }

        // Ne pas avancer jusque dans le joueur.
        float pas = vitesseActuelle * dt;
        Vector3 versJoueur = cible ? cible.position - ici : Vector3.zero; versJoueur.y = 0f;
        if (cible && versJoueur.magnitude - pas < distanceAttrape * 0.8f) pas = Mathf.Max(0f, versJoueur.magnitude - distanceAttrape * 0.8f);

        Vector3 direction = vers.normalized;
        Vector3 nouvelle = ici + direction * Mathf.Min(pas, vers.magnitude);
        nouvelle.y = Mathf.Lerp(ici.y, cibleChemin.y, 10f * dt); // suit le sol

        Quaternion rot = Quaternion.Slerp(corps ? corps.rotation : transform.rotation,
                                          Quaternion.LookRotation(direction, Vector3.up), vitesseRotation * dt);
        if (corps) { corps.MovePosition(nouvelle); corps.MoveRotation(rot); }
        else { transform.position = nouvelle; transform.rotation = rot; }
    }

    private void Teleporter(Vector3 position, Quaternion rotation)
    {
        if (corps) { corps.position = position; corps.rotation = rotation; }
        transform.SetPositionAndRotation(position, rotation);
    }

    // 0 -> Idle, 0.5 -> Walk (a vitesseMarche), 1 -> Run (a vitesseCourse).
    private void ChangerVitesseAnimation()
    {
        float v = vitesseActuelle;
        float valeur = v <= vitesseMarche
            ? 0.5f * v / Mathf.Max(0.01f, vitesseMarche)
            : 0.5f + 0.5f * Mathf.Clamp01((v - vitesseMarche) / Mathf.Max(0.01f, vitesseCourse - vitesseMarche));
        animateur.SetFloat(ParametreVitesse, valeur, lissageAnimation, Time.deltaTime);
    }

    // ------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(p, distanceEcoute);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Quaternion g = Quaternion.Euler(0, -angleVision * 0.5f, 0), d = Quaternion.Euler(0, angleVision * 0.5f, 0);
        Vector3 yeux = p + Vector3.up * hauteurYeux;
        Gizmos.DrawLine(yeux, yeux + g * transform.forward * distanceVision);
        Gizmos.DrawLine(yeux, yeux + d * transform.forward * distanceVision);

        Gizmos.color = Color.cyan;
        Vector3 precedent = p + Vector3.up * 0.2f;
        for (int i = prochainPoint; i < chemin.Count; i++)
        {
            Vector3 q = chemin[i] + Vector3.up * 0.2f;
            Gizmos.DrawLine(precedent, q);
            precedent = q;
        }
        if (afficherGrille && grille != null) grille.DessinerGizmos(p.y + 0.05f);
    }
}
