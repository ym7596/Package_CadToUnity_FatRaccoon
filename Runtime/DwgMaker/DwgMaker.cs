using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Types.Units;
using UnityEngine;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;
using Mesh = UnityEngine.Mesh;

namespace CadToUnityPlugin
{
    public enum LoadType
    {
        StreamingAssets = 0,
        Download,
        FilePicker
    }

    public enum DrawType
    {
        Sync = 0,
        Async,
    }

    [Serializable]
    public class DwgData
    {
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public EntityType type;
        public List<GameObject> entities = new();
    }

    public class DwgMaker : MonoBehaviour
    {
        [Header("Setting Scriptable Object")] 
        public DwgPluginSetting pluginSetting;

        [Header("Result")] 
        public GameObject dwgObject;
        // public List<DwgData> entityData = new();
        
        private Mesh _bakedMesh;
        private float _unit = 0f;

        private DwgLoader _dwgLoader;
        private DwgDrawer _dwgDrawer;
        
        private string _previousFileNameOrUrl;

        public Mesh BakedMesh => _bakedMesh;
        public float Unit => _unit;

        [Header("Debug")] 
        public bool autoLoad;
        public LoadType loadType;
        [HideInInspector] public DrawType drawType;
        [HideInInspector] public string fileName; // StreamingAssets folder sample : SDC_A4_Right_01_22.02.17_1
        [HideInInspector] public string url; // Url sample : https://seerslabcj.github.io/gltfTest/SDC_A4_Right_01_22.02.17_1.dwg

#if UNITY_EDITOR
        [HideInInspector] public bool showDwg;
        [HideInInspector] public bool showBoundingBox;
#endif

        private Material _dwgMaterial;
        
        protected void Start()
        {
            if (!autoLoad) return;

            var fileNameOrUrl = loadType == LoadType.StreamingAssets ? fileName : url;
            _ = MakeDwgAsync(fileNameOrUrl, loadType, drawType);
        }

        public async Awaitable<GameObject> MakeDwgAsync(string fileNameOrUrl, LoadType newLoadType, DrawType newDrawType)
        {
            var cadDocument = await LoadCadDocumentAsync(fileNameOrUrl, newLoadType);
            var dwgRawObject = await DrawDwgObjectAsync(pluginSetting, cadDocument, newDrawType);

            _bakedMesh = BakeMesh(dwgRawObject);
            
            dwgObject = CreateDwgMeshObject(dwgObject, _bakedMesh);
            #if UNITY_EDITOR
            showDwg = true;
            #endif
            
            return dwgObject;
        }

        /// <summary>
        /// DWG 파일 정보(CadDocument) 로드
        /// </summary>
        private async Awaitable<CadDocument> LoadCadDocumentAsync(string fileNameOrUrl, LoadType newLoadType)
        {
            if (_previousFileNameOrUrl == fileNameOrUrl)
            {
                Debug.LogWarning($"{fileNameOrUrl} is loading or already loaded.");
                return null;
            }

            if (_previousFileNameOrUrl != null)
            {
                Debug.LogWarning($"Delete {_previousFileNameOrUrl} and Load {fileNameOrUrl}");
                Clear();
            }
            _previousFileNameOrUrl = fileNameOrUrl;

            _dwgLoader ??= new DwgLoader();
            var cadDocument = newLoadType switch
            {
                LoadType.StreamingAssets => await _dwgLoader.LoadStreamingAssetsFolderDwgAsync(fileNameOrUrl),
                LoadType.Download => await _dwgLoader.LoadRemoteDwgAsync(fileNameOrUrl),
                _ => throw new ArgumentOutOfRangeException(nameof(newLoadType), newLoadType, null)
            };

            if (cadDocument == null)
            {
                throw new Exception("cadDocument is null");
            }
            return cadDocument;
        }
        
