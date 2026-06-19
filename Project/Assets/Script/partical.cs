using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class partical : MonoBehaviour
{
    public GameObject prefacCircle;
    [Range(0,10f)]
    public float radius = 1f;
    public Color particalColor;
    [Range(0,11f)]
    public float gravity = 0f;

    public float boxWidth = 10f;
    public float boxHeight = 10f;

    [Range(0,1f)]
    public float boundDamping=0.9f;
    private Vector2 initialPos;
    public List<GameObject> particalList = new List<GameObject>();

    public int numPartical = 0;

    [Range(0, 5f)]
    public float spacing;
    // Start is called before the first frame update
    void Start()
    { 
        initialPos = transform.position;

        if (numPartical != 0)
        {
            int particalCol = (int)Mathf.Sqrt(numPartical);
            int particalRow = (numPartical - 1) / particalCol + 1;

            float step = radius * 2 + spacing;

            //Debug.Log(particalRow);
            //Debug.Log(particalCol);
            float cell = (spacing + 2 * radius)/2f;
            float particalTotalWidth = (particalCol - 1) * cell;
            float particalTotalHeight = (particalRow - 1) * cell;
            float StartX = initialPos.x - particalTotalWidth / 2f;
            float StartY = initialPos.y + particalTotalHeight / 2f;
            //Debug.Log(StartX);
            //Debug.Log(StartY);
            for (int i = 0; i <numPartical; i++)
            {
                int col = i % particalRow;
                int row = i / particalRow;

                float px = StartX + col * cell;
                float py = StartY - row * cell;
                Vector2 pos = new Vector2(px, py);
                DrawCircle(pos, radius, particalColor);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        updateCircle();
    }
    void DrawCircle(Vector2 pos,float radius,Color color)
    {
        GameObject temp = Instantiate(prefacCircle, pos, Quaternion.identity);
        temp.GetComponent<SpriteRenderer>().color = color;
        temp.AddComponent<liquid>();
        particalList.Add(temp);
    }
    void updateCircle()
    {
        for(int i=0;i<particalList.Count;i++)
        {
            GameObject temp = particalList[i];
            temp.transform.localScale = new Vector3(radius, radius, 1);
            temp.GetComponent<liquid>().radius = radius;
            temp.GetComponent<liquid>().gravity = gravity;
            temp.GetComponent<liquid>().damping = boundDamping;
            temp.GetComponent<liquid>().boxBottom = initialPos.y - boxHeight / 2;
            temp.GetComponent<liquid>().boxTop = initialPos.y + boxHeight / 2;
            temp.GetComponent<liquid>().boxLeft = initialPos.x - boxWidth / 2;
            temp.GetComponent<liquid>().boxRight = initialPos.x + boxWidth / 2;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 center = initialPos;
        Vector2 size = new Vector2(boxWidth, boxHeight);
        Gizmos.DrawWireCube(center, size);
    }
}
