using UnityEngine;
using UnityEngine.UI;

// Indicateur de degats a l'ecran :
// 1. un flash rouge sur les bords de l'ecran (vignette) ;
// 2. une fleche courbe qui montre D'OU vient le coup
//    (en haut = devant, en bas = derriere, a gauche/droite = sur le cote) ;
// 3. un texte "TOUCHE !" (optionnel).
//
// Aucune configuration : l'interface est creee automatiquement au premier coup.
// Depuis n'importe quel script :  IndicateurDegats.Signaler(positionDeLaSource);
// Pour changer les reglages, ajoutez ce composant a un objet de la scene.
public class IndicateurDegats : MonoBehaviour
{
    [Header("Couleur et duree")]
    [SerializeField] private Color couleur = new Color(0.85f, 0.05f, 0.05f, 1f);
    [Tooltip("Temps pendant lequel l'indicateur reste au maximum (secondes).")]
    [SerializeField] private float tenue = 0.2f;
    [Tooltip("Temps du fondu (secondes).")]
    [SerializeField] private float fondu = 1.2f;

    [Header("Vignette rouge")]
    [SerializeField, Range(0f, 1f)] private float intensiteVignette = 0.75f;

    [Header("Fleche de direction")]
    [SerializeField] private bool afficherDirection = true;
    [Tooltip("Distance de la fleche au centre, en part de la hauteur de l'ecran.")]
    [SerializeField, Range(0.1f, 0.45f)] private float distanceFleche = 0.28f;

    [Header("Texte")]
    [SerializeField] private bool afficherTexte = true;
    [SerializeField] private string texte = "TOUCHÉ !";

    private static IndicateurDegats instance;

    private Image vignette;
    private Image fleche;
    private RectTransform pivotFleche;
    private Text message;
    private float debut = -100f;
    private float angleSource;   // angle de la source par rapport au regard (0 = devant)
    private bool aUneDirection;

    // ------------------------------------------------------------------
    // Point d'entree pour les autres scripts.

    // source : position de ce qui a fait mal (ex. le clown). Le regard utilise est celui de Camera.main.
    public static void Signaler(Vector3 source)
    {
        Instance.Declencher(source, true);
    }

    // Sans direction (ex. zone dangereuse sous les pieds).
    public static void Signaler()
    {
        Instance.Declencher(Vector3.zero, false);
    }

    private static IndicateurDegats Instance
    {
        get
        {
            if (instance) return instance;
            instance = FindFirstObjectByType<IndicateurDegats>();
            if (!instance) instance = new GameObject("INDICATEUR_DEGATS").AddComponent<IndicateurDegats>();
            return instance;
        }
    }

