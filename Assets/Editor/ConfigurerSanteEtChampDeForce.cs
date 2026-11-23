using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Outils > Joueuse > Ajouter sante + champ de force".
// Ajoute SanteJoueuse sur la joueuse (tag Player) et cree un objet CHAMP_DE_FORCE
// relie au sol "Sol_Labyrinthe". Annulable avec Ctrl+Z.
public static class ConfigurerSanteEtChampDeForce
{
    [MenuItem("Outils/Joueuse/Ajouter sante + champ de force")]
    private static void Configurer()
    {
        Undo.SetCurrentGroupName("Ajouter sante + champ de force");
        int groupe = Undo.GetCurrentGroup();
        string rapport = "";

        GameObject joueuse = null;
        try { joueuse = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }
        if (joueuse)
        {
            if (!joueuse.GetComponent<SanteJoueuse>()) { Undo.AddComponent<SanteJoueuse>(joueuse); rapport += "SanteJoueuse ajoutee sur " + joueuse.name + ".\n"; }
            else rapport += joueuse.name + " a deja SanteJoueuse.\n";
        }
        else rapport += "Aucun objet avec le tag Player : ajoutez SanteJoueuse a la main.\n";

        var champ = Object.FindFirstObjectByType<ChampDeForce>();
        if (!champ)
        {
            var go = new GameObject("CHAMP_DE_FORCE");
            Undo.RegisterCreatedObjectUndo(go, "");
            champ = go.AddComponent<ChampDeForce>();
            var sol = GameObject.Find("Sol_Labyrinthe");
            if (sol && sol.TryGetComponent(out Collider c))
            {
                var so = new SerializedObject(champ);
                so.FindProperty("sol").objectReferenceValue = c;
                so.ApplyModifiedPropertiesWithoutUndo();
                rapport += "CHAMP_DE_FORCE cree autour de Sol_Labyrinthe.\n";
            }
            else rapport += "CHAMP_DE_FORCE cree : glissez le collider du sol dans le champ 'Sol'.\n";
            Selection.activeGameObject = go;
        }
        else rapport += "Il y a deja un ChampDeForce dans la scene.\n";

        Undo.CollapseUndoOperations(groupe);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Joueuse", rapport + "\nSauvegardez la scene puis appuyez sur Play.", "OK");
    }
}
