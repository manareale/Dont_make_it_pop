using UnityEngine;
using UnityEngine.UI;

public class GameManagerM : MonoBehaviour
{
    [Header("Panneaux")]
    [SerializeField] private GameObject panneauDemarrage;   // StartPrincipalPanel
    [SerializeField] private GameObject panneauControles;   // ControlsPanel
    [SerializeField] private GameObject panneauPause;       // PauseMenuPanel
    [SerializeField] private GameObject panneauFin;         // EndMenuPanel
    [SerializeField] private GameObject panneauSon;         // SoundPanel (plus tard)

    [Header("Boutons")]
    [SerializeField] private Button boutonJouer;            // ButtonPlay
    [SerializeField] private Button boutonControles;        // ControlsButton
    [SerializeField] private Button boutonRetourControles;  // "Button" (retour), optionnel
    [SerializeField] private Button boutonContinuer;        // ContinueButton
    [SerializeField] private Button boutonRecommencer;      // RestartButton
    [SerializeField] private Button boutonQuitter;          // ExitButton
    [SerializeField] private Button boutonRejouer;          // ReplayButton

    [Header("Joueur")]
    [SerializeField] private ControleurJoueuse controleurJoueuse;
    [Tooltip("Le Rigidbody de la joueuse, pour la remettre au départ.")]
    [SerializeField] private Rigidbody corpsJoueuse;

    private enum Etat { Demarrage, Jeu, Pause, Fin }
    private Etat etat;

    private Vector3 positionDepart;
    private Quaternion rotationDepart;

    private void Awake()
    {
        if (boutonJouer) boutonJouer.onClick.AddListener(Jouer);
        if (boutonControles) boutonControles.onClick.AddListener(OuvrirControles);
        if (boutonRetourControles) boutonRetourControles.onClick.AddListener(FermerControles);
        if (boutonContinuer) boutonContinuer.onClick.AddListener(Continuer);
        if (boutonRecommencer) boutonRecommencer.onClick.AddListener(Recommencer);
        if (boutonQuitter) boutonQuitter.onClick.AddListener(Quitter);
        if (boutonRejouer) boutonRejouer.onClick.AddListener(Recommencer);

        if (corpsJoueuse)
        {
            positionDepart = corpsJoueuse.position;
            rotationDepart = corpsJoueuse.rotation;
        }
    }

    private void Start()
    {
        AfficherDemarrage();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (etat == Etat.Jeu) Pause();
        else if (etat == Etat.Pause) Continuer();
        else if (etat == Etat.Demarrage && panneauControles && panneauControles.activeSelf)
            FermerControles();
    }

    // ---------- États ----------

    private void AfficherDemarrage()
    {
        etat = Etat.Demarrage;
        MontrerSeulement(panneauDemarrage);
        if (panneauControles) panneauControles.SetActive(false);
        if (panneauSon) panneauSon.SetActive(false);
        Figer(true);
    }

    private void DemarrerJeu()
    {
        etat = Etat.Jeu;
        MontrerSeulement(null);
        Figer(false);
    }

    private void Pause()
    {
        etat = Etat.Pause;
        MontrerSeulement(panneauPause);
        Figer(true);
    }

    // À appeler quand le joueur gagne :
    // FindFirstObjectByType<GestionnaireUI>().AfficherVictoire();
    public void AfficherVictoire()
    {
        etat = Etat.Fin;
        MontrerSeulement(panneauFin);
        Figer(true);
    }

    // ---------- Boutons ----------

    public void Jouer() => DemarrerJeu();
    public void Continuer() => DemarrerJeu();

    public void OuvrirControles()
    {
        if (panneauControles) panneauControles.SetActive(true);
    }

    public void FermerControles()
    {
        if (panneauControles) panneauControles.SetActive(false);
    }

    // Remet la joueuse au départ SANS recharger la scène.
    public void Recommencer()
    {
        if (corpsJoueuse)
        {
            corpsJoueuse.linearVelocity = Vector3.zero;   // Unity 6 (sinon .velocity)
            corpsJoueuse.angularVelocity = Vector3.zero;
            corpsJoueuse.position = positionDepart;
            corpsJoueuse.rotation = rotationDepart;
            corpsJoueuse.transform.SetPositionAndRotation(positionDepart, rotationDepart);
        }

        // Ici, plus tard : remettre les batteries, la porte, etc.
        DemarrerJeu();
    }

    public void Quitter()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Utilitaires ----------

    private void MontrerSeulement(GameObject panneau)
    {
        if (panneauDemarrage) panneauDemarrage.SetActive(panneau == panneauDemarrage);
        if (panneauPause) panneauPause.SetActive(panneau == panneauPause);
        if (panneauFin) panneauFin.SetActive(panneau == panneauFin);
    }

    private void Figer(bool figer)
    {
        Time.timeScale = figer ? 0f : 1f;

        if (controleurJoueuse)
            controleurJoueuse.enabled = !figer;

        if (figer)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}