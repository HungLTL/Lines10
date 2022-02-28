using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class MovePositionAStarPathfinding : MonoBehaviour, IMovePosition
{
    AILerp aiLerp;
    public Transform target;

    private void Awake()
    {
        aiLerp = GetComponent<AILerp>();
        
    }

    private void Update()
    {
        //SetMovePosition(target.position);

        if (Input.GetMouseButtonDown(0))
        {
            SetMovePosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    public void SetMovePosition(Vector3 movePosition)
    {
        aiLerp.destination = movePosition;
    }
}
