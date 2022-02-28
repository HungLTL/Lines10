using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class LinesGrid : MonoBehaviour
{

    [SerializeField]
    private GameObject gridCell;
    [SerializeField]
    private GameObject ball;
 
    Vector3 destination;
    public bool isFocusMoving { get; private set; }
    public bool isPaused { get; private set; }
    public bool isGameOver{get; private set;}

    public float Timer { get; private set; }
    public int Score { get; private set; }

    public List<BallQueue> queue { get; private set; }
    public Ball currentFocus { get; private set; }
    public Ball[,] gridData { get; private set; }

    public int width, height;

    private void Awake()
    {
        gridData = new Ball[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Instantiate(gridCell, new Vector3(x, y, 0), Quaternion.identity);
                gridData[x, y] = null;
            }
        }
    }

    private void Start()
    {
        isFocusMoving = false;
        isPaused = false;
        isGameOver = false;

        Score = 0;
        Timer = 0f;

        GenerateNewGrid();
        AstarPath.active.Scan();

        queue = new List<BallQueue>();
        GenerateNewBalls();
    }

    private void Update()
    {
        AstarPath.active.Scan();

        if (!isPaused && !isGameOver)
            Timer += Time.deltaTime;

        //ghostball logic - temporarily make all active balls use the layermask that's not detected by the pathfinding graph
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        if (currentFocus != null)
        {
            if (currentFocus.type == BallType.Ghost)
            {
                for (int i = 0; i < balls.Length; i++)
                    balls[i].GetComponent<Ball>().isGhosted = true;
            } else
            {
                for (int i = 0; i < balls.Length; i++)
                    balls[i].GetComponent<Ball>().isGhosted = false;
            }    
        } else
        {
            for (int i = 0; i < balls.Length; i++)
                balls[i].GetComponent<Ball>().isGhosted = false;
        }

        if (Input.GetMouseButtonDown(0) && !isFocusMoving && !isPaused && !isGameOver)
        {
            Vector3 MousePos = ConvertMousePositionToCell(Camera.main.ScreenToWorldPoint(Input.mousePosition));

            if (MousePos.x >= 0 && MousePos.x < width && MousePos.y >= 0 && MousePos.y < width) // this check ensures the player is interacting within the board
            {
                if (currentFocus == null)
                {
                    if (gridData[(int)MousePos.x, (int)MousePos.y] != null) // no ball selected, clicking on ball => focus on ball
                    {
                        gridData[(int)MousePos.x, (int)MousePos.y].ToggleFocus();
                        currentFocus = gridData[(int)MousePos.x, (int)MousePos.y];
                    }
                } else
                {
                    if (gridData[(int)MousePos.x, (int)MousePos.y] != null) // ball selected, selecting a ball
                    {
                        if (currentFocus == gridData[(int)MousePos.x, (int)MousePos.y]) // if selecting the current ball
                        {
                            currentFocus.ToggleFocus();
                            currentFocus = null;
                        } else // if selecting another ball, unfocus the current ball and switch focus
                        {
                            currentFocus.ToggleFocus();
                            gridData[(int)MousePos.x, (int)MousePos.y].ToggleFocus();
                            currentFocus = gridData[(int)MousePos.x, (int)MousePos.y];
                        }
                    } else
                    {
                        GraphNode node1 = AstarPath.active.GetNearest(currentFocus.transform.position).node;
                        GraphNode node2 = AstarPath.active.GetNearest(MousePos, NNConstraint.Default).node;

                        if (PathUtilities.IsPathPossible(node1, node2)) // if a path is possible, set destination for current focus and prepare handling
                        {
                            // save the current position of the focused ball on the grid into temp cords
                            float initX = currentFocus.transform.position.x, initY = currentFocus.transform.position.y;
                            currentFocus.ToggleFocus();
                            currentFocus.aiLerp.destination = (MousePos);
                            destination = MousePos;

                            gridData[(int)initX, (int)initY] = null;
                            isFocusMoving = true;
                        } else //if not, unfocus current focus
                        {
                            currentFocus.ToggleFocus();
                            currentFocus = null;
                        } 
                    }
                }
            } else {
                if (currentFocus != null)
                {
                    currentFocus.ToggleFocus();
                    currentFocus = null;
                }
            }
        }

        if (isFocusMoving)
        {
            if (currentFocus.gameObject.transform.position == destination) // once ball arrives, set the new location as current focus, handle on arrival events, unfocus, then spawn new balls
            {                
                gridData[(int)destination.x, (int)destination.y] = currentFocus;

                switch(currentFocus.type)
                {
                    case BallType.Bomb: // if bomb, find all adjacent cells and blow up whatever's in them
                        TriggerBombBall((int)destination.x, (int)destination.y);
                        break;                   
                    case BallType.Wildcard: // if wildcard, only spawn new balls - placebo solution to prevent complications from 2 different colors in 2 opposite directions casting from a wildcard
                        SpawnBallsInQueue();
                        break;
                    case BallType.Double: // otherwise, simply check for lines and spawn new balls if applicable
                    case BallType.Ghost:
                    default:
                        CheckForLinesWithQueueSpawn(currentFocus);
                        break;
                }

                currentFocus = null;
                isFocusMoving = false;
            }
        }
    }

    public static Vector3 ConvertMousePositionToCell(Vector3 mousePos)
    {
        return new Vector3(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y), 0);
    }

    void GenerateNewGrid()
    {
        int count = 0;

        while (count < 3)
        {
            int x = 0, y = 0;
            do
            {
                x = Random.Range(0, width);
                y = Random.Range(0, height);
            } while (gridData[x, y] != null);
            gridData[x, y] = Instantiate(ball, new Vector3(x, y, 0), Quaternion.identity).GetComponent<Ball>();
            gridData[x, y].Activate();
            count++;
        }
    }

    void GenerateNewBalls()
    {
        // count number of empty grid cells to determine if the game can continue
        int nullCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x, y] == null)
                    nullCount++;
            }
        }

        if (nullCount > 1)
        {
            for (int i = 0; i < (3 < nullCount ? 3 : nullCount); i++)
            {
                int x = 0, y = 0;
                do
                {
                    x = Random.Range(0, width);
                    y = Random.Range(0, height);
                } while (gridData[x, y] != null || CheckIfPositionIsOccupiedInQueue(x, y));
                queue.Add(new BallQueue(x, y, Instantiate(ball, new Vector3(x, y, 0), Quaternion.identity).GetComponent<Ball>()));
            }
        } else
            GameOver();
    }

    void GameOver()
    {
        isGameOver = true;
    }

    bool CheckIfPositionIsOccupiedInQueue(int x, int y)
    {
        if (queue.Count == 0)
            return false;
        else
        {
            foreach (BallQueue b in queue)
            {
                if (b.x == x && b.y == y)
                    return true;
            }
            return false;
        }
    }

    public void SpawnBallsInQueue()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (gridData[queue[i].x, queue[i].y] != null)
                Destroy(queue[i].ball.gameObject);
            else
            {
                queue[i].ball.Activate();
                gridData[queue[i].x, queue[i].y] = queue[i].ball;
                CheckForLines(queue[i].ball);
            }
        }
        queue.Clear();
        GenerateNewBalls();
    }

    // these 4 raycasts are used to detect matching balls in lines
    // 1. cast rays in opposite directions, horizontally, vertically and diagonally
    // 2. look for results that are not a bomb, has valid data on grid (since queued balls are only shown, not saved into grid data, and definitely
    // doesn't count for matches) and are either a wildcard or matches the ball calling the cast (a.k.a the ball moved by the player in the last turn)
    // 3. if the origin ball is a double ball, increase extras by 1 ONCE
    // 4. once done, filter the results again and look for entries that would form a line if placed together, adding 1 to extras if an entry is a double ball
    // 5. return the number of detected matches plus extras; a line requires a sum of at least 4 (since we don't count the originating ball)
    int HorizontalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] leftRay = Physics2D.RaycastAll(origin.transform.position, Vector2.left, Mathf.Infinity);
        RaycastHit2D[] rightRay = Physics2D.RaycastAll(origin.transform.position, Vector2.right, Mathf.Infinity);
        int extras = 0;

        List<Vector2> leftHit = new List<Vector2>();
        for (int i = 0; i < leftRay.Length; i++)
        {
            RaycastHit2D hit = leftRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x)
                    {
                        if (origin.type == BallType.Double)
                            extras++;
                        continue;
                    }
                    else
                        leftHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < leftHit.Count; i++)
        {
            if ((int)leftHit[i].x == (int)origin.transform.position.x - i - 1)
            {
                coordinates.Add(new Vector2(leftHit[i].x, leftHit[i].y));
                if (gridData[(int)leftHit[i].x, (int)leftHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        List<Vector2> rightHit = new List<Vector2>();
        for (int i = 0; i < rightRay.Length; i++)
        {
            RaycastHit2D hit = rightRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x)
                        continue;
                    else
                        rightHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < rightHit.Count; i++)
        {
            if ((int)rightHit[i].x == (int)origin.transform.position.x + i + 1)
            {
                coordinates.Add(new Vector2(rightHit[i].x, rightHit[i].y));
                if (gridData[(int)rightHit[i].x, (int)rightHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        return coordinates.Count + extras;
    }

    int VerticalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] upwardsRay = Physics2D.RaycastAll(origin.transform.position, Vector2.up, Mathf.Infinity);
        RaycastHit2D[] downwardsRay = Physics2D.RaycastAll(origin.transform.position, Vector2.down, Mathf.Infinity);
        int extras = 0;

        List<Vector2> upwardsHit = new List<Vector2>();
        for (int i = 0; i < upwardsRay.Length; i++)
        {
            RaycastHit2D hit = upwardsRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.y == (int)origin.transform.position.y)
                    {
                        if (origin.type == BallType.Double)
                            extras++;
                        continue;
                    }
                    else
                        upwardsHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < upwardsHit.Count; i++)
        {
            if ((int)upwardsHit[i].y == (int)origin.transform.position.y + i + 1)
            {
                coordinates.Add(new Vector2(upwardsHit[i].x, upwardsHit[i].y));
                if (gridData[(int)upwardsHit[i].x, (int)upwardsHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        List<Vector2> downwardsHit = new List<Vector2>();
        for (int i = 0; i < downwardsRay.Length; i++)
        {
            RaycastHit2D hit = downwardsRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.y == (int)origin.transform.position.y)
                        continue;
                    else
                        downwardsHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < downwardsHit.Count; i++)
        {
            if ((int)downwardsHit[i].y == (int)origin.transform.position.y - i - 1)
            {
                coordinates.Add(new Vector2(downwardsHit[i].x, downwardsHit[i].y));
                if (gridData[(int)downwardsHit[i].x, (int)downwardsHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        return coordinates.Count + extras;
    }

    int LeftRightDiagonalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] NWRay = Physics2D.RaycastAll(origin.transform.position, new Vector2(-1f, 1f), Mathf.Infinity);
        RaycastHit2D[] SERay = Physics2D.RaycastAll(origin.transform.position, new Vector2(1f, -1f), Mathf.Infinity);
        int extras = 0;

        List<Vector2> NWHit = new List<Vector2>();
        for (int i = 0; i < NWRay.Length; i++)
        {
            RaycastHit2D hit = NWRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x && (int)b.transform.position.y == (int)origin.transform.position.y)
                    {
                        if (origin.type == BallType.Double)
                            extras++;
                        continue;
                    }
                    else
                        NWHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < NWHit.Count; i++)
        {
            if (((int)NWHit[i].x == (int)origin.transform.position.x - i - 1) && ((int)NWHit[i].y == (int)origin.transform.position.y + i + 1))
            {
                coordinates.Add(new Vector2(NWHit[i].x, NWHit[i].y));
                if (gridData[(int)NWHit[i].x, (int)NWHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        List<Vector2> SEHit = new List<Vector2>();
        for (int i = 0; i < SERay.Length; i++)
        {
            RaycastHit2D hit = SERay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x && (int)b.transform.position.y == (int)origin.transform.position.y)
                        continue;
                    else
                        SEHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < SEHit.Count; i++)
        {
            if (((int)SEHit[i].x == (int)origin.transform.position.x + i + 1) && ((int)SEHit[i].y == (int)origin.transform.position.y - i - 1))
            {
                coordinates.Add(new Vector2(SEHit[i].x, SEHit[i].y));
                if (gridData[(int)SEHit[i].x, (int)SEHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        return coordinates.Count + extras;
    }

    int RightLeftDiagonalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] NERay = Physics2D.RaycastAll(origin.transform.position, new Vector2(1f, 1f), Mathf.Infinity);
        RaycastHit2D[] SWRay = Physics2D.RaycastAll(origin.transform.position, new Vector2(-1f, -1f), Mathf.Infinity);
        int extras = 0;

        List<Vector2> NEHit = new List<Vector2>();
        for (int i = 0; i < NERay.Length; i++)
        {
            RaycastHit2D hit = NERay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x && (int)b.transform.position.y == (int)origin.transform.position.y)
                    {
                        if (origin.type == BallType.Double)
                            extras++;
                        continue;
                    }
                    else
                        NEHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < NEHit.Count; i++)
        {
            if (((int)NEHit[i].x == (int)origin.transform.position.x + i + 1) && ((int)NEHit[i].y == (int)origin.transform.position.y + i + 1))
            {
                coordinates.Add(new Vector2(NEHit[i].x, NEHit[i].y));
                if (gridData[(int)NEHit[i].x, (int)NEHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        List<Vector2> SWHit = new List<Vector2>();
        for (int i = 0; i < SWRay.Length; i++)
        {
            RaycastHit2D hit = SWRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if ((b.color == origin.color || b.type == BallType.Wildcard) && b.type != BallType.Bomb && gridData[(int)b.transform.position.x, (int)b.transform.position.y] != null)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x && (int)b.transform.position.y == (int)origin.transform.position.y)
                        continue;
                    else
                        SWHit.Add(new Vector2(b.transform.position.x, b.transform.position.y));
                }
                else
                    break;
            }
        }
        for (int i = 0; i < SWHit.Count; i++)
        {
            if (((int)SWHit[i].x == (int)origin.transform.position.x - i - 1) && ((int)SWHit[i].y == (int)origin.transform.position.y - i - 1))
            {
                coordinates.Add(new Vector2(SWHit[i].x, SWHit[i].y));
                if (gridData[(int)SWHit[i].x, (int)SWHit[i].y].type == BallType.Double)
                    extras++;
            }
            else
                break;
        }

        return coordinates.Count + extras;
    }

    // used for checking potential new lines formed by newly spawned balls in queue - this doesn't call the function to generate and queue up
    // new balls since the lines formed are not because of player action
    void CheckForLines(Ball position)
    {
        List<Vector2> matches = new List<Vector2>();

        List<Vector2> horizontalCast = new List<Vector2>();
        if (HorizontalRaycast(position, horizontalCast) >= 4)
            matches.AddRange(horizontalCast);

        List<Vector2> verticalCast = new List<Vector2>();
        if (VerticalRaycast(position, verticalCast) >= 4)
            matches.AddRange(verticalCast);

        List<Vector2> L2RCast = new List<Vector2>();
        if (LeftRightDiagonalRaycast(position, L2RCast) >= 4)
            matches.AddRange(L2RCast);

        List<Vector2> R2LCast = new List<Vector2>();
        if (RightLeftDiagonalRaycast(position, R2LCast) >= 4)
            matches.AddRange(R2LCast);

        if (matches.Count > 0)
        {
            foreach (Vector2 v in matches)
            {
                if (gridData[(int)v.x, (int)v.y].type == BallType.Double)
                    Score++;

                Destroy(gridData[(int)v.x, (int)v.y].gameObject);
                gridData[(int)v.x, (int)v.y] = null;
                Score++;
            }
            if (gridData[(int)position.transform.position.x, (int)position.transform.position.y].type == BallType.Double)
                Score++;

            Destroy(gridData[(int)position.transform.position.x, (int)position.transform.position.y].gameObject);
            gridData[(int)position.transform.position.x, (int)position.transform.position.y] = null;
            Score++;
        }
    }

    void CheckForLinesWithQueueSpawn(Ball position)
    {
        List<Vector2> matches = new List<Vector2>();

        List<Vector2> horizontalCast = new List<Vector2>();
        if (HorizontalRaycast(position, horizontalCast) >= 4)
            matches.AddRange(horizontalCast);

        List<Vector2> verticalCast = new List<Vector2>();
        if (VerticalRaycast(position, verticalCast) >= 4)
            matches.AddRange(verticalCast);

        List<Vector2> L2RCast = new List<Vector2>();
        if (LeftRightDiagonalRaycast(position, L2RCast) >= 4)
            matches.AddRange(L2RCast);

        List<Vector2> R2LCast = new List<Vector2>();
        if (RightLeftDiagonalRaycast(position, R2LCast) >= 4)
            matches.AddRange(R2LCast);

        if (matches.Count > 0)
        {
            foreach (Vector2 v in matches)
            {
                if (gridData[(int)v.x, (int)v.y].type == BallType.Double)
                    Score++;

                Destroy(gridData[(int)v.x, (int)v.y].gameObject);
                gridData[(int)v.x, (int)v.y] = null;
                Score++;
            }
            if (gridData[(int)position.transform.position.x, (int)position.transform.position.y].type == BallType.Double)
                Score++;

            Destroy(gridData[(int)position.transform.position.x, (int)position.transform.position.y].gameObject);
            gridData[(int)position.transform.position.x, (int)position.transform.position.y] = null;
            Score++;
        }
        else
            SpawnBallsInQueue(); // if there are no matches, spawn balls currently in queue, then queue up a new batch of balls
    }

    List<Vector2> GetAdjacentCells(Vector2 pos)
    {
        List<Vector2> list = new List<Vector2>();

        list.Add(new Vector2(pos.x - 1, pos.y));
        list.Add(new Vector2(pos.x + 1, pos.y));
        list.Add(new Vector2(pos.x, pos.y - 1));
        list.Add(new Vector2(pos.x, pos.y + 1));
        list.Add(new Vector2(pos.x - 1, pos.y + 1));
        list.Add(new Vector2(pos.x + 1, pos.y - 1));
        list.Add(new Vector2(pos.x - 1, pos.y - 1));
        list.Add(new Vector2(pos.x + 1, pos.y + 1));

        return list;
    }

    void TriggerBombBall(int x, int y)
    {
        Vector2 pos = new Vector2(x, y);
        List<Vector2> adjPos = GetAdjacentCells(pos);
        List<Vector2> realAdjPos = new List<Vector2>();

        // remove all coordinates that are not within the 9*9 grid
        for (int i = 0; i < adjPos.Count; i++)
        {
            if (adjPos[i].x >= 0 && adjPos[i].x <= 8 && adjPos[i].y >= 0 && adjPos[i].y <= 8)
                realAdjPos.Add(adjPos[i]);
        }
        realAdjPos.Add(pos); // this is to ensure that the bomb itself is blown up as well

        for(int i = 0;i < realAdjPos.Count;i++)
        {
            if (gridData[(int)realAdjPos[i].x, (int)realAdjPos[i].y] != null)
            {
                Destroy(gridData[(int)realAdjPos[i].x, (int)realAdjPos[i].y].gameObject);
                gridData[(int)realAdjPos[i].x, (int)realAdjPos[i].y] = null;
                Score++;
            }
            for (int j = 0; j < queue.Count; j++)
            {
                if (queue[j].x == realAdjPos[i].x && queue[j].y == realAdjPos[i].y)
                {
                    Destroy(queue[j].ball.gameObject);
                    queue.RemoveAt(j);
                }
            }
        }
        SpawnBallsInQueue();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    public void Restart()
    {
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");
        for (int i = 0; i < cells.Length; i++)
        {
            Destroy(cells[i]);
        }

        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        for (int i = 0; i < balls.Length; i++)
        {
            Destroy(balls[i]);
        }

        Awake();
        Start();
    }
}

// class to hold queue data - x and y are the coordinates of the upcoming ball, and "ball" holds its data
public class BallQueue
{
    public int x { get; private set; }
    public int y { get; private set; }
    public Ball ball { get; private set; }

    public BallQueue(int x, int y, Ball ball)
    {
        this.x = x;
        this.y = y;
        this.ball = ball;
    }
}