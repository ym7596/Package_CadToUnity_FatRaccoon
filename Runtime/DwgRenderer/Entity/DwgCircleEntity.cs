using ACadSharp.Entities;
using UnityEngine;

namespace CadToUnityPlugin
{
	public class DwgCircleEntity : DwgLineEntity
	{
		protected override bool Process(float unit, Entity entity, EntitySetting setting)
		{
			if (entity is not Circle circle || setting is not CurveSetting curveSetting) 
				return false;
			
			var position = circle.Center.ToVector3() * unit;
			var radius = (float)circle.Radius * unit;
			var segment = curveSetting.segment;
			
			_lineRenderer.positionCount = segment;
			
			for (var i = 0; i < segment; ++i)
			{
				var radian = (i / (float)segment) * 2 * Mathf.PI;
				_lineRenderer.SetPosition(i, position.CurvePosition(radius, radian));
			}
			
			//transform.position = position;
			
			return true;
		}
	}
}

