using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignOut : MonoBehaviour {
    private void Awake()
    {
        SignOut[] so = FindObjectsOfType<SignOut>();
        foreach (SignOut s in so)
        {
            if (s.gameObject != this.gameObject)
                Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        
        DontDestroyOnLoad(this.gameObject);
        transform.GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.AddListener( () => {
            UserManager.SighOut();
        });
        transform.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
            UserManager.BackToLevelSelect();
        });

    }

}
