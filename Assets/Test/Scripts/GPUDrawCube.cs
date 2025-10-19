using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


namespace Game.Test
{
    public class GPUDrawCube : MonoBehaviour
    {
        public enum EDrawMode
        {
            Draw,
            DrawInstance,
            DrawIndirect,
        }

        public enum EShaderMode
        {
            Instanced,
            IndirectInstance,
            IndirectInstanceSetup,
        }


        //=============================================================
        public Material material;
        public ComputeShader computeShader;
        public uint instanceCount = 10000;
        public EDrawMode drawMode = EDrawMode.Draw;
        public EShaderMode shaderMode = EShaderMode.Instanced;
        public bool enableInstancing = false;

        //=============================================================
        private Mesh _mesh;
        private Matrix4x4[] _matrices;
        private Vector4[] _colors;
        private MaterialPropertyBlock _materialPropertyBlock;
        private Bounds _bounds;
        private ComputeBuffer _argsBuffer;
        private ComputeBuffer _matrixBuffer;
        private ComputeBuffer _colorBuffer;

        //=============================================================
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private void Start()
        {
            switch (drawMode)
            {
                case EDrawMode.Draw:
                    DrawInit();
                    break;
                case EDrawMode.DrawInstance:
                    DrawInstanceInit();
                    break;
                case EDrawMode.DrawIndirect:
                    DrawIndirectInit();
                    break;
            }
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
                case EDrawMode.DrawIndirect:
                    DrawIndirect();
                    break;
            }
        }

        private void CommonInit()
        {
            // 创建一个简单的立方体 mesh
            _mesh = CubeDefine.CreateCubeMeshData();
            // 创建材质属性块
            _materialPropertyBlock = new MaterialPropertyBlock();
            //位置和颜色数据初始化
            _matrices = CubeDefine.GetRandomMatrix4x4(instanceCount);
            _colors = CubeDefine.GetRandomVector4(instanceCount);
            //设置Shader
            SetShader();
        }

        private void SetShader()
        {
            if (drawMode is EDrawMode.Draw) shaderMode = EShaderMode.Instanced;
            switch (shaderMode)
            {
                case EShaderMode.Instanced:
                    material.shader = Shader.Find("Custom/CustomInstanced");
                    break;
                case EShaderMode.IndirectInstance:
                    material.shader = Shader.Find("Custom/IndirectInstance");
                    break;
                case EShaderMode.IndirectInstanceSetup:
                    material.shader = Shader.Find("Custom/IndirectInstanceSetup");
                    break;
            }

            //启用GPU Instancing
            material.enableInstancing = enableInstancing;
        }


        private void DrawInit()
        {
            CommonInit();
        }

        private void Draw()
        {
            for (int i = 0; i < instanceCount; i++)
            {
                _materialPropertyBlock.SetColor(ColorProp, _colors[i]);
                Graphics.DrawMesh(_mesh, _matrices[i], material, 0, null, 0, _materialPropertyBlock);
            }
        }


        private void DrawInstanceInit()
        {
            CommonInit();
        }

        // private void DrawInstance()
        // {
        //     // 一次性绘制所有实例（最高效的方法）
        //     _materialPropertyBlock.SetVectorArray(ColorProp, _colors);
        //     Graphics.DrawMeshInstanced(_mesh, 0, material, _matrices, _matrices.Length, _materialPropertyBlock);
        // }

