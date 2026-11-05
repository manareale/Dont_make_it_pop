
using UnityEngine;

// Ce script permet au joueur d'activer un interrupteur
// pour ouvrir une porte dans un environnement 3D.
//
// Fonctionnement :
// 1. Détecter si le joueur s'approche de l'interrupteur.
// 2. Vérifier si le joueur appuie sur la touche E.
// 3. Ouvrir la porte associée à l'interrupteur.
// 4. Désactiver l'interaction lorsque le joueur s'éloigne.

public class Interrupteur3D : MonoBehaviour
{
    // Référence à la porte contrôlée par l'interrupteur.
    //
    // SerializeField permet d'associer une porte
    // directement dans l'Inspector de Unity.
    [SerializeField] private PorteSortie3D porte;

    // Variable indiquant si le joueur se trouve
    // dans la zone d'interaction de l'interrupteur.
    //
    // Par défaut, un booléen vaut false.
    private bool joueurProche;

    // Méthode publique permettant d'associer une porte
    // à cet interrupteur par programmation.
    //
    // porteAssociee représente la porte à contrôler.
    public void Configurer(PorteSortie3D porteAssociee)
    {
        // Enregistrer la référence à la porte.
        porte = porteAssociee;
    }

    // Update() est appelée une fois par image.
    //
    // Elle permet de vérifier si le joueur
    // souhaite activer l'interrupteur.
    private void Update()
    {
        // Vérifier trois conditions :
        //
        // 1. joueurProche :
        //    Le joueur est dans la zone d'interaction.
        //
        // 2. Input.GetKeyDown(KeyCode.E) :
        //    Le joueur vient d'appuyer sur la touche E.
        //
        // 3. porte != null :
        //    Une porte est associée à l'interrupteur.
        //
        // L'opérateur && signifie ET logique.
        // Les trois conditions doivent être vraies.
        if (joueurProche &&
            Input.GetKeyDown(KeyCode.E) &&
            porte != null)
        {
            // Appeler la méthode Ouvrir()
            // du script PorteSortie3D.
            porte.Ouvrir();
        }
    }

    // OnTriggerEnter() est appelée lorsqu'un Collider
    // entre dans la zone Trigger de l'interrupteur.
    //
    // Le paramètre "autre" représente le Collider
    // de l'objet qui entre dans cette zone.
    private void OnTriggerEnter(Collider autre)
    {
        // Vérifier si l'objet détecté est le joueur.
        // CompareTag("Player") compare son tag à "Player".
        if (autre.CompareTag("Player"))
        {
            // Le joueur est maintenant suffisamment proche
            // pour pouvoir utiliser l'interrupteur.
            joueurProche = true;
        }
    }

    // OnTriggerExit() est appelée lorsqu'un Collider
    // quitte la zone Trigger de l'interrupteur.
    private void OnTriggerExit(Collider autre)
    {
        // Vérifier si l'objet qui quitte la zone
        // possède le tag Player.
        if (autre.CompareTag("Player"))
        {
            // Le joueur n'est plus à proximité.
            // L'interaction avec l'interrupteur
            // n'est donc plus autorisée.
            joueurProche = false;
        }
    }
}
