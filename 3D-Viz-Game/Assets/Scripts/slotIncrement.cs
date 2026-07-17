using UnityEngine;

public class slotIncrement : MonoBehaviour
{
    public int slotNum = 0; //To monitor which slot item goes to
    //Should not be < 0 or > 6

    public void slotInc()
    {
        if (slotNum < 6) slotNum++;
        
    }

    public void slotDec()
    {
       if (slotNum > 0) slotNum--;
    }

    public void slotReset()
    {
        slotNum = 0;
    }
}
