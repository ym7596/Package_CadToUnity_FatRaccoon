using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp.Entities;
using UnityEngine;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

namespace CadToUnityPlugin
{
    public class CircleGenerator : EntityGenerator
    {
        /// <summary>
        /// Circle 객체 생성
        /// </summary>
        public override void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            DrawCircle(entities.Cast<Circle>().ToList(), root, entitySetting, unitConversionConstant);
        }

        private void DrawCircle(List<Circle> circles, Transform circleRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = circles.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var circle = circles[i];
                    var center = circleRoot.position + GetVector3(circle.Center) * unitConversionConstant;
                    var radius = (float)circle.Radius * unitConversionConstant;

                    var lineRenderer = CreateLineRenderer(circle, circleRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(circle, lineRenderer, unitConversionConstant);
#endif

                    lineRenderer.loop = true;
                    lineRenderer.transform.position = center;

                    var segment = ((CurveSetting)entitySetting).segment;
                    for (var j = 0; j <= segment; j++)
                    {
                        // 360° (degree) = 2π (radian)
                        float radian = (j / (float)segment) * 2 * Mathf.PI;
                        lineRenderer.SetPosition(j, GetCurvePosition(center, radius, radian));
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }
        
        /// <summary>
        /// Circle 객체 생성
        /// </summary>
        public override async Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            await DrawCircleAsync(entities.Cast<Circle>().ToList(), root, entitySetting,
                unitConversionConstant);
        }

        private async Awaitable DrawCircleAsync(List<Circle> circles, Transform circleRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = circles.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var circle = circles[i];
                    var center = circleRoot.position + GetVector3(circle.Center) * unitConversionConstant;
                    var radius = (float)circle.Radius * unitConversionConstant;

                    var lineRenderer = CreateLineRenderer(circle, circleRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(circle, lineRenderer, unitConversionConstant);
#endif

                    lineRenderer.loop = true;
                    lineRenderer.transform.position = center;

                    var segment = ((CurveSetting)entitySetting).segment;
                    for (var j = 0; j <= segment; j++)
                    {
                        // 360° (degree) = 2π (radian)
                        float radian = (j / (float)segment) * 2 * Mathf.PI;
                        lineRenderer.SetPosition(j, GetCurvePosition(center, radius, radian));
                    }

                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }
    }
}