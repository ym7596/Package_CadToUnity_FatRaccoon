using ACadSharp.Entities;
using UnityEngine;

namespace CadToUnityPlugin
{
	[RequireComponent(typeof(LineRenderer))]
	public class DwgLineEntity : DwgEntity
	{
		[SerializeField] protected LineRenderer _lineRenderer;
		
		protected virtual void Reset()
		{
			if(_lineRenderer == false)
				_lineRenderer = GetComponent<LineRenderer>();
		}

		public UnityEngine.Mesh BakeMesh()
		{
			if (_lineRenderer == false || _lineRenderer.positionCount < 2)
				return null;
			
			var bakeMesh = new UnityEngine.Mesh();
			_lineRenderer.BakeMesh(bakeMesh);
			
			return bakeMesh;
		}
		
		protected override bool DrawEntity(float unit, Entity entity, EntitySetting setting)
		{
			if (SetEntitySetting(setting) == false)
				return false;

			BeginProcess();

			if (Process(unit, entity, setting) == false)
				return false;
			
			EndProcess();
			
			return true;
		}

		protected virtual void BeginProcess()
		{
			_lineRenderer.positionCount = 0;
			_lineRenderer.useWorldSpace = false;
			_lineRenderer.alignment = LineAlignment.TransformZ;
			transform.rotation = Quaternion.identity;
		}

		protected virtual bool Process(float unit, Entity entity, EntitySetting setting)
		{
			if (entity is not Line line)
				return false;
			
			var startPoint = line.StartPoint.ToVector3() * unit;
			var endPoint = line.EndPoint.ToVector3() * unit;
			
			_lineRenderer.positionCount = 2;
			
			_lineRenderer.SetPosition(0, startPoint);
			_lineRenderer.SetPosition(1, endPoint);
			
			//transform.position = (startPoint + endPoint) * 0.5f;
			
			return true;
		}

		protected virtual void EndProcess()
		{
			transform.rotation = Quaternion.Euler(90, 0, 0);
		}

		protected virtual bool SetEntitySetting(EntitySetting setting)
		{
			if (setting is null || (setting is LineSetting lineSetting) == false)
				return false;
			
			if(lineSetting.material)
				_lineRenderer.material = lineSetting.material;
			
			var width = lineSetting.lineWidth;
			_lineRenderer.SetWidth(width);
			_lineRenderer.SetColor(lineSetting.GetColor());

			return true;
		}
	}
}

