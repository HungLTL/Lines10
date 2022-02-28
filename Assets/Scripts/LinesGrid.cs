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

        if (Input.GetMouseButtonDown(0) && !isFocusMoving && !isPaused && !isGameOver)
        {
            Vector3 MousePos = ConvertMousePositionToCell(Camera.main.ScreenToWorldPoint(Input.mousePosition));

            if (MousePos.x >= 0 && MousePos.x < width && MousePos.y >= 0 && MousePos.x < width) // this check ensures the player is interacting within the board
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
            if (currentFocus.gameObject.transform.position == destination) // once ball arrives, set the new location as current focus, unfocus, then spawn new balls
            {                
                gridData[(int)destination.x, (int)destination.y] = currentFocus;
                CheckForLinesWithQueueSpawn(currentFocus);
                currentFocus = null;
                //SpawnBallsInQueue();
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

    bool HorizontalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] leftRay = Physics2D.RaycastAll(origin.transform.position, Vector2.left, Mathf.Infinity, LayerMask.GetMask("Block"));
        RaycastHit2D[] rightRay = Physics2D.RaycastAll(origin.transform.position, Vector2.right, Mathf.Infinity, LayerMask.GetMask("Block"));

        List<Vector2> leftHit = new List<Vector2>();
        for (int i = 0; i < leftRay.Length; i++)
        {
            RaycastHit2D hit = leftRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if (b.color == origin.color)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x)
                        continue;
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
                coordinates.Add(new Vector2(leftHit[i].x, leftHit[i].y));
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
                if (b.color == origin.color)
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
                coordinates.Add(new Vector2(rightHit[i].x, rightHit[i].y));
            else
                break;
        }

        if (coordinates.Count >= 4)
            return true;
        else
            return false;
    }

    bool VerticalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] upwardsRay = Physics2D.RaycastAll(origin.transform.position, Vector2.up, Mathf.Infinity, LayerMask.GetMask("Block"));
        RaycastHit2D[] downwardsRay = Physics2D.RaycastAll(origin.transform.position, Vector2.down, Mathf.Infinity, LayerMask.GetMask("Block"));

        List<Vector2> upwardsHit = new List<Vector2>();
        for (int i = 0; i < upwardsRay.Length; i++)
        {
            RaycastHit2D hit = upwardsRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if (b.color == origin.color)
                {
                    if ((int)b.transform.position.y == (int)origin.transform.position.y)
                        continue;
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
                coordinates.Add(new Vector2(upwardsHit[i].x, upwardsHit[i].y));
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
                if (b.color == origin.color)
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
                coordinates.Add(new Vector2(downwardsHit[i].x, downwardsHit[i].y));
            else
                break;
        }

        if (coordinates.Count >= 4)
            return true;
        else
            return false;
    }

    bool LeftRightDiagonalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] NWRay = Physics2D.RaycastAll(origin.transform.position, new Vector2(-1f, 1f), Mathf.Infinity, LayerMask.GetMask("Block"));
        RaycastHit2D[] SERay = Physics2D.RaycastAll(origin.transform.position, new Vector2(1f, -1f), Mathf.Infinity, LayerMask.GetMask("Block"));

        List<Vector2> NWHit = new List<Vector2>();
        for (int i = 0; i < NWRay.Length; i++)
        {
            RaycastHit2D hit = NWRay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if (b.color == origin.color)
                {
                    if ((int)b.transform.position.x == (int) origin.transform.position.x && (int)b.transform.position.y == (int)origin.transform.position.y)
                        continue;
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
                coordinates.Add(new Vector2(NWHit[i].x, NWHit[i].y));
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
                if (b.color == origin.color)
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
                coordinates.Add(new Vector2(SEHit[i].x, SEHit[i].y));
            else
                break;
        }

        if (coordinates.Count >= 4)
            return true;
        else
            return false;
    }

    bool RightLeftDiagonalRaycast(Ball origin, List<Vector2> coordinates)
    {
        RaycastHit2D[] NERay = Physics2D.RaycastAll(origin.transform.position, new Vector2(1f, 1f), Mathf.Infinity, LayerMask.GetMask("Block"));
        RaycastHit2D[] SWRay = Physics2D.RaycastAll(origin.transform.position, new Vector2(-1f, -1f), Mathf.Infinity, LayerMask.GetMask("Block"));

        List<Vector2> NEHit = new List<Vector2>();
        for (int i = 0; i < NERay.Length; i++)
        {
            RaycastHit2D hit = NERay[i];
            Ball b = hit.transform.GetComponent<Ball>();
            if (b != null)
            {
                if (b.color == origin.color)
                {
                    if ((int)b.transform.position.x == (int)origin.transform.position.x && (int)b.transform.position.y == (int)origin.transform.position.y)
                        continue;
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
                coordinates.Add(new Vector2(NEHit[i].x, NEHit[i].y));
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
                if (b.color == origin.color)
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
                coordinates.Add(new Vector2(SWHit[i].x, SWHit[i].y));
            else
                break;
        }

        if (coordinates.Count >= 4)
            return true;
        else
            return false;
    }

    void CheckForLines(Ball position)
    {
        List<Vector2> matches = new List<Vector2>();

        List<Vector2> horizontalCast = new List<Vector2>();
        if (HorizontalRaycast(position, horizontalCast))
            matches.AddRange(horizontalCast);

        List<Vector2> verticalCast = new List<Vector2>();
        if (VerticalRaycast(position, verticalCast))
            matches.AddRange(verticalCast);

        List<Vector2> L2RCast = new List<Vector2>();
        if (LeftRightDiagonalRaycast(position, L2RCast))
            matches.AddRange(L2RCast);

        List<Vector2> R2LCast = new List<Vector2>();
        if (RightLeftDiagonalRaycast(position, R2LCast))
            matches.AddRange(R2LCast);

        if (matches.Count > 0)
        {
            foreach (Vector2 v in matches)
            {
                Destroy(gridData[(int)v.x, (int)v.y].gameObject);
                gridData[(int)v.x, (int)v.y] = null;
                Score++;
            }
            Destroy(gridData[(int)position.transform.position.x, (int)position.transform.position.y].gameObject);
            gridData[(int)position.transform.position.x, (int)position.transform.position.y] = null;
            Score++;
        }
    }

    void CheckForLinesWithQueueSpawn(Ball position)
    {
        List<Vector2> matches = new List<Vector2>();

        List<Vector2> horizontalCast = new List<Vector2>();
        if (HorizontalRaycast(position, horizontalCast))
            matches.AddRange(horizontalCast);

        List<Vector2> verticalCast = new List<Vector2>();
        if (VerticalRaycast(position, verticalCast))
            matches.AddRange(verticalCast);

        List<Vector2> L2RCast = new List<Vector2>();
        if (LeftRightDiagonalRaycast(position, L2RCast))
            matches.AddRange(L2RCast);

        List<Vector2> R2LCast = new List<Vector2>();
        if (RightLeftDiagonalRaycast(position, R2LCast))
            matches.AddRange(R2LCast);

        if (matches.Count > 0)
        {
            foreach (Vector2 v in matches)
            {
                Destroy(gridData[(int)v.x, (int)v.y].gameObject);
                gridData[(int)v.x, (int)v.y] = null;
                Score++;
            }
            Destroy(gridData[(int)position.transform.position.x, (int)position.transform.position.y].gameObject);
            gridData[(int)position.transform.position.x, (int)position.transform.position.y] = null;
            Score++;
        }
        else
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