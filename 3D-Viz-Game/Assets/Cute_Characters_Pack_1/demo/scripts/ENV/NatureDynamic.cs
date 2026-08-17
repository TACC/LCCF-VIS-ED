using UnityEngine;

namespace My.DemoScene
{

    public class SkyRotator : MonoBehaviour
    {
        public Material skyboxMaterial;
        public float rotationSpeed = 1f;

        private float _angle;

        void Start()
        {
            if (skyboxMaterial != null)
                _angle = skyboxMaterial.GetFloat("_Rotation");
        }

        void Update()
        {
            if (skyboxMaterial == null) return;
            _angle = (_angle + rotationSpeed * Time.deltaTime) % 360f;
            skyboxMaterial.SetFloat("_Rotation", _angle);
        }

        void OnDisable()
        {
            if (skyboxMaterial != null)
                skyboxMaterial.SetFloat("_Rotation", 0f);
        }
    }
}