        /// <summary>
        /// DWG 데이터 비동기 그리기
        /// </summary>
        private async Awaitable<GameObject> DrawDwgObjectAsync(DwgPluginSetting newDwgPluginSetting, CadDocument cadDocument, DrawType newDrawType)
        {
            _dwgDrawer ??= new DwgDrawer();
            _unit = GetUnit(cadDocument);
            
            dwgObject = newDrawType switch
            {
                DrawType.Sync => _dwgDrawer.Draw(newDwgPluginSetting, cadDocument, _unit),
                DrawType.Async => await _dwgDrawer.DrawAsync(newDwgPluginSetting, cadDocument, _unit),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (dwgObject is null)
            {
                throw new Exception("dwgObject is null");
            }

            return dwgObject;
        }
        
        /// <summary>
        /// mesh 굽기
        /// </summary>
        private Mesh BakeMesh(GameObject dwgRawObject)
        {
            var allLineRenderers = dwgRawObject.GetComponentsInChildren<LineRenderer>();
            var combineInstances = new List<CombineInstance>();

            foreach (var lr in allLineRenderers)
            {
                if (lr.positionCount < 2) continue;

                var bakedMesh = new Mesh();
                lr.BakeMesh(bakedMesh, true);

                var ci = new CombineInstance
                {
                    mesh = bakedMesh,
                    transform = Matrix4x4.identity
                };

                combineInstances.Add(ci);
            }

            var combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            return combinedMesh;
        }

        /// <summary>
        /// dwg mesh object 생성
        /// </summary>
        private GameObject CreateDwgMeshObject(GameObject newDwgObject, Mesh newMesh)
        {
            if (newDwgObject)
                Destroy(newDwgObject);
            
            var dwgMeshObject = new GameObject("CombinedLineRendererMesh");
            var meshFilter = dwgMeshObject.AddComponent<MeshFilter>();
            meshFilter.mesh = newMesh;
            var meshRenderer = dwgMeshObject.AddComponent<MeshRenderer>();
            
            if (_dwgMaterial is null)
                _dwgMaterial = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.material = _dwgMaterial;
            return dwgMeshObject;
        }
        
        public void ChangeColor(Color color)
        {
            if (dwgObject)
                _dwgMaterial.color = color;
        }

        public void ShowDwg(bool show)
        {
            if (dwgObject)
                dwgObject.SetActive(show);
        }

        public void Clear()
        {
            if (dwgObject)
            {
                Destroy(dwgObject);
                dwgObject = null;

                // entityData.Clear();
                _dwgMaterial.color = Color.white;
                _bakedMesh.Clear();
                #if UNITY_EDITOR
                showBoundingBox = false;
                #endif
                _unit = 0f;
            }

            _previousFileNameOrUrl = null;
        }

        /// <summary>
        /// dwg 에 저장된 단위로 unity 단위 환산 상수 설정 
        /// </summary>
        private float GetUnit(CadDocument cadDocument)
        {
            var units = cadDocument.Header.InsUnits;
            Debug.Log($"Units => {units}");
            var unit = units switch
            {
                UnitsType.Millimeters => 0.001f,
                UnitsType.Centimeters => 0.01f,
                UnitsType.Meters => 1f,
                UnitsType.Kilometers => 10f,
                _ => 0f,
            };
            return unit;
        }

        #region Draw Bounding Box

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (showBoundingBox)
            {
                var boundingBox = GetBoundingBox();
                var bottomLeft = boundingBox.bottomLeft;
                var topRight = boundingBox.topRight;

                var topLeft = new Vector3(boundingBox.bottomLeft.x, 0, boundingBox.topRight.z);
                var bottomRight = new Vector3(boundingBox.topRight.x, 0, boundingBox.bottomLeft.z);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(bottomLeft, topLeft);
                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topRight, bottomRight);
            }
        }

        /// <summary>
        /// 모든 enitity 를 포함한 boundingBox 를 가져옴
        /// </summary>
        private (Vector3 bottomLeft, Vector3 topRight) GetBoundingBox()
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            // var entitiesRoot = rootObjects.Where(o => o.name == "EntitiesRoot").FirstOrDefault()?.transform;
            var entitiesRoot = rootObjects.FirstOrDefault(o => o.name == "EntitiesRoot")?.transform;

            var bottomLeft = new Vector3(float.MaxValue, 0, float.MaxValue);
            var topRight = new Vector3(float.MinValue, 0, float.MinValue);

            if (entitiesRoot != null)
            {
                var entityProperties = entitiesRoot.GetComponentsInChildren<EntityProperty>().ToList();
                foreach (var entityProperty in entityProperties)
                {
                    bottomLeft = GetMinVector3(bottomLeft, entityProperty.bottomLeft);
                    topRight = GetMaxVector3(topRight, entityProperty.topRight);
                }
            }

            return (bottomLeft, topRight);
        }

        private Vector3 GetMinVector3(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Min(a.x, b.x), 0, Math.Min(a.z, b.z));
        }

        private Vector3 GetMaxVector3(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Max(a.x, b.x), 0, Math.Max(a.z, b.z));
        }
#endif

        #endregion
    }
}