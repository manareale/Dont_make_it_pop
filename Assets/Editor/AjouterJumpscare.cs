using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Outils > Jumpscare > Ajouter aux poupees" :
// ajoute JumpscarePoupee a chaque SKM_DemonDoll de la scene ouverte. Annulable avec Ctrl+Z.
public static class AjouterJumpscare
{
    [MenuItem("Outils/Jumpscare/Ajouter aux poupees")]
    private static void Ajouter()
    {
        Undo.SetCurrentGroupName("Ajouter jumpscare aux poupees");
        int groupe = Undo.GetCurrentGroup();
        int ajoutees = 0, deja = 0;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("SKM_DemonDoll")) continue;
            if (t.parent && t.parent.name.StartsWith("SKM_DemonDoll")) continue; // seulement la racine
            if (t.GetComponent<JumpscarePoupee>()) { deja++; continue; }
            Undo.AddComponent<JumpscarePoupee>(t.gameObject);
            ajoutees++;
        }

        Undo.CollapseUndoOperations(groupe);
        if (ajoutees > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Jumpscare",
            ajoutees > 0 ? ajoutees + " poupee(s) configuree(s)" + (deja > 0 ? " (" + deja + " l'etaient deja)" : "") + ".\nSauvegardez la scene puis approchez-vous d'une poupee."
                         : (deja > 0 ? "Toutes les poupees ont deja le jumpscare." : "Aucun objet SKM_DemonDoll trouve dans la scene ouverte."),
            "OK");
    }
}
