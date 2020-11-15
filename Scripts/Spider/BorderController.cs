using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderController : MonoBehaviour
{
    private float borderDistance;

    //Table borders
    public GameObject tableBorderLeftStart;
    public GameObject tableBorderLeftEnd;
    public GameObject tableBorderRightStart;
    public GameObject tableBorderRightEnd;
    public GameObject tableBorderDownStart;
    public GameObject tableBorderDownEnd;

    //TAKING THESE TWO 
    public GameObject tableBorderUpStart;
    public GameObject tableBorderUpEnd;

    // Start is called before the first frame update
    void Start() {
        borderDistance = tableBorderUpEnd.transform.position.z - tableBorderUpStart.transform.position.z;
        //Debug.Log("---------------BBL: " + borderDistance);

        tableBorderUpStart.transform.position = tableBorderUpEnd.transform.position - new Vector3(0, 0, borderDistance);
        tableBorderDownStart.transform.position = tableBorderDownEnd.transform.position + new Vector3(0, 0, borderDistance);
        tableBorderLeftStart.transform.position = tableBorderLeftEnd.transform.position + new Vector3(borderDistance, 0, 0);
        tableBorderRightStart.transform.position = tableBorderRightEnd.transform.position - new Vector3(borderDistance, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float getBorderDistance() {
        return borderDistance;
    }
}
