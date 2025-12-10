using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;
using System.Runtime.InteropServices;

public class traceinjector : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture Skybox;
    // add material kernels
    // add raycast kernels
    public int RaysPerPixel;
    public int numBounces;

    private RenderTexture _target;
    private int rtsIndex;
    Camera _camera;

    private List<RayTracingObject> _rtMeshes = new List<RayTracingObject>();
    struct MeshObject
    {
        public Matrix4x4 transform;
        public int indexOffset;
        public int indexCount;
        public RayTracingMaterial rtm;
    };
    private MeshObject[] _meshHandles;
    private ComputeBuffer _meshBuffer = null;
    private ComputeBuffer _vertexBuffer = null;
    private ComputeBuffer _indexBuffer = null;

    private List<ImplicitObject> _rtImplicits = new List<ImplicitObject>();
    struct Sphere
    {
        public Vector3 pos;
        public float radius;
        public RayTracingMaterial rtm;
    };
    private Sphere[] _implicitsHandles;
    private ComputeBuffer _implicitsBuffer = null;

    private bool geo_rebuild = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rtsIndex = rayTracingShader.FindKernel("Raycast");
        _camera = GetComponent<Camera>();

        _rtMeshes = new List<RayTracingObject>(FindObjectsByType<RayTracingObject>(FindObjectsSortMode.None));
        _rtImplicits = new List<ImplicitObject>(FindObjectsByType<ImplicitObject>(FindObjectsSortMode.None));

    }

    public void RegisterObject(RayTracingObject obj)
    {
        _rtMeshes.Add(obj);
    }

    public void UnegisterObject(RayTracingObject obj)
    {
        _rtMeshes.Remove(obj);
    }

    private void RebuildGeometryStructures() // this is where i would implement bvh construction if i had time
    {
        if (!geo_rebuild) {
            return;
        }
        geo_rebuild = false;

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        _meshHandles = new MeshObject[_rtMeshes.Count];

        for(int i = 0; i < _rtMeshes.Count; i++)
        {
            Mesh mesh = _rtMeshes[i].GetComponent<MeshFilter>().sharedMesh;

            // Add vertex data
            int firstVertex = vertices.Count;
            vertices.AddRange(mesh.vertices);

            // Add index data - if the vertex buffer wasn't empty before, the
            // indices need to be offset
            int firstIndex = indices.Count;
            var mesh_indices = mesh.GetIndices(0);
            indices.AddRange(mesh_indices.Select(index => index + firstVertex));

            // Add the object itself
            _meshHandles[i] = new MeshObject()
            {
                transform = _rtMeshes[i].transform.localToWorldMatrix,
                indexOffset = firstIndex,
                indexCount = indices.Count,
                rtm = _rtMeshes[i].material
            };
        }
        //foreach(var v in vertices)
        //{
        //    Debug.Log(v);
        //}

        //foreach (var i in indices)
        //{
        //    Debug.Log(i);
        //}
        CreateComputeBuffer(ref _meshBuffer, new List<MeshObject>(_meshHandles), 112);
        CreateComputeBuffer(ref _vertexBuffer, vertices, 12);
        CreateComputeBuffer(ref _indexBuffer, indices, 4);

        _implicitsHandles = new Sphere[_rtImplicits.Count];
        for (int i = 0; i < _rtImplicits.Count; i++)
        {
            RayTracingMaterial material = _rtImplicits[i].material;
            _implicitsHandles[i] = new Sphere()
            {
                pos = _rtImplicits[i].transform.position,
                radius = _rtImplicits[i].transform.localScale.x,
                rtm = material

            };
        }
        CreateComputeBuffer(ref _implicitsBuffer, new List<Sphere>(_implicitsHandles), 56);

    }

    private void UpdateGeometryStructures() // only cheap operations allowed!
    {
        if (_meshBuffer != null)
        {
            for (int i = 0; i < _rtMeshes.Count; i++)
            {
                _meshHandles[i].transform = _rtMeshes[i].transform.localToWorldMatrix;
            } 
            _meshBuffer.SetData(_meshHandles);
        }

        if (_implicitsBuffer != null)
        {
            for (int i = 0; i < _rtImplicits.Count; i++)
            {
                _implicitsHandles[i].pos = _rtImplicits[i].transform.position;
                _implicitsHandles[i].radius = _rtImplicits[i].transform.localScale.x;
            }
            _implicitsBuffer.SetData(_implicitsHandles);
        }
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }

            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void initRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            Debug.Log("init rt");
            // Release render texture if we already have one
            if (_target != null)
            {
                _target.Release();
            }

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private void setShaderParameters()
    {
        if(_implicitsBuffer != null)
        {
            rayTracingShader.SetBuffer(rtsIndex, "spheres", _implicitsBuffer);
            rayTracingShader.SetInt("sphereCount", _implicitsHandles.Length);
        }
        else
        {
            rayTracingShader.SetInt("sphereCount", 0);
        }

        if (_meshBuffer != null)
        {
            rayTracingShader.SetBuffer(rtsIndex, "meshes", _meshBuffer);
            rayTracingShader.SetBuffer(rtsIndex, "vertices", _vertexBuffer);
            rayTracingShader.SetBuffer(rtsIndex, "indices", _indexBuffer);
            rayTracingShader.SetInt("meshCount", _meshHandles.Length);
        }
        else
        {
            ComputeBuffer meshBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(MeshObject)));
            rayTracingShader.SetBuffer(rtsIndex, "meshes", meshBuffer);
            ComputeBuffer vbuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(Vector3)));
            rayTracingShader.SetBuffer(rtsIndex, "vertices", meshBuffer);
            ComputeBuffer ibuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(int)));
            rayTracingShader.SetBuffer(rtsIndex, "indices", meshBuffer);
            rayTracingShader.SetInt("meshCount", 0);
        }

        rayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(rtsIndex, "_SkyboxTexture", Skybox);
        rayTracingShader.SetTexture(rtsIndex, "Result", _target);
        //rayTracingShader.SetInt("StartSeed", 123456);
        rayTracingShader.SetInt("numRays", RaysPerPixel);
        rayTracingShader.SetInt("numBounces", numBounces);

        rayTracingShader.SetInt("StartSeed", UnityEngine.Random.Range(0, 9999999));
    }

    Material _taaMaterial = null;
    float _currentSample = 0;
    private void render(RenderTexture destination)
    {
        rayTracingShader.Dispatch(rtsIndex, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);
        if (_taaMaterial == null)
        {
            _taaMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        _taaMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _taaMaterial);
        _currentSample++;
    }


    public bool accumulate = false;

    private void Update()
    {
        if (!accumulate)
        {
            _currentSample = 0;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RebuildGeometryStructures();
        UpdateGeometryStructures();
        initRenderTexture();
        setShaderParameters();
        render(destination);
    }
}
