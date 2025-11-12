using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CadToUnityPlugin
{
    /// <summary>
    /// 생성중인 Entity type
    /// </summary>
    public enum EntityType
    {
        None = -1,
        Line = 0,
        LwPolyline,
        Arc,
        Circle,
        TextEntity,
        MText,
        Block = 200,
    }

    [Serializable]
    public class EntitySetting
    {
        public EntityType type;
        public bool useCustomColor;
        public Color color;
        [HideInInspector] public Color defaultColor;

        public Color GetColor() => useCustomColor ? color : defaultColor;
    }

    [Serializable]
    public class LineSetting : EntitySetting
    {
        [Range(0f, 10f)] public float lineWidth;
        public Material material;
    }
    
    [Serializable]
    public class CurveSetting : LineSetting
    {
        public int segment;
    }

    [Serializable]
    public class TextSetting : EntitySetting
    {
        [Range(1, 50)] public int fontSize;
    }

    [Serializable]
    public class EntityPrefab
    {
        public EntityType type;
        public DwgEntity prefab;
    }

    /// <summary>
    /// dwg 데이터 표출 설정
    /// </summary>
    [CreateAssetMenu(fileName = "DwgPluginSetting", menuName = "DwgPlguin/PluginSetting")]
    public class DwgPluginSetting : ScriptableObject
    {
        [Header("Common")] 
        public Material defaultMaterial;

        public Color defaultColor = Color.white;
        
        [Space(10)] [SerializeReference]
        public List<EntitySetting> entitySettings;
        
        [SerializeField] private List<EntityPrefab> _entityPrefabs; 

        #if UNITY_EDITOR
        public void Initialize()
        {
            if (entitySettings.Count > 0)
                return;

            entitySettings = new List<EntitySetting>();

            foreach (EntityType entity in Enum.GetValues(typeof(EntityType)))
            {
                EntitySetting setting = entity switch
                {
                    EntityType.Line or EntityType.LwPolyline => new LineSetting
                    {
                        type = entity,
                        material = defaultMaterial,
                        lineWidth = 0.1f
                    },
                    EntityType.Arc or EntityType.Circle => new CurveSetting
                    {
                        type = entity,
                        material = defaultMaterial,
                        lineWidth = 0.1f,
                        segment = 20
                    },
                    EntityType.TextEntity or EntityType.MText => new TextSetting
                    {
                        type = entity,
                        fontSize = 2
                    },
                    _ => null
                };

                if (setting == null)
                    continue;

                setting.useCustomColor = false;
                setting.color = Color.white;
                entitySettings.Add(setting);
            }
        }
        #endif
        
        public EntitySetting GetEntitySetting(EntityType type)
        {
            return entitySettings.FirstOrDefault(setting => setting.type == type);
        }

        public DwgEntity GetEntityPrefab(EntityType type)
        {
            return _entityPrefabs.FirstOrDefault(prefab => prefab.type == type)?.prefab;
        }
    }
}