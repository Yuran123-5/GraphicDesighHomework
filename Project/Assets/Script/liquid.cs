using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class liquid : MonoBehaviour
{
    public float gravity = 0f;
    public float radius;
    public Vector2 veloctiy = Vector2.zero;
    public float boxBottom;
    public float boxTop;
    public float boxLeft;
    public float boxRight;
    public float damping = 0.9f;
    // Update is called once per frame
    void Update()
    {
        veloctiy += Vector2.down * gravity * Time.deltaTime;
        transform.position += new Vector3(veloctiy.x, veloctiy.y, 0) * Time.deltaTime;
        boundForBox();
    }
    void boundForBox()
    {
        if(transform.position.y-radius/2<boxBottom)
        {
            transform.position = new Vector3(transform.position.x, boxBottom+radius/2, 0);
            veloctiy.y *= -1 * damping;
        }
        if(transform.position.y+radius/2>boxTop)
        {
            transform.position = new Vector3(transform.position.x, boxTop-radius/2, 0);
            veloctiy.y *= -1 * damping;
        }
        if(transform.position.x-radius/2<boxLeft)
        {
            transform.position = new Vector3(boxLeft+radius/2, transform.position.y, 0);
            veloctiy.x *= -1 * damping;
        }
        if(transform.position.x+radius/2>boxRight)
        {
            transform.position = new Vector3(boxRight-radius/2, transform.position.y, 0);
            veloctiy.x *= -1 * damping;
        }
    }
}
