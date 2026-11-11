using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Outils > Joueur > Passer en premiere personne" :
// configure la scene ouverte en un clic (annulable avec Ctrl+Z).
public static class PasserEnPremierePersonne
{
    [MenuItem("Outils/Joueur/Passer en premiere personne")]
    private static void PremierePersonne()
    {
        GameObject robot = TrouverRobot();
        if (!robot)
        {
            EditorUtility.DisplayDialog("Premiere personne", "Aucun joueur trouve (tag \"Player\" ou objet \"Robot_Explorateur\").", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Passer en premiere personne");
        int groupe = Undo.GetCurrentGroup();

        var ancien = robot.GetComponent<ControleurRobot3D>();
        if (ancien) { Undo.RecordObject(ancien, ""); ancien.enabled = false; }
        var fps = robot.GetComponent<ControleurPremierePersonne3D>();
        if (!fps) fps = Undo.AddComponent<ControleurPremierePersonne3D>(robot);
        else { Undo.RecordObject(fps, ""); fps.enabled = true; }

        if (Camera.main)
        {
            var suivi = Camera.main.GetComponent<CameraSuiveuse3D>();
            if (suivi) { Undo.RecordObject(suivi, ""); suivi.enabled = false; }
        }

        Undo.CollapseUndoOperations(groupe);
        EditorSceneManager.MarkSceneDirty(robot.scene);
        Selection.activeGameObject = robot;
        Debug.Log("[Joueur] Premiere personne activee sur " + robot.name + ". Echap libere la souris, clic pour la reprendre.");
    }

    [MenuItem("Outils/Joueur/Revenir a la troisieme personne")]
    private static void TroisiemePersonne()
    {
        GameObject robot = TrouverRobot();
        if (!robot) return;

        Undo.SetCurrentGroupName("Revenir a la troisieme personne");
        int groupe = Undo.GetCurrentGroup();

        var fps = robot.GetComponent<ControleurPremierePersonne3D>();
        if (fps) { Undo.RecordObject(fps, ""); fps.enabled = false; }
        var ancien = robot.GetComponent<ControleurRobot3D>();
        if (ancien) { Undo.RecordObject(ancien, ""); ancien.enabled = true; }
        if (Camera.main)
        {
            var suivi = Camera.main.GetComponent<CameraSuiveuse3D>();
            if (suivi) { Undo.RecordObject(suivi, ""); suivi.enabled = true; }
        }

        Undo.CollapseUndoOperations(groupe);
        EditorSceneManager.MarkSceneDirty(robot.scene);
    }

    private static GameObject TrouverRobot()
    {
        GameObject robot = null;
        try { robot = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
        if (!robot) robot = GameObject.Find("Robot_Explorateur");
        return robot;
    }
}
