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

    void Update()
    {
        score.text = grid.Score.ToString();
        time.text = Mathf.RoundToInt(grid.Timer).ToString();
        for (int i = 0; i < 3; i++)
        {
            if (i < grid.queue.Count)
                queue[i].color = Ball.convertToColor(grid.queue[i].ball.color);
            else
                queue[i].color = Color.white;
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
                            string currentFocusInfo = "FOCUSING " + grid.currentFocus.color + " AT (" +
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
                                    location += grid.queue[i].ball.color + ", QUEUED.";
                                    info.text = location;
                                    return;
                                }
                            }
                            if (grid.gridData[(int)MousePos.x, (int)MousePos.y] == null)
                                location += "NO DATA AVAILABLE.";
                            else
                                location += grid.gridData[(int)MousePos.x, (int)MousePos.y].color + ". CLICK TO SELECT.";
                            info.text = location;
                        }
                        else
                        {
                            if (grid.gridData[(int)MousePos.x, (int)MousePos.y] != null)
                            {
                                if (grid.gridData[(int)MousePos.x, (int)MousePos.y] == grid.currentFocus)
                                    location += grid.currentFocus.color + ", FOCUSED. CLICK TO UNFOCUS.";
                                else
                                    location += grid.gridData[(int)MousePos.x, (int)MousePos.y].color + ". CLICK TO SET NEW FOCUS.";
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
                            info.text = location;
                        }
                    }
                }
            }
        }
    }
}
