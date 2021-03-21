using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpiderLevelOne : LevelManager {

    private int numberOfSpidersAtLevelStart;
    private int answeredCorrectly; 

    public override void Awake() {
        base.Awake();
    }

    // Start is called before the first frame update
    public override void Start() {
        base.Start();

        numberOfSpidersAtLevelStart = GetNumberOfActiveSpiders();
        PlayCorrectIncorrectSounds = true;
    }

    // Update is called once per frame
    public override void Update() {
        base.Update();
    }

    public override void ResetLevel() {
        answeredCorrectly = 0;

        GameObject[] spiderGOs = GameObject.FindGameObjectsWithTag("SpiderCluster");
        foreach (GameObject spider in spiderGOs) {
            spider.GetComponent<SpiderManager>().PrepForSpawn();
        }

        base.ResetLevel();
    }

    private int GetNumberOfActiveSpiders() {
        return GameObject.FindGameObjectsWithTag("SpiderCluster").Length;
    }

    public override void SubmitAnswer(bool correct) {
        base.SubmitAnswer(correct);

        if (correct) {
            score += 100 / (numberOfSpidersAtLevelStart);
            answeredCorrectly++;
        }

        //1 because the answer is submitted before the last spider deactivates
        if (GetNumberOfActiveSpiders() == 1) {
            Debug.Log("LEVEL COMPLETE");
            HandleLevelEnd();
        }
    }

    protected override void HandleLevelEnd() {
        SetEndLevelTitleText("You answered " + answeredCorrectly + "/" + numberOfSpidersAtLevelStart + " correctly");
        ShowEndLevelText();
    }
}
