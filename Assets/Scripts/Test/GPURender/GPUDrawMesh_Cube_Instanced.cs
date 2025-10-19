using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Game.Test
{
    public class GPUDrawMesh_Cube_Instanced : MonoBehaviour
    {
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        public Material material;
        public int instanceCount = 100;
        private Mesh _mesh;
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;
        private MaterialPropertyBlock _materialPropertyBlock;
        
        public EDrawMode drawMode = EDrawMode.DrawInstance;
        public enum EDrawMode
        {
            Draw,
            DrawInstance
        }
        
        void Start()
        {
            _mesh = CubeDefine.CreateCubeMeshData();
            _matrices = new Matrix4x4[instanceCount];
            _colors = new Vector4[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f,
                    Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f), Vector3.one);
                _colors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
            }

            //启用GPU Instancing
            material.enableInstancing = true;
            _materialPropertyBlock = new MaterialPropertyBlock();
            //_materialPropertyBlock.SetVectorArray(ColorProp, _colors);
        }

        void Update()
        {
            switch (drawMode)
            {
                case EDrawMode.Draw:
                    Draw();
                    break;
                case EDrawMode.DrawInstance:
                    DrawInstance();
                    break;
            }
        }

        private void Draw()
        {
            Debug.Log("Draw");
            for (int i = 0; i < instanceCount; i++)
            {
                // _materialPropertyBlock = new MaterialPropertyBlock();
                _materialPropertyBlock.SetColor(ColorProp, _colors[i]);
                Graphics.DrawMesh(_mesh, _matrices[i], material, 0, null, 0, _materialPropertyBlock);
            }
        }

        private void DrawInstance()
        {
            //Graphics.DrawMeshInstanced一次调用最多绘制 1023 个实例
            //超过1023，需要分批绘制逻辑
            
            int totalInstancesDrawn = 0;
            int batches = Mathf.CeilToInt((float)instanceCount / 1023);
            
            for (int i = 0; i < batches; i++)
            {
                int startIndex = i * 1023;
                int count = Mathf.Min(1023, instanceCount - startIndex);
                totalInstancesDrawn += count;
                // 提取当前批次的矩阵和颜色
                var batchMatrices = new Matrix4x4[count];
                var batchColors = new Vector4[count];
                System.Array.Copy(_matrices, startIndex, batchMatrices, 0, count);
                System.Array.Copy(_colors, startIndex, batchColors, 0, count);

                _materialPropertyBlock.SetVectorArray(ColorProp, batchColors);
                Graphics.DrawMeshInstanced(_mesh, 0, material, batchMatrices, count, _materialPropertyBlock);
            }
            Debug.Log($"总实例数: {instanceCount}, 实际绘制批次: {batches}, 总实例绘制数: {totalInstancesDrawn}");
        }
    }
}