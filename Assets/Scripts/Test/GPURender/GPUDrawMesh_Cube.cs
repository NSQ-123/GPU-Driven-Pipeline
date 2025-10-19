using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// @Author ：NiShiqiang
// @Created ：2025/10/18 13:03:08
namespace Game.Test
{
    public class GPUDrawMesh_Cube : MonoBehaviour
    {
        public Material material;
        private Mesh _mesh;
        private readonly Vector3[] positions = 
        {
            new Vector3(0, 0, 0),
            //new Vector3(1, 1, 1),
            //new Vector3(2, 2, 2),
            // 可以添加更多位置
        };
        
        void Start()
        {
            _mesh = CubeDefine.CreateCubeMeshData();
        }


        void Update()
        {
            foreach (var pos in positions)
            {
                Graphics.DrawMesh(_mesh,pos,Quaternion.identity,material,0);
            }
        }


   
    }
}