using UnityEngine;

namespace My.DemoScene
{

    public class CurtainAnimationBridge : MonoBehaviour
    {
        public CurtainUI curtainUI;

        public void curtainclosed()
        {
            curtainUI.curtainclosed();
        }

        public void curtainanimfinished()
        {
            curtainUI.curtainanimfinished();
        }
    }
}