using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderMeshInstanced_CustomData : MonoBehaviour
{
    public Material material;
    public Mesh mesh;
    const int numInstances = 10;

    struct MyInstanceData
    {
        public Matrix4x4 objectToWorld;
        public float myOtherData;
        public uint renderingLayerMask;
    };

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        MyInstanceData[] instData = new MyInstanceData[numInstances];
        for(int i=0; i<numInstances; ++i)
        {
            instData[i].objectToWorld = Matrix4x4.Translate(new Vector3(-4.5f+i, 0.0f, 5.0f));
            instData[i].renderingLayerMask = (i & 1) == 0 ? 1u : 2u;
        }
        Graphics.RenderMeshInstanced(rp, mesh, 0, instData);
    }
}
