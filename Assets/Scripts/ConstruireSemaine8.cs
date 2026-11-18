using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConstruireSemaine8
{
    private const string Racine = "Assets/Semaine8_Generee";

    [MenuItem("Outils/Semaine 8 - 3D/Construire la scene complete")]
    public static void Construire()
    {
        CreerDossier("Assets", "Semaine8_Generee");
        CreerDossier(Racine, "Materials");
        CreerDossier(Racine, "Prefabs");
        CreerDossier(Racine, "Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material sol = Materiau("M_Sol", new Color(0.23f, 0.30f, 0.34f));
        Material mur = Materiau("M_Mur", new Color(0.34f, 0.40f, 0.46f));
        Material obstacle = Materiau("M_Obstacle", new Color(0.84f, 0.34f, 0.18f));
        Material accent = Materiau("M_Accent", new Color(0.12f, 0.55f, 0.90f));
        Material energie = Materiau("M_Energie", new Color(0.95f, 0.78f, 0.12f));
        Material robot = Materiau("M_Robot", new Color(0.18f, 0.72f, 0.62f));

        Transform env = Parent("ENVIRONNEMENT");
        Transform objets = Parent("OBJETS_DE_JEU");
        Transform reperes = Parent("REPERES_PEDAGOGIQUES");
        Transform murs = Parent("Murs", env);
        Transform obstacles = Parent("Obstacles", env);
        Transform batteries = Parent("Batteries", objets);

        Primitive(PrimitiveType.Plane, "Sol", env, Vector3.zero, new Vector3(2.2f,1,2.2f), Vector3.zero, sol);
        Primitive(PrimitiveType.Cube, "Mur_Nord", murs, new Vector3(0,1,10), new Vector3(20,2,.5f), Vector3.zero, mur);
        Primitive(PrimitiveType.Cube, "Mur_Sud", murs, new Vector3(0,1,-10), new Vector3(20,2,.5f), Vector3.zero, mur);
        Primitive(PrimitiveType.Cube, "Mur_Est", murs, new Vector3(10,1,0), new Vector3(.5f,2,20), Vector3.zero, mur);
        Primitive(PrimitiveType.Cube, "Mur_Ouest", murs, new Vector3(-10,1,0), new Vector3(.5f,2,20), Vector3.zero, mur);

        Primitive(PrimitiveType.Cube, "Obstacle_01", obstacles, new Vector3(-4,.75f,-2), new Vector3(4,1.5f,1), new Vector3(0,25,0), obstacle);
        Primitive(PrimitiveType.Cube, "Obstacle_02", obstacles, new Vector3(3,.75f,-1), new Vector3(1,1.5f,5), Vector3.zero, obstacle);
        Primitive(PrimitiveType.Cube, "Obstacle_03", obstacles, new Vector3(-2,.75f,4), new Vector3(5,1.5f,1), new Vector3(0,-20,0), obstacle);
        Primitive(PrimitiveType.Cube, "Obstacle_04", obstacles, new Vector3(5,.75f,5), new Vector3(2,1.5f,2), Vector3.zero, obstacle);

        GameObject depart = Primitive(PrimitiveType.Cylinder, "Zone_Depart", objets, new Vector3(0,.05f,-7), new Vector3(2,.05f,2), Vector3.zero, accent);
        depart.GetComponent<Collider>().isTrigger = true;
        GameObject joueur = Primitive(PrimitiveType.Capsule, "Robot_Explorateur", objets, new Vector3(0,1,-7), Vector3.one, Vector3.zero, robot);
        Rigidbody rb = joueur.AddComponent<Rigidbody>(); rb.isKinematic = true;

        GameObject prefabSource = Batterie(energie);
        string prefabPath = Racine + "/Prefabs/PF_Batterie.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, prefabPath);
        Object.DestroyImmediate(prefabSource);
        Instance(prefab, "Batterie_01", batteries, new Vector3(-5,1,-1));
        Instance(prefab, "Batterie_02", batteries, new Vector3(4,1,1));
        Instance(prefab, "Batterie_03", batteries, new Vector3(0,1,6));

        Transform porte = Parent("Porte_Sortie", objets);
        Primitive(PrimitiveType.Cube, "Montant_G", porte, new Vector3(-2,1.75f,8), new Vector3(.5f,3.5f,.7f), Vector3.zero, mur);
        Primitive(PrimitiveType.Cube, "Montant_D", porte, new Vector3(2,1.75f,8), new Vector3(.5f,3.5f,.7f), Vector3.zero, mur);
        Primitive(PrimitiveType.Cube, "Linteau", porte, new Vector3(0,3.25f,8), new Vector3(4.5f,.5f,.7f), Vector3.zero, mur);
        GameObject battant = Primitive(PrimitiveType.Cube, "Battant", porte, new Vector3(0,1.5f,8), new Vector3(3.5f,2.8f,.35f), Vector3.zero, accent);
        battant.GetComponent<Collider>().isTrigger = true;

        Axes(reperes);
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.15f; lightGo.transform.rotation=Quaternion.Euler(48,-32,0);
        GameObject pointGo = new GameObject("Lumiere_Sortie");
        Light point = pointGo.AddComponent<Light>(); point.type=LightType.Point; point.color=new Color(.2f,.65f,1); point.intensity=4; point.range=8; pointGo.transform.position=new Vector3(0,3,6.5f);
        GameObject cameraGo = new GameObject("Main Camera"); cameraGo.tag="MainCamera"; Camera camera=cameraGo.AddComponent<Camera>(); cameraGo.AddComponent<AudioListener>(); cameraGo.transform.position=new Vector3(0,16,-18); cameraGo.transform.rotation=Quaternion.Euler(32,0,0); camera.fieldOfView=55;

        EditorSceneManager.SaveScene(scene, Racine + "/Scenes/Environnement3D_S08.unity");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeObject = joueur;
        EditorUtility.DisplayDialog("Semaine 8", "La scene 3D a ete construite et enregistree.", "OK");
    }

    private static void CreerDossier(string parent, string nom) { if (!AssetDatabase.IsValidFolder(parent+"/"+nom)) AssetDatabase.CreateFolder(parent,nom); }
    private static Transform Parent(string nom, Transform parent=null) { var g=new GameObject(nom); if(parent) g.transform.SetParent(parent); return g.transform; }
    private static GameObject Primitive(PrimitiveType type,string nom,Transform parent,Vector3 pos,Vector3 scale,Vector3 rot,Material mat) { var g=GameObject.CreatePrimitive(type); g.name=nom; g.transform.SetParent(parent); g.transform.position=pos; g.transform.localScale=scale; g.transform.eulerAngles=rot; g.GetComponent<Renderer>().sharedMaterial=mat; return g; }
    private static Material Materiau(string nom,Color couleur) { string p=Racine+"/Materials/"+nom+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(p); if(m) return m; Shader s=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); m=new Material(s){color=couleur}; AssetDatabase.CreateAsset(m,p); return m; }
    private static GameObject Batterie(Material mat) { var root=new GameObject("Batterie"); var corps=GameObject.CreatePrimitive(PrimitiveType.Cube); corps.name="Corps"; corps.transform.SetParent(root.transform); corps.transform.localScale=new Vector3(.7f,1,.45f); corps.GetComponent<Renderer>().sharedMaterial=mat; var borne=GameObject.CreatePrimitive(PrimitiveType.Cylinder); borne.name="Borne"; borne.transform.SetParent(root.transform); borne.transform.localPosition=new Vector3(0,.62f,0); borne.transform.localScale=new Vector3(.18f,.12f,.18f); borne.GetComponent<Renderer>().sharedMaterial=mat; BoxCollider col=root.AddComponent<BoxCollider>(); col.size=new Vector3(.9f,1.5f,.7f); col.isTrigger=true; root.AddComponent<AnimationObjetPedagogique>(); return root; }
    private static void Instance(GameObject prefab,string nom,Transform parent,Vector3 pos) { var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab); g.name=nom; g.transform.SetParent(parent); g.transform.position=pos; }
    private static void Axes(Transform parent) { Material x=Materiau("M_Axe_X",Color.red), y=Materiau("M_Axe_Y",Color.green), z=Materiau("M_Axe_Z",Color.blue); Primitive(PrimitiveType.Cube,"Axe_X",parent,new Vector3(1.5f,.1f,0),new Vector3(3,.08f,.08f),Vector3.zero,x); Primitive(PrimitiveType.Cube,"Axe_Y",parent,new Vector3(0,1.5f,0),new Vector3(.08f,3,.08f),Vector3.zero,y); Primitive(PrimitiveType.Cube,"Axe_Z",parent,new Vector3(0,.1f,1.5f),new Vector3(.08f,.08f,3),Vector3.zero,z); parent.position=new Vector3(-8,.1f,-8); }
}
