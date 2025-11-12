using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp.Entities;
using UnityEngine;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

namespace CadToUnityPlugin
{
    public class LineGenerator : EntityGenerator
    {
        /// <summary>
        /// Line 객체 생성
        /// </summary>
        public override void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            DrawLine(entities.Cast<Line>().ToList(), root, entitySetting, unitConversionConstant);

            // line.LineType ,line.Normal, line.Handle -> 용도를 모르겠음
            // line.Layer -> 레이어 설정 필요
            // line.Thickness -> 2d 객체 간의 높이?로 추정
            // line.LineWeight -> 그려지는 우선순위로 추정됨
        }

        private void DrawLine(List<Line> lineEntities, Transform lineRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = lineEntities.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var line = lineEntities[i];
                    var lineRenderer = CreateLineRenderer(line, lineRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(line, lineRenderer, unitConversionConstant);
#endif
                    var startPoint = GetVector3(line.StartPoint) * unitConversionConstant;
                    var endPoint = GetVector3(line.EndPoint) * unitConversionConstant;

                    lineRenderer.SetPosition(0, lineRoot.position + startPoint);
                    lineRenderer.SetPosition(1, lineRoot.position + endPoint);

                    lineRenderer.transform.position = (startPoint + endPoint) / 2;
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }
        
        /// <summary>
        /// Line 객체 생성
        /// </summary>
        public override async Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            await DrawLineAsync(entities.Cast<Line>().ToList(), root, entitySetting, unitConversionConstant);
        }
        private const int MAX_CONCURRENT = 8;
        private async Awaitable DrawLineAsync(List<Line> lineEntities, Transform lineRoot, EntitySetting entitySetting, float unitConversionConstant)
        { 
            try
            {
                var cnt = lineEntities.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var line = lineEntities[i];
                    var lineRenderer = CreateLineRenderer(line, lineRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(line, lineRenderer, unitConversionConstant);
#endif

                    var startPoint = GetVector3(line.StartPoint) * unitConversionConstant;
                    var endPoint = GetVector3(line.EndPoint) * unitConversionConstant;
                    /*
                    var pts = new List<Vector3>(2)
                    {
                        lineRoot.position + startPoint,
                        lineRoot.position + endPoint
                    };

                    // LineRenderer는 월드 스페이스로 사용(권장)
                    lineRenderer.useWorldSpace = true;
                    
                    
                    await LineRevealUtil.RevealAlongPointsAsync(
                        lineRenderer,
                        pts,
                        500f,
                        useWorldSpace: true,
                        root: null);
                        */
                    
                    lineRenderer.SetPosition(0, lineRoot.position + startPoint);
                    lineRenderer.SetPosition(1, lineRoot.position + endPoint);

                    lineRenderer.transform.position = (startPoint + endPoint) / 2;

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