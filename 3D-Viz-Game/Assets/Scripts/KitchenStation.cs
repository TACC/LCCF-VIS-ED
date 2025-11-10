using UnityEngine;
using UnityEngine.Playables;

public class KitchenStation : MonoBehaviour
{
    public Collider triggerZone;         // a trigger around the station
    public PlayableDirector knifeDirector;
    public float autoStopDelay = 0.1f;

    int occupants = 0;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        occupants++;
        if (occupants == 1 && knifeDirector) knifeDirector.Play();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        occupants = Mathf.Max(0, occupants - 1);
        if (occupants == 0 && knifeDirector) Invoke(nameof(StopKnife), autoStopDelay);
    }

    void StopKnife()
    {
        if (occupants == 0) knifeDirector.Stop(); // or .Pause() if you want to resume mid-clip
    }
}
