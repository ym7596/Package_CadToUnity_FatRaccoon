using System;
using ACadSharp.Entities;
using TMPro;
using UnityEngine;

namespace CadToUnityPlugin
{
	[RequireComponent(typeof(TextMeshPro))]
	public class DwgTextEntity : DwgEntity
	{
		[SerializeField] protected TMP_Text _text;

		private void Reset()
		{
			if(_text == false)
				_text = GetComponent<TMP_Text>();
		}
		
		protected override bool DrawEntity(float unit, Entity entity, EntitySetting setting)
		{
			if (entity is not TextEntity textEntity)
				return false;
			
			SetEntitySetting(setting);

			_text.text = textEntity.Value;

			var position = textEntity.InsertPoint.SwapZY() * unit;
			var rotation = Quaternion.Euler(90, 0, (float)textEntity.Rotation * Mathf.Rad2Deg);
			transform.SetPositionAndRotation(position, rotation);
			
			_text.horizontalAlignment = GetHorizontalAlignment(textEntity.HorizontalAlignment);
			_text.verticalAlignment = GetVerticalAlignment(textEntity.VerticalAlignment);
			
			return true;
		}
		
		protected virtual void SetEntitySetting(EntitySetting setting)
		{
			if (setting is null || (setting is TextSetting textSetting) == false)
				return;

			_text.color = textSetting.GetColor();
			_text.fontSize = textSetting.fontSize;
			_text.autoSizeTextContainer = true;
		}
		
		protected HorizontalAlignmentOptions GetHorizontalAlignment(TextHorizontalAlignment alignment)
		{
			return alignment switch
					{
						TextHorizontalAlignment.Left => HorizontalAlignmentOptions.Left,
						TextHorizontalAlignment.Center => HorizontalAlignmentOptions.Center,
						TextHorizontalAlignment.Right => HorizontalAlignmentOptions.Right,
						_ => throw new ArgumentOutOfRangeException()
					};
		}
		
		protected VerticalAlignmentOptions GetVerticalAlignment(TextVerticalAlignmentType alignment)
		{
			return alignment switch
					{
						TextVerticalAlignmentType.Baseline => VerticalAlignmentOptions.Baseline,
						TextVerticalAlignmentType.Bottom => VerticalAlignmentOptions.Bottom,
						TextVerticalAlignmentType.Middle => VerticalAlignmentOptions.Middle,
						TextVerticalAlignmentType.Top => VerticalAlignmentOptions.Top,
						_ => throw new ArgumentOutOfRangeException()
					};
		}
	}
}


