using ACadSharp.Entities;
using UnityEngine;

namespace CadToUnityPlugin
{
	public abstract class DwgEntity : MonoBehaviour
	{
		public bool Draw(float unit, Entity entity, EntitySetting setting)
		{
			name = entity.ToString();
			return DrawEntity(unit, entity, setting);
		}
		
		protected abstract bool DrawEntity(float unit, Entity entity, EntitySetting setting);
	}
}