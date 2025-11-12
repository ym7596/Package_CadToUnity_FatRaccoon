using ACadSharp.Entities;
using UnityEngine;

namespace CadToUnityPlugin
{
	public class ArcEntity : DwgLineEntity
	{
		protected override bool Process(float unit, Entity entity, EntitySetting setting)
		{
			if (entity is not Arc arc || setting is not CurveSetting curveSetting) 
				return false;
			
			var position = arc.Center.ToVector3() * unit;
			var radius = (float)arc.Radius * unit;
			var segment = curveSetting.segment;
			var startAngle = (float)arc.StartAngle;
			var endAngle = (float)arc.EndAngle;
			endAngle = endAngle < startAngle ? endAngle + 2 * Mathf.PI : endAngle;
			
			_lineRenderer.positionCount = segment;
			
			for (var i = 0; i < segment; ++i)
			{
				var t = i / (float)segment;
				var radian = Mathf.Lerp(startAngle, endAngle, t);
				_lineRenderer.SetPosition(i, position.CurvePosition(radius, radian));
			}
			
			//transform.position = position;
			
			return true;
		}
	}
}


