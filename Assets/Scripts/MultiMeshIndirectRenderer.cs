using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// @Author ：#Name#
// @Created ：#CreateTime#
namespace Game
{
    public class MultiMeshIndirectRenderer : MonoBehaviour
    {
        [Header("Models")] 
        public Mesh[] meshes; // 要实例化的不同模型，如Cube, Sphere, Capsule
        public Material material; // 支持GPU Instancing的材质

        [Header("Instance Settings")] 
        public int instancesPerModel = 1000;
        public Vector3 areaSize = new Vector3(100, 10, 100);

        [Header("Culling")] 
        public ComputeShader cullingComputeShader;
        private Camera mainCam;

        // Compute Buffers
        private ComputeBuffer modelMatrixBuffer; // 所有实例的变换矩阵和模型ID
        private ComputeBuffer indirectArgsBuffer;
        private ComputeBuffer[] modelArgsBuffers; // 每个模型一个参数缓冲区
        private ComputeBuffer visibleInstanceBuffer; // 存储剔除后的实例数据

        // 内核数据
        private int kernelIndex;
        private uint threadGroupSize;

        // 定义与Shader和Compute Shader通信的数据结构
        private struct InstanceData
        {
            public Matrix4x4 objectToWorld; // 实例的变换矩阵
            public uint modelID; // 用于索引meshes数组
        }

        void Start()
        {
            mainCam = Camera.main;
            InitializeBuffers();
            kernelIndex = cullingComputeShader.FindKernel("CSMain");
            cullingComputeShader.GetKernelThreadGroupSizes(kernelIndex, out threadGroupSize, out _, out _);
        }

        void InitializeBuffers()
        {
            int totalInstances = meshes.Length * instancesPerModel;

            // 1. 初始化所有实例的数据
            InstanceData[] instanceDatas = new InstanceData[totalInstances];
            for (int modelIndex = 0; modelIndex < meshes.Length; modelIndex++)
            {
                for (int i = 0; i < instancesPerModel; i++)
                {
                    int index = modelIndex * instancesPerModel + i;
                    Vector3 position = new Vector3(
                        Random.Range(-areaSize.x / 2, areaSize.x / 2),
                        Random.Range(-areaSize.y / 2, areaSize.y / 2),
                        Random.Range(-areaSize.z / 2, areaSize.z / 2)
                    );
                    instanceDatas[index].objectToWorld = Matrix4x4.TRS(position, Random.rotation, Vector3.one);
                    instanceDatas[index].modelID = (uint)modelIndex;
                }
            }

            // 创建主数据缓冲区
            int instanceDataStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(InstanceData));
            modelMatrixBuffer = new ComputeBuffer(totalInstances, instanceDataStride);
            modelMatrixBuffer.SetData(instanceDatas);

            // 2. 创建可见实例输出缓冲区
            visibleInstanceBuffer = new ComputeBuffer(totalInstances, instanceDataStride, ComputeBufferType.Append);

            // 3. 为每个模型创建独立的间接参数缓冲区
            modelArgsBuffers = new ComputeBuffer[meshes.Length];
            for (int i = 0; i < meshes.Length; i++)
            {
                uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
                args[0] = (uint)meshes[i].GetIndexCount(0); // 索引数量
                args[1] = (uint)0; // 实例数量将由Compute Shader填充
                args[2] = (uint)meshes[i].GetIndexStart(0);
                args[3] = (uint)meshes[i].GetBaseVertex(0);
                args[4] = 0; // 起始实例位置
                modelArgsBuffers[i] =
                    new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                modelArgsBuffers[i].SetData(args);
            }

            // 4. 将缓冲区设置到Compute Shader和材质
            cullingComputeShader.SetBuffer(kernelIndex, "_InstanceDataBuffer", modelMatrixBuffer);
            cullingComputeShader.SetBuffer(kernelIndex, "_VisibleInstanceBuffer", visibleInstanceBuffer);
            material.SetBuffer("_InstanceDataBuffer", modelMatrixBuffer);
        }

        void Update()
        {
            // 每帧执行剔除并绘制
            FrustumCullAndDispatch();
            RenderMeshes();
        }

        void FrustumCullAndDispatch()
        {
            // 重置可见实例缓冲区计数器
            visibleInstanceBuffer.SetCounterValue(0);
            cullingComputeShader.SetBuffer(kernelIndex, "_VisibleInstanceBuffer", visibleInstanceBuffer);

            // 传递摄像机参数
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCam);
            Vector4[] planes = new Vector4[6];
            for (int i = 0; i < 6; i++)
            {
                planes[i] = new Vector4(frustumPlanes[i].normal.x, frustumPlanes[i].normal.y, frustumPlanes[i].normal.z,
                    frustumPlanes[i].distance);
            }

            cullingComputeShader.SetVectorArray("_FrustumPlanes", planes);
            cullingComputeShader.SetMatrix("_CameraViewProjectionMatrix",
                mainCam.projectionMatrix * mainCam.worldToCameraMatrix);

            // 传递模型包围盒信息（简化处理，实际应用中可能需要为每个模型传入准确的包围球）
            cullingComputeShader.SetInt("_TotalInstances", meshes.Length * instancesPerModel);

            // 调度Compute Shader
            int threadGroups = Mathf.CeilToInt((float)(meshes.Length * instancesPerModel) / threadGroupSize);
            cullingComputeShader.Dispatch(kernelIndex, threadGroups, 1, 1);

            // 为每个模型拷贝可见实例数到其参数缓冲区（这里简化了，实际需要为每个模型单独计数）
            // 更复杂的实现需要在CS中维护每个模型可见数量的AppendBuffer，并使用CopyCount
            for (int i = 0; i < modelArgsBuffers.Length; i++)
            {
                // 此处是简化逻辑。完整实现需在CS中为每个模型维护一个独立的可见实例AppendBuffer和计数器。
                // 然后使用 ComputeBuffer.CopyCount 将 visibleInstanceBuffer 中属于当前模型的实例数量拷贝到 modelArgsBuffers[i] 的相应参数位置。
                // 例如：ComputeBuffer.CopyCount(visibleInstanceBufferForModelI, modelArgsBuffers[i], 4/*字节偏移*/);
            }
        }

        void RenderMeshes()
        {
            // 确保材质拥有正确的数据缓冲区
            material.SetBuffer("_InstanceDataBuffer", modelMatrixBuffer);

            // 绘制每个模型
            for (int i = 0; i < meshes.Length; i++)
            {
                // 这里需要确保每个modelArgsBuffers[i]中的实例数量已被Compute Shader更新
                Graphics.DrawMeshInstancedIndirect(
                    meshes[i],
                    0,
                    material,
                    new Bounds(Vector3.zero, areaSize * 2), // 粗略的包围盒
                    modelArgsBuffers[i] // 这个buffer包含了更新后的实例数量
                );
            }
        }

        void OnDestroy()
        {
            // 释放所有ComputeBuffer
            modelMatrixBuffer?.Release();
            visibleInstanceBuffer?.Release();
            if (modelArgsBuffers != null)
            {
                foreach (var buffer in modelArgsBuffers)
                {
                    buffer?.Release();
                }
            }
        }
    }
}