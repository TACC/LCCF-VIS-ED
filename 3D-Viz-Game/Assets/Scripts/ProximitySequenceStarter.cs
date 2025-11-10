using UnityEngine;
using UnityEngine.AI;

public class ProximitySequencerStarter : MonoBehaviour
{
    [Header("Who to start")]
    public SimpleClipSequencer sequencer;   // drag your sequencer component here

    [Header("Where to start")]
    public Transform startPoint;            // empty Transform at the spot
    public float startRadius = 0.9f;        // start when within this distance

    [Header("Optional")]
    public NavMeshAgent agent;              // if you use NavMesh; otherwise leave null
    public bool useRemainingDistance = false; // true = use agent.remainingDistance

    bool started;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (started || sequencer == null || startPoint == null) return;

        float dist = useRemainingDistance && agent != null && agent.enabled && !agent.pathPending && agent.hasPath
            ? agent.remainingDistance
            : Vector3.Distance(transform.position, startPoint.position);

        if (dist <= startRadius)
        {
            sequencer.StartSequence();
            started = true; // one-shot
        }
    }
}
