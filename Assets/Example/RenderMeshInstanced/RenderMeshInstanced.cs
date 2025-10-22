using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderMeshInstanced : MonoBehaviour
{
    public Material material;
    public Mesh mesh;
    const int numInstances = 10;

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        Matrix4x4[] instData = new Matrix4x4[numInstances];
        for(int i=0; i<numInstances; ++i)
            instData[i] = Matrix4x4.Translate(new Vector3(-4.5f+i, 0.0f, 5.0f));
        Graphics.RenderMeshInstanced(rp, mesh, 0, instData);
    }
}
