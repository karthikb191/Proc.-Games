using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelScript : MonoBehaviour {
    public string nextLevel;
	// Use this for initialization
	void Start () {
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { GoToLevel(nextLevel); });
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    //public void GoToNextLevel() {
    //    GameManager.Instance.IncreaseLevel();
    //}
    public void GoToLevel(string levelName) {
        if (nextLevel != "")
            SceneManager.LoadScene(levelName);
        else
            SceneManager.LoadScene(0);  //Going to the main menu
    }
}
