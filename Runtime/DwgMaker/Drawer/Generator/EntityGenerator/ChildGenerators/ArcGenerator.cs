using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp.Entities;
using UnityEngine;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

namespace CadToUnityPlugin
{
    public class ArcGenerator : EntityGenerator
    {
        /// <summary>
        /// Arc 객체 생성
        /// </summary>
        public override void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            DrawArc(entities.Cast<Arc>().ToList(), root, entitySetting, unitConversionConstant);
        }

        private void DrawArc(List<Arc> arcs, Transform arcRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = arcs.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var arc = arcs[i];
                    var center = arcRoot.position + GetVector3(arc.Center) * unitConversionConstant;
                    var radius = (float)arc.Radius * unitConversionConstant;

                    // startRadian < endRadian 일 경우 반시계방향으로 그려짐
                    // startRadian > endRadian 일 경우 시계방향으로 그려짐
                    // 시계 방향으로 그려지면 원본 dwg 의 호와 다른모양으로 표출됨
                    // 2π 를 더해서 반시계 방향으로 그려지도록 수정
                    var startRadian = arc.StartAngle;
                    var endRadian = arc.EndAngle < arc.StartAngle ? arc.EndAngle + 2 * Mathf.PI : arc.EndAngle;

                    var lineRenderer = CreateLineRenderer(arc, arcRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(arc, lineRenderer, unitConversionConstant);
#endif
                    lineRenderer.transform.position = center;

                    var segment = ((CurveSetting)entitySetting).segment;
                    for (var j = 0; j <= segment; j++)
                    {
                        var t = j / (float)segment;
                        var radian = Mathf.Lerp((float)startRadian, (float)endRadian, t);
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
        /// Arc 객체 비동기 생성
        /// </summary>
        public override async Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            await DrawArcAsync(entities.Cast<Arc>().ToList(), root, entitySetting,
                unitConversionConstant);
        }

        private async Awaitable DrawArcAsync(List<Arc> arcs, Transform arcRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = arcs.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var arc = arcs[i];
                    var center = arcRoot.position + GetVector3(arc.Center) * unitConversionConstant;
                    var radius = (float)arc.Radius * unitConversionConstant;

                    // startRadian < endRadian 일 경우 반시계방향으로 그려짐
                    // startRadian > endRadian 일 경우 시계방향으로 그려짐
                    // 시계 방향으로 그려지면 원본 dwg 의 호와 다른모양으로 표출됨
                    // 2π 를 더해서 반시계 방향으로 그려지도록 수정
                    var startRadian = arc.StartAngle;
                    var endRadian = arc.EndAngle < arc.StartAngle ? arc.EndAngle + 2 * Mathf.PI : arc.EndAngle;

                    var lineRenderer = CreateLineRenderer(arc, arcRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(arc, lineRenderer, unitConversionConstant);
#endif
                    lineRenderer.transform.position = center;

                    var segment = ((CurveSetting)entitySetting).segment;
                    for (var j = 0; j <= segment; j++)
                    {
                        var t = j / (float)segment;
                        var radian = Mathf.Lerp((float)startRadian, (float)endRadian, t);
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