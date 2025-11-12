#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace CadToUnityPlugin.Editor
{
    [CustomEditor(typeof(DwgPluginSetting))]
    public class DwgPluginSettingEditor : UnityEditor.Editor
    {
        private DwgPluginSetting _setting;
        private SerializedProperty _defaultMaterialProperty;
        private SerializedProperty _defaultColorProperty;
        private SerializedProperty _entitySettingsProperty;
        private SerializedProperty _entityPrefabProperty;
        private ReorderableList _list;

        private static readonly Dictionary<EntityType, Type> _typeMap = new()
        {
            { EntityType.Line, typeof(LineSetting) },
            { EntityType.LwPolyline, typeof(LineSetting) },
            { EntityType.Arc, typeof(CurveSetting) },
            { EntityType.Circle, typeof(CurveSetting) },
            { EntityType.TextEntity, typeof(TextSetting) },
            { EntityType.MText, typeof(TextSetting) },
        };

        private void OnEnable()
        {
            _setting = target as DwgPluginSetting;
            
            CacheSerializedProperties();
            AddAllTypesToList();
            InitializeReorderableList();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_defaultMaterialProperty);
            EditorGUILayout.PropertyField(_defaultColorProperty);
            EditorGUILayout.Space();

            _list.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_entityPrefabProperty);
            
            if(GUILayout.Button("Default Settings"))
                _setting.Initialize();
                
            serializedObject.ApplyModifiedProperties();
        }
        
        private void CacheSerializedProperties()
        {
            _defaultMaterialProperty = serializedObject.FindProperty(nameof(DwgPluginSetting.defaultMaterial));
            _defaultColorProperty = serializedObject.FindProperty(nameof(DwgPluginSetting.defaultColor));
            _entitySettingsProperty = serializedObject.FindProperty(nameof(DwgPluginSetting.entitySettings));
            _entityPrefabProperty = serializedObject.FindProperty("_entityPrefabs");
        }

        private void AddAllTypesToList()
        {
            // Automatically add all types when enabling
            foreach (var kv in _typeMap)
                AddElementToList(kv);
        }

        private void AddElementToList(KeyValuePair<EntityType, Type> keyValuePair)
        {
            var newIndex = _entitySettingsProperty.arraySize;
            _entitySettingsProperty.InsertArrayElementAtIndex(newIndex);
            var newElement = _entitySettingsProperty.GetArrayElementAtIndex(newIndex);
            var instance = Activator.CreateInstance(keyValuePair.Value);
            ((EntitySetting)instance).type = keyValuePair.Key;
            newElement.managedReferenceValue = instance;
        }
        
        private void InitializeReorderableList()
        {
            _list = new ReorderableList(serializedObject, _entitySettingsProperty, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                onAddCallback = AddEntityMenu
            };
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Entity Settings");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _entitySettingsProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            float y = rect.y;
            float height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, height), $"Entity {index}", EditorStyles.boldLabel);
            y += height + 2;

            EditorGUI.indentLevel++;

            // Type (display as label if set)
            var typeProperty = element.FindPropertyRelative("type");
            var oldType = (EntityType)typeProperty.intValue;
            
            // Display the type as a label (read-only) once set
            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, height), "Type", oldType.ToString());
            y += height + 2;
            
            // Common properties
            DrawField("useCustomColor", element, rect, ref y, height);

            var useColorProperty = element.FindPropertyRelative("useCustomColor");
            if (useColorProperty.boolValue)
            {
                DrawField("color", element, rect, ref y, height);
            }

            DrawSubclassFields(element, rect, ref y, height);

            EditorGUI.indentLevel--;
        }

        // Subclass-specific fields
        private void DrawSubclassFields(SerializedProperty element, Rect rect, ref float y, float height)
        {
            if (element.managedReferenceValue is LineSetting)
            {
                DrawField("lineWidth", element, rect, ref y, height);
                DrawField("material", element, rect, ref y, height);
            }

            if (element.managedReferenceValue is CurveSetting)
                DrawField("segment", element, rect, ref y, height);

            if (element.managedReferenceValue is TextSetting)
                DrawField("fontSize", element, rect, ref y, height);
        }

        private void DrawField(string propertyName, SerializedProperty element, Rect rect, ref float y, float height)
        {
            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), element.FindPropertyRelative(propertyName));
            y += height + 2;
        }

        private float GetElementHeight(int index)
        {
            var height = EditorGUIUtility.singleLineHeight * 2 + 6; // Header + Type
            var element = _entitySettingsProperty.GetArrayElementAtIndex(index);
            height += EditorGUIUtility.singleLineHeight + 2; // useCustomColor
            if (element.FindPropertyRelative("useCustomColor")?.boolValue ?? false)
                height += EditorGUIUtility.singleLineHeight + 2;

            if (element.managedReferenceValue is LineSetting)
                height += EditorGUIUtility.singleLineHeight * 2 + 4;
            if (element.managedReferenceValue is CurveSetting)
                height += EditorGUIUtility.singleLineHeight + 2;
            if (element.managedReferenceValue is TextSetting)
                height += EditorGUIUtility.singleLineHeight + 2;

            return height;
        }

        private void AddEntityMenu(ReorderableList list)
        {
            // First, collect all EntityTypes that are already used
            var existingTypes = new HashSet<EntityType>();
            for (var i = 0; i < _entitySettingsProperty.arraySize; i++)
            {
                var typeProperty = _entitySettingsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("type");
                if (typeProperty != null)
                    existingTypes.Add((EntityType)typeProperty.intValue);
            }

            // Then build the menu only with types not already added
            var menu = new GenericMenu();
            var hasAvailable = false;

            foreach (var kv in _typeMap)
            {
                if (existingTypes.Contains(kv.Key)) 
                    continue;

                hasAvailable = true;
                var captured = kv;
                menu.AddItem(new GUIContent(captured.Key.ToString()), false, () =>
                {
                    AddElementToList(captured);
                    serializedObject.ApplyModifiedProperties();
                });
            }

            if (!hasAvailable)
                menu.AddDisabledItem(new GUIContent("All EntityTypes added"));

            menu.ShowAsContext();
        }
    }
}
#endif