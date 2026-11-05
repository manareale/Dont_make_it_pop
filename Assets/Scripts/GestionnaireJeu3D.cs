
using UnityEngine;

// Ce script représente le gestionnaire principal du jeu.
//
// Il permet de :
// 1. Définir le nombre de batteries à collecter.
// 2. Compter les batteries ramassées par le joueur.
// 3. Vérifier si l'objectif est atteint.
// 4. Ouvrir la porte lorsque toutes les batteries
//    nécessaires ont été collectées.

public class GestionnaireJeu3D : MonoBehaviour
{
    // Nombre de batteries que le joueur doit collecter
    // pour pouvoir ouvrir la porte de sortie.
    //
    // La valeur par défaut est 3.
    // SerializeField permet de modifier cette valeur
    // directement dans l'Inspector de Unity.
    [SerializeField] private int objectifBatteries = 3;

    // Référence à la porte de sortie du niveau.
    //
    // Cette variable permet au gestionnaire
    // de communiquer avec le script PorteSortie3D.
    [SerializeField] private PorteSortie3D porte;

    // Propriété contenant le nombre de batteries collectées.
    //
    // get : permet aux autres scripts de lire la valeur.
    // private set : seul ce script peut modifier la valeur.
    //
    // Par défaut, un entier est initialisé à 0.
    public int BatteriesCollectees { get; private set; }

    // Méthode publique permettant de configurer
    // le gestionnaire pendant l'exécution du jeu.
    //
    // Paramètres :
    // porteAssociee : référence à la porte du niveau.
    // objectif : nombre de batteries à collecter.
    public void Configurer(PorteSortie3D porteAssociee, int objectif)
    {
        // Enregistrer la référence à la porte.
        porte = porteAssociee;

        // Définir le nombre de batteries nécessaires
        // pour atteindre l'objectif du niveau.
        objectifBatteries = objectif;
    }

    // Méthode publique appelée lorsqu'une batterie
    // est ramassée par le joueur.
    //
    // Elle est utilisée par le script BatterieCollectable.
    public void AjouterBatterie()
    {
        // Augmenter le compteur de batteries de 1.
        //
        // BatteriesCollectees++ est équivalent à :
        // BatteriesCollectees = BatteriesCollectees + 1;
        BatteriesCollectees++;

        // Afficher le nombre de batteries collectées
        // dans la fenêtre Console de Unity.
        //
        // Le symbole $ permet d'insérer directement
        // des variables dans une chaîne de caractères.
        //
        // Exemple : Batteries : 2/3
        Debug.Log(
            $"Batteries : {BatteriesCollectees}/{objectifBatteries}"
        );

        // Vérifier deux conditions :
        //
        // 1. Le nombre de batteries collectées
        //    est supérieur ou égal à l'objectif.
        //
        // 2. Une référence à la porte existe.
        //
        // L'opérateur && signifie ET logique.
        // Les deux conditions doivent être vraies.
        if (BatteriesCollectees >= objectifBatteries
            && porte != null)
        {
            // Appeler la méthode Ouvrir()
            // du script PorteSortie3D.
            //
            // Cette méthode déclenche l'ouverture
            // de la porte de sortie.
            porte.Ouvrir();
        }
    }
}
