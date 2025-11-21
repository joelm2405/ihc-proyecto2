using UnityEngine;
using UnityEditor;

public static class SplitSubmeshes
{
    [MenuItem("Tools/Split Selected Mesh Into Submeshes")]
    public static void Split()
    {
        var go = Selection.activeGameObject;
        if (!go) { Debug.LogError("Selecciona un GameObject con MeshFilter."); return; }

        var mf = go.GetComponent<MeshFilter>();
        var mr = go.GetComponent<MeshRenderer>();
        if (!mf || !mr || !mf.sharedMesh) { Debug.LogError("Falta MeshFilter/MeshRenderer o Mesh."); return; }

        var mesh = mf.sharedMesh;
        var mats = mr.sharedMaterials;

        for (int si = 0; si < mesh.subMeshCount; si++)
        {
            var indices = mesh.GetTriangles(si);
            var verts = mesh.vertices;
            var norms = mesh.normals;
            var tans = mesh.tangents;
            var uvs = mesh.uv;

            // Mapa de vértices: old->new
            var map = new System.Collections.Generic.Dictionary<int,int>();
            var newVerts = new System.Collections.Generic.List<Vector3>();
            var newNorms = new System.Collections.Generic.List<Vector3>();
            var newTans = new System.Collections.Generic.List<Vector4>();
            var newUVs = new System.Collections.Generic.List<Vector2>();
            var newTris = new System.Collections.Generic.List<int>();

            for (int i = 0; i < indices.Length; i++)
            {
                int old = indices[i];
                if (!map.TryGetValue(old, out int neu))
                {
                    neu = newVerts.Count;
                    map[old] = neu;
                    newVerts.Add(verts[old]);
                    if (norms != null && norms.Length == verts.Length) newNorms.Add(norms[old]); else newNorms.Add(Vector3.up);
                    if (tans != null && tans.Length == verts.Length) newTans.Add(tans[old]); else newTans.Add(Vector4.zero);
                    if (uvs != null && uvs.Length == verts.Length) newUVs.Add(uvs[old]); else newUVs.Add(Vector2.zero);
                }
                newTris.Add(neu);
            }

            var outMesh = new Mesh();
            outMesh.name = mesh.name + "_sub" + si;
            outMesh.SetVertices(newVerts);
            outMesh.SetNormals(newNorms);
            outMesh.SetTangents(newTans);
            outMesh.SetUVs(0, newUVs);
            outMesh.SetTriangles(newTris, 0);
            outMesh.RecalculateBounds();

            var child = new GameObject(go.name + "_sub" + si);
            child.transform.SetParent(go.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = outMesh;

            var childMr = child.AddComponent<MeshRenderer>();
            childMr.sharedMaterial = (si < mats.Length ? mats[si] : null);
        }

        // Opcional: desactivar el render del original
        mr.enabled = false;
        Debug.Log("Submeshes separados. Revisa los hijos para identificar la ventana.");
    }
}
