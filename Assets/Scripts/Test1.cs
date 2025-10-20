using System;
using UnityEngine;

namespace Game
{
    public class Test1: MonoBehaviour
    {
        private void Start()
        {
            Matrix4x4[] matrices = Define.GetRandomMatrix4x4(100, new Vector3(1.1f, 1.1f, 0f));
            for (int i = 0; i < matrices.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                
                // 从矩阵中提取位置、旋转、缩放信息并应用到go
                go.transform.position = matrices[i].GetColumn(3); // 位置
                go.transform.rotation = matrices[i].rotation;      // 旋转
                go.transform.localScale = matrices[i].lossyScale;  // 缩放
            }
        }
    }
}