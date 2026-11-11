using System.Collections.Generic;
using UnityEngine;

// Construit des maillages low-poly de plant de mais, sans aucun modele 3D.
//
// - CreerTige() : tige effilee + feuilles qui retombent + panicule au sommet (vert)
// - CreerEpi()  : un epi de mais accroche a la tige (jaune)
//
// Le generateur de labyrinthe combine ensuite des milliers de copies de ces
// maillages en quelques gros maillages (rapide a afficher).
public static class MaisProcedural
{
    private const int Cotes = 6;

    public static Mesh CreerTige(float hauteur)
    {
        var v = new List<Vector3>();
        var t = new List<int>();

        // Tige : prisme a 6 faces qui s'affine vers le haut.
        Prisme(v, t, Matrix4x4.identity, 0.045f, 0.018f, hauteur);

        // Feuilles : reparties en spirale (angle d'or), plus longues au milieu.
        const int nbFeuilles = 6;
        for (int i = 0; i < nbFeuilles; i++)
        {
            float f = (i + 0.5f) / nbFeuilles;
            float y = Mathf.Lerp(0.18f, 0.82f, f) * hauteur;
            float longueur = hauteur * Mathf.Lerp(0.28f, 0.36f, Mathf.Sin(f * Mathf.PI));
            float angle = i * 137.5f;
            Feuille(v, t, Matrix4x4.TRS(new Vector3(0, y, 0), Quaternion.Euler(0, angle, 0), Vector3.one), longueur, 0.055f);
        }

        // Panicule (fleur au sommet) : 3 petites pointes.
        for (int i = 0; i < 3; i++)
        {
            var m = Matrix4x4.TRS(new Vector3(0, hauteur, 0), Quaternion.Euler(0, i * 120f, 25f), Vector3.one);
            Triangle(v, t, m, new Vector3(-0.012f, 0, 0), new Vector3(0.012f, 0, 0), new Vector3(0, 0.22f, 0));
        }

        return Finaliser("Mais_Tige", v, t);
    }

    public static Mesh CreerEpi(float hauteur)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        var m = Matrix4x4.TRS(new Vector3(0.05f, hauteur * 0.52f, 0), Quaternion.Euler(0, 0, -28f), Vector3.one);
        float longueur = hauteur * 0.09f;
        Prisme(v, t, m, 0.045f, 0.04f, longueur);
        // Pointe de l'epi.
        Vector3 sommet = m.MultiplyPoint3x4(new Vector3(0, longueur + 0.06f, 0));
        for (int i = 0; i < Cotes; i++)
        {
            Vector3 a = m.MultiplyPoint3x4(PointCercle(i, 0.04f, longueur));
            Vector3 b = m.MultiplyPoint3x4(PointCercle(i + 1, 0.04f, longueur));
            AjouterTri(v, t, a, sommet, b);
        }
        return Finaliser("Mais_Epi", v, t);
    }

    private static Vector3 PointCercle(int i, float rayon, float y)
    {
        float a = i * Mathf.PI * 2f / Cotes;
        return new Vector3(Mathf.Cos(a) * rayon, y, Mathf.Sin(a) * rayon);
    }

    // Prisme a facettes (sommets separes par face = ombrage "low-poly").
    private static void Prisme(List<Vector3> v, List<int> t, Matrix4x4 m, float rayonBas, float rayonHaut, float hauteur)
    {
        for (int i = 0; i < Cotes; i++)
        {
            Vector3 a = m.MultiplyPoint3x4(PointCercle(i, rayonBas, 0));
            Vector3 b = m.MultiplyPoint3x4(PointCercle(i + 1, rayonBas, 0));
            Vector3 c = m.MultiplyPoint3x4(PointCercle(i + 1, rayonHaut, hauteur));
            Vector3 d = m.MultiplyPoint3x4(PointCercle(i, rayonHaut, hauteur));
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.Add(s); t.Add(s + 2); t.Add(s + 1);
            t.Add(s); t.Add(s + 3); t.Add(s + 2);
        }
    }

    // Feuille qui part de la tige, monte puis retombe. Visible des deux cotes.
    private static void Feuille(List<Vector3> v, List<int> t, Matrix4x4 m, float longueur, float largeur)
    {
        Vector3 b1 = new Vector3(0.02f, 0, -largeur * 0.5f);
        Vector3 b2 = new Vector3(0.02f, 0, largeur * 0.5f);
        Vector3 m1 = new Vector3(longueur * 0.45f, longueur * 0.28f, -largeur);
        Vector3 m2 = new Vector3(longueur * 0.45f, longueur * 0.28f, largeur);
        Vector3 pointe = new Vector3(longueur, longueur * 0.02f, 0);
        DeuxFaces(v, t, m, b1, b2, m2);
        DeuxFaces(v, t, m, b1, m2, m1);
        DeuxFaces(v, t, m, m1, m2, pointe);
    }

    private static void Triangle(List<Vector3> v, List<int> t, Matrix4x4 m, Vector3 a, Vector3 b, Vector3 c)
    {
        DeuxFaces(v, t, m, a, b, c);
    }

    private static void DeuxFaces(List<Vector3> v, List<int> t, Matrix4x4 m, Vector3 a, Vector3 b, Vector3 c)
    {
        a = m.MultiplyPoint3x4(a); b = m.MultiplyPoint3x4(b); c = m.MultiplyPoint3x4(c);
        AjouterTri(v, t, a, b, c);
        AjouterTri(v, t, a, c, b);
    }

    private static void AjouterTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int s = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(s); t.Add(s + 1); t.Add(s + 2);
    }

    private static Mesh Finaliser(string nom, List<Vector3> v, List<int> t)
    {
        var mesh = new Mesh { name = nom };
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
