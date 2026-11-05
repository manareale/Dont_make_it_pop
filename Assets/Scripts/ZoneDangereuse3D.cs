
using UnityEngine;

// Ce script permet de créer une zone dangereuse en 3D.
//
// Fonctionnement :
// 1. Détecter si le joueur entre dans la zone dangereuse.
// 2. Vérifier que le joueur possède un Rigidbody.
// 3. Téléporter le joueur à un point de réapparition.
// 4. Annuler ses vitesses linéaire et angulaire
//    pour éviter qu'il continue à se déplacer.

public class ZoneDangereuse3D : MonoBehaviour
{
    // Référence au point où le joueur doit réapparaître.
    //
    // Transform permet d'accéder à la position
    // du point de réapparition dans la scène.
    //
    // SerializeField permet de définir ce point
    // directement dans l'Inspector de Unity.
    [SerializeField] private Transform pointReapparition;

    // Méthode publique permettant de définir
    // le point de réapparition par programmation.
    //
    // Paramètre :
    // point : Transform représentant le point
    //         où le joueur doit être téléporté.
    public void Configurer(Transform point)
    {
        // Enregistrer le nouveau point de réapparition.
        pointReapparition = point;
    }

    // OnTriggerEnter() est appelée lorsqu'un Collider
    // entre dans la zone Trigger de cet objet.
    //
    // Le paramètre "autre" représente le Collider
    // de l'objet qui entre dans la zone dangereuse.
    private void OnTriggerEnter(Collider autre)
    {
        // Vérifier deux conditions :
        //
        // 1. !autre.CompareTag("Player") :
        //    L'objet détecté n'est pas le joueur.
        //
        // 2. pointReapparition == null :
        //    Aucun point de réapparition n'a été défini.
        //
        // L'opérateur || signifie OU logique.
        //
        // Si au moins une condition est vraie,
        // on quitte immédiatement la méthode.
        if (!autre.CompareTag("Player") ||
            pointReapparition == null)
        {
            return;
        }

        // Récupérer le Rigidbody associé
        // au Collider du joueur.
        //
        // attachedRigidbody permet de retrouver
        // le Rigidbody lié au Collider,
        // même si celui-ci se trouve sur un objet enfant.
        Rigidbody rb = autre.attachedRigidbody;

        // Vérifier que le Rigidbody existe.
        //
        // Si aucun Rigidbody n'est trouvé,
        // la téléportation est annulée.
        if (rb == null)
        {
            return;
        }

        // Téléporter le joueur à la position
        // du point de réapparition.
        //
        // rb.position représente la position
        // du Rigidbody dans le monde 3D.
        //
        // On remplace sa position actuelle
        // par celle du point de réapparition.
        rb.position = pointReapparition.position;

        // Annuler la vitesse linéaire du joueur.
        //
        // linearVelocity représente la vitesse
        // de déplacement sur les axes X, Y et Z.
        //
        // Vector3.zero correspond à (0, 0, 0).
        //
        // Cela empêche le joueur de conserver
        // son mouvement après la téléportation.
        rb.linearVelocity = Vector3.zero;

        // Annuler la vitesse angulaire du joueur.
        //
        // angularVelocity représente la vitesse
        // de rotation du Rigidbody.
        //
        // Vector3.zero arrête toute rotation
        // causée par le moteur physique.
        rb.angularVelocity = Vector3.zero;
    }
}
