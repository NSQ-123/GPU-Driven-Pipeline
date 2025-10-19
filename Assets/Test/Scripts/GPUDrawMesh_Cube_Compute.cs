using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// @Author ：NiShiqiang
// @Created ：2025/10/18 14:05:10
namespace Game.Test
{
    public class GPUDrawMesh_Cube_Compute : MonoBehaviour
    {
        private static readonly int Indices = Shader.PropertyToID("Indices");
        private static readonly int Positions = Shader.PropertyToID("Positions");
        public Material material;
        public ComputeShader computeShader;
        private ComputeBuffer _positionsBuffer;
        private ComputeBuffer _indexBuffer;
        private Mesh _mesh;
        private readonly Vector3[] positions = 
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 1),
            //new Vector3(2, 2, 2),
            // 可以添加更多位置
        };


        void Start()
        {
            //创建顶点位置和索引的缓冲区
            _positionsBuffer = new ComputeBuffer(8, sizeof(float)*3);
            _indexBuffer = new ComputeBuffer(36, sizeof(int));

            //设置缓冲区
            computeShader.SetBuffer(0, Positions, _positionsBuffer);
            computeShader.SetBuffer(0, Indices, _indexBuffer);
            //computeShader.Dispatch(0, 1, 1, 1);//线程组如果不够 则不能正确的显示
            // 计算需要的线程组数量：36个索引 ÷ 8线程/组 = 4.5 → 向上取整为5
            int threadGroupsX = Mathf.CeilToInt(36f / 8f);
            computeShader.Dispatch(0, threadGroupsX, 1, 1);

            //创建网格
            _mesh = new Mesh();
            CubeDefine.CreateCubeMeshData(_mesh, _positionsBuffer, _indexBuffer);
            
        }

        void Update()
        {
            foreach (var pos in positions)
            {
                Graphics.DrawMesh(_mesh,pos,Quaternion.identity,material,0);
            }
        }

         void OnDestroy()
        {
            // 释放缓冲区
            _positionsBuffer.Release();
            _indexBuffer.Release();
        }

    }
}