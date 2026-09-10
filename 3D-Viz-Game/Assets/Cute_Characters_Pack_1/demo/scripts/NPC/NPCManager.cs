using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        [Header("Waypoints")]
        public List<Transform> waypoints = new List<Transform>();

        [Header("Movement Settings")]
        [Range(0f, 1f)]
        [Tooltip("0 = all walk, 1 = all run")]
        public float runRatio = 0.3f;
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public float rotationSpeed = 5f;

        [Header("Idle Settings")]
        public float minIdleTime = 2f;
        public float maxIdleTime = 5f;

        [Header("Wander Settings")]
        [Tooltip("Side offset strength")]
        public float wanderStrength = 1.5f;
        [Tooltip("Side offset frequency")]
        public float wanderFrequency = 0.8f;
        [Tooltip("How close to waypoint counts as arrived")]
        public float waypointArrivalRadius = 2f;

        [Header("Obstacle Avoidance")]
        [Tooltip("How close before steering away")]
        public float obstacleDetectDistance = 1.2f;
        public List<Collider> obstacles = new List<Collider>();

        [Header("Wave Interaction")]
        [Tooltip("Distance to trigger wave")]
        public float waveDetectDistance = 2.5f;
        [Range(0f, 1f)]
        [Tooltip("Chance to interact when another NPC is nearby (0 = never, 1 = always)")]
        public float interactionChance = 0.6f;
        [Tooltip("Delay before responder waves back")]
        public float waveResponseDelay = 0.5f;
        [Tooltip("How long wave animation plays")]
        public float waveDuration = 2f;
        [Tooltip("Short pause after waving before moving on")]
        public float wavePostPauseDuration = 0.5f;
        [Tooltip("Minimum time before same two NPCs wave again")]
        public float waveCooldown = 30f;
        [Range(0f, 1f)]
        [Tooltip("Chance the other NPC ignores the wave (causing initiator to get disappointed)")]
        public float disappointedNoResponseChance = 0.15f;

        [Header("Talk Interaction")]
        [Range(0f, 1f)]
        [Tooltip("Chance to start talking after wave")]
        public float talkChance = 0.5f;
        public int minTalkRounds = 1;
        public int maxTalkRounds = 3;
        [Tooltip("Duration of each talk round")]
        public float talkRoundDuration = 2f;
        [Tooltip("Duration of Yes/No reaction")]
        public float talkReactionDuration = 1f;

        [Header("Disappointed Settings")]
        [Range(0f, 1f)]
        [Tooltip("Chance to get disappointed after talk ends")]
        public float disappointedAfterTalkChance = 0.2f;
        [Tooltip("How long disappointed animation plays")]
        public float disappointedDuration = 3f;

        [Header("Dance Settings")]
        [Range(0f, 1f)]
        [Tooltip("Chance to start dancing during talk")]
        public float danceChance = 0.3f;
        [Range(0f, 1f)]
        [Tooltip("Chance for nearby NPCs to join the dance")]
        public float joinDanceChance = 0.5f;
        [Tooltip("Radius to invite nearby NPCs to join")]
        public float danceInviteRadius = 5f;
        [Tooltip("How long the group dances")]
        public float danceDuration = 8f;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public IEnumerator StartGroupDance(NPCController initiatorA, NPCController initiatorB)
        {
            List<NPCController> dancers = new List<NPCController> { initiatorA, initiatorB };

            NPCController[] allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            foreach (NPCController npc in allNPCs)
            {
                if (npc == null || npc == initiatorA || npc == initiatorB) continue;
                if (npc.isBusy) continue;
                if (Vector3.Distance(initiatorA.transform.position, npc.transform.position) > danceInviteRadius) continue;
                if (Random.value <= joinDanceChance)
                    dancers.Add(npc);
            }

            foreach (NPCController npc in dancers)
                npc.StartDancing();

            yield return new WaitForSeconds(danceDuration);

            foreach (NPCController npc in dancers)
                npc.StopDancing();
        }

        public Transform GetRandomWaypoint(Transform current = null)
        {
            return GetNearestWaypoint(null, current);
        }

        [Range(0f, 1f)]
        public float backtrackChance = 0.15f;

        public Transform GetNearestWaypoint(Vector3? fromPosition, Transform exclude = null, Transform previous = null)
        {
            if (waypoints.Count == 0) return null;
            if (waypoints.Count == 1) return waypoints[0];

            if (fromPosition == null)
            {
                Transform selected;
                int attempts = 0;
                do
                {
                    selected = waypoints[Random.Range(0, waypoints.Count)];
                    attempts++;
                }
                while (selected == exclude && attempts < 10);
                return selected;
            }

            List<Transform> candidates = new List<Transform>();
            foreach (Transform wp in waypoints)
            {
                if (wp == null || wp == exclude) continue;
                candidates.Add(wp);
            }

            candidates.Sort((a, b) =>
                Vector3.Distance(fromPosition.Value, a.position)
                .CompareTo(Vector3.Distance(fromPosition.Value, b.position)));

            foreach (Transform wp in candidates)
            {
                if (wp == previous && Random.value > backtrackChance)
                    continue;
                return wp;
            }

            return candidates.Count > 0 ? candidates[0] : null;
        }

        public bool ShouldRun()
        {
            return Random.value < runRatio;
        }

        public bool RaycastObstacles(Vector3 origin, Vector3 direction, float distance)
        {
            if (obstacles.Count == 0) return false;

            Ray ray = new Ray(origin, direction);
            foreach (Collider col in obstacles)
            {
                if (col == null) continue;
                if (col.Raycast(ray, out _, distance))
                    return true;
            }
            return false;
        }

        void OnDrawGizmos()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.cyan;
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    Gizmos.DrawSphere(wp.position, 0.3f);
            }
        }
    }
}