using System; // Action 사용을 위해 추가
using System.Collections;
using UnityEngine;
using Pathfinding;

public class AgentPathFinder : MonoBehaviour
{
    Seeker seeker;
    public bool IsMoving { get; private set; } = false;

    void Start()
    {
        seeker = GetComponent<Seeker>();
    }

    public void MoveToTarget(Vector3 targetPosition, float speed, Action onComplete = null, Action<float> onDirectionChange = null)
    {
        if (IsMoving) return;
        IsMoving = true;

        seeker.StartPath(transform.position, targetPosition, (Path p) =>
        {
            if (!p.error)
            {
                StartCoroutine(FollowPath(p, speed, onComplete, onDirectionChange));
            }
            else
            {
                IsMoving = false;
                onComplete?.Invoke();
            }
        });
    }

    public void StopMovement()
    {
        StopAllCoroutines();
        IsMoving = false;
    }

    private IEnumerator FollowPath(Path p, float speed, Action onComplete, Action<float> onDirectionChange)
    {
        for (int i = 0; i < p.vectorPath.Count; i++)
        {
            Vector3 currentWaypoint = p.vectorPath[i];

            while ((transform.position - currentWaypoint).sqrMagnitude > 0.004f)
            {
                float deltaX = currentWaypoint.x - transform.position.x;

                if (Mathf.Abs(deltaX) > 0.001f)
                {
                    onDirectionChange?.Invoke(deltaX);
                }

                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
                yield return null;
            }
        }

        IsMoving = false;
        onComplete?.Invoke();
    }
}