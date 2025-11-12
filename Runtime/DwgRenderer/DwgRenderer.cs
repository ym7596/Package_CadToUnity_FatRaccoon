using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Types.Units;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CadToUnityPlugin
{
	public enum DwgFilePath
	{
		DataPath,
		PersistentDataPath,
		StreamingAssetsPath,
		CustomPath,
	}
	
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class DwgRenderer : MonoBehaviour
	{
		public bool IsLoaded => meshFilter && meshFilter.mesh;

		public Color BaseColor
		{
			get => _baseColor;
			set => SetColor(value);
		}

		private Color _baseColor;

		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;
		
		public float Unit => _unit;
		[SerializeField] private float _unit;
		[SerializeField] private DwgPluginSetting _setting;

		public bool isAutoLoad = true;
		public DwgFilePath pathType = DwgFilePath.DataPath;
		public string filePath;

		private bool _isProcessing = false;
		private Coroutine _drawCoroutine;
		
		private readonly Dictionary<EntityType, Transform> _entityRoots = new Dictionary<EntityType, Transform>();
		private readonly Dictionary<EntityType, List<DwgEntity>> _loadedEntities = new Dictionary<EntityType, List<DwgEntity>>();

		#region Unity LifeCycle
		private void Reset()
		{
			if (meshRenderer == false)
			{
				meshRenderer = GetComponent<MeshRenderer>();
				meshRenderer.lightProbeUsage = LightProbeUsage.Off;
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
				meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			}
			
			if (meshFilter == false)
				meshFilter = GetComponent<MeshFilter>();
		}

		private void OnDisable()
		{
			if(_drawCoroutine != null)
				StopCoroutine(_drawCoroutine);

			_drawCoroutine = null;
		}

		private void Start()
		{
			if (isAutoLoad == false)
				return;
			
			Draw();
		}
		
		#endregion

		#region Public Methods
		
		public void SetColor(Color color)
		{
			var material = meshRenderer ? meshRenderer.material : null;

			if (material)
				meshRenderer.material.color = color;
			
			_baseColor = color;
		}

		public void Clear()
		{
			DestroyEntity();
		}
		
		#endregion
		
		#region Draw
		
		public void Draw()
		{
			Draw(pathType, filePath);
		}

		public void Draw(DwgFilePath dwgFilePathType, string dwgFilePath)
		{
			if (_isProcessing || string.IsNullOrEmpty(filePath))
				 return;
			
			var path = GetFilePath(dwgFilePathType, dwgFilePath);

			if (File.Exists(path) == false)
			{
				Debug.Log($"dwg file not found. path : {path}");
				return;
			}
			
			var cadDocument = ReadDwg(path);

			if (cadDocument is null)
			{
				Debug.Log("Failed to read the file");
				return;
			}
			
			//그리기
			DrawCadDocument(cadDocument);
		}

		public void Draw(byte[] dwgData)
		{
			if (_isProcessing || dwgData is null || dwgData.Length == 0)
				return;
			
			var cadDocument = ReadDwg(dwgData);
			
			//그리기
			DrawCadDocument(cadDocument);
		}
		
		private void DrawCadDocument(CadDocument cadDocument)
		{
			if (cadDocument is null || _isProcessing == true)
				return;

			_isProcessing = true;
			
			if(IsLoaded)
				DestroyEntity();
			
			_drawCoroutine = StartCoroutine(DrawCadDocumentCo(cadDocument));
		}

		private IEnumerator DrawCadDocumentCo(CadDocument cadDocument)
		{
			_unit = GetUnit(cadDocument);
			
			var entities = cadDocument.Entities;

			foreach (var entity in entities)
			{
				var dwgEntity = DrawEntity(entity);
				
				if(dwgEntity == false)
					continue;
				
				yield return null;
			}

			if (meshFilter == false)
			{
				_drawCoroutine = null;
				yield break;
			}
			
			//메시로 만들기
			if(meshFilter.mesh)
				Destroy(meshFilter.mesh);
				
			var mesh = BakeMesh();
			
			if (mesh)
				meshFilter.mesh = mesh;
			
			var ignoreEntities = new List<EntityType>() {EntityType.TextEntity, EntityType.MText};
			
			#if UNITY_EDITOR
			DisableEntity(ignoreEntities);
			#else
			DestroyEntity(ignoreEntities);
			#endif
			
			SetColor(_setting.defaultColor);
			
			_isProcessing = false;
			_drawCoroutine = null;
		}
		
		public async Awaitable<bool> DrawAsync(byte[] dwgData, CancellationToken cancellationToken = default)
		{
			if (_isProcessing || dwgData is null || dwgData.Length == 0 || meshFilter == false)
				return false;

			_isProcessing = true;
			
			var cadDocument = ReadDwg(dwgData);

			try
			{
				var result = await DrawCadDocumentAsync(cadDocument, cancellationToken);
				_isProcessing = false;

				//그리기
				return result;
			}
			catch (Exception e)
			{
				Debug.Log($"DrawAsync failed: {e}");
				_isProcessing = false;
				return false;
			}
		}
		
		private async Awaitable<bool> DrawCadDocumentAsync(CadDocument cadDocument, CancellationToken cancellationToken = default)
		{
			_unit = GetUnit(cadDocument);
			
			var entities = cadDocument.Entities;

			foreach (var entity in entities)
			{
				var dwgEntity = DrawEntity(entity);
				
				if(dwgEntity == false)
					continue;

			 	await Awaitable.NextFrameAsync(cancellationToken);
			}
			
			//메시로 만들기
			if(meshFilter.mesh)
				Destroy(meshFilter.mesh);
				
			var mesh = BakeMesh();
			
			if (mesh)
				meshFilter.mesh = mesh;
			
			var ignoreEntities = new List<EntityType>() {EntityType.TextEntity, EntityType.MText};
			
			#if UNITY_EDITOR
			DisableEntity(ignoreEntities);
			#else
			DestroyEntity(ignoreEntities);
			#endif
			
			SetColor(_setting.defaultColor);

			return true;
		}
		
		#endregion
		
		#region DwgRead

		private CadDocument ReadDwg(string path)
		{
			return DwgReader.Read(path);
		}

		private CadDocument ReadDwg(byte[] data)
		{
			using var stream = new MemoryStream(data);
			return DwgReader.Read(stream, new DwgReaderConfiguration());
		}
		
		#endregion

		#region DrawEntity

		private DwgEntity DrawEntity(Entity entity)
		{
			var type = EntityToEntityType(entity);

			if (type == EntityType.None)
			 	return null;
			
			var entityObj = CreateEntity(type);

			if (entityObj == false)
				return null;
			
			var entitySetting = _setting.GetEntitySetting(type);

			if (entitySetting != null)
				entitySetting.defaultColor = _setting.defaultColor;
			
			SetDwgEntity(type, entityObj);
			
			entityObj.Draw(_unit, entity, entitySetting);

			return entityObj;
		}

		private EntityType EntityToEntityType(Entity entity)
		{
			var type = entity.GetType();

			if (type == typeof(Line))
				return EntityType.Line;
			if (type == typeof(LwPolyline))
				return EntityType.LwPolyline;
			if (type == typeof(Arc))
				return EntityType.Arc;
			if (type == typeof(Circle))
				return EntityType.Circle;
			if (type == typeof(TextEntity))
				return EntityType.TextEntity;
			if (type == typeof(MText))
				return EntityType.MText;

			return EntityType.Block;
		}

		private Transform GetEntityRoot(EntityType entityType)
		{
			if (_entityRoots.TryGetValue(entityType, out var entityRoot))
				return entityRoot;

			var rootName = $"{Enum.GetName(typeof(EntityType), entityType)} Root";
			
			entityRoot = new GameObject(rootName).transform;
			entityRoot.transform.SetParent(transform);
			_entityRoots.Add(entityType, entityRoot.transform);

			return entityRoot;
		}

		private DwgEntity CreateEntity(EntityType entityType)
		{
			var prefab = _setting.GetEntityPrefab(entityType);

			return prefab == false ? null : Instantiate(prefab, GetEntityRoot(entityType));
		}
		
		#endregion
		
		#region BakeMesh

		private UnityEngine.Mesh BakeMesh()
		{
			var meshes = new List<CombineInstance>();
			
			foreach (var entityGroup in _loadedEntities)
			{
				if(IsLineEntity(entityGroup.Key) == false)
					continue;
				
				foreach (var entity in entityGroup.Value)
				{
					if (entity is not DwgLineEntity lineEntity)
						continue;
					
					var mesh = lineEntity.BakeMesh();
						
					if(mesh == false)
						continue;
						
					meshes.Add(new CombineInstance()
					{
						mesh = mesh,
						transform = lineEntity.transform.localToWorldMatrix,
					});
				}
			}
			
			var combineMesh = new UnityEngine.Mesh();
			combineMesh.CombineMeshes(meshes.ToArray(), true, true);

			return combineMesh;
		}
		
		#endregion
		
		#region Utils

		private float GetUnit(CadDocument cadDocument)
		{
			var units = cadDocument.Header.InsUnits;

			switch (units)
			{
				case UnitsType.Millimeters:
					return 0.001f;
				case UnitsType.Centimeters:
					return 0.01f;
				case UnitsType.Meters:
					return 1f;
				case UnitsType.Kilometers:
					return 1000f;
				case UnitsType.Inches:
					return 25.4f;
				default:
					return 0;
			}
		}

		private bool IsLineEntity(EntityType entityType)
		{
			switch (entityType)
			{
				case EntityType.Arc:
				case EntityType.Circle:
				case EntityType.Line:
				case EntityType.LwPolyline:
					return true;
				default:
					return false;
			}
		}

		private string GetFilePath(DwgFilePath dwgFilePathType, string dwgFilePath)
		{
			var length = dwgFilePath.Length;

			var extension = dwgFilePath.Substring(length - 4);
			
			if(extension.Equals(".dwg", StringComparison.OrdinalIgnoreCase) == false)
				dwgFilePath += ".dwg";
			
			switch (dwgFilePathType)
			{
				case DwgFilePath.DataPath:
					return Path.Combine(Application.dataPath, dwgFilePath);
				case DwgFilePath.PersistentDataPath:
					return Path.Combine(Application.persistentDataPath, dwgFilePath);
				case DwgFilePath.StreamingAssetsPath:
					return Path.Combine(Application.streamingAssetsPath, dwgFilePath);
				case DwgFilePath.CustomPath:
					return dwgFilePath;
				default:
					return string.Empty;
			}
		}
		
		#endregion

		#region LoadedDwgEntity
		
		private void SetDwgEntity(EntityType entityType, DwgEntity dwgEntity)
		{
			if (_loadedEntities.TryGetValue(entityType, out var entities) == false)
			{
				entities = new List<DwgEntity>();
				_loadedEntities.Add(entityType, entities);
			}
			
			entities.Add(dwgEntity);
		}

		private void DisableEntity(List<EntityType> ignoreTypes = null)
		{
			foreach (var root in _entityRoots)
			{
				var key = root.Key;
				
				if(ignoreTypes != null && ignoreTypes.Contains(key))
					continue;
				
				root.Value.gameObject.SetActive(false);
			}
		}

		private void DestroyEntity(List<EntityType> ignoreTypes = null)
		{
			var removeTypes = new List<EntityType>();
			
			foreach (var root in _entityRoots)
			{
				var key = root.Key;
				
				if(ignoreTypes != null && ignoreTypes.Contains(key))
					continue;
				
				Destroy(root.Value.gameObject);
				removeTypes.Add(key);
			}

			foreach (var removeType in removeTypes)
			{
				_entityRoots.Remove(removeType);
				_loadedEntities.Remove(removeType);
			}
		}
		
		#endregion

		#region Custom Editor
		#if UNITY_EDITOR
		
		public Dictionary<EntityType, List<DwgEntity>> GetLoadedEntities() => _loadedEntities;
		
		#endif
		#endregion
	}
	
	#region DwgRenderer Custom Editor
	#if UNITY_EDITOR
	[CustomEditor(typeof(DwgRenderer))]
	public class DwgRendererEditor : Editor
	{
		private DwgRenderer _dwgRenderer;
		private bool _isShow = false;
		
		private readonly Dictionary<EntityType, bool> _isShowEntites = new Dictionary<EntityType, bool>();
		
		private void OnEnable()
		{
			_dwgRenderer = target as DwgRenderer;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			var entityGroups = _dwgRenderer.GetLoadedEntities();
			
			if (entityGroups is  null || entityGroups.Count == 0)
				return;
			
			EditorGUILayout.Space();
			_isShow = EditorGUILayout.Foldout(_isShow, "Loaded Entities");

			if (_isShow)
			{
				foreach (var group in entityGroups)
				{
					EditorGUI.indentLevel++;

					var key = group.Key;

					var isEntityShow = false;
					
					if(_isShowEntites.TryGetValue(key, out isEntityShow) == false)
						_isShowEntites.Add(key, isEntityShow);
					
					isEntityShow = EditorGUILayout.Foldout(isEntityShow, $"{group.Key} List");

					if (isEntityShow)
					{
						GUI.enabled = false;
						
						foreach (var entity in group.Value)
						{
							EditorGUILayout.ObjectField(entity, typeof(DwgEntity), false);
						}
						
						GUI.enabled = true;
					}
					
					EditorGUI.indentLevel--;
					
					_isShowEntites[key] = isEntityShow;
				}
			}
		}
	}
	#endif
	#endregion
}


