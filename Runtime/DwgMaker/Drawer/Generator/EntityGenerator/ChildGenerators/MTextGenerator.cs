using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ACadSharp.Entities;
using ACadSharp.Tables;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CadToUnityPlugin
{
    public class MTextGenerator : EntityGenerator
    {
        /// <summary>
        /// MText 객체 생성
        /// </summary>
        public override void Generate<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            WriteText(entities.Cast<MText>().ToList(), root, entitySetting, unitConversionConstant);
            // mText.Style.Name // 폰트명
        }

        private void WriteText(List<MText> mTextList, Transform textRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = mTextList.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var mText = mTextList[i];
                    var textMeshPro = CreateTextMeshPro(mText, textRoot, entitySetting);
                    textMeshPro.text = Regex.Replace(mText.Value, @"\\[Cc]\d+;", "").Trim(new char[] { '{', '}' });

                    textMeshPro.transform.position =
                        textRoot.position + GetVector3(mText.InsertPoint) * unitConversionConstant;
                    // xy 축에서 xz 축으로 변경
                    textMeshPro.transform.rotation = Quaternion.Euler(90, 0, (float)mText.Rotation * Mathf.Rad2Deg);
                    textMeshPro.fontStyle = GetFontStyle(mText.Style.TrueType);

                    var rectTransform = textMeshPro.GetComponent<RectTransform>();
                    rectTransform.pivot = GetPivot(mText.AttachmentPoint);

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
        /// MText 객체 비동기 생성
        /// </summary>
        public override async Awaitable GenerateAsync<T>(List<T> entities, Transform root, EntitySetting entitySetting, float unitConversionConstant = 0f)
        {
            await WriteTextAsync(entities.Cast<MText>().ToList(), root, entitySetting,
                unitConversionConstant);
        }

        private async Awaitable WriteTextAsync(List<MText> mTextList, Transform textRoot, EntitySetting entitySetting, float unitConversionConstant)
        {
            try
            {
                var cnt = mTextList.Count;
                for (var i = 0; i < cnt; i++)
                {
                    var mText = mTextList[i];
                    var textMeshPro = CreateTextMeshPro(mText, textRoot, entitySetting);
                    textMeshPro.text = Regex.Replace(mText.Value, @"\\[Cc]\d+;", "").Trim(new char[] { '{', '}' });

                    textMeshPro.transform.position =
                        textRoot.position + GetVector3(mText.InsertPoint) * unitConversionConstant;
                    // xy 축에서 xz 축으로 변경
                    textMeshPro.transform.rotation = Quaternion.Euler(90, 0, (float)mText.Rotation * Mathf.Rad2Deg);
                    textMeshPro.fontStyle = GetFontStyle(mText.Style.TrueType);

                    var rectTransform = textMeshPro.GetComponent<RectTransform>();
                    rectTransform.pivot = GetPivot(mText.AttachmentPoint);

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
        /// 폰트 스타일 가져오기
        /// </summary>
        private FontStyles GetFontStyle(FontFlags styleTrueType)
        {
            return styleTrueType switch
            {
                FontFlags.Regular => FontStyles.Normal,
                FontFlags.Italic => FontStyles.Italic,
                FontFlags.Bold => FontStyles.Bold,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// text entity 피벗 가져오기
        /// </summary>
        private Vector2 GetPivot(AttachmentPointType mTextAttachmentPoint)
        {
            return mTextAttachmentPoint switch
            {
                AttachmentPointType.TopLeft => Vector2.up,
                AttachmentPointType.TopCenter => new Vector2(0.5f, 1f),
                AttachmentPointType.TopRight => Vector2.one,

                AttachmentPointType.MiddleLeft => new Vector2(0f, 0.5f),
                AttachmentPointType.MiddleCenter => new Vector2(0.5f, 0.5f),
                AttachmentPointType.MiddleRight => new Vector2(1f, 0.5f),

                AttachmentPointType.BottomLeft => Vector2.zero,
                AttachmentPointType.BottomCenter => new Vector2(0.5f, 0f),
                AttachmentPointType.BottomRight => Vector2.right,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}