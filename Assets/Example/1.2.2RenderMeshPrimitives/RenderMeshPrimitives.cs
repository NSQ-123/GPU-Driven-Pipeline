using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This method renders multiple instances of the same Mesh, 
//similar to Graphics.RenderMeshIndirect, 
//but provides the number of instances and other rendering command arguments as function arguments.
public class RenderMeshPrimitives : MonoBehaviour
{
    public Material material;
    public Mesh mesh;

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(-4.5f, 0, 0)));
        rp.matProps.SetFloat("_NumInstances", 10.0f);
        Graphics.RenderMeshPrimitives(rp, mesh, 0, 10);
    }
}
