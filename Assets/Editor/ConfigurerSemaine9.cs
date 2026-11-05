using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurerSemaine9
{
    [MenuItem("Outils/Semaine 9 - 3D/Construire le prototype jouable")]
    public static void ConstruirePrototypeJouable()
    {
        ConstruireSemaine8.Construire();
        AjouterTag("Player");
        GameObject robot = GameObject.Find("Robot_Explorateur");
        robot.tag = "Player";
        Rigidbody rb = robot.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        robot.AddComponent<ControleurRobot3D>();

        CameraSuiveuse3D suivi = Camera.main.gameObject.AddComponent<CameraSuiveuse3D>();
        suivi.DefinirCible(robot.transform);

        GameObject porteGo = GameObject.Find("Porte_Sortie");
        PorteSortie3D porte = porteGo.AddComponent<PorteSortie3D>();
        porte.Configurer(porteGo.transform.Find("Battant"));
        GameObject gestionnaireGo = new GameObject("GESTIONNAIRE_JEU");
        GestionnaireJeu3D gestionnaire = gestionnaireGo.AddComponent<GestionnaireJeu3D>();
        gestionnaire.Configurer(porte, 3);

        foreach (AnimationObjetPedagogique animation in Object.FindObjectsByType<AnimationObjetPedagogique>(FindObjectsSortMode.None))
            animation.gameObject.AddComponent<BatterieCollectable>();

        GameObject point = new GameObject("Point_Reapparition"); point.transform.position = new Vector3(0f, 1f, -7f);
        GameObject danger = GameObject.CreatePrimitive(PrimitiveType.Cube); danger.name="Zone_Dangereuse"; danger.transform.position=new Vector3(-5f,.1f,3f); danger.transform.localScale=new Vector3(3f,.2f,3f); danger.GetComponent<Collider>().isTrigger=true; danger.AddComponent<ZoneDangereuse3D>().Configurer(point.transform);
        GameObject interrupteur = GameObject.CreatePrimitive(PrimitiveType.Cylinder); interrupteur.name="Interrupteur_E"; interrupteur.transform.position=new Vector3(6f,.4f,6f); interrupteur.transform.localScale=new Vector3(.6f,.4f,.6f); interrupteur.GetComponent<Collider>().isTrigger=true; interrupteur.AddComponent<Interrupteur3D>().Configurer(porte);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Semaine8_Generee/Scenes/PrototypeJouable_S09.unity");
        EditorUtility.DisplayDialog("Semaine 9", "Le prototype jouable est pret.", "OK");
    }

    private static void AjouterTag(string tag)
    {
        SerializedObject manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = manager.FindProperty("tags");
        for (int i=0;i<tags.arraySize;i++) if(tags.GetArrayElementAtIndex(i).stringValue==tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize); tags.GetArrayElementAtIndex(tags.arraySize-1).stringValue=tag; manager.ApplyModifiedProperties();
    }
}
