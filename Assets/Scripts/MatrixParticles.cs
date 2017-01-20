using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MatrixParticle
{
    public struct _Particle
    {
        Vector3 position;
        Vector3 direction;
        Vector3 scale;
        Vector2 uv;
        Vector4 color;
        float lifeTime;
    };
    public class MatrixParticles : MonoBehaviour
    {
        const int VERTEX_MAX = 65534;
        public ComputeShader shader;
        public Material mat;
        public Mesh mesh;
        [SerializeField]
        private int xMod = 1, yMod = 1, zMod = 1;
        [SerializeField]
        private Vector3 scale = Vector3.one;
        private ComputeBuffer particlesBuffer;
        private int initKernal, updateKernal, emitKernal;
        private int maxKernal;
        private List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();
        private int perMeshNum, comMeshNum;
        private Mesh combinedMesh;
        void Start()
        {
            maxKernal = xMod * yMod * zMod * 1000;
            shader.SetInt("_xMod", xMod * 10);
            shader.SetInt("_yMod", yMod * 10);
            shader.SetInt("_zMod", zMod * 10);
            shader.SetVector("_Scale", scale);

            initKernal = shader.FindKernel("Init");
            updateKernal = shader.FindKernel("Update");
            emitKernal = shader.FindKernel("Emit");

            particlesBuffer = new ComputeBuffer(maxKernal, Marshal.SizeOf(typeof(_Particle)), ComputeBufferType.Default);
            CreateMesh();

            InitParticles();
        }
        void CreateMesh()
        {
            perMeshNum = VERTEX_MAX / mesh.vertexCount;
            comMeshNum = (int)Mathf.Ceil((float)maxKernal / perMeshNum);
            combinedMesh = CreateCombinedMesh(mesh, perMeshNum);
            for (int i = 0; i < comMeshNum; i++)
            {
                MaterialPropertyBlock property = new MaterialPropertyBlock();
                property.SetFloat("_Offset", perMeshNum * i);
                propertyBlocks.Add(property);
            }
        }

        void Update()
        {
            UpdateParticles();
            DrawParticles(Camera.main);
#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView)
            {
                DrawParticles(SceneView.lastActiveSceneView.camera);
            }
#endif
        }
        void InitParticles()
        {
            shader.SetBuffer(initKernal, "_Particles", particlesBuffer);
            shader.Dispatch(initKernal, xMod, yMod, zMod);
        }
        void UpdateParticles()
        {
            shader.SetFloat("_Time", Time.deltaTime);
            shader.SetBuffer(updateKernal, "_Particles", particlesBuffer);
            shader.Dispatch(updateKernal, xMod, yMod, zMod);
        }
        public void EmitParticles(Vector3 pos, float height)
        {
            shader.SetVector("_Pos", pos);
            shader.SetFloat("_Height", -height);
            shader.SetBuffer(emitKernal, "_Particles", particlesBuffer);
            shader.Dispatch(emitKernal, xMod, yMod, zMod);
        }
        void DrawParticles(Camera camera)
        {
            mat.SetBuffer("_Particles", particlesBuffer);
            for (int i = 0; i < comMeshNum; ++i)
            {
                var props = propertyBlocks[i];
                props.SetFloat("_IdOffset", perMeshNum * i);
                Graphics.DrawMesh(combinedMesh, transform.position, transform.rotation, mat, 0, camera, 0, props);
            }
            //Graphics.DrawProcedural(MeshTopology.Points, maxKernal);
        }
        void OnDestroy()
        {
            particlesBuffer.Release();
        }
        Mesh CreateCombinedMesh(Mesh mesh, int num)
        {
            int[] meshIndices = mesh.GetIndices(0);
            int indexNum = meshIndices.Length;

            List<Vector3> verts = new List<Vector3>();
            int[] indices = new int[num * indexNum];
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tans = new List<Vector4>();
            List<Vector2> uv0 = new List<Vector2>();
            List<Vector2> uv1 = new List<Vector2>();
            for (int i = 0; i < num; i++)
            {
                verts.AddRange(mesh.vertices);
                normals.AddRange(mesh.normals);
                tans.AddRange(mesh.tangents);
                uv0.AddRange(mesh.uv);
                for (int n = 0; n < indexNum; n++)
                {
                    indices[i * indexNum + n] = i * mesh.vertexCount + meshIndices[n];
                }
                for (int n = 0; n < mesh.uv.Length; n++)
                {
                    uv1.Add(new Vector2(i, i));
                }
            }
            Mesh combinedMesh = new Mesh();
            combinedMesh.SetVertices(verts);
            combinedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            combinedMesh.SetNormals(normals);
            combinedMesh.SetTangents(tans);
            combinedMesh.SetUVs(0, uv0);
            combinedMesh.SetUVs(1, uv1);
            combinedMesh.RecalculateBounds();
            Vector3 size = new Vector3(xMod * 10 * scale.x, yMod * 10 * scale.y, zMod * 10 * scale.z);
            combinedMesh.bounds = new Bounds(transform.position + size * 0.5f, size);
            return combinedMesh;
        }
    }
}