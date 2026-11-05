
using UnityEngine;

// Oblige l'objet à posséder un composant Rigidbody.
// Si le composant est absent, Unity l'ajoute
// automatiquement lorsque ce script est attaché.
[RequireComponent(typeof(Rigidbody))]

// Ce script permet de contrôler un robot en 3D.
//
// Fonctionnement :
// 1. Lire les touches de déplacement du clavier.
// 2. Calculer la direction du mouvement.
// 3. Déplacer le robot avec son Rigidbody.
// 4. Orienter progressivement le robot
//    dans la direction de son déplacement.
public class ControleurRobot3D : MonoBehaviour
{
    // Vitesse de déplacement du robot en unités/seconde.
    // Modifiable dans l'Inspector de Unity.
    [SerializeField] private float vitesse = 5f;

    // Contrôle la rapidité avec laquelle le robot
    // s'oriente vers la direction de déplacement.
    [SerializeField] private float vitesseRotation = 10f;

    // Référence au composant Rigidbody du robot.
    // Ce composant permet d'interagir avec
    // le système physique 3D de Unity.
    private Rigidbody corps;

    // Vecteur qui représente la direction
    // dans laquelle le robot doit se déplacer.
    private Vector3 direction;

    // Awake() est appelée lors de l'initialisation.
    private void Awake()
    {
        // Récupérer le Rigidbody attaché
        // au même GameObject que ce script.
        corps = GetComponent<Rigidbody>();
    }

    // Update() est appelée à chaque image.
    // Elle sert ici à lire les entrées du joueur.
    private void Update()
    {
        // Lire les touches de déplacement horizontal.
        //
        // A ou flèche gauche  : -1
        // D ou flèche droite  :  1
        // Aucune touche       :  0
        float horizontal = Input.GetAxisRaw("Horizontal");

        // Lire les touches de déplacement vertical.
        //
        // S ou flèche bas     : -1
        // W ou flèche haut    :  1
        // Aucune touche       :  0
        //
        // Dans ce jeu 3D, cet axe correspond
        // au déplacement sur l'axe Z du monde.
        float vertical = Input.GetAxisRaw("Vertical");

        // Construire le vecteur de déplacement.
        //
        // X = déplacement horizontal.
        // Y = 0 : aucun déplacement vertical.
        // Z = déplacement avant/arrière.
        //
        // ClampMagnitude limite la longueur
        // du vecteur à 1.
        //
        // Cela évite que le robot se déplace
        // plus rapidement en diagonale.
        direction = Vector3.ClampMagnitude(
            new Vector3(horizontal, 0f, vertical),
            1f
        );
    }

    // FixedUpdate() est appelée à intervalles
    // de temps fixes par le moteur physique.
    //
    // Elle est adaptée aux déplacements
    // effectués avec un Rigidbody.
    private void FixedUpdate()
    {
        // Calculer et appliquer la nouvelle position.
        //
        // corps.position : position actuelle du robot.
        // direction : direction du déplacement.
        // vitesse : vitesse en unités/seconde.
        // Time.fixedDeltaTime : durée du pas physique.
        //
        // MovePosition déplace le Rigidbody
        // vers la position calculée.
        corps.MovePosition(
            corps.position
            + direction * vitesse * Time.fixedDeltaTime
        );

        // Vérifier si le robot est presque immobile.
        //
        // sqrMagnitude représente la longueur
        // du vecteur au carré.
        //
        // Si la direction est presque nulle,
        // on arrête la méthode pour éviter
        // de calculer une rotation inutile.
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        // Calculer la rotation souhaitée.
        //
        // Quaternion.LookRotation crée une orientation
        // qui dirige l'avant du robot (axe Z local)
        // vers la direction de déplacement.
        //
        // Vector3.up indique que l'axe Y
        // doit rester orienté vers le haut.
        Quaternion cible = Quaternion.LookRotation(
            direction,
            Vector3.up
        );

        // Faire tourner progressivement le robot
        // vers la rotation souhaitée.
        //
        // Quaternion.Slerp réalise une interpolation
        // sphérique entre deux orientations.
        //
        // corps.rotation : rotation actuelle.
        // cible : rotation souhaitée.
        // vitesseRotation * Time.fixedDeltaTime :
        // facteur d'interpolation.
        //
        // MoveRotation applique la nouvelle rotation
        // au Rigidbody.
        corps.MoveRotation(
            Quaternion.Slerp(
                corps.rotation,
                cible,
                vitesseRotation * Time.fixedDeltaTime
            )
        );
    }
}
