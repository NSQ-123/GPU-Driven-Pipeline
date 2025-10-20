using UnityEngine;

namespace Game
{

    public class Define
    {
        public static Matrix4x4[] GetRandomMatrix4x4(uint count, Vector3 spacing, bool randomRotation = false)
        {
            var matrices = new Matrix4x4[count];
            float zValue = 0f; // 固定的Z值，所有物体在同一个垂直平面

            // 计算方阵的边长（例如100个物体 = 10x10）
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));

            // 计算中心偏移量，使整个方阵以(0,0,0)为中心
            Vector3 centerOffset = new Vector3(
                (gridSize - 1) * spacing.x * 0.5f,
                (gridSize - 1) * spacing.y * 0.5f,
                0f
            );

            for (var i = 0; i < matrices.Length; i++)
            {
                // 计算在方阵中的行列位置
                int row = i / gridSize; // 行索引
                int col = i % gridSize; // 列索引

                // 使用spacing作为间隔，在垂直平面内排列，并减去中心偏移
                Vector3 position = new Vector3(col * spacing.x, row * spacing.y, zValue) - centerOffset;
                var rotation = Quaternion.identity;
                if (randomRotation)
                {
                    rotation = Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f);
                }

                matrices[i] = Matrix4x4.TRS
                (
                    position,
                    rotation,
                    Vector3.one
                );
            }

            return matrices;
        }
    }
}