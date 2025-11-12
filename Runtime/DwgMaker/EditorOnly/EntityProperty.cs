using UnityEngine;

namespace CadToUnityPlugin
{
#if UNITY_EDITOR
    public class EntityProperty : MonoBehaviour
    {
        public Vector3 bottomLeft;
        public Vector3 topRight;

        private Vector3 _bottomRight;
        private Vector3 _topLeft;

        public bool showBoundingBox;
        
        public void SetBoundingPoint(Vector3 boundingBoxMin, Vector3 boundingBoxMax)
        {
            bottomLeft = boundingBoxMin;
            topRight = boundingBoxMax;
            
            _topLeft = new Vector3(boundingBoxMin.x, 0, boundingBoxMax.z); 
            _bottomRight = new Vector3(boundingBoxMax.x, 0, boundingBoxMin.z); 
        }
        
        private void OnDrawGizmos()
        {
            if (showBoundingBox)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(bottomLeft, _topLeft);
                Gizmos.DrawLine(bottomLeft, _bottomRight);
                Gizmos.DrawLine(topRight, _topLeft);
                Gizmos.DrawLine(topRight, _bottomRight);
            }
        }
    }
#endif
}