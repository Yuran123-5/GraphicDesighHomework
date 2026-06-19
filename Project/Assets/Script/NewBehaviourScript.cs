using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [Range(1, 10)]
    public float HighGravity= 2.5f;
    [Range(1,10)]
    public float LowGravity = 1f;
    [Range(1,10)]
    public float UpVelocity = 5f;

    public Rigidbody2D rb;
    public float timeRecord = 0f;
    [Range(0,1.5f)]
    public float TimeLimited = 1.5f;
    [Range(0,10)]
    public float MoveSpeed = 5f;
    [Range(0, 0.5f)]
    public float rotateTime = 0.3f;

    public bool isGrounded = false;
    private void Start()
    {
        rb=transform.GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void Update()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity+=new Vector2(0,Physics2D.gravity.y * (HighGravity - 1))*Time.deltaTime;
        }
        else if(rb.velocity.y > 0&&!Input.GetKey(KeyCode.Space))
        {
            rb.velocity += new Vector2(0, Physics2D.gravity.y * (LowGravity - 1)) * Time.deltaTime;
        }
        if(Input.GetKey(KeyCode.Space)&&timeRecord<TimeLimited)
        {
            timeRecord+=Time.deltaTime;
            rb.velocity = new Vector2(rb.velocity.x, UpVelocity);
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (!isGrounded)
            {
                StartCoroutine(Rotate());
            }
        }
        if(Input.GetKeyUp(KeyCode.Space))
        {
            timeRecord = 0f;
        }
        float x = Input.GetAxis("Horizontal");
        transform.position += new Vector3(x * MoveSpeed * Time.deltaTime, 0, 0);
    }
    IEnumerator Rotate()
    {
        float t = 0f;
        Quaternion startRot = transform.localRotation;
        Quaternion endRot =startRot*Quaternion.Euler(180, 0, 0); 

        while (t < rotateTime)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(startRot, endRot, t / rotateTime);
            yield return null;
        }

        transform.localRotation = endRot;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag=="Ground")
        {
            isGrounded = true;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            isGrounded = false;
        }
    }
}
