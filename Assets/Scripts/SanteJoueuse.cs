using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Systeme de VIES de la joueuse (la girl scout).
//
// - 3 vies par defaut, affichees en coeurs en bas a gauche (crees automatiquement).
// - Chaque coup retire 1 vie. Apres un coup : courte invincibilite.
// - A 0 vie : ecran noir "VOUS ETES MORTE", retour au point de reapparition, vies au maximum.
//
// Depuis un autre script :
//   SanteJoueuse sante = joueur.GetComponent<SanteJoueuse>();
//   sante.RecevoirDegats(1, positionDeLaSource);   // retire 1 vie
//   sante.Soigner(1);                              // rend 1 vie
[DisallowMultipleComponent]
public class SanteJoueuse : MonoBehaviour
{
    [Header("Vies")]
    [SerializeField, Range(1, 10)] private int viesMax = 3;
    [Tooltip("Temps d'invincibilite apres un coup (secondes).")]
    [SerializeField, Min(0f)] private float invincibiliteApresCoup = 1.5f;

    [Header("Mort")]
    [Tooltip("Ou reapparaitre. Vide = la position de depart de la joueuse.")]
    [SerializeField] private Transform pointReapparition;
    [SerializeField] private string messageMort = "VOUS ÊTES MORT :(";
    [SerializeField, Min(0f)] private float dureeEcranMort = 2f;

    [Header("Interface")]
    [SerializeField] private bool afficherCoeurs = true;
    [SerializeField] private bool afficherIndicateurDegats = true;
    [SerializeField] private Color couleurCoeur = new Color(0.85f, 0.08f, 0.12f);

    [Header("Evenements")]
    public UnityEvent<int> quandDegats;   // vies restantes
    public UnityEvent quandMort;
    public UnityEvent quandReapparition;

    public int Vies { get; private set; }
    public int ViesMax => viesMax;
    public bool EstMorte { get; private set; }
    public bool EstInvincible => EstMorte || Time.time < finInvincibilite;

    private float finInvincibilite;
    private Vector3 positionDepart;
    private Quaternion rotationDepart;
    private Rigidbody corps;
    private InterfaceCoeurs ui;

    private void Awake()
    {
        Vies = viesMax;
        corps = GetComponent<Rigidbody>();
        positionDepart = transform.position;
        rotationDepart = transform.rotation;
        if (afficherCoeurs) ui = new InterfaceCoeurs(viesMax, couleurCoeur);
    }

    private void OnDestroy()
    {
        ui?.Detruire();
    }

    private void Update()
    {
        ui?.MettreAJour(Vies, EstInvincible && !EstMorte);
    }

    // ------------------------------------------------------------------
    // API publique

    // Retire des vies. Renvoie true si le coup a compte (false si invincible ou deja morte).
    public bool RecevoirDegats(int vies, Vector3 source)
    {
        return AppliquerDegats(vies, source, true);
    }

    public bool RecevoirDegats(int vies = 1)
    {
        return AppliquerDegats(vies, Vector3.zero, false);
    }

    public void Soigner(int vies = 1)
    {
        if (EstMorte || vies <= 0) return;
        Vies = Mathf.Min(viesMax, Vies + vies);
    }

    // ------------------------------------------------------------------
    private bool AppliquerDegats(int vies, Vector3 source, bool avecSource)
    {
        if (vies <= 0 || EstInvincible) return false;

        Vies = Mathf.Max(0, Vies - vies);
        finInvincibilite = Time.time + invincibiliteApresCoup;

        if (afficherIndicateurDegats)
        {
            if (avecSource) IndicateurDegats.Signaler(source);
            else IndicateurDegats.Signaler();
        }
        ui?.PerdreCoeur(Vies);
        quandDegats?.Invoke(Vies);

        if (Vies <= 0) StartCoroutine(Mourir());
        return true;
    }

    private IEnumerator Mourir()
    {
        EstMorte = true;
        quandMort?.Invoke();

        // Bloquer les controles pendant l'ecran de mort.
        var controleur = GetComponent<ControleurJoueuse>();
        bool controleurActif = controleur && controleur.enabled;
        if (controleur) controleur.enabled = false;
        Immobiliser();

        if (ui != null) yield return ui.FonduNoir(true, 0.6f, messageMort);
        yield return new WaitForSeconds(dureeEcranMort);

        // Reapparition.
        Vector3 pos = pointReapparition ? pointReapparition.position : positionDepart;
        Quaternion rot = pointReapparition ? pointReapparition.rotation : rotationDepart;
        if (corps) { corps.position = pos; corps.rotation = rot; }
        transform.SetPositionAndRotation(pos, rot);
        Physics.SyncTransforms();
        Immobiliser();

        Vies = viesMax;
        EstMorte = false;
        finInvincibilite = Time.time + 2f; // le temps de reprendre ses esprits
        if (controleur && controleurActif) controleur.enabled = true;
        quandReapparition?.Invoke();

        if (ui != null) yield return ui.FonduNoir(false, 0.8f, messageMort);
    }

    private void Immobiliser()
    {
        if (corps && !corps.isKinematic)
        {
            corps.linearVelocity = Vector3.zero;
            corps.angularVelocity = Vector3.zero;
        }
    }

