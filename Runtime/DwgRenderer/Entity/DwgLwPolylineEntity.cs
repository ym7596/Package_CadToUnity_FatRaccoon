using ACadSharp.Entities;
using UnityEngine;

namespace CadToUnityPlugin
{
	public class DwgLwPolylineEntity : DwgLineEntity
	{
		protected override bool Process(float unit, Entity entity, EntitySetting setting)
		{
			if (entity is not LwPolyline lwPolyline)
				return false;
			
			var vertices = lwPolyline.Vertices;
			var pointCount = vertices.Count;
			_lineRenderer.positionCount = pointCount;
			
			var accumulatedPosition = Vector3.zero;
			
			for (var i = 0; i < pointCount; ++i)
			{
				var position = vertices[i].Location.ToVector3() * unit;
				_lineRenderer.SetPosition(i, position);
				accumulatedPosition += position;
			}

			_lineRenderer.loop = lwPolyline.IsClosed;

			//transform.position = accumulatedPosition / pointCount;
			
			return true;
		}
	}
}


