using CSMath;
using UnityEngine;

namespace CadToUnityPlugin
{
	public static class DwgPluginExtensions
	{
		public static void SetColor(this LineRenderer lineRenderer, Color color)
		{
			lineRenderer.startColor = lineRenderer.endColor = color;
		}
    
		public static void SetWidth(this LineRenderer lineRenderer, float width)
		{
			lineRenderer.startWidth = lineRenderer.endWidth = width;
		}
		
		public static Vector3 SwapZY(this XYZ vector)
		{
			return new Vector3((float)vector.X, (float)vector.Z, (float)vector.Y);
		}
    
		public static Vector3 SwapZY(this Vector3 vector)
		{
			return new Vector3(vector.x, vector.z, vector.y);
		}
		
		public static Vector3 SwapZY(this XY vector)
		{
			return new Vector3((float)vector.X, 0, (float)vector.Y);
		}
		
		public static Vector3 ToVector3(this XYZ vector)
		{
			return new Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
		} 
		
		public static Vector3 ToVector3(this XY vector)
		{
			return new Vector3((float)vector.X, (float)vector.Y, 0);
		}
		
		public static Vector3 CurvePosition(this Vector3 vector, float radius, float radian)
		{
			var x = vector.x + radius * Mathf.Cos(radian);
			var y = vector.y + radius * Mathf.Sin(radian);
			return new Vector3(x, y, vector.z);
		}
	} 
}

