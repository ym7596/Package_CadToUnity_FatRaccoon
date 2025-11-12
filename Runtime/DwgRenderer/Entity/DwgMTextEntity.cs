using System;
using ACadSharp.Entities;
using ACadSharp.Tables;
using TMPro;
using UnityEngine;

namespace CadToUnityPlugin
{
	public class DwgMTextEntity : DwgTextEntity
	{
		protected override bool DrawEntity(float unit, Entity entity, EntitySetting setting)
		{
			if (entity is not MText mText)
				return false;
			
			SetEntitySetting(setting);
			
			_text.text = mText.Value;

			var position = mText.InsertPoint.SwapZY() * unit;
			var rotation = Quaternion.Euler(90, 0, (float)mText.Rotation * Mathf.Rad2Deg);
			transform.SetPositionAndRotation(position, rotation);
			
			_text.fontStyle = GetFontStyle(mText.Style.TrueType);
			
			if (transform is RectTransform rectTransform)
				rectTransform.pivot = GetPivot(mText.AttachmentPoint);
			
			return true;
		}
		
		protected FontStyles GetFontStyle(FontFlags styleTrueType)
		{
			return styleTrueType switch
					{
						FontFlags.Regular => FontStyles.Normal,
						FontFlags.Italic => FontStyles.Italic,
						FontFlags.Bold => FontStyles.Bold,
						_ => throw new ArgumentOutOfRangeException()
					};
		}
		
		protected Vector2 GetPivot(AttachmentPointType attachmentPointType)
		{
			return attachmentPointType switch
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


