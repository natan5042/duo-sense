using UnityEngine;
using UnityEditor;

public static class RampCreator
{
    [MenuItem("GameObject/2D/Create Ramp", false, 0)]
    public static void CreateRampMenu()
    {
        CreateRamp(2f, 1f, false);
    }

    public static GameObject CreateRamp(float width, float height, bool flip)
    {
        GameObject go = new GameObject("Ramp");
        // Optionnel: ajoute un SpriteRenderer pour voir la rampe en scène
        var sr = go.AddComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Sliced;

        var poly = go.AddComponent<PolygonCollider2D>();
        Vector2[] points;
        if (!flip)
        {
            // pente montant vers la droite (triangle: bottom-left, bottom-right, top-left)
            points = new Vector2[] {
                new Vector2(0f, 0f),
                new Vector2(width, 0f),
                new Vector2(0f, height)
            };
        }
        else
        {
            // pente montant vers la gauche (bottom-left, bottom-right, top-right)
            points = new Vector2[] {
                new Vector2(0f, 0f),
                new Vector2(width, 0f),
                new Vector2(width, height)
            };
        }

        poly.points = points;

        // Marquer comme statique pour l'éditeur
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create Ramp");
        return go;
    }
}
