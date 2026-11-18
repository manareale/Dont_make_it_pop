using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ajoute des boutons dans l'Inspector du GenerateurLabyrintheWFC
// et un menu "Outils > Labyrinthe WFC > Creer un generateur".
// Toutes les actions peuvent etre annulees avec Ctrl+Z.
[CustomEditor(typeof(GenerateurLabyrintheWFC))]
public class GenerateurLabyrintheWFCEditor : Editor
{
    private const string DossierMateriaux = "Assets/Labyrinthe_Genere/Materials";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var gen = (GenerateurLabyrintheWFC)target;

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.4f);
        if (GUILayout.Button("Generer le labyrinthe", GUILayout.Height(32))) Generer(gen);
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Effacer le labyrinthe")) Effacer(gen);

        if (gen.Plan != null)
        {
            EditorGUILayout.HelpBox(
                $"Chemin entree -> sortie : {gen.Plan.LongueurChemin} cellules\n" +
                $"Impasses : {gen.Plan.CulsDeSac.Count}\n" +
                $"Murs ouverts pour tout relier : {gen.Plan.MursOuvertsPourRaccorder}",
                MessageType.Info);
        }
    }

    private static void Generer(GenerateurLabyrintheWFC gen)
    {
        if (Application.isPlaying) { gen.Generer(); return; }

        Undo.SetCurrentGroupName("Generer le labyrinthe");
        int groupe = Undo.GetCurrentGroup();

        Undo.RecordObject(gen, "Generer le labyrinthe");
        foreach (var t in gen.ObjetsDeplaces) Undo.RecordObject(t, "Generer le labyrinthe");
        Transform ancien = gen.Racine;
        if (ancien) Undo.DestroyObjectImmediate(ancien.gameObject);

        AssignerMateriauxAssets(gen);
        gen.Generer();

        Transform racine = gen.Racine;
        if (racine)
        {
            Undo.RegisterCreatedObjectUndo(racine.gameObject, "Generer le labyrinthe");
            // Statique = moins d'appels de rendu et compatible avec l'eclairage precalcule.
            foreach (var r in racine.GetComponentsInChildren<Renderer>(true))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }
        Undo.CollapseUndoOperations(groupe);
        EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
    }

    private static void Effacer(GenerateurLabyrintheWFC gen)
    {
        if (Application.isPlaying) { gen.Effacer(); return; }
        Transform ancien = gen.Racine;
        if (ancien) Undo.DestroyObjectImmediate(ancien.gameObject);
        EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
    }

    // Les materiaux du mais sont enregistres comme assets pour etre sauvegardes avec la scene.
    private static void AssignerMateriauxAssets(GenerateurLabyrintheWFC gen)
    {
        var so = new SerializedObject(gen);
        Assigner(so.FindProperty("materielTige"), "M_Mais_Tige", new Color(0.40f, 0.58f, 0.18f));
        Assigner(so.FindProperty("materielEpi"), "M_Mais_Epi", new Color(0.95f, 0.78f, 0.28f));
        so.ApplyModifiedProperties();
    }

    private static void Assigner(SerializedProperty prop, string nom, Color couleur)
    {
        if (prop == null || prop.objectReferenceValue != null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Labyrinthe_Genere")) AssetDatabase.CreateFolder("Assets", "Labyrinthe_Genere");
        if (!AssetDatabase.IsValidFolder(DossierMateriaux)) AssetDatabase.CreateFolder("Assets/Labyrinthe_Genere", "Materials");
        string chemin = DossierMateriaux + "/" + nom + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (!mat)
        {
            mat = GenerateurLabyrintheWFC.CreerMateriau(nom, couleur);
            AssetDatabase.CreateAsset(mat, chemin);
        }
        prop.objectReferenceValue = mat;
    }

    [MenuItem("Outils/Labyrinthe WFC/Creer un generateur")]
    private static void CreerGenerateur()
    {
        var go = new GameObject("LABYRINTHE_MAIS");
        Undo.RegisterCreatedObjectUndo(go, "Creer un generateur de labyrinthe");
        var gen = go.AddComponent<GenerateurLabyrintheWFC>();
        Selection.activeGameObject = go;
        Generer(gen);
    }
}
