using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Game
{
    public class MultiMeshIndirectRenderer : MonoBehaviour
    {
        [Header("Models")] public Mesh[] meshes;
        public Material material;

        [Header("Instance Settings")] public int instancesPerModel = 1000;
        public Vector3 areaSize = new Vector3(100, 10, 100);

        [Header("Culling")] public ComputeShader cullingComputeShader;
        private Camera mainCam;

        // Compute Buffers - 修改为每个模型独立的数据流
        private ComputeBuffer modelMatrixBuffer;
        private ComputeBuffer[] visibleInstanceBuffers; // 每个模型一个可见实例缓冲区
        private ComputeBuffer[] argsBuffers; // 每个模型一个参数缓冲区
        private ComputeBuffer[] modelInstanceCountBuffers; // 每个模型的实例计数器

        private struct InstanceData
        {
            public Matrix4x4 objectToWorld;
            public uint modelID;
        }

        void Start()
        {
            mainCam = Camera.main;
            InitializeBuffers();
        }

        void InitializeBuffers()
        {
            int totalInstances = meshes.Length * instancesPerModel;

            // 1. 初始化实例数据
            InstanceData[] instanceDatas = new InstanceData[totalInstances];
            for (int modelIndex = 0; modelIndex < meshes.Length; modelIndex++)
            {
                for (int i = 0; i < instancesPerModel; i++)
                {
                    int index = modelIndex * instancesPerModel + i;
                    instanceDatas[index].modelID = (uint)modelIndex;
                    Vector3 position = new Vector3(
                        Random.Range(-areaSize.x / 2, areaSize.x / 2),
                        Random.Range(-areaSize.y / 2, areaSize.y / 2),
                        Random.Range(-areaSize.z / 2, areaSize.z / 2)
                    );
                    // 给一些随机缩放让实例更明显
                    instanceDatas[index].objectToWorld = Matrix4x4.TRS(
                        position,
                        Random.rotation,
                        Vector3.one * Random.Range(0.5f, 2f));
                }
            }

            // 创建主数据缓冲区
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(InstanceData));
            modelMatrixBuffer = new ComputeBuffer(totalInstances, stride);
            modelMatrixBuffer.SetData(instanceDatas);

            // 2. 为每个模型创建独立的缓冲区和参数缓冲区
            visibleInstanceBuffers = new ComputeBuffer[meshes.Length];
            argsBuffers = new ComputeBuffer[meshes.Length];
            modelInstanceCountBuffers = new ComputeBuffer[meshes.Length];

            for (int i = 0; i < meshes.Length; i++)
            {
                // 每个模型的可见实例缓冲区
                visibleInstanceBuffers[i] = new ComputeBuffer(instancesPerModel, stride, ComputeBufferType.Append);

                // 实例计数器（用于CopyCount）
                modelInstanceCountBuffers[i] = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);

                // 间接绘制参数缓冲区
                uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
                args[0] = (uint)meshes[i].GetIndexCount(0);
                args[1] = (uint)0; // 实例数量由Compute Shader填充
                args[2] = (uint)meshes[i].GetIndexStart(0);
                args[3] = (uint)meshes[i].GetBaseVertex(0);
                args[4] = 0;

                argsBuffers[i] = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                argsBuffers[i].SetData(args);
            }

            // 3. 设置材质缓冲区
            material.SetBuffer("_InstanceDataBuffer", modelMatrixBuffer);
        }

        void Update()
        {
            FrustumCullAndDispatch();
            RenderMeshes();
        }

        void FrustumCullAndDispatch()
        {
            // 简化：先不执行真正的Compute Shader剔除，直接绘制所有实例进行测试
            for (int i = 0; i < meshes.Length; i++)
            {
                // 重置可见实例缓冲区
                visibleInstanceBuffers[i].SetCounterValue(0);

                // 临时方案：假设所有实例都可见，直接使用总实例数
                // 在实际项目中，这里应该调度Compute Shader进行真正的剔除
                uint visibleCount = (uint)instancesPerModel;

                // 更新参数缓冲区中的实例数量
                uint[] args = new uint[5];
                argsBuffers[i].GetData(args);
                args[1] = visibleCount; // 设置实例数量
                argsBuffers[i].SetData(args);
            }
        }

        void RenderMeshes()
        {
            // 确保材质有正确的缓冲区引用
            material.SetBuffer("_InstanceDataBuffer", modelMatrixBuffer);

            // 检查材质是否启用了GPU Instancing
            if (!material.enableInstancing)
            {
                Debug.LogWarning("材质未启用GPU Instancing，已自动启用");
                material.enableInstancing = true;
            }

            // 绘制每个模型
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null)
                {
                    Debug.LogError($"Mesh at index {i} is null!");
                    continue;
                }

                // 设置正确的实例数据起始索引
                material.SetInt("_StartInstanceIndex", i * instancesPerModel);

                Graphics.DrawMeshInstancedIndirect(
                    meshes[i],
                    0,
                    material,
                    new Bounds(Vector3.zero, areaSize * 2),
                    argsBuffers[i]
                );
            }
        }

        void OnDestroy()
        {
            modelMatrixBuffer?.Release();

            if (visibleInstanceBuffers != null)
                foreach (var buffer in visibleInstanceBuffers)
                    buffer?.Release();

            if (argsBuffers != null)
                foreach (var buffer in argsBuffers)
                    buffer?.Release();

            if (modelInstanceCountBuffers != null)
                foreach (var buffer in modelInstanceCountBuffers)
                    buffer?.Release();
        }
    }
}