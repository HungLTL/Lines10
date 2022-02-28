using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Pathfinding.Util;

public enum BallColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Pink,
    Cyan,
    Brown
}

public enum BallType
{
    Normal,
    Ghost,
    Bomb,
    Double,
    Wildcard
}

public class Ball : MonoBehaviour
{
    Animator anim;
    SpriteRenderer sr;
    bool isFocus;
    public AILerp aiLerp;
    public bool isGhosted;

    [SerializeField]
    SpriteRenderer filter;
    [SerializeField]
    Sprite doubleBall;
    [SerializeField]
    Sprite wildcard;
    [SerializeField]
    Sprite doubleBallInQueue;

    public BallColor color { get; private set; }
    public BallType type { get; private set; }

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        aiLerp = GetComponent<AILerp>();

        color = (BallColor)Random.Range(0, 7);
        sr.color = convertToColor(color);

        if (Random.value <= 0.9) // 90% to spawn a normal ball, 10% for a special one
            type = BallType.Normal;
        else
        {
            type = (BallType)Random.Range(0, 5);
        }

        switch (type)
        {
            case BallType.Ghost: // if ghost ball, make the sprite transparent
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
                break;
            case BallType.Bomb: // if bomb, make the ball black
                sr.color = Color.black;
                break;
            case BallType.Double: // if double ball, activate the child gameObject with the filter SpriteRenderer, then have filter render the double filter
                filter.gameObject.SetActive(true);
                filter.sprite = doubleBallInQueue;
                break;
            case BallType.Wildcard: // if wildcard, render it white
                sr.color = Color.white;
                break;
        }

        isFocus = false;
        isGhosted = false;
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("idle", !isFocus);

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            if (isFocus || isGhosted)
                gameObject.layer = LayerMask.NameToLayer("Default");
            else
                gameObject.layer = LayerMask.NameToLayer("Block");
        }

        if (aiLerp.reachedEndOfPath)
        {
            transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0); // required line - without this the balls will almost always be ~0.0000001f away from a rounded position and thus will mess up calculations.
            aiLerp.SetPath(null);
            aiLerp.enabled = false;
            aiLerp.enabled = true;
        }
    }

    public void ToggleFocus()
    {
        isFocus = !isFocus;
    }

    public void SetDestination(Vector3 pos)
    {
        aiLerp.destination = pos;
    }

    // balls spawn as "queued", meaning they're small, don't count for matches, and can be destroyed.
    // this function "activates" a ball, allowing them to be selected, and can be matched.
    public void Activate()
    {
        anim.SetTrigger("activate");

        if (type == BallType.Double)
            filter.sprite = doubleBall;
        else
        {
            // the small wildcard ball is simply the default sprite without any color.
            // when activated, this codeblock will give it the "rainbow" filter.
            if (type == BallType.Wildcard)
            {
                filter.gameObject.SetActive(true);
                filter.sprite = wildcard;
            }
        }
    }

    public static Color convertToColor(BallColor color)
    {
        switch (color)
        {
            case BallColor.Red:
                return Color.red;
            case BallColor.Green:
                return Color.green;
            case BallColor.Blue:
                return Color.blue;
            case BallColor.Yellow:
                return Color.yellow;
            case BallColor.Pink:
                return new Color32(255, 105, 180, 255);
            case BallColor.Cyan:
                return Color.cyan;
            case BallColor.Brown:
                return new Color32(139, 69, 19, 255);
            default: // this should never happen - a white ball must always be a queued wildcard.
                return Color.white;
        }
    }
}