    // ------------------------------------------------------------------
    private void Awake()
    {
        if (instance && instance != this) { Destroy(this); return; }
        instance = this;
        ConstruireInterface();
        Appliquer(0f);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Declencher(Vector3 source, bool avecDirection)
    {
        debut = Time.unscaledTime;
        aUneDirection = avecDirection && afficherDirection;

        Camera cam = Camera.main;
        if (aUneDirection && cam)
        {
            // Angle entre le regard (a plat) et la direction de la source.
            Vector3 regard = cam.transform.forward; regard.y = 0f;
            Vector3 vers = source - cam.transform.position; vers.y = 0f;
            aUneDirection = regard.sqrMagnitude > 0.0001f && vers.sqrMagnitude > 0.0001f;
            if (aUneDirection) angleSource = Vector3.SignedAngle(regard, vers, Vector3.up);
        }
        else aUneDirection = false;

        // La fleche garde la direction du moment du coup
        // (utile car le joueur est renvoye au point de depart juste apres).
        pivotFleche.localEulerAngles = new Vector3(0f, 0f, -angleSource);
    }

    private void Update()
    {
        float t = Time.unscaledTime - debut;
        float alpha = t <= tenue ? 1f : 1f - Mathf.Clamp01((t - tenue) / Mathf.Max(0.01f, fondu));
        Appliquer(alpha * alpha); // fondu plus doux a la fin
    }

    private void Appliquer(float alpha)
    {
        if (!vignette) return;
        // La vignette "pulse" un peu au debut du coup.
        float pulse = 1f + 0.25f * Mathf.Clamp01(1f - (Time.unscaledTime - debut) / 0.15f);
        vignette.color = new Color(couleur.r, couleur.g, couleur.b, alpha * intensiteVignette);
        vignette.rectTransform.localScale = Vector3.one * (1f + 0.06f * (pulse - 1f) * 4f);
        fleche.color = new Color(couleur.r, couleur.g, couleur.b, aUneDirection ? alpha : 0f);
        message.color = new Color(1f, 0.92f, 0.92f, afficherTexte ? alpha : 0f);
        message.text = texte;

        bool visible = alpha > 0.001f;
        if (vignette.enabled != visible) { vignette.enabled = visible; fleche.enabled = visible; message.enabled = visible; }

        // Garder la fleche a la bonne distance si la fenetre change de taille.
        float h = ((RectTransform)transform.GetChild(0)).rect.height;
        fleche.rectTransform.anchoredPosition = new Vector2(0f, h * distanceFleche);
    }

    // ------------------------------------------------------------------
    // Creation de l'interface (Canvas + images generees, aucun asset requis).

    private void ConstruireInterface()
    {
        var canvasGo = new GameObject("Canvas_Degats", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // au-dessus du reste de l'interface
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Vignette plein ecran.
        vignette = NouvelleImage("Vignette", canvasGo.transform, CreerSpriteVignette());
        var rv = vignette.rectTransform;
        rv.anchorMin = Vector2.zero; rv.anchorMax = Vector2.one;
        rv.offsetMin = rv.offsetMax = Vector2.zero;

        // Pivot au centre de l'ecran, qui tourne vers la source du coup.
        var pivotGo = new GameObject("Pivot_Direction", typeof(RectTransform));
        pivotFleche = (RectTransform)pivotGo.transform;
        pivotFleche.SetParent(canvasGo.transform, false);
        pivotFleche.anchorMin = pivotFleche.anchorMax = new Vector2(0.5f, 0.5f);
        pivotFleche.sizeDelta = Vector2.zero;

        fleche = NouvelleImage("Fleche", pivotFleche, CreerSpriteFleche());
        fleche.rectTransform.sizeDelta = new Vector2(360f, 110f);

        // Texte.
        var texteGo = new GameObject("Texte", typeof(RectTransform), typeof(Text), typeof(Outline));
        texteGo.transform.SetParent(canvasGo.transform, false);
        message = texteGo.GetComponent<Text>();
        message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        message.fontSize = 64;
        message.fontStyle = FontStyle.Bold;
        message.alignment = TextAnchor.MiddleCenter;
        message.raycastTarget = false;
        message.horizontalOverflow = HorizontalWrapMode.Overflow;
        var ombre = texteGo.GetComponent<Outline>();
        ombre.effectColor = new Color(0.4f, 0f, 0f, 0.9f);
        ombre.effectDistance = new Vector2(3f, -3f);
        var rt = message.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -140f);
        rt.sizeDelta = new Vector2(800f, 100f);
    }

    private static Image NouvelleImage(string nom, Transform parent, Sprite sprite)
    {
        var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // Degrade : transparent au centre, opaque sur les bords.
    private static Sprite CreerSpriteVignette()
    {
        const int taille = 256;
        var tex = new Texture2D(taille, taille, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Vignette_Degats" };
        var pixels = new Color32[taille * taille];
        for (int y = 0; y < taille; y++)
            for (int x = 0; x < taille; x++)
            {
                float dx = (x + 0.5f) / taille * 2f - 1f, dy = (y + 0.5f) / taille * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41421356f; // 0 au centre, 1 dans les coins
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.95f, d));
                pixels[y * taille + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, taille, taille), new Vector2(0.5f, 0.5f));
    }

    // Arc de cercle epais avec une pointe au milieu (pointe vers le haut = vers la source).
    private static Sprite CreerSpriteFleche()
    {
        const int l = 360, h = 110;
        var tex = new Texture2D(l, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Fleche_Degats" };
        var pixels = new Color32[l * h];
        // Cercle dont le centre est loin sous l'image : on n'en voit qu'un arc.
        float cx = l * 0.5f, cy = -320f, rayon = 390f, epaisseur = 22f, demiAngle = 26f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < l; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Abs(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg);
                // Epaisseur plus grande au centre (effet de pointe).
                float e = epaisseur * Mathf.Lerp(1.9f, 0.6f, angle / demiAngle);
                float bord = Mathf.Clamp01(e * 0.5f - Mathf.Abs(r - rayon - e * 0.15f));
                float fin = Mathf.Clamp01((demiAngle - angle) / 4f);
                float a = Mathf.Clamp01(bord) * fin;
                pixels[y * l + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, l, h), new Vector2(0.5f, 0.5f));
    }
}
