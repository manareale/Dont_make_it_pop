// using UnityEngine;

// [RequireComponent(typeof(Animator))]
// public class AnimationRobot3D_02 : MonoBehaviour
// {
//     private Animator animateur;

//     private static readonly int ParametreVitesse =
//         Animator.StringToHash("Vitesse");

//     private void Awake()
//     {
//         animateur = GetComponent<Animator>();
//     }

//     private void Update()
//     {
//         float horizontal = Input.GetAxisRaw("Horizontal");
//         float vertical = Input.GetAxisRaw("Vertical");

//         Vector3 mouvement = new Vector3(
//             horizontal,
//             0f,
//             vertical
//         );

//         float vitesseAnimation = 0f;

//         if (mouvement.sqrMagnitude > 0.01f)
//         {
//             vitesseAnimation = Input.GetKey(KeyCode.LeftShift)
//                 ? 1f
//                 : 0.5f;
//         }

//         animateur.SetFloat(
//             ParametreVitesse,
//             vitesseAnimation,
//             0.15f,
//             Time.deltaTime
//         );
//     }
// }

using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class AnimationRobot3D_02 : MonoBehaviour
{
    [Header("Déplacement")]
    [SerializeField] private float vitesseMarche = 2.5f;
    [SerializeField] private float vitesseCourse = 5f;
    [SerializeField] private float vitesseRotation = 10f;

    private Animator animateur;
    private Rigidbody corps;

    private Vector3 direction;
    private float vitesseAnimation;
    private float vitesseDeplacement;

    private static readonly int ParametreVitesse =
        Animator.StringToHash("Vitesse");

    private void Awake()
    {
        animateur = GetComponent<Animator>();
        corps = GetComponent<Rigidbody>();

        // Empêche le personnage de tomber sur le côté.
        corps.freezeRotation = true;

        // Rend le déplacement plus fluide.
        corps.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        direction = new Vector3(
            horizontal,
            0f,
            vertical
        ).normalized;

        bool seDeplace = direction.sqrMagnitude > 0.01f;
        bool court = seDeplace &&
                     Input.GetKey(KeyCode.LeftShift);

        if (!seDeplace)
        {
            vitesseAnimation = 0f;
            vitesseDeplacement = 0f;
        }
        else if (court)
        {
            vitesseAnimation = 1f;
            vitesseDeplacement = vitesseCourse;
        }
        else
        {
            vitesseAnimation = 0.5f;
            vitesseDeplacement = vitesseMarche;
        }

        animateur.SetFloat(
            ParametreVitesse,
            vitesseAnimation,
            0.15f,
            Time.deltaTime
        );
    }

    private void FixedUpdate()
    {
        if (direction.sqrMagnitude <= 0.01f)
            return;

        // Calcule la prochaine position.
        Vector3 nouvellePosition =
            corps.position
            + direction
            * vitesseDeplacement
            * Time.fixedDeltaTime;

        corps.MovePosition(nouvellePosition);

        // Oriente le personnage vers son déplacement.
        Quaternion rotationCible =
            Quaternion.LookRotation(direction, Vector3.up);

        Quaternion rotationFluide =
            Quaternion.Slerp(
                corps.rotation,
                rotationCible,
                vitesseRotation * Time.fixedDeltaTime
            );

        corps.MoveRotation(rotationFluide);
    }
}