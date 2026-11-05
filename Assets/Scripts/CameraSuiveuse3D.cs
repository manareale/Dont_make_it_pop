
using UnityEngine;

// Ce script permet à la caméra de suivre un objet 3D
// (généralement le joueur) avec un déplacement fluide.
//
// Fonctionnement :
// 1. Identifier l'objet à suivre.
// 2. Calculer la position souhaitée de la caméra.
// 3. Déplacer progressivement la caméra vers cette position.
// 4. Orienter la caméra vers le joueur.

public class CameraSuiveuse3D : MonoBehaviour
{
    // Référence à l'objet que la caméra doit suivre.
    // Transform permet d'accéder à sa position,
    // sa rotation et son échelle.
    //
    // SerializeField permet de sélectionner la cible
    // directement dans l'Inspector de Unity.
    [SerializeField] private Transform cible;

    // Décalage entre la position du joueur et la caméra.
    //
    // X = 0  : caméra centrée horizontalement.
    // Y = 8  : caméra située au-dessus du joueur.
    // Z = -10 : caméra située derrière le joueur
    //           selon l'axe Z du monde.
    [SerializeField]
    private Vector3 decalage = new Vector3(0f, 8f, -10f);

    // Contrôle la rapidité avec laquelle la caméra
    // rejoint sa position souhaitée.
    //
    // Plus cette valeur est élevée,
    // plus la caméra réagit rapidement.
    [SerializeField] private float fluidite = 6f;

    // Méthode publique permettant de modifier
    // l'objet suivi par la caméra pendant le jeu.
    //
    // nouvelleCible : Transform du nouvel objet à suivre.
    public void DefinirCible(Transform nouvelleCible)
    {
        // Enregistrer la nouvelle cible.
        cible = nouvelleCible;
    }

    // LateUpdate() est appelée après Update().
    //
    // Cette méthode est particulièrement adaptée
    // aux caméras suiveuses, car elle permet
    // d'ajuster la caméra après le déplacement du joueur.
    private void LateUpdate()
    {
        // Vérifier qu'une cible a été définie.
        // Sinon, arrêter l'exécution de cette méthode
        // pour éviter une erreur de référence nulle.
        if (cible == null)
        {
            return;
        }

        // Calculer la position que la caméra doit atteindre.
        //
        // Position souhaitée = position du joueur + décalage.
        //
        // Exemple :
        // Joueur : (5, 0, 3)
        // Décalage : (0, 8, -10)
        // Caméra souhaitée : (5, 8, -7)
        Vector3 positionVoulue = cible.position + decalage;

        // Déplacer progressivement la caméra
        // de sa position actuelle vers la position souhaitée.
        //
        // Vector3.Lerp(a, b, t) effectue une interpolation
        // entre deux positions :
        //
        // a : position actuelle de la caméra.
        // b : position souhaitée.
        // t : facteur d'interpolation entre 0 et 1.
        //
        // Time.deltaTime tient compte du temps écoulé
        // depuis l'image précédente.
        transform.position = Vector3.Lerp(
            transform.position,
            positionVoulue,
            fluidite * Time.deltaTime
        );

        // Orienter la caméra vers le joueur.
        //
        // cible.position représente la position du joueur.
        // Vector3.up ajoute une unité sur l'axe Y.
        //
        // La caméra regarde donc un point légèrement
        // au-dessus de l'origine du joueur,
        // plutôt que directement vers ses pieds.
        transform.LookAt(cible.position + Vector3.up);
    }
}
