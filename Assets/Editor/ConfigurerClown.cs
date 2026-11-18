using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Outils > Clown > Configurer le clown (animations + IA)".
//
// 1. Importe les animations Mixamo en HUMANOID, avec l'avatar du modele
//    (elles etaient en "Generic" sans avatar : elles ne pouvaient pas animer le clown).
//    Boucle activee, animations "sur place" (c'est le script qui deplace le clown).
// 2. Cree AC_Clown.controller avec un Blend Tree 1D sur le parametre "Vitesse" :
//    0 = Idle, 0.5 = Walk, 1 = Run.
// 3. Dans la scene ouverte, sur chaque Whiteclown :
//    assigne AC_Clown, retire AnimationRobot3D_02 (controle clavier) et ajoute ClownPoursuivant.
public static class ConfigurerClown
{
    private const string DossierModele = "Assets/Models_TaAnh";
    private const string DossierAnimations = "Assets/Animations_taanh";
    private const string CheminControleur = DossierAnimations + "/Controllers/AC_Clown.controller";

    [MenuItem("Outils/Clown/Configurer le clown (animations + IA)")]
    public static void Configurer()
    {
        // --- Trouver le modele et son avatar ---
        string cheminModele = TrouverFbx(DossierModele, f => !f.Contains("@") && f.ToLower().Contains("clown"));
        if (cheminModele == null) { Erreur("Modele Whiteclown introuvable dans " + DossierModele + "."); return; }
        var importeurModele = (ModelImporter)AssetImporter.GetAtPath(cheminModele);
        if (importeurModele.animationType != ModelImporterAnimationType.Human)
        {
            importeurModele.animationType = ModelImporterAnimationType.Human;
            importeurModele.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importeurModele.SaveAndReimport();
        }
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(cheminModele).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman) { Erreur("L'avatar Humanoid du modele n'est pas valide. Ouvrez le modele > Rig > Configure."); return; }

        // --- Trouver les 3 animations ---
        string idle = TrouverFbx(DossierAnimations, f => f.Contains("@") && f.ToLower().Contains("idle"));
        string marche = TrouverFbx(DossierAnimations, f => f.Contains("@") && f.ToLower().Contains("walk"));
        string course = TrouverFbx(DossierAnimations, f => f.Contains("@") && f.ToLower().Contains("run"));
        if (idle == null || marche == null || course == null) { Erreur("Il faut 3 FBX dans " + DossierAnimations + " : ...@Idle, ...@Walk..., ...@Run."); return; }

        AnimationClip clipIdle = ImporterAnimation(idle, avatar);
        AnimationClip clipMarche = ImporterAnimation(marche, avatar);
        AnimationClip clipCourse = ImporterAnimation(course, avatar);
        if (!clipIdle || !clipMarche || !clipCourse) { Erreur("Impossible de lire un des clips d'animation."); return; }

        // --- Animator Controller + Blend Tree ---
        AnimatorController controleur = CreerControleur(clipIdle, clipMarche, clipCourse);

        // --- Scene ---
        int nb = BrancherDansLaScene(controleur, avatar);

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Clown",
            "Animations importees en Humanoid et " + CheminControleur + " cree.\n\n" +
            (nb > 0 ? nb + " clown(s) configure(s) dans la scene. Sauvegardez la scene puis appuyez sur Play."
                    : "Aucun clown trouve dans la scene ouverte : glissez le modele dans la scene puis relancez ce menu."),
            "OK");
    }

    private static string TrouverFbx(string dossier, System.Func<string, bool> filtre)
    {
        if (!AssetDatabase.IsValidFolder(dossier)) return null;
        return AssetDatabase.FindAssets("t:Model", new[] { dossier })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.ToLower().EndsWith(".fbx"))
            .FirstOrDefault(p => filtre(System.IO.Path.GetFileNameWithoutExtension(p)));
    }

    private static AnimationClip ImporterAnimation(string chemin, Avatar avatar)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(chemin);
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        imp.sourceAvatar = avatar;
        imp.importAnimation = true;

        ModelImporterClipAnimation[] clips = imp.clipAnimations;
        if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = true;
            // Rotation et hauteur : garder celles de l'animation, sans derive.
            c.lockRootRotation = true;
            c.keepOriginalOrientation = true;
            c.lockRootHeightY = true;
            c.keepOriginalPositionY = true;
            // Deplacement XZ "sur place" : c'est ClownPoursuivant qui fait avancer le clown.
            c.lockRootPositionXZ = true;
            c.keepOriginalPositionXZ = false;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(chemin).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    private static AnimatorController CreerControleur(AnimationClip idle, AnimationClip marche, AnimationClip course)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CheminControleur) != null) AssetDatabase.DeleteAsset(CheminControleur);
        string dossier = System.IO.Path.GetDirectoryName(CheminControleur).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dossier)) AssetDatabase.CreateFolder(DossierAnimations, "Controllers");

        var controleur = AnimatorController.CreateAnimatorControllerAtPath(CheminControleur);
        controleur.AddParameter("Vitesse", AnimatorControllerParameterType.Float);

        // Etat "Locomotion" (etat par defaut) qui contient le Blend Tree.
        controleur.CreateBlendTreeInController("Locomotion", out BlendTree arbre, 0);
        arbre.name = "Locomotion";
        arbre.blendType = BlendTreeType.Simple1D;
        arbre.blendParameter = "Vitesse";
        arbre.useAutomaticThresholds = false;
        arbre.AddChild(idle, 0f);
        arbre.AddChild(marche, 0.5f);
        arbre.AddChild(course, 1f);

        // Retirer le parametre "Blend" si Unity l'a ajoute automatiquement.
        foreach (var p in controleur.parameters)
            if (p.name == "Blend") controleur.RemoveParameter(p);

        EditorUtility.SetDirty(controleur);
        return controleur;
    }

    private static int BrancherDansLaScene(AnimatorController controleur, Avatar avatar)
    {
        var clowns = new List<Animator>();
        foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a.avatar == avatar || a.gameObject.name.ToLower().Contains("clown")) clowns.Add(a);

        Undo.SetCurrentGroupName("Configurer le clown");
        int groupe = Undo.GetCurrentGroup();
        foreach (Animator a in clowns)
        {
            GameObject go = a.gameObject;
            Undo.RecordObject(a, "");
            a.runtimeAnimatorController = controleur;
            a.applyRootMotion = false;
            if (a.avatar == null) a.avatar = avatar;

            // L'ancien script deplacait le clown avec le clavier du joueur.
            foreach (var ancien in go.GetComponents<AnimationRobot3D_02>()) Undo.DestroyObjectImmediate(ancien);

            var rb = go.GetComponent<Rigidbody>();
            if (rb) { Undo.RecordObject(rb, ""); rb.isKinematic = true; rb.useGravity = false; }

            if (!go.GetComponent<ClownPoursuivant>()) Undo.AddComponent<ClownPoursuivant>(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        Undo.CollapseUndoOperations(groupe);
        if (clowns.Count > 0) Selection.activeGameObject = clowns[0].gameObject;
        return clowns.Count;
    }

    private static void Erreur(string message)
    {
        EditorUtility.DisplayDialog("Clown", message, "OK");
        Debug.LogError("[Clown] " + message);
    }
}
