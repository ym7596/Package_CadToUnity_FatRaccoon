using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CadToUnityPlugin
{
    /// <summary>
    /// TextMeshPro 를 이용해 데이터를 표출하는 객체를 생성하는 클래스
    /// </summary>
    public class TextEntityGenerator : EntityGenerator
    {
        /// <summary>
        /// TextEntity 객체 생성
        /// </summary>
        public override void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            WriteTextEntity(entities.Cast<TextEntity>().ToList(), root, entitySetting, unitConversionConstant);
            // textEntity.Height -> 값이 들어오는데 어디에 사용해야될지 모르겠음;
        }

        private void WriteTextEntity(List<TextEntity> textEntities, Transform textRoot,
            EntitySetting textSetting, float unitConversionConstant)
        {
            try
            {
                var cnt = textEntities.Count;
                for (int i = 0; i < cnt; i++)
                {
                    var textEntity = textEntities[i];
                    var textMeshPro = CreateTextMeshPro(textEntity, textRoot, textSetting);
                    textMeshPro.text = textEntity.Value;

                    textMeshPro.transform.position = textRoot.position + GetVector3(textEntity.InsertPoint) * unitConversionConstant;
                    // xy 축에서 xz 축으로 변경
                    textMeshPro.transform.rotation = Quaternion.Euler(90, 0, (float)textEntity.Rotation * Mathf.Rad2Deg);

                    textMeshPro.horizontalAlignment = GetHorizontalAlignmentOption(textEntity.HorizontalAlignment);
                    textMeshPro.verticalAlignment = GetVerticalAlignmentOption(textEntity.VerticalAlignment);
                
#if UNITY_EDITOR
                    SetTextEntityProperty(textMeshPro);
#endif
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }
        
        /// <summary>
        /// TextEntity 객체 비동기 생성
        /// </summary>
        public override async Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            await WriteTextEntityAsync(entities.Cast<TextEntity>().ToList(), root, entitySetting, unitConversionConstant);
        }

        private async Awaitable WriteTextEntityAsync(List<TextEntity> textEntities, Transform textRoot,
            EntitySetting textSetting, float unitConversionConstant)
        {
            try
            {
                var cnt = textEntities.Count;
                for (int i = 0; i < cnt; i++)
                {
                    var textEntity = textEntities[i];
                    var textMeshPro = CreateTextMeshPro(textEntity, textRoot, textSetting);
                    textMeshPro.text = textEntity.Value;

                    textMeshPro.transform.position = textRoot.position + GetVector3(textEntity.InsertPoint) * unitConversionConstant;
                    // xy 축에서 xz 축으로 변경
                    textMeshPro.transform.rotation = Quaternion.Euler(90, 0, (float)textEntity.Rotation * Mathf.Rad2Deg);

                    textMeshPro.horizontalAlignment = GetHorizontalAlignmentOption(textEntity.HorizontalAlignment);
                    textMeshPro.verticalAlignment = GetVerticalAlignmentOption(textEntity.VerticalAlignment);
                
#if UNITY_EDITOR
                    SetTextEntityProperty(textMeshPro);
#endif
                    
                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }

        /// <summary>
        /// 수평 정렬 정보 가져오기
        /// </summary>
        private HorizontalAlignmentOptions GetHorizontalAlignmentOption(TextHorizontalAlignment textEntityHorizontalAlignment)
        {
            switch (textEntityHorizontalAlignment)
            {
                case TextHorizontalAlignment.Left:
                    return HorizontalAlignmentOptions.Left;
                case TextHorizontalAlignment.Center:
                    return HorizontalAlignmentOptions.Center;
                case TextHorizontalAlignment.Right:
                    return HorizontalAlignmentOptions.Right;

                // CAD 특수 정렬: TMP에 직접 대응 없음 → 보통 Center로 폴백
                // (필요하면 이후 위치/스케일 보정 로직 추가)
                // Aligned: 시작/끝점에 맞춰 문자 간격을 늘리는 정렬
                // Middle: 좌우 가운데 정렬과 유사
                // Fit: 주어진 길이에 맞춰 문자를 늘려 배치
                case TextHorizontalAlignment.Aligned:
                case TextHorizontalAlignment.Middle:
                case TextHorizontalAlignment.Fit:
                    Debug.LogWarning($"[TextEntityGenerator] Unsupported or special TextHorizontalAlignment '{textEntityHorizontalAlignment}'. Fallback to Center.");
                    return HorizontalAlignmentOptions.Center;

                default:
                    Debug.LogWarning($"[TextEntityGenerator] Unknown TextHorizontalAlignment '{textEntityHorizontalAlignment}'. Fallback to Left.");
                    return HorizontalAlignmentOptions.Left;
            }

        }

        /// <summary>
        /// 수직 정렬 정보 가져오기
        /// </summary>
        private VerticalAlignmentOptions GetVerticalAlignmentOption(TextVerticalAlignmentType textEntityVerticalAlignment)
        {
            switch (textEntityVerticalAlignment)
            {
                case TextVerticalAlignmentType.Baseline:
                    return VerticalAlignmentOptions.Baseline;
                case TextVerticalAlignmentType.Bottom:
                    return VerticalAlignmentOptions.Bottom;
                case TextVerticalAlignmentType.Middle:
                    return VerticalAlignmentOptions.Middle;
                case TextVerticalAlignmentType.Top:
                    return VerticalAlignmentOptions.Top;

                default:
                    Debug.LogWarning($"[TextEntityGenerator] Unknown TextVerticalAlignmentType '{textEntityVerticalAlignment}'. Fallback to Baseline.");
                    return VerticalAlignmentOptions.Baseline;
            }

        }
    }
}