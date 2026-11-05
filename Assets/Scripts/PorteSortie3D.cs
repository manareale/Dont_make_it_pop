
using UnityEngine;

// Ce script permet de contrôler l'ouverture
// d'une porte dans un environnement 3D.
//
// Fonctionnement :
// 1. Identifier la partie mobile de la porte.
// 2. Mémoriser sa position initiale (fermée).
// 3. Calculer sa position lorsqu'elle est ouverte.
// 4. Déplacer progressivement la porte vers
//    la position souhaitée.
// 5. Déclencher l'ouverture avec la méthode Ouvrir().

public class PorteSortie3D : MonoBehaviour
{
    // Référence à la partie mobile de la porte.
    //
    // Transform permet de modifier sa position.
    // SerializeField permet d'associer le battant
    // directement dans l'Inspector de Unity.
    [SerializeField] private Transform battant;

    // Déplacement à appliquer pour ouvrir la porte.
    //
    // X = 0 : aucun déplacement horizontal.
    // Y = 3 : la porte monte de 3 unités.
    // Z = 0 : aucun déplacement en profondeur.
    //
    // La porte s'ouvre donc en glissant vers le haut.
    [SerializeField]
    private Vector3 decalageOuverture = new Vector3(0f, 3f, 0f);

    // Vitesse de déplacement de la porte
    // en unités par seconde.
    //
    // Plus cette valeur est élevée,
    // plus la porte s'ouvre rapidement.
    [SerializeField] private float vitesse = 2f;

    // Position initiale du battant lorsque
    // la porte est complètement fermée.
    private Vector3 positionFermee;

    // Position que le battant doit atteindre
    // lorsque la porte est complètement ouverte.
    private Vector3 positionOuverte;

    // Variable indiquant si l'ouverture
    // de la porte a été demandée.
    //
    // Par défaut, un booléen vaut false.
    private bool ouverte;

    // Méthode publique permettant d'associer
    // le battant de la porte par programmation.
    //
    // nouveauBattant : Transform de la partie mobile.
    public void Configurer(Transform nouveauBattant)
    {
        // Enregistrer la référence au battant.
        battant = nouveauBattant;
    }

    // Start() est appelée une seule fois
    // au démarrage du script.
    private void Start()
    {
        // Vérifier qu'un battant a été associé.
        //
        // Si aucun battant n'existe,
        // arrêter l'exécution de Start().
        if (battant == null)
        {
            return;
        }

        // Mémoriser la position locale initiale
        // du battant.
        //
        // localPosition représente la position
        // par rapport à son objet parent.
        positionFermee = battant.localPosition;

        // Calculer la position d'ouverture.
        //
        // Position ouverte =
        // position fermée + décalage d'ouverture.
        //
        // Exemple :
        // Position fermée : (0, 0, 0)
        // Décalage : (0, 3, 0)
        // Position ouverte : (0, 3, 0)
        positionOuverte =
            positionFermee + decalageOuverture;
    }

    // Update() est appelée à chaque image.
    //
    // Elle permet d'animer progressivement
    // le déplacement de la porte.
    private void Update()
    {
        // Vérifier qu'un battant existe.
        // Sinon, aucune animation n'est possible.
        if (battant == null)
        {
            return;
        }

        // Déterminer la position à atteindre.
        //
        // L'opérateur ? : est un opérateur ternaire.
        //
        // Si ouverte == true :
        //     cible = positionOuverte
        //
        // Sinon :
        //     cible = positionFermee
        Vector3 cible = ouverte
            ? positionOuverte
            : positionFermee;

        // Déplacer progressivement le battant
        // vers la position souhaitée.
        //
        // Vector3.MoveTowards(a, b, distanceMax)
        //
        // a : position actuelle du battant.
        // b : position cible.
        // distanceMax : déplacement maximal
        //               autorisé pendant cette image.
        //
        // vitesse * Time.deltaTime permet
        // un déplacement exprimé en unités/seconde.
        //
        // MoveTowards s'arrête automatiquement
        // lorsque la position cible est atteinte.
        battant.localPosition = Vector3.MoveTowards(
            battant.localPosition,
            cible,
            vitesse * Time.deltaTime
        );
    }

    // Méthode publique permettant
    // de demander l'ouverture de la porte.
    //
    // Elle peut être appelée par :
    // - GestionnaireJeu3D
    // - Interrupteur3D
    public void Ouvrir()
    {
        // Indiquer que la porte doit s'ouvrir.
        //
        // L'animation sera exécutée progressivement
        // dans Update().
        ouverte = true;
    }
}
