using System;
using System.Collections.Generic;
using ACadSharp;
using ACadSharp.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CadToUnityPlugin
{
    public class BlockGenerator
    {
        private readonly Dictionary<ulong, GameObject> _blockDictionary = new();
        
        /// <summary>
        /// block 객체 생성
        /// </summary>
        public async Awaitable GenerateBlockAsync(List<Entity> inserts, float unitConvertConstant, Func<CadObjectCollection<Entity>, Transform, Awaitable> generateEntitiesAsync, Transform blockRoot)
        {
            foreach (var entity in inserts)
            {
                var insert = (Insert)entity;
                var blockHandle = insert.Block.Handle; // block id
                if (_blockDictionary.TryGetValue(blockHandle, out var blockObject))
                    ReuseBlock(blockObject, blockRoot, insert, unitConvertConstant);
                else
                    await GenerateBlock(insert, generateEntitiesAsync, blockHandle, blockRoot, unitConvertConstant);
            }
        }
        
        /// <summary>
        /// block 객체 재사용
        /// </summary>
        private void ReuseBlock(GameObject blockObject, Transform blockRoot, Insert insert, float unitConvertConstant)
        {
            var block = Object.Instantiate(blockObject, blockRoot, true).transform;
            block.name = $"{insert}/{insert.Block}";
            block.position = new Vector3((float)insert.InsertPoint.X, (float)insert.InsertPoint.Z, (float)insert.InsertPoint.Y) * unitConvertConstant;
            block.eulerAngles = new Vector3(0, (float)insert.Rotation * Mathf.Rad2Deg, 0);

            ResetLineRendererPositions(block);
        }
        
        /// <summary>
        /// block 객체 생성
        /// </summary>
        private async Awaitable GenerateBlock(Insert insert, Func<CadObjectCollection<Entity>, Transform, Awaitable> generateEntitiesAsync, ulong blockHandle, Transform blockRoot, float unitConvertConstant)
        {
            var newBlock = new GameObject($"{insert}/{insert.Block}")
            {
                transform =
                {
                    position = new Vector3((float)insert.InsertPoint.X, (float)insert.InsertPoint.Z, (float)insert.InsertPoint.Y) * unitConvertConstant
                }
            };

            await generateEntitiesAsync.Invoke(insert.Block.Entities, newBlock.transform);
            _blockDictionary.Add(blockHandle, newBlock);
            newBlock.transform.SetParent(blockRoot);
        }

        /// <summary>
        /// // block 내에 linerenderer 위치 값 전부 변경
        /// </summary>
        private void ResetLineRendererPositions(Transform block)
        {
            foreach (Transform entityTransform in block)
            {
                if (entityTransform.TryGetComponent<LineRenderer>(out var lineRenderer))
                {
                    // 현재 lineRenderer 가 있는 모든 entity(Line, LwPolyline, Arc, Circle)에 적용됨 
                    // 텍스트의 경우 부모 포지션 따라서 자동 적용됨
                    // 기존 블럭의 중심 기준 상대좌표(역방향) + 새로운 블럭의 중심점
                    // entityTransform.localPosition + block.position
                    var offset = entityTransform.localPosition + block.position;
                    var cnt = lineRenderer.positionCount;
                    for (int i = 0; i < cnt; i++)
                    {
                        var position = lineRenderer.GetPosition(i) + offset;
                        lineRenderer.SetPosition(i, position);
                    }
                }
            }
        }
    }
}