    // ==================================================================
    // Coeurs + ecran de mort (crees par code, aucun asset requis).
    private class InterfaceCoeurs
    {
        private const float Taille = 64f, Espace = 12f;
        private readonly GameObject racine;
        private readonly Image[] pleins;       // coeur rouge
        private readonly Image[] vides;        // contour sombre derriere
        private readonly float[] animation;    // 1 = vient d'etre perdu
        private readonly Image noir;
        private readonly Text texteMort;
        private readonly Color couleur;

        public InterfaceCoeurs(int nombre, Color couleur)
        {
            this.couleur = couleur;
            racine = new GameObject("INTERFACE_VIES", typeof(Canvas), typeof(CanvasScaler));
            var canvas = racine.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = racine.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Sprite plein = CreerSpriteCoeur(false), contour = CreerSpriteCoeur(true);
            pleins = new Image[nombre]; vides = new Image[nombre]; animation = new float[nombre];
            for (int i = 0; i < nombre; i++)
            {
                var pos = new Vector2(40f + i * (Taille + Espace) + Taille * 0.5f, 40f + Taille * 0.5f);
                vides[i] = Icone("Coeur_Vide_" + (i + 1), contour, pos, new Color(0.3f, 0.3f, 0.3f, 0.75f));
                pleins[i] = Icone("Coeur_" + (i + 1), plein, pos, couleur);
            }

            // Ecran de mort.
            noir = Icone("Ecran_Mort", null, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var rn = noir.rectTransform;
            rn.anchorMin = Vector2.zero; rn.anchorMax = Vector2.one; rn.offsetMin = rn.offsetMax = Vector2.zero;

            var go = new GameObject("Texte_Mort", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(rn, false);
            texteMort = go.GetComponent<Text>();
            texteMort.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texteMort.fontSize = 72;
            texteMort.fontStyle = FontStyle.Bold;
            texteMort.alignment = TextAnchor.MiddleCenter;
            texteMort.horizontalOverflow = HorizontalWrapMode.Overflow;
            texteMort.raycastTarget = false;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var rt = texteMort.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            noir.enabled = texteMort.enabled = false;
        }

        public void Detruire()
        {
            if (racine) Object.Destroy(racine);
        }

        public void PerdreCoeur(int viesRestantes)
        {
            if (viesRestantes >= 0 && viesRestantes < animation.Length) animation[viesRestantes] = 1f;
        }

        public void MettreAJour(int vies, bool invincible)
        {
            if (!racine) return;
            for (int i = 0; i < pleins.Length; i++)
            {
                bool present = i < vies;
                animation[i] = Mathf.MoveTowards(animation[i], 0f, Time.deltaTime * 2.5f);
                float a = animation[i];

                // Coeur perdu : il grossit et disparait.
                float echelle = present ? 1f : 1f + 0.6f * (1f - a);
                float alpha = present ? 1f : a;
                // Derniere vie : le coeur bat.
                if (present && vies == 1) echelle = 1f + 0.12f * Mathf.Max(0f, Mathf.Sin(Time.time * 7f));
                // Clignote pendant l'invincibilite.
                if (present && invincible && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f) alpha = 0.45f;

                pleins[i].enabled = alpha > 0.01f;
                pleins[i].color = new Color(couleur.r, couleur.g, couleur.b, alpha);
                pleins[i].rectTransform.localScale = Vector3.one * echelle;
            }
        }

        public IEnumerator FonduNoir(bool versNoir, float duree, string message)
        {
            texteMort.text = message;
            noir.enabled = texteMort.enabled = true;
            float t = 0f;
            while (t < duree)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duree);
                if (!versNoir) a = 1f - a;
                noir.color = new Color(0f, 0f, 0f, a * 0.92f);
                texteMort.color = new Color(0.75f, 0.05f, 0.05f, a);
                yield return null;
            }
            if (!versNoir) noir.enabled = texteMort.enabled = false;
        }

        private Image Icone(string nom, Sprite sprite, Vector2 position, Color c)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(racine.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = c;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.anchoredPosition = position;
            r.sizeDelta = new Vector2(Taille, Taille);
            return img;
        }

        // Coeur dessine avec l'equation (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0.
        // contour = true : coeur un peu plus grand, pour faire le fond sombre.
        private static Sprite CreerSpriteCoeur(bool contour)
        {
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = contour ? "Coeur_Contour" : "Coeur" };
            var px = new Color32[n * n];
            float zoom = contour ? 1.32f : 1.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // 4 echantillons par pixel pour des bords lisses.
                    int dedans = 0;
                    for (int s = 0; s < 4; s++)
                    {
                        float fx = ((x + 0.25f + 0.5f * (s & 1)) / n * 2f - 1f) * zoom;
                        float fy = ((y + 0.25f + 0.5f * (s >> 1)) / n * 2f - 1f) * zoom + 0.12f;
                        float k = fx * fx + fy * fy - 1f;
                        if (k * k * k - fx * fx * fy * fy * fy <= 0f) dedans++;
                    }
                    // Le coeur est teinte par Image.color : ici on donne juste un leger relief
                    // (plus sombre en bas). Le contour reste uni.
                    float a = dedans / 4f;
                    byte lum = contour ? (byte)255 : (byte)(Mathf.Lerp(0.7f, 1f, (float)y / n) * 255f);
                    px[y * n + x] = new Color32(lum, lum, lum, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
