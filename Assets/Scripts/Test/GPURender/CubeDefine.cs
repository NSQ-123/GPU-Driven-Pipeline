using UnityEngine;

namespace Game.Test
{
    public class CubeDefine
    {
        public static Mesh CreateCubeMeshData()
        {
            Mesh mesh = new Mesh();
            //cube的顶点
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
            };

            // 立方体的12个三角形（每个面2个三角形，共6个面）
            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, // 前面
                1, 2, 6, 6, 5, 1, // 右面
                4, 5, 6, 6, 7, 4, // 后面
                0, 7, 3, 0, 4, 7, // 左面
                0, 1, 5, 5, 4, 0, // 底面
                2, 3, 7, 7, 6, 2  // 顶面
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();//计算法线
            //mesh.RecalculateBounds();//计算边界
            //mesh.RecalculateTangents();//计算切线
            return mesh;
        }


        public static void CreateCubeMeshData(Mesh mesh,ComputeBuffer positionsBuffer,ComputeBuffer indexBuffer)
        {
            var vertices = new Vector3[positionsBuffer.count];
            positionsBuffer.GetData(vertices);

            var indices = new int[indexBuffer.count];
            indexBuffer.GetData(indices);

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            mesh.RecalculateNormals();
            //mesh.RecalculateBounds();
            //mesh.RecalculateTangents();
        }
        
    }
}