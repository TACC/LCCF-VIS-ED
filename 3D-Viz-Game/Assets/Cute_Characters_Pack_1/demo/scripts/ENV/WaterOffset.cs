using UnityEngine;

namespace My.DemoScene
{

    public class UVScroller : MonoBehaviour
    {
        public Material targetMaterial;

        public float scrollX = 0f;
        public float scrollY = 0.5f;

        private Vector2 _offset;

        void Start()
        {
            if (targetMaterial != null)
                _offset = targetMaterial.mainTextureOffset;
        }

        void Update()
        {
            if (targetMaterial == null) return;

            _offset.x += scrollX * Time.deltaTime;
            _offset.y += scrollY * Time.deltaTime;

            
            _offset.x %= 1f;
            _offset.y %= 1f;

            targetMaterial.mainTextureOffset = _offset;
        }

        void OnDisable()
        {
            if (targetMaterial != null)
                targetMaterial.mainTextureOffset = Vector2.zero;
        }
    }
}