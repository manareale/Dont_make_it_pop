using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Bouton a l'ecran (en haut a droite) pour passer de la 1re a la 3e personne.
//
// Il est ajoute automatiquement par ControleurJoueuse (option "Afficher Bouton Vue").
// Pendant le jeu la souris est capturee : appuyez sur Echap pour la liberer,
// puis cliquez sur le bouton. La touche V fait la meme chose.
public class BoutonChangerVue : MonoBehaviour
{
    [Tooltip("Vide = le ControleurJoueuse de l'objet avec le tag Player.")]
    [SerializeField] private ControleurJoueuse joueuse;

    private GameObject racine;
    private Text texte;
    private Image fond;

    private static readonly Color CouleurFond = new Color(0.05f, 0.05f, 0.06f, 0.7f);
    private static readonly Color CouleurSurvol = new Color(0.25f, 0.06f, 0.06f, 0.85f);

    private void Start()
    {
        if (!joueuse) joueuse = GetComponent<ControleurJoueuse>();
        if (!joueuse)
        {
            var j = GameObject.FindGameObjectWithTag("Player");
            if (j) joueuse = j.GetComponentInParent<ControleurJoueuse>();
        }
        if (!joueuse)
        {
            Debug.LogWarning("[Bouton vue] Aucun ControleurJoueuse trouve.", this);
            enabled = false;
            return;
        }

        AssurerEventSystem();
        Construire();
        joueuse.VueChangee += MettreAJourTexte;
        MettreAJourTexte(joueuse.PremierePersonne);
    }

    private void OnDestroy()
    {
        if (joueuse) joueuse.VueChangee -= MettreAJourTexte;
        if (racine) Destroy(racine);
    }

    private void OnEnable() { if (racine) racine.SetActive(true); }
    private void OnDisable() { if (racine) racine.SetActive(false); }

    private void Cliquer()
    {
        joueuse.ChangerVue();
        joueuse.ReprendreSouris(); // retour au jeu tout de suite
    }

    private void MettreAJourTexte(bool premierePersonne)
    {
        if (texte) texte.text = (premierePersonne ? "VUE : 1re PERSONNE" : "VUE : 3e PERSONNE") + "   [V]";
    }

    // Les boutons ont besoin d'un EventSystem dans la scene pour recevoir les clics.
    private static void AssurerEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void Construire()
    {
        racine = new GameObject("INTERFACE_BOUTON_VUE", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = racine.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
        var scaler = racine.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Bouton en haut a droite.
        var go = new GameObject("Bouton_Vue", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(racine.transform, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-30f, -30f);
        r.sizeDelta = new Vector2(360f, 56f);

        fond = go.GetComponent<Image>();
        fond.color = Color.white;
        var bouton = go.GetComponent<Button>();
        bouton.targetGraphic = fond;
        var couleurs = bouton.colors;
        couleurs.normalColor = CouleurFond;
        couleurs.highlightedColor = CouleurSurvol;
        couleurs.selectedColor = CouleurFond;
        couleurs.pressedColor = new Color(0.45f, 0.08f, 0.08f, 0.95f);
        couleurs.colorMultiplier = 1f;
        couleurs.fadeDuration = 0.08f;
        bouton.colors = couleurs;
        // Pas de "selection" au clavier : sinon Espace/Entree recliqueraient le bouton.
        var nav = bouton.navigation; nav.mode = Navigation.Mode.None; bouton.navigation = nav;
        bouton.onClick.AddListener(Cliquer);

        var cadre = go.AddComponent<Outline>();
        cadre.effectColor = new Color(0.8f, 0.15f, 0.15f, 0.6f);
        cadre.effectDistance = new Vector2(2f, -2f);

        var t = new GameObject("Texte", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(go.transform, false);
        texte = t.GetComponent<Text>();
        texte.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texte.fontSize = 24;
        texte.fontStyle = FontStyle.Bold;
        texte.alignment = TextAnchor.MiddleCenter;
        texte.color = new Color(0.95f, 0.92f, 0.88f);
        texte.raycastTarget = false;
        var rt = texte.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
