using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhobiaSceneManager : MonoBehaviour {

    private UIManager uiManager;

    protected int currentLevelNumber;
    private List<LevelManager> levelManagers = new List<LevelManager>(); //No need to restrict ourselves to 10

    public virtual void Awake() {
        uiManager = GameObject.FindGameObjectWithTag("UIManagerObject").GetComponent<UIManager>();
        FindLevelManagers();
    }

    // Start is called before the first frame update
    public virtual void Start() {
        currentLevelNumber = GameManager.startingLevel;
        levelManagers[currentLevelNumber].ActivateLevel();

        int highscore = GameManager.GetHighscore(currentLevelNumber);
        uiManager.UpdateInfo(currentLevelNumber, highscore);
    }

    // Update is called once per frame
    public virtual void Update() {
        //TODO review this, UIManager could just get the score itself
        //Don't be updating the UI if the UI isn't actually showing
        uiManager.UpdateScore((int)LevelManager.score);

        if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
            PrintSaveData();
        }
    }

    public void NextLevel(int suds) {
        GameManager.UpdateSave(currentLevelNumber, (int)LevelManager.score, suds);

        //Don't need to deactivate the level here as that happens when the SUDS UI switches on
        currentLevelNumber++;

        if (currentLevelNumber < levelManagers.Count) {
            ActivateCurrentLevel();
        }
        else {
            //TODO show some kind of special "you finished this mode" screen, return to level selection? 
        }
    }

    public void DeactivateCurrentLevel() {
        levelManagers[currentLevelNumber].DeactivateLevel();
    }

    private void ActivateCurrentLevel() {
        int highscore = GameManager.GetHighscore(currentLevelNumber);
        uiManager.UpdateInfo(currentLevelNumber, highscore);
        levelManagers[currentLevelNumber].ActivateLevel();
    }

    public void PreviousLevel() {
        Debug.Log("PREV LEVEL: cln is " + currentLevelNumber);
        if (currentLevelNumber > 0) {
            DeactivateCurrentLevel();
            currentLevelNumber--;
            ActivateCurrentLevel();
        } else {
            //TODO maybe play an error sound?
        }
        Debug.Log("cln is now " + currentLevelNumber);
    }
        

    public void ResetLevel() {
        levelManagers[currentLevelNumber].ResetLevel();
    }

    public void ResetLevel(int suds) {
        GameManager.UpdateSave(currentLevelNumber, (int)LevelManager.score, suds);
        ResetLevel();
    }

    //Finds all the level managers in the current scene and adds them to the list (in ascending order)
    private void FindLevelManagers() {
        GameObject[] levelManagerObjects = GameObject.FindGameObjectsWithTag("LevelManagerObject");
        Array.Sort(levelManagerObjects, CompareGameObjectNames);
        foreach (GameObject go in levelManagerObjects) {
            levelManagers.Add(go.GetComponent<LevelManager>());
        }
    }

    private int CompareGameObjectNames(GameObject x, GameObject y) {
        return x.name.CompareTo(y.name);
    }

    private void PrintSaveData() {
        String saveDataString = "---------- BEGIN SAVE DATA ----------\n";
        //For each game mode
        foreach (GameMode mode in Enum.GetValues(typeof(GameMode))) {
            saveDataString += "\n----- Mode: " + mode.ToString() + " ----- ";
            saveDataString += "\nMax level unlocked: " + (GameManager.GetUnlockedLevelsForMode(mode) + 1);

            int[] highscores = GameManager.GetHighscoresForMode(mode);
            List<SUDSObject>[] sudsArray = GameManager.GetSUDSForMode(mode);

            //For each level in the mode
            for (int level = 0; level < highscores.Length; level++) {
                saveDataString += "\n--- LEVEL " + (level + 1) + " ---";
                saveDataString += "\nHighscore: " + highscores[level];

                String sudsString = ""; 
                foreach (SUDSObject suds in sudsArray[level]) {
                    sudsString += suds.date.ToShortDateString() + ": " + suds.rating + ", ";
                }
                saveDataString += "\nSUDS ratings: " + sudsString;
            }
            saveDataString += "\n";
        }
        saveDataString += "\n---------- END SAVE DATA ----------";
        Debug.Log(saveDataString);
    }

}
