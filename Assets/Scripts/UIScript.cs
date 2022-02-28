using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Pathfinding;

public class UIScript : MonoBehaviour
{
    public LinesGrid grid;
    public TextMeshProUGUI score, time, info;
    public SpriteRenderer[] queue;

    public Sprite normalQueueSprite, doubleQueueSprite, wildcardQueueSprite;

    void Update()
    {
        score.text = grid.Score.ToString();
        time.text = Mathf.RoundToInt(grid.Timer).ToString();
        for (int i = 0; i < 3; i++)
        {
            if (i < grid.queue.Count)
            {
                queue[i].sprite = normalQueueSprite;
                switch (grid.queue[i].ball.type)
                {
                    case BallType.Ghost:
                        queue[i].color = Ball.convertToColor(grid.queue[i].ball.color);
                        queue[i].color = new Color(queue[i].color.r, queue[i].color.g, queue[i].color.b, 0.5f);
                        continue;
                    case BallType.Bomb:
                        queue[i].color = Color.black;
                        continue;
                    case BallType.Double:
                        queue[i].sprite = doubleQueueSprite;
                        queue[i].color = Ball.convertToColor(grid.queue[i].ball.color);
                        continue;
                    case BallType.Wildcard:
                        queue[i].sprite = wildcardQueueSprite;
                        queue[i].color = Color.white;
                        continue;
                    default:
                        queue[i].color = Ball.convertToColor(grid.queue[i].ball.color);
                        continue;
                }
            }
            else
                queue[i].color = new Color(0, 0, 0, 0);
        }

        if (grid.isGameOver)
            info.text = "GAME OVER.";
        else
        {
            if (grid.isPaused)
                info.text = "PAUSED.";
            else {
                if (grid.isFocusMoving)
                    info.text = "MOVING...";
                else
                {
                    Vector3 MousePos = LinesGrid.ConvertMousePositionToCell(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                    if ((int)MousePos.x < 0 || (int)MousePos.x > 8 || (int)MousePos.y < 0 || (int)MousePos.y > 8)
                    {
                        if (grid.currentFocus == null)
                            info.text = "SELECT A BALL.";
                        else
                        {
                            string currentFocusInfo = "FOCUSING ";
                            switch (grid.currentFocus.type)
                            {
                                case BallType.Bomb:
                                    currentFocusInfo += "BOMB";
                                    break;
                                case BallType.Ghost:
                                    currentFocusInfo += grid.currentFocus.color + " GHOST";
                                    break;
                                case BallType.Double:
                                    currentFocusInfo += "DOUBLE " + grid.currentFocus.color;
                                    break;
                                case BallType.Wildcard:
                                    currentFocusInfo += "WILDCARD";
                                    break;
                                default:
                                    currentFocusInfo += grid.currentFocus.color;
                                    break;
                            }
                            currentFocusInfo += " AT (" +
                            Mathf.RoundToInt(grid.currentFocus.transform.position.x) + ", " + Mathf.RoundToInt(grid.currentFocus.transform.position.y)
                            + "). SELECT NEW POSITION TO MOVE BALL.";
                            info.text = currentFocusInfo;
                        }
                    }
                    else
                    {
                        string location = "(" + (int)MousePos.x + ", " + (int)MousePos.y + ") - ";
                        if (grid.currentFocus == null)
                        {

                            for (int i = 0; i < grid.queue.Count; i++)
                            {
                                if (grid.queue[i].x == (int)MousePos.x && grid.queue[i].y == (int)MousePos.y)
                                {
                                    switch (grid.queue[i].ball.type)
                                    {
                                        case BallType.Bomb:
                                            location += "BOMB, QUEUED.";
                                            break;
                                        case BallType.Double:
                                            location += "DOUBLE " + grid.queue[i].ball.color + ", QUEUED.";
                                            break;
                                        case BallType.Ghost:
                                            location += grid.queue[i].ball.color + " GHOST, QUEUED.";
                                            break;
                                        case BallType.Wildcard:
                                            location += "WILDCARD, QUEUED.";
                                            break;
                                        default:
                                            location += grid.queue[i].ball.color + ", QUEUED.";
                                            break;
                                    }
                                    info.text = location;
                                    return;
                                }
                            }
                            if (grid.gridData[(int)MousePos.x, (int)MousePos.y] == null)
                                location += "NO DATA AVAILABLE.";
                            else
                            {
                                switch (grid.gridData[(int)MousePos.x, (int)MousePos.y].type)
                                {
                                    case BallType.Bomb:
                                        location += "BOMB. CLICK TO SELECT.";
                                        break;
                                    case BallType.Double:
                                        location += "DOUBLE " + grid.gridData[(int)MousePos.x, (int)MousePos.y].color + ". CLICK TO SELECT.";
                                        break;
                                    case BallType.Ghost:
                                        location += grid.gridData[(int)MousePos.x, (int)MousePos.y].color + " GHOST. CLICK TO SELECT.";
                                        break;
                                    case BallType.Wildcard:
                                        location += "WILDCARD. CLICK TO SELECT.";
                                        break;
                                    default:
                                        location += grid.gridData[(int)MousePos.x, (int)MousePos.y].color + ". CLICK TO SELECT.";
                                        break;
                                }
                            }              
                            info.text = location;
                        }
                        else
                        {
                            if (grid.gridData[(int)MousePos.x, (int)MousePos.y] != null)
                            {
                                if (grid.gridData[(int)MousePos.x, (int)MousePos.y] == grid.currentFocus)
                                {
                                    switch (grid.currentFocus.type)
                                    {
                                        case BallType.Bomb:
                                            location += "BOMB, FOCUSED. CLICK TO UNFOCUS.";
                                            break;
                                        case BallType.Double:
                                            location += "DOUBLE " + grid.currentFocus.color + ", FOCUSED. CLICK TO UNFOCUS.";
                                            break;
                                        case BallType.Ghost:
                                            location += grid.currentFocus.color + " GHOST, FOCUSED. CLICK TO UNFOCUS.";
                                            break;
                                        case BallType.Wildcard:
                                            location += "WILDCARD, FOCUSED. CLICK TO UNFOCUS.";
                                            break;
                                        default:
                                            location += grid.currentFocus.color + ", FOCUSED. CLICK TO UNFOCUS.";
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (grid.gridData[(int)MousePos.x, (int)MousePos.y].type)
                                    {
                                        case BallType.Bomb:
                                            location += "BOMB. CLICK TO SET NEW FOCUS.";
                                            break;
                                        case BallType.Double:
                                            location += "DOUBLE " + grid.gridData[(int)MousePos.x, (int)MousePos.y].color + ". CLICK TO SET NEW FOCUS.";
                                            break;
                                        case BallType.Ghost:
                                            location += grid.gridData[(int)MousePos.x, (int)MousePos.y].color + " GHOST. CLICK TO SET NEW FOCUS.";
                                            break;
                                        case BallType.Wildcard:
                                            location += "WILDCARD. CLICK TO SET NEW FOCUS.";
                                            break;
                                        default:
                                            location += grid.gridData[(int)MousePos.x, (int)MousePos.y].color + ". CLICK TO SET NEW FOCUS.";
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                if (grid.currentFocus.type == BallType.Ghost)
                                {
                                    for (int i = 0; i < grid.queue.Count; i++)
                                    {
                                        if (grid.queue[i].x == (int)MousePos.x && grid.queue[i].y == (int)MousePos.y)
                                        {
                                            location += grid.queue[i].ball.color + ", QUEUED. MOVING CURRENT FOCUS HERE WILL DESTROY THE QUEUED BALL.";
                                            info.text = location;
                                            return;
                                        }
                                    }
                                    location += " NO DATA AVAILABLE. CLICK TO MOVE BALL TO THIS POSITION.";
                                }
                                else
                                {
                                    GraphNode node1 = AstarPath.active.GetNearest(grid.currentFocus.transform.position).node;
                                    GraphNode node2 = AstarPath.active.GetNearest(MousePos, NNConstraint.Default).node;

                                    if (PathUtilities.IsPathPossible(node1, node2))
                                    {
                                        for (int i = 0; i < grid.queue.Count; i++)
                                        {
                                            if (grid.queue[i].x == (int)MousePos.x && grid.queue[i].y == (int)MousePos.y)
                                            {
                                                location += grid.queue[i].ball.color + ", QUEUED. MOVING CURRENT FOCUS HERE WILL DESTROY THE QUEUED BALL.";
                                                info.text = location;
                                                return;
                                            }
                                        }
                                        location += " NO DATA AVAILABLE. CLICK TO MOVE BALL TO THIS POSITION.";
                                    }
                                    else
                                        location += " PATHNOTFOUND EXCEPTION. ONE OR MORE ENTITIES IS BLOCKING THE CURRENT BALL FROM REACHING THIS POSITION.";
                                }
                            }
                            info.text = location;
                        }
                    }
                }
            }
        }
    }
}
