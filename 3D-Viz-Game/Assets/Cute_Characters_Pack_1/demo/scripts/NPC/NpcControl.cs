using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    [RequireComponent(typeof(CharacterController))]
    public class NPCController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Debug")]
        [SerializeField] private bool showRaycast = true;

        // Animator layer indexes
        private const int LAYER_BASE = 0;
        private const int LAYER_WAVE = 1;
        private const int LAYER_YES  = 2;
        private const int LAYER_TALK = 3;
        private const int LAYER_NO    = 4;
        private const int LAYER_LAUGH  = 5;

        private CharacterController characterController;
        private NPCManager manager;

        // Movement
        private Transform targetWaypoint;
        private bool isRunning;
        private float currentSpeed;
        private float velocityY;

        // Wander
        private float wanderOffset;
        private float wanderTimer;

        // Obstacle avoidance
        private Vector3 avoidanceDir = Vector3.zero;
        private float avoidanceTimer = 0f;
        private bool obstacleDetectedLastFrame = false;
        private const float AVOIDANCE_DURATION = 1.2f;

        // Stuck detection
        private Vector3 lastPosition;
        private float stuckTimer = 0f;
        private const float STUCK_CHECK_INTERVAL = 1f;
        private const float STUCK_THRESHOLD = 0.1f;

        // Interaction
        private Dictionary<NPCController, float> waveHistory = new Dictionary<NPCController, float>();
        public bool isBusy = false;

        // Dance
        public Transform currentWaypoint => targetWaypoint;
        private Transform previousWaypoint;
        private bool isDancing = false;

        // State
        public enum State { Wander, Idle, Waving, WaveResponding, Talking, Disappointed, Paused, Dancing }
        public State currentState;
        private float idleTimer;

        // Look around
        private float lookTimer;
        private float lookTargetAngle;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            manager = NPCManager.Instance;

            if (animator == null)
                animator = GetComponent<Animator>();

            wanderOffset = Random.Range(0f, 100f);
            lastPosition = transform.position;

            PickNewWaypoint();
        }

        void Update()
        {
            ApplyGravity();

            switch (currentState)
            {
                case State.Wander: UpdateWander(); CheckForNearbyNPCs(); break;
                case State.Idle:   UpdateIdle();   CheckForNearbyNPCs(); break;
                case State.Paused:        break;
                case State.Waving:        break;
                case State.WaveResponding:break;
                case State.Talking:       break;
                case State.Disappointed:  break;
                case State.Dancing:       break;
            }

            UpdateAnimation();
        }

        // ─── WANDER ───────────────────────────────────────────

        public void PickNewWaypoint()
        {
            if (manager == null) return;

            // Pick nearest waypoint, avoiding going back to previous one
            Transform next = manager.GetNearestWaypoint(transform.position, targetWaypoint, previousWaypoint);
            previousWaypoint = targetWaypoint;
            targetWaypoint = next;
            isRunning = manager.ShouldRun();
            currentSpeed = isRunning ? manager.runSpeed : manager.walkSpeed;
            currentState = State.Wander;
            avoidanceTimer = 0f;
            stuckTimer = 0f;
            lastPosition = transform.position;
            isBusy = false;
            isDancing = false;
        }

        void GoToNearestWaypoint()
        {
            if (manager == null) return;

            Transform nearest = manager.GetNearestWaypoint(transform.position, targetWaypoint);
            if (nearest != null)
            {
                targetWaypoint = nearest;
                avoidanceTimer = 0f;
                stuckTimer = 0f;
                lastPosition = transform.position;
            }
        }

        void UpdateWander()
        {
            if (targetWaypoint == null) { PickNewWaypoint(); return; }

            Vector3 toTarget = targetWaypoint.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance < manager.waypointArrivalRadius)
            {
                EnterIdle();
                return;
            }

            stuckTimer += Time.deltaTime;
            if (stuckTimer >= STUCK_CHECK_INTERVAL)
            {
                float moved = Vector3.Distance(transform.position, lastPosition);
                if (moved < STUCK_THRESHOLD)
                    GoToNearestWaypoint();

                lastPosition = transform.position;
                stuckTimer = 0f;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            obstacleDetectedLastFrame = manager.RaycastObstacles(rayOrigin, transform.forward, manager.obstacleDetectDistance);

            if (obstacleDetectedLastFrame && avoidanceTimer <= 0f)
            {
                float side = Random.value > 0.5f ? 1f : -1f;
                avoidanceDir = Quaternion.Euler(0f, 60f * side, 0f) * transform.forward;
                avoidanceTimer = AVOIDANCE_DURATION;
            }

            Vector3 moveDir;

            if (avoidanceTimer > 0f)
            {
                avoidanceTimer -= Time.deltaTime;
                moveDir = avoidanceDir;
            }
            else
            {
                wanderTimer += Time.deltaTime * manager.wanderFrequency;
                float sideOffset = Mathf.Sin(wanderTimer + wanderOffset) * manager.wanderStrength;
                Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
                moveDir = (toTarget.normalized + right * sideOffset * (1f / Mathf.Max(distance, 1f))).normalized;
            }

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, manager.rotationSpeed * Time.deltaTime);

            characterController.Move(moveDir * currentSpeed * Time.deltaTime);
        }

        // ─── IDLE ─────────────────────────────────────────────

        public void EnterIdle()
        {
            currentState = State.Idle;
            idleTimer = Random.Range(manager.minIdleTime, manager.maxIdleTime);
            lookTimer = 0f;
            PickNewLookAngle();
        }

        void UpdateIdle()
        {
            idleTimer -= Time.deltaTime;
            lookTimer -= Time.deltaTime;

            if (lookTimer <= 0f)
                PickNewLookAngle();

            float smoothAngle = Mathf.LerpAngle(transform.eulerAngles.y, lookTargetAngle, 2f * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            if (idleTimer <= 0f)
                PickNewWaypoint();
        }

        void PickNewLookAngle()
        {
            lookTargetAngle = transform.eulerAngles.y + Random.Range(-80f, 80f);
            lookTimer = Random.Range(1f, 2.5f);
        }

        // ─── NPC DETECTION ────────────────────────────────────

        void CheckForNearbyNPCs()
        {
            if (isBusy) return;

            Collider[] nearby = Physics.OverlapSphere(transform.position, manager.waveDetectDistance);

            foreach (Collider col in nearby)
            {
                if (col.gameObject == gameObject) continue;

                NPCController other = col.GetComponent<NPCController>();
                if (other == null || other.isBusy) continue;

                if (waveHistory.ContainsKey(other))
                {
                    if (Time.time - waveHistory[other] < manager.waveCooldown)
                        continue;
                }

                if (Random.value <= manager.interactionChance)
                    StartCoroutine(InitiateInteraction(other));
                return;
            }
        }

        // ─── INTERACTION ──────────────────────────────────────

        IEnumerator InitiateInteraction(NPCController other)
        {
            isBusy = true;
            other.isBusy = true;

            currentState = State.Waving;
            other.currentState = State.Paused;

            FaceTarget(other.transform);
            other.FaceTarget(transform);

            // Initiator waves
            animator.Play("Wave", LAYER_WAVE);

            // Does the other NPC want to respond?
            bool otherWantsToRespond = Random.value > manager.disappointedNoResponseChance;

            if (!otherWantsToRespond)
            {
                // Scenario 1: Other NPC ignores — just looks, no wave back
                yield return new WaitForSeconds(manager.waveDuration);

                // Other runs away, initiator gets disappointed
                other.RunAway();
                yield return StartCoroutine(PlayDisappointed(this));
            }
            else
            {
                // Respond with wave
                yield return new WaitForSeconds(manager.waveResponseDelay);
                other.currentState = State.WaveResponding;
                other.animator.Play("Wave", LAYER_WAVE);

                yield return new WaitForSeconds(manager.waveDuration);

                // Log cooldown
                float now = Time.time;
                waveHistory[other] = now;
                other.waveHistory[this] = now;

                // 50% chance to start talking
                if (Random.value < manager.talkChance)
                {
                    yield return StartCoroutine(TalkSequence(other));
                }
                else
                {
                    // Short pause then resume
                    yield return new WaitForSeconds(manager.wavePostPauseDuration);
                    ResumeWander();
                    other.ResumeWander();
                }
            }
        }

        IEnumerator TalkSequence(NPCController other)
        {
            currentState = State.Talking;
            other.currentState = State.Talking;

            FaceTarget(other.transform);
            other.FaceTarget(transform);

            // How many talk rounds
            int rounds = Random.Range(manager.minTalkRounds, manager.maxTalkRounds + 1);

            for (int i = 0; i < rounds; i++)
            {
                // Alternate who talks, with occasional Yes/No reactions
                animator.Play("Talk", LAYER_TALK);
                yield return new WaitForSeconds(manager.talkRoundDuration);

                // Other reacts with Yes, No or Laugh
                PlayReaction(other.animator);
                yield return new WaitForSeconds(manager.talkReactionDuration);

                // Swap roles next round
                other.animator.Play("Talk", LAYER_TALK);
                yield return new WaitForSeconds(manager.talkRoundDuration);

                // Self reacts
                PlayReaction(animator);

                yield return new WaitForSeconds(manager.talkReactionDuration);
            }

            // Dance chance after talk
            if (Random.value < manager.danceChance)
            {
                yield return StartCoroutine(manager.StartGroupDance(this, other));
                ResumeWander();
                other.ResumeWander();
                yield break;
            }

            // Post-talk: disappointed check
            bool selfDisappointed  = Random.value < manager.disappointedAfterTalkChance;
            bool otherDisappointed = Random.value < manager.disappointedAfterTalkChance;

            // Goodbye wave before parting
            animator.Play("Wave", LAYER_WAVE);
            other.animator.Play("Wave", LAYER_WAVE);
            yield return new WaitForSeconds(manager.waveDuration);

            // Scenario 2: one or both disappointed after talk
            if (selfDisappointed && !otherDisappointed)
            {
                // Self disappointed, other runs away
                other.RunAway();
                yield return StartCoroutine(PlayDisappointed(this));
            }
            else if (!selfDisappointed && otherDisappointed)
            {
                // Other disappointed, self runs away
                RunAway();
                yield return StartCoroutine(PlayDisappointed(other));
            }
            else if (selfDisappointed && otherDisappointed)
            {
                // Both disappointed simultaneously
                yield return StartCoroutine(PlayDisappointed(this));
                yield return StartCoroutine(PlayDisappointed(other));
            }
            else
            {
                ResumeWander();
                other.ResumeWander();
            }
        }

        IEnumerator PlayDisappointed(NPCController npc)
        {
            npc.currentState = State.Disappointed;
            npc.animator.Play("Disappointed", LAYER_BASE);
            yield return new WaitForSeconds(manager.disappointedDuration);
            npc.ResumeWander();
        }

        void RunAway()
        {
            isBusy = false;
            isRunning = true;
            currentSpeed = manager.runSpeed;
            PickNewWaypoint();
        }

        void ResumeWander()
        {
            isBusy = false;
            PickNewWaypoint();
        }

        void PlayReaction(Animator anim)
        {
            int reaction = Random.Range(0, 3);
            if (reaction == 0)      anim.Play("Yes",   LAYER_YES);
            else if (reaction == 1) anim.Play("No",    LAYER_NO);
            else                    anim.Play("Laugh",  LAYER_LAUGH);
        }

        void FaceTarget(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // ─── DANCE ────────────────────────────────────────────

        public void StartDancing()
        {
            if (isDancing) return;
            isDancing = true;
            isBusy = true;
            currentState = State.Dancing;
            animator.Play("HappyDance", LAYER_BASE);
        }

        public void StopDancing()
        {
            isDancing = false;
            animator.Play("Locomotion", LAYER_BASE);
            PickNewWaypoint();
        }

        // ─── GRAVITY ──────────────────────────────────────────

        void ApplyGravity()
        {
            if (characterController.isGrounded && velocityY < 0f)
                velocityY = -2f;

            velocityY += Physics.gravity.y * Time.deltaTime;
            characterController.Move(Vector3.up * velocityY * Time.deltaTime);
        }

        // ─── ANIMATION ────────────────────────────────────────

        void UpdateAnimation()
        {
            if (animator == null) return;

            bool isStationary = currentState == State.Idle      ||
                                currentState == State.Paused     ||
                                currentState == State.Waving     ||
                                currentState == State.WaveResponding ||
                                currentState == State.Talking    ||
                                currentState == State.Disappointed ||
                                currentState == State.Dancing;

            if (isStationary)
                animator.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
            else
            {
                float animSpeed = isRunning ? 1f : 0.333f;
                animator.SetFloat("Speed", animSpeed, 0.15f, Time.deltaTime);
            }
        }

        // ─── DEBUG ────────────────────────────────────────────

        void OnDrawGizmos()
        {
            if (!showRaycast || !Application.isPlaying || manager == null) return;

            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Gizmos.color = obstacleDetectedLastFrame ? Color.red : Color.green;
            Gizmos.DrawRay(rayOrigin, transform.forward * manager.obstacleDetectDistance);
            Gizmos.DrawSphere(rayOrigin + transform.forward * manager.obstacleDetectDistance, 0.05f);

            Gizmos.color = new Color(1f, 1f, 0f, 0.08f);
            Gizmos.DrawSphere(transform.position, manager.waveDetectDistance);
        }
    }
}