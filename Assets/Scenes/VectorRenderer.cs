using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Inspired from : https://github.com/SunboX/SVG-to-GLSL-Converter
public class VectorRenderer : MonoBehaviour
{

    private List<Vector3> m_vertices;

    private void AddVertex(float x, float y, float z)
    {
        m_vertices.Add(new Vector3(x, y, z));

    }

    private void AddVertex(float x, float y)
    {
        AddVertex(x, y, 1.0f);
    }


    public void GetMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = m_vertices.ToArray();
    }

}
