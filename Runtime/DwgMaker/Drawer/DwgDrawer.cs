using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Blocks;
using ACadSharp.Entities;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace CadToUnityPlugin
{
    public class DwgDrawer
    {
        private float _unit;

        private DwgPluginSetting _pluginSetting;

        private Dictionary<EntityType, Transform> _entityRoots = new();

        private readonly Dictionary<EntityType, EntityGenerator> _entityGenerators = new()
        {
            { EntityType.Line, new LineGenerator() },
            { EntityType.LwPolyline, new LwPolylineGenerator() },
            { EntityType.Arc, new ArcGenerator() },
            { EntityType.Circle, new CircleGenerator() },
            { EntityType.TextEntity, new TextEntityGenerator() },
            { EntityType.MText, new MTextGenerator() },
        };

        private readonly Dictionary<EntityType, Type> _entityTypeDictionary = new()
        {
            { EntityType.Line, typeof(Line) },
            { EntityType.LwPolyline, typeof(LwPolyline) },
            { EntityType.Arc, typeof(Arc) },
            { EntityType.Circle, typeof(Circle) },
            { EntityType.TextEntity, typeof(TextEntity) },
            { EntityType.MText, typeof(MText) }
        };
        
        /// <summary>
        /// dwg 데이터 표출
        /// </summary>
        public GameObject Draw(DwgPluginSetting pluginSetting, CadDocument cadDocument, float unit, bool drawBlock = false)
        {
            InitSetting(pluginSetting, unit);
            var dwgRoot = GenerateRootObjects();
            
            // dwg 파일 실행했을 때의 view 설정
            // 2d 좌표 cadDocument.VPorts.FirstOrDefault()!.Center
            // 카메라로부터 거리 cadDocument.VPorts.FirstOrDefault()!.ViewHeight
            GenerateEntities(cadDocument.Entities, null);

            // 블럭 데이터 그리기
            if (drawBlock)
                _ = DrawBlockEntitiesAsync(cadDocument.Entities);
            else
            {
                if (_entityRoots.TryGetValue(EntityType.Block, out var blockRoot))
                    Object.Destroy(blockRoot.gameObject);
            }

            // 구현 불가 데이터 Hatch -> 생성 포지션 값 확인 불가
            // 데이터 추출 후보군 MLine, Polyline, Point, Ray, XLine, Ellipse
            return dwgRoot;
        }
        
        /// <summary>
        /// 초기 설정
        /// </summary>
        private void InitSetting(DwgPluginSetting pluginSetting, float unit)
        {
            _pluginSetting = pluginSetting;
            _unit = unit != 0 ? unit : 1f;
        }

        /// <summary>
        /// Root 오브젝트 동적 생성
        /// </summary>
        private GameObject GenerateRootObjects()
        {
            var enumValues = Enum.GetValues(typeof(EntityType));
            var entitiesRoot = new GameObject("EntitiesRoot").transform;
            _entityRoots.Clear();
            foreach (EntityType value in enumValues)
            {
                var entityName = Enum.GetName(typeof(EntityType), value);
                var rootTransform = new GameObject($"{entityName}Root").transform;
                rootTransform.SetParent(entitiesRoot);
                _entityRoots.Add(value, rootTransform);
            }

            return entitiesRoot.gameObject;
        }

        /// <summary>
        /// dwg 데이터 표출
        /// </summary>
        public async Awaitable<GameObject> DrawAsync(DwgPluginSetting pluginSetting, CadDocument cadDocument, float unit, bool drawBlock = false)
        {
            InitSetting(pluginSetting, unit);
            var dwgRoot = GenerateRootObjects();
            
            // dwg 파일 실행했을 때의 view 설정
            // 2d 좌표 cadDocument.VPorts.FirstOrDefault()!.Center
            // 카메라로부터 거리 cadDocument.VPorts.FirstOrDefault()!.ViewHeight
            await GenerateEntitiesAsync(cadDocument.Entities, null);
            // 블럭 데이터 그리기
            if (drawBlock)
            {
                await DrawBlockEntitiesAsync(cadDocument.Entities);
            }
            else
            {
                if (_entityRoots.TryGetValue(EntityType.Block, out var blockRoot))
                    Object.Destroy(blockRoot.gameObject);
            }

            // 구현 불가 데이터 Hatch -> 생성 포지션 값 확인 불가
            // 데이터 추출 후보군 MLine, Polyline, Point, Ray, XLine, Ellipse

            return dwgRoot;
        }

        private async Awaitable DrawBlockEntitiesAsync(CadObjectCollection<Entity> entities)
        {
            var inserts = entities.Where(e => e.GetType() == typeof(Insert)).ToList();
            if (inserts.Count > 0)
            {
                await new BlockGenerator().GenerateBlockAsync(inserts, _unit, (entities1, parent) => GenerateEntitiesAsync(entities1, parent), _entityRoots[EntityType.Block]);
            }
        }

        /// <summary>
        /// 단일 Entity 타입별 순차 생성
        /// </summary>
        private void GenerateEntities(CadObjectCollection<Entity> entities, Transform parent)
        {
            if (_pluginSetting is null)
                return;

            var tempEntities = entities.OfType<Entity>().ToList();
            foreach (var (type, generator) in _entityGenerators)
            {
                if (!_entityTypeDictionary.TryGetValue(type, out var entityType))
                    continue;

                var entityList = tempEntities.Where(e => e.GetType() == entityType).ToList();
                var root = parent ?? _entityRoots[type];
                var setting = _pluginSetting.entitySettings.FirstOrDefault(e => e.type == type);

                if (setting == null)
                    continue;

                generator.Generate(entityList, root, setting, _unit);
            }
        }
        
        /// <summary>
        /// 공통 로직을 처리하는 내부 메서드
        /// </summary>
        private async Awaitable GenerateEntitiesAsync(CadObjectCollection<Entity> entities, Transform parent)
        {
            if (_pluginSetting is null)
                return;

            var tempEntities = entities.OfType<Entity>().ToList();
            foreach (var (type, generator) in _entityGenerators)
            {
                if (!_entityTypeDictionary.TryGetValue(type, out var entityType))
                    continue;

                var entityList = tempEntities.Where(e => e.GetType() == entityType).ToList();
                var root = parent ?? _entityRoots[type];
                var setting = _pluginSetting.entitySettings.FirstOrDefault(e => e.type == type);

                if (setting == null)
                    continue;

                await generator.GenerateAsync(entityList, root, setting, _unit);
            }
        }

        #region Use Later maybe

        // Group class 는 NonGraphicalObject 클래스의 자식
        // 용도를 모르겠으나 그리지는 않는 것 같음 
        // private void DrawGroupEntities(GroupCollection groups)
        // {
        //     foreach (var group in groups)
        //     {
        //         var entities = group.Entities;
        //         var groupRoot = new GameObject(group.ToString()).transform;
        //         _ = GenerateEntities(entities, groupRoot, async: false);
        //     }
        // }
        //
        // private async Task GenerateEntities(Dictionary<ulong, Entity> entities, Transform parent,
        //     bool async)
        // {
        //     var tempEntities = entities.Values.ToList();
        //     foreach (var (type, generator) in _entityGenerators)
        //     {
        //         if (!_entityTypeDictionary.TryGetValue(type, out var entityType))
        //             continue;
        //
        //         var entityList = tempEntities.Where(e => e.GetType() == entityType).ToList();
        //         var setting = _pluginSetting.entitySettings.FirstOrDefault(e => e.type == type);
        //
        //         if (setting == null)
        //             continue;
        //
        //         if (async)
        //         {
        //             await generator.GenerateAsync(entityList, parent, setting, _unitConversionConstant);
        //         }
        //         else
        //         {
        //             generator.Generate(entityList, parent, setting, _unitConversionConstant);
        //         }
        //     }
        // }

        #endregion
    }
}