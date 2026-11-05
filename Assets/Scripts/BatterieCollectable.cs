
using UnityEngine;

// Ce script permet au joueur de ramasser une batterie
// lorsqu'il entre en contact avec elle.
//
// Fonctionnement :
// 1. Détecter l'entrée du joueur dans la zone de la batterie.
// 2. Vérifier que la batterie n'a pas déjà été collectée.
// 3. Informer le gestionnaire du jeu.
// 4. Faire disparaître la batterie de la scène.

public class BatterieCollectable : MonoBehaviour
{
    // Référence au gestionnaire principal du jeu.
    // Ce gestionnaire contient la méthode AjouterBatterie()
    // qui permet de comptabiliser les batteries ramassées.
    private GestionnaireJeu3D gestionnaire;

    // Variable booléenne indiquant si la batterie
    // a déjà été collectée.
    // Par défaut, sa valeur est false.
    private bool collectee;

    // Awake() est appelée lors de l'initialisation
    // de l'objet, avant Start().
    private void Awake()
    {
        // Rechercher un objet contenant le composant
        // GestionnaireJeu3D dans la scène.
        // Conserver sa référence dans la variable gestionnaire.
        gestionnaire =
            FindFirstObjectByType<GestionnaireJeu3D>();
    }

    // OnTriggerEnter() est appelée par Unity lorsqu'un
    // autre Collider entre dans la zone Trigger de la batterie.
    //
    // Le paramètre "autre" représente le Collider
    // de l'objet qui vient d'entrer dans cette zone.
    private void OnTriggerEnter(Collider autre)
    {
        // Vérifier trois conditions :
        //
        // 1. collectee :
        //    La batterie a déjà été ramassée.
        //
        // 2. !autre.CompareTag("Player") :
        //    L'objet détecté n'a pas le tag Player.
        //
        // 3. gestionnaire == null :
        //    Aucun gestionnaire de jeu n'a été trouvé.
        //
        // Si au moins une condition est vraie,
        // on quitte immédiatement la méthode.
        if (collectee ||
            !autre.CompareTag("Player") ||
            gestionnaire == null)
        {
            return;
        }

        // Marquer la batterie comme collectée.
        // Cela empêche de la comptabiliser plusieurs fois.
        collectee = true;

        // Appeler la méthode AjouterBatterie()
        // du gestionnaire principal du jeu.
        // Cette méthode peut mettre à jour le compteur
        // et l'interface utilisateur.
        gestionnaire.AjouterBatterie();

        // Détruire l'objet batterie dans la scène.
        // La batterie disparaît après sa collecte.
        Destroy(gameObject);
    }
}
