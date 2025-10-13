using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private List<Vector3> waypoints;
    private int currentIndex = 0;
    public float speed = 2f;
    
    public int CurrentIndex => currentIndex;

    private float formationOffset = 0f;
    
    private float originalSpeed;
    private Coroutine slowRoutine;
    private Coroutine stunRoutine;
    private bool isStunned = false;

    // shared stop flag for the group
    [HideInInspector] public BoolWrapper stopMovement;
    
    void Start()
    {
        originalSpeed = speed;
    }
    
    public void SetBaseSpeed(float newSpeed)
    {
        originalSpeed = newSpeed;
        speed = newSpeed;
    }

    public void InitPath(List<Vector2Int> path, GridManager grid, float offset = 0f, BoolWrapper groupStopFlag = null)
    {
        waypoints = new List<Vector3>();
        foreach (Vector2Int tile in path)
        {
            Vector3 wp = grid.GridToWorld(tile);
            wp.y = 1f; // ensure correct height
            waypoints.Add(wp);
        }

        formationOffset = offset;
        stopMovement = groupStopFlag;

        if (waypoints.Count > 1)
        {
            Vector3 firstDir = (waypoints[1] - waypoints[0]).normalized;
            transform.position = waypoints[0] - firstDir * formationOffset;
        }
        else
        {
            transform.position = waypoints[0];
        }
    }

    void Update()
    {
        if (waypoints == null || currentIndex >= waypoints.Count) return;
        if (stopMovement != null && stopMovement.value) return; // stop if group flag is true

        Vector3 target = waypoints[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Count && stopMovement != null)
            {
                // First enemy reached the end, stop the whole group
                stopMovement.value = true;
            }
        }
    }
    
    public void ApplySlow(float slowFactor, float duration)
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);
        slowRoutine = StartCoroutine(SlowCoroutine(slowFactor, duration));
    }

    private IEnumerator SlowCoroutine(float factor, float duration)
    {
        speed = originalSpeed * (1f - factor);
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
    }

    public void Stun(float duration)
    {
        if (stunRoutine != null)
            StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        float storedSpeed = speed;
        speed = 0f;

        yield return new WaitForSeconds(duration);

        speed = originalSpeed;
        isStunned = false;
    }
}

// Simple wrapper so we can share a bool between multiple enemies
[System.Serializable]
public class BoolWrapper
{
    public bool value;
}
