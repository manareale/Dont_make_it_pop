
using UnityEngine;

// Ce script permet d'animer un objet 3D.
// L'objet effectue deux mouvements simultanément :
// 1. Une rotation continue autour de l'axe Y.
// 2. Un flottement vertical (montée et descente).
public class AnimationObjetPedagogique : MonoBehaviour
{
    // Vitesse de rotation de l'objet en degrés par seconde.
    // SerializeField permet de modifier cette valeur
    // directement dans l'Inspector de Unity.
    [SerializeField] private float vitesseRotation = 45f;

    // Distance maximale du flottement vertical.
    // Exemple : 0.15 signifie que l'objet peut monter
    // ou descendre de 0.15 unité par rapport à sa position initiale.
    [SerializeField] private float amplitudeFlottement = 0.15f;

    // Vitesse du mouvement de flottement.
    // Plus cette valeur est élevée, plus l'objet oscille rapidement.
    [SerializeField] private float vitesseFlottement = 2f;

    // Variable qui mémorise la position de départ de l'objet.
    private Vector3 positionInitiale;

    // Start() est appelée une seule fois au démarrage.
    private void Start()
    {
        // Enregistrer la position initiale de l'objet.
        positionInitiale = transform.position;
    }

    // Update() est appelée à chaque image (frame).
    private void Update()
    {
        // Faire tourner l'objet autour de l'axe vertical Y.
        // Vector3.up représente l'axe Y : (0, 1, 0).
        // Time.deltaTime rend la rotation indépendante
        // du nombre d'images affichées par seconde.
        // Space.World indique une rotation selon l'axe Y du monde.
        transform.Rotate(
            Vector3.up,
            vitesseRotation * Time.deltaTime,
            Space.World
        );

        // Calculer le déplacement vertical avec une fonction sinus.
        // Mathf.Sin produit une valeur entre -1 et 1.
        // Time.time représente le temps écoulé depuis le démarrage.
        // amplitudeFlottement détermine la hauteur du mouvement.
        float decalageY =
            Mathf.Sin(Time.time * vitesseFlottement)
            * amplitudeFlottement;

        // Appliquer le déplacement vertical à l'objet.
        // La position X et Z reste inchangée.
        // Seule la position Y varie autour de la position initiale.
        transform.position =
            positionInitiale + Vector3.up * decalageY;
    }
}
