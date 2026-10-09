using System;
using UnityEngine;
using UnityEngine.AI;

public enum CuratorState { Patrol, Investigate, Chase, Stunned, Asleep }

/// <summary>
/// The Curator: blind, hunts by sound (GDD Exhibit VI).
/// Patrol -> Investigate (loudest noise heard) -> Chase (close and loud).
/// Stunned / Asleep are set by weapons through Stun() and Sleep().
///
/// Setup: put on a capsule with a NavMeshAgent. Bake a NavMesh first.
/// Assign patrol points. The player is found automatically.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CuratorAI : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float waitAtPoint = 2f;

    [Header("Speeds (m/s). Player: crouch 1.5, walk 3.0, sprint 5.5")]
    [SerializeField] float patrolSpeed = 2.0f;
    [SerializeField] float investigateSpeed = 3.5f;
    [SerializeField] float chaseSpeed = 4.5f; // faster than a walk, slower than a sprint

    [Header("Hearing")]
    [Tooltip("Meters of hearing range per point of loudness. Keep the same as PlayerDebugOverlay.")]
    [SerializeField] float metersPerNoiseUnit = 3f;
    [Tooltip("Ignore noises older than this (seconds) when deciding.")]
    [SerializeField] float noiseMemory = 4f;

    [Header("Chase")]
    [SerializeField] float chaseDistance = 8f;
    [Tooltip("Player noise must be at least this loud to start a chase (3 = walking).")]
    [SerializeField] float chaseNoiseMin = 3f;
    [SerializeField] float loseTrackAfter = 3f;

    [Header("Attack")]
    [SerializeField] float attackRange = 1.4f;
    [SerializeField] float attackCooldown = 1.5f;

    [SerializeField] PlayerMovement player;

    public CuratorState State { get; private set; } = CuratorState.Patrol;

    /// <summary>Fires when the Curator hits the player. The health system listens to this.</summary>
    public static event Action OnHitPlayer;

    NavMeshAgent agent;
    readonly NoiseHeap heard = new NoiseHeap();

    int patrolIndex;
    float waitTimer;
    float stateTimer;       // stun/sleep time left
    float loseTimer;
    float attackTimer;
    Vector3 investigateTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player == null)
        {
#if UNITY_2023_1_OR_NEWER
            player = FindFirstObjectByType<PlayerMovement>();
#else
            player = FindObjectOfType<PlayerMovement>();
#endif
        }
    }

    void OnEnable() { NoiseSystem.OnNoise += OnNoiseHeard; }
    void OnDisable() { NoiseSystem.OnNoise -= OnNoiseHeard; }

    void Start() { GoToPatrolPoint(); }

    // Only keep noises that are within hearing range of the Curator.
    void OnNoiseHeard(NoiseEvent e)
    {
        if (e.Source == gameObject) return;
        if (State == CuratorState.Stunned || State == CuratorState.Asleep) return;

        float range = e.Loudness * metersPerNoiseUnit;
        if ((e.Position - transform.position).sqrMagnitude <= range * range)
            heard.Push(e);
    }

    void Update()
    {
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        switch (State)
        {
            case CuratorState.Stunned:
            case CuratorState.Asleep:
                TickDisabled();
                break;
            case CuratorState.Patrol:
                CheckForNoise();
                TickPatrol();
                break;
            case CuratorState.Investigate:
                CheckForNoise();
                TickInvestigate();
                break;
            case CuratorState.Chase:
                TickChase();
                break;
        }
    }

    // ---------- Patrol ----------
    void TickPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                GoToPatrolPoint();
            }
        }
    }

    void GoToPatrolPoint()
    {
        State = CuratorState.Patrol;
        agent.speed = patrolSpeed;
        waitTimer = waitAtPoint;
        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    // ---------- Hearing (uses the heap) ----------
    void CheckForNoise()
    {
        // Pop the loudest recent noise, throw the rest away.
        NoiseEvent loudest = default;
        bool found = false;
        while (heard.TryPop(out NoiseEvent e))
        {
            if (Time.time - e.Time > noiseMemory) continue; // too old
            loudest = e;
            found = true;
            break;
        }
        heard.Clear();

        if (found)
        {
            investigateTarget = loudest.Position;
            State = CuratorState.Investigate;
            agent.speed = investigateSpeed;
            agent.SetDestination(investigateTarget);
        }

        TryStartChase();
    }

    void TickInvestigate()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            // Nothing here. Go back to patrol.
            GoToPatrolPoint();
        }
    }

    // ---------- Chase ----------
    void TryStartChase()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= chaseDistance && player.CurrentNoise >= chaseNoiseMin)
        {
            State = CuratorState.Chase;
            agent.speed = chaseSpeed;
            loseTimer = loseTrackAfter;
        }
    }

    void TickChase()
    {
        if (player == null) { GoToPatrolPoint(); return; }

        float dist = Vector3.Distance(transform.position, player.transform.position);
        bool loud = player.CurrentNoise >= chaseNoiseMin;

        // Blind: only follows while the player keeps making noise nearby.
        if (loud && dist <= chaseDistance * 1.5f) loseTimer = loseTrackAfter;
        else loseTimer -= Time.deltaTime;

        if (loseTimer <= 0f)
        {
            investigateTarget = player.transform.position;
            State = CuratorState.Investigate;
            agent.speed = investigateSpeed;
            agent.SetDestination(investigateTarget);
            return;
        }

        agent.SetDestination(player.transform.position);

        if (dist <= attackRange && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            OnHitPlayer?.Invoke();
        }
    }

    // ---------- Weapons call these ----------
    public void Stun(float seconds) { Disable(CuratorState.Stunned, seconds); }
    public void Sleep(float seconds) { Disable(CuratorState.Asleep, seconds); }

    void Disable(CuratorState s, float seconds)
    {
        State = s;
        stateTimer = seconds;
        agent.isStopped = true;
        heard.Clear();
    }

    void TickDisabled()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            agent.isStopped = false;
            GoToPatrolPoint();
        }
    }

    // ---------- Debug ----------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(12, Screen.height - 30, 300, 20), $"Curator: {State}");
    }
}