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

public class Ball : MonoBehaviour
{
    Animator anim;
    SpriteRenderer sr;
    public BallColor color { get; private set; }
    bool isFocus;
    public AILerp aiLerp;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        aiLerp = GetComponent<AILerp>();
        color = (BallColor)Random.Range(0, 7);
        switch (color)
        {
            case BallColor.Red:
                sr.color = Color.red;
                break;
            case BallColor.Green:
                sr.color = Color.green;
                break;
            case BallColor.Blue:
                sr.color = Color.blue;
                break;
            case BallColor.Yellow:
                sr.color = Color.yellow;
                break;
            case BallColor.Pink:
                sr.color = new Color32(255, 105, 180, 255);
                break;
            case BallColor.Cyan:
                sr.color = Color.cyan;
                break;
            case BallColor.Brown:
                sr.color = new Color32(139, 69, 19, 255);
                break;
            default: //this should never happen
                sr.color = Color.white;
                break;

        }
        isFocus = false;
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("idle", !isFocus);

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            if (isFocus)
                gameObject.layer = LayerMask.NameToLayer("Default");
            else
                gameObject.layer = LayerMask.NameToLayer("Block");
        }

        if (aiLerp.reachedEndOfPath)
        {
            transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
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
    
    public void Activate()
    {
        anim.SetTrigger("activate");
    }
}
