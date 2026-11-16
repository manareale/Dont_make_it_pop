using UnityEngine;
[DefaultExecutionOrder(100)]
public class BrouillardHorreur : MonoBehaviour
{
    [SerializeField] private Camera cameraJoueur;
    [SerializeField] private Transform joueur;
    [SerializeField] private Light lumiereDirectionnelle;
    [SerializeField] private Color couleurBrouillard = new Color(0.16f, 0.17f, 0.13f);
    [SerializeField] private float distanceNette = 6f; // Plus la valeur est petite, plus le joueur voit court.
    [SerializeField] private float epaisseurBrume = 8f; // Plus la valeur est petite, plus le mur de brume est brutal.
    [SerializeField] private Color lumiereAmbiante = new Color(0.05f, 0.05f, 0.06f);
    [SerializeField] private float intensiteLumiereDirectionnelle = 0.15f;
    [SerializeField] private Color couleurLumiereDirectionnelle = new Color(0.6f, 0.7f, 1f); // Clair de lune
    [SerializeField] private bool creerLampeJoueur = true; // Elle éclaire le maïs proche et laisse le reste dans le noir.
    [SerializeField] private Color couleurLampe = new Color(1f, 0.82f, 0.55f);
    [SerializeField] private float porteeLampe = 9f; // Rayon d'éclairage de la lampe, en mètres.
    [SerializeField] private float intensiteLampe = 2f;
    [SerializeField] private Vector3 positionLampe = new Vector3(0f, 1f, 0f); // Position de la lampe par rapport au joueur (au-dessus de lui).

    private Light lampeJoueur;

    private void Awake() {
        if (cameraJoueur == null) {
            cameraJoueur = Camera.main;
        }
        if (joueur == null) {
            GameObject objetJoueur = GameObject.FindGameObjectWithTag("Player");

            if (objetJoueur != null) {
                joueur = objetJoueur.transform;
            }
        }
        if (lumiereDirectionnelle == null) {
            Light[] lumieres = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light lumiere in lumieres) {
                if (lumiere.type == LightType.Directional) {
                    lumiereDirectionnelle = lumiere;
                    break;
                }
            }
        }
        CreerLampe();
        Appliquer();
    }
    private void LateUpdate() {
        MettreAJourDistancesBrouillard();
    }

    private void OnValidate() {
        distanceNette = Mathf.Max(0f, distanceNette);
        epaisseurBrume = Mathf.Max(0.1f, epaisseurBrume);

        if (Application.isPlaying) {
            Appliquer();
        }
    }

    private void CreerLampe() {
        if (!creerLampeJoueur || joueur == null) {
            return;
        }
        GameObject objetLampe = new GameObject("Lampe_Joueur");
        objetLampe.transform.SetParent(joueur, false);
        objetLampe.transform.localPosition = positionLampe;

        lampeJoueur = objetLampe.AddComponent<Light>();
        lampeJoueur.type = LightType.Point;
        lampeJoueur.shadows = LightShadows.None;
    }

    private void Appliquer() {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = couleurBrouillard;

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = lumiereAmbiante;

        if (cameraJoueur != null) {
            cameraJoueur.clearFlags = CameraClearFlags.SolidColor;
            cameraJoueur.backgroundColor = couleurBrouillard;
        }

        if (lumiereDirectionnelle != null) {
            lumiereDirectionnelle.intensity = intensiteLumiereDirectionnelle;
            lumiereDirectionnelle.color = couleurLumiereDirectionnelle;
        }
        if (lampeJoueur != null) {
            lampeJoueur.color = couleurLampe;
            lampeJoueur.range = porteeLampe;
            lampeJoueur.intensity = intensiteLampe;
            lampeJoueur.transform.localPosition = positionLampe;
        }
        MettreAJourDistancesBrouillard();
    }

    private void MettreAJourDistancesBrouillard() {
        float distanceCameraJoueur = 0f;

        if (cameraJoueur != null && joueur != null) {
            distanceCameraJoueur = Vector3.Distance(
                cameraJoueur.transform.position,
                joueur.position
            );
        }
        RenderSettings.fogStartDistance = distanceCameraJoueur + distanceNette;
        RenderSettings.fogEndDistance = distanceCameraJoueur + distanceNette + epaisseurBrume;
    }
}