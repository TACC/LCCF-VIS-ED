using UnityEngine;

namespace My.DemoScene
{

    public class FootShrinkUI : MonoBehaviour
    {
        public GameObject[] targetObjects;
        public SkinnedMeshRenderer skinnedMesh;
        public int blendshapeIndex;

        void Update()
        {
            if (skinnedMesh == null) return;

            bool anyActive = false;
            foreach (var obj in targetObjects)
            {
                if (obj != null && obj.activeSelf)
                {
                    anyActive = true;
                    break;
                }
            }

            skinnedMesh.SetBlendShapeWeight(blendshapeIndex, anyActive ? 100f : 0f);
        }
    }
}