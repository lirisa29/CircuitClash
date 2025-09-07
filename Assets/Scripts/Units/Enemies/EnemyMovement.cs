using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private List<Vector3> waypoints;
    private int currentIndex = 0;
    public float speed = 2f;
    
    public int CurrentIndex => currentIndex;

    private float formationOffset = 0f;

    // shared stop flag for the group
    [HideInInspector] public BoolWrapper stopMovement;

    public void InitPath(List<Vector2Int> path, GridManager grid, float offset = 0f, BoolWrapper groupStopFlag = null)
    {
        waypoints = new List<Vector3>();
        foreach (Vector2Int tile in path)
        {
            Vector3 wp = grid.GridToWorld(tile);
            wp.y = 1.5f; // ensure correct height
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
}

// Simple wrapper so we can share a bool between multiple enemies
[System.Serializable]
public class BoolWrapper
{
    public bool value;
}
