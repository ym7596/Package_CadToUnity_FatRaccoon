using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp.Entities;
using UnityEngine;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

namespace CadToUnityPlugin
{
    public class LwPolylineGenerator : EntityGenerator
    {
        /// <summary>
        /// LwPolyline 객체 생성
        /// </summary>
        public override void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            DrawLwPolyLine(entities.Cast<LwPolyline>().ToList(), root, entitySetting, unitConversionConstant);
        }

                private void DrawLwPolyLine(List<LwPolyline> lwPolyLineEntities, Transform lineRoot,
            EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = lwPolyLineEntities.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var lwPolyline = lwPolyLineEntities[i];
                    var lineRenderer = CreateLineRenderer(lwPolyline, lineRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(lwPolyline, lineRenderer, unitConversionConstant);
#endif

                    var center = Vector3.zero;
                    var verticesCount = lwPolyline.Vertices.Count;
                    for (var j = 0; j < verticesCount; j++)
                    {
                        var vertexPoint = GetVector3(lwPolyline.Vertices[j].Location) * unitConversionConstant;
                        lineRenderer.SetPosition(j, lineRoot.position + vertexPoint);
                        center += vertexPoint;
                    }

                    lineRenderer.transform.position = center / verticesCount;

                    if (lwPolyline.IsClosed)
                    {
                        lineRenderer.loop = true;
                        lineRenderer.SetPosition(verticesCount,
                            lineRoot.position + GetVector3(lwPolyline.Vertices[0].Location) * unitConversionConstant);
                    }
                    else
                    {
                        lineRenderer.positionCount--;
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
        /// LwPolyline 객체 생성
        /// </summary>
        public override async Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            await DrawLwPolyLineAsync(entities.Cast<LwPolyline>().ToList(), root, entitySetting, unitConversionConstant);
        }

        private async Awaitable DrawLwPolyLineAsync(List<LwPolyline> lwPolyLineEntities, Transform lineRoot,
            EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = lwPolyLineEntities.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var lwPolyline = lwPolyLineEntities[i];
                    var lineRenderer = CreateLineRenderer(lwPolyline, lineRoot, entitySetting);
#if UNITY_EDITOR
                    SetLineEntityProperty(lwPolyline, lineRenderer, unitConversionConstant);
#endif

                    var center = Vector3.zero;
                    var verticesCount = lwPolyline.Vertices.Count;
                    for (var j = 0; j < verticesCount; j++)
                    {
                        var vertexPoint = GetVector3(lwPolyline.Vertices[j].Location) * unitConversionConstant;
                        lineRenderer.SetPosition(j, lineRoot.position + vertexPoint);
                        center += vertexPoint;
                    }

                    lineRenderer.transform.position = center / verticesCount;

                    if (lwPolyline.IsClosed)
                    {
                        lineRenderer.loop = true;
                        lineRenderer.SetPosition(verticesCount,
                            lineRoot.position + GetVector3(lwPolyline.Vertices[0].Location) * unitConversionConstant);
                    }
                    else
                    {
                        lineRenderer.positionCount--;
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