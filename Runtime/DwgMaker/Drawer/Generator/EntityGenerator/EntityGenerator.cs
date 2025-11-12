using System;
using System.Collections.Generic;
using ACadSharp.Entities;
using CSMath;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

namespace CadToUnityPlugin
{
    /// <summary>
    /// 객체를 생성하는 클래스
    /// </summary>
    public abstract class EntityGenerator
    {
        /// <summary>
        /// 객체 생성
        /// </summary>
        public abstract void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f) where T : Entity ;
        
        /// <summary>
        /// 비동기 객체 생성
        /// </summary>
        public abstract Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f) where T : Entity ;
        
        /// <summary>
        /// 게임 오브젝트 생성 및 T Component 추가 후 반환
        /// </summary>
        protected T CreateObjectAndComponent<T>(Entity entity) where T : Component
        {
            return new GameObject(entity.ToString()).AddComponent<T>();
        }

        /// <summary>
        /// dwg XY 좌표 데이터를 unity Vector3 로 변환
        /// </summary>
        protected Vector3 GetVector3(XY lineData)
        {
            return new Vector3((float)lineData.X, 0, (float)lineData.Y);
        }

        /// <summary>
        /// dwg XYZ 좌표 데이터를 unity Vector3 로 변환
        /// </summary>
        protected Vector3 GetVector3(XYZ lineData)
        {
            return new Vector3((float)lineData.X, (float)lineData.Z, (float)lineData.Y);
        }

        /// <summary>
        /// radian 에 따른 호의 끝점 좌표 반환
        /// </summary>
        protected Vector3 GetCurvePosition(Vector3 center, float radius, float radian)
        {
            var x = center.x + radius * Mathf.Cos(radian);
            var y = center.y;
            var z = center.z + radius * Mathf.Sin(radian);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// LineRenderer 생성 및 공통 설정
        /// </summary>
        protected LineRenderer CreateLineRenderer(Entity entity, Transform parent, EntitySetting entitySetting)
        {
            var lineRenderer = CreateObjectAndComponent<LineRenderer>(entity);
            lineRenderer.transform.SetParent(parent);
            lineRenderer.positionCount = entity switch
            {
                Line => 2,
                LwPolyline lwPolyline => lwPolyline.Vertices.Count + 1,
                Arc or Circle => ((CurveSetting)entitySetting).segment + 1,
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, null)
            };

            var color = entitySetting.color;
            // dwg color 데이터 받아올 경우
            // color = new Color(entity.Color.R, entity.Color.G, entity.Color.B);

            // lineRenderer.startColor = lineRenderer.endColor = color;
            if (entitySetting is LineSetting lineSetting)
            {
                var width = (float)entity.LineTypeScale * lineSetting.lineWidth;
                lineRenderer.SetWidth(width);
                lineRenderer.material = lineSetting.material;
            }
            lineRenderer.SetColor(color);
            return lineRenderer;
        }

        /// <summary>
        /// textmeshpro 생성 및 공통 설정
        /// </summary>
        protected TextMeshPro CreateTextMeshPro(Entity entity, Transform parent, EntitySetting entitySetting)
        {
            var textMeshPro = CreateObjectAndComponent<TextMeshPro>(entity);
            textMeshPro.transform.SetParent(parent);
            var color = entitySetting.color;
            textMeshPro.color = color;
            textMeshPro.fontSize = ((TextSetting)entitySetting).fontSize;
            textMeshPro.autoSizeTextContainer = true;
            return textMeshPro;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Textmeshpro bounding box 영역 설정하는 코드 
        /// </summary>
        protected void SetTextEntityProperty(TextMeshPro textMeshPro)
        {
            textMeshPro.ForceMeshUpdate(); // Ensure mesh data is up to date

            var mesh = textMeshPro.mesh;
            var vertices = mesh.vertices;
            var textTransform = textMeshPro.transform;

            // Find min and max bounds in local space
            var min = vertices[0];
            var max = vertices[0];

            foreach (Vector3 vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            // Convert to world space
            min = textTransform.TransformPoint(min);
            max = textTransform.TransformPoint(max);
            textMeshPro.gameObject.AddComponent<EntityProperty>().SetBoundingPoint(min, max);
        }

        /// <summary>
        /// lineRenderer bounding box 영역 설정하는 코드 
        /// </summary>
        protected void SetLineEntityProperty(Entity entity, LineRenderer lineRenderer, float unitConversionConstant)
        {
            var min = GetVector3(entity.GetBoundingBox().Min) * unitConversionConstant;
            var max = GetVector3(entity.GetBoundingBox().Max) * unitConversionConstant;

            // 이유는 모르겠으나 호의 GetBoundingBox().Min - 0 임
            if (entity is Arc arc && min == Vector3.zero)
            {
                min = max - new Vector3(1, 0, 1) * ((float)arc.Radius * unitConversionConstant);
            }
            lineRenderer.gameObject.AddComponent<EntityProperty>().SetBoundingPoint(min, max);
        }
#endif
    }
}