        private void DrawInstance()
        {
            // 检查关键资源是否存在
            if (_mesh == null || material == null || _matrices == null || _matrices.Length == 0)
            {
                return;
            }

            // 定义每批的最大实例数
            var maxInstancesPerBatch = 1023;

            // 计算需要分成多少批
            var totalInstances = _matrices.Length;
            var batchCount = Mathf.CeilToInt((float)totalInstances / maxInstancesPerBatch);

            // 分批进行绘制
            for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                var startIndex = batchIndex * maxInstancesPerBatch;
                var instancesInThisBatch = Mathf.Min(maxInstancesPerBatch, totalInstances - startIndex);

                // 提取当前批次的矩阵数据
                Matrix4x4[] currentBatchMatrices = new Matrix4x4[instancesInThisBatch];
                Vector4[] currentBatchColors = new Vector4[instancesInThisBatch];
                System.Array.Copy(_matrices, startIndex, currentBatchMatrices, 0, instancesInThisBatch);
                System.Array.Copy(_colors, startIndex, currentBatchColors, 0, instancesInThisBatch);

                // 绘制当前批次
                Graphics.DrawMeshInstanced(
                    _mesh,
                    0, // submesh index
                    material,
                    currentBatchMatrices,
                    instancesInThisBatch,
                    _materialPropertyBlock
                );
            }
        }


        private void DrawIndirectInit()
        {
            CommonInit();
            // 设置边界
            _bounds = CalculateBounds();
            // 创建 ArgsBuffer 并设置 ComputeBufferType.IndirectArguments 标志
            _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            // 创建矩阵缓冲区
            _matrixBuffer = new ComputeBuffer((int)instanceCount, sizeof(float) * 16); // 4x4矩阵 = 16个float × 4字节
            _matrixBuffer.SetData(_matrices);
            // 创建颜色缓冲区
            _colorBuffer = new ComputeBuffer((int)instanceCount, sizeof(float) * 4); // 4D向量 = 4个float × 4字节
            _colorBuffer.SetData(_colors);
            // 将矩阵和颜色缓冲区绑定到材质属性块中，以便材质 Shader 能够直接访问
            _materialPropertyBlock.SetBuffer("_MatrixBuffer", _matrixBuffer);
            _materialPropertyBlock.SetBuffer("_ColorBuffer", _colorBuffer);
            
            // 确保 ArgsBuffer 中的实例数量正确设置
            uint[] args = new uint[5];
            args[0] = (uint)_mesh.GetIndexCount(0); // 索引数量
            args[1] = instanceCount; // 实例数量
            args[2] = (uint)_mesh.GetIndexStart(0); // 索引起始位置
            args[3] = (uint)_mesh.GetBaseVertex(0); // 基础顶点位置
            args[4] = 0; // 实例起始位置
            _argsBuffer.SetData(args);
        }

        // 根据实例位置动态计算边界
        private Bounds CalculateBounds()
        {
            if (_matrices == null || _matrices.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Vector3 min = _matrices[0].GetColumn(3);
            Vector3 max = min;

            for (int i = 1; i < _matrices.Length; i++)
            {
                Vector3 pos = _matrices[i].GetColumn(3);
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min + Vector3.one; // 加上 Vector3.one 以考虑实例的大小
            return new Bounds(center, size);
        }


        private void DrawIndirect()
        {
            if (computeShader == null || _argsBuffer == null)
            {
                Debug.LogError("ComputeShader or ArgsBuffer is not initialized.");
                return;
            }
            
            // // 设置 ComputeShader 参数
            // int kernelIndex = computeShader.FindKernel("CSMain");
            // computeShader.SetBuffer(kernelIndex, "ArgsBuffer", _argsBuffer);
            // computeShader.SetInt("InstanceCount", (int)instanceCount);
            //
            // // 调度 ComputeShader
            // computeShader.GetKernelThreadGroupSizes(kernelIndex, out var threadGroupSizeX, out var threadGroupSizeY,
            //     out var threadGroupSizeZ);
            // int threadGroupsX = Mathf.CeilToInt(instanceCount / (float)threadGroupSizeX);
            // computeShader.Dispatch(kernelIndex, threadGroupsX, 1, 1);


            // // 确保 ArgsBuffer 中的实例数量正确设置
            // uint[] args = new uint[5];
            // args[0] = (uint)_mesh.GetIndexCount(0); // 索引数量
            // args[1] = instanceCount; // 实例数量
            // args[2] = (uint)_mesh.GetIndexStart(0); // 索引起始位置
            // args[3] = (uint)_mesh.GetBaseVertex(0); // 基础顶点位置
            // args[4] = 0; // 实例起始位置
            // _argsBuffer.SetData(args);

            // 完全由GPU驱动的渲染
            Graphics.DrawMeshInstancedIndirect(_mesh, 0, material, _bounds, _argsBuffer, 0, _materialPropertyBlock,
                ShadowCastingMode.On, true);
        }

        private void OnDestroy()
        {
            // 释放 ComputeBuffer
            _argsBuffer?.Release();
            _matrixBuffer?.Release();
            _colorBuffer?.Release();
        }
    }
}