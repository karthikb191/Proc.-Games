using System.Collections;
using System.Collections.Generic;
using UnityEngine;








public class SampleScript : MonoBehaviour {
    float multiplier = 0;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Move();	
	}

    void Move() {
        //multiplier = 0;
        if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A)) {
            if (Input.GetKey(KeyCode.D)) {
                multiplier += Time.deltaTime;
                multiplier = Mathf.Clamp(multiplier, -1, 1);
            }
            if (Input.GetKey(KeyCode.A)) {
                multiplier -= Time.deltaTime;
                multiplier = Mathf.Clamp(multiplier, -1, 1);
            }
        }
        else {
            if (multiplier != 0) {
                multiplier -= Mathf.Sign(multiplier) * Time.deltaTime;
                if (Mathf.Abs(multiplier) < 0.1f)
                    multiplier = 0;
            }
        }

        gameObject.transform.position += gameObject.transform.right * multiplier * 4 * Time.deltaTime;
    }


}
