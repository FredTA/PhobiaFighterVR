using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//TODO correct diagram, we don't inherit from monobeh
public static class GameManager{

    //TODO start in level selection scene, assign starting level
    private static GameMode currentMode = GameMode.Spider;
    public static int startingLevel = 0;
    
    private static Save save = new Save();

    //TODO add all scenes 
    public static void LoadScene(GameMode mode, int level) {
        startingLevel = level;
        switch (mode) {
            case GameMode.Spider:
                SceneManager.LoadScene("Spider");
                break;
            default:
                Debug.Log("Couldn't find scene");
                break;
        }
    }

    public static void LoadLevelSelectionScene() {
        SceneManager.LoadScene("LevelSelection");
    }

    public static int[] GetUnlockedLevels() {
        return save.unlockedLevels;
    }

    public static int GetUnlockedLevelsForMode(GameMode mode) {
        return save.unlockedLevels[(int)mode];
    }

    //TODO update diagram
    //Gets highscore for given level at current mode
    public static int GetHighscore(int level) {
        return save.highscores[(int)currentMode][level];
    }

    //Returns an array of all highscores for the given mode
    public static int[] GetHighscoresForMode(GameMode mode) {
        return save.highscores[(int)mode];
    }

    public static List<SUDSObject>[][] GetSUDS() {
        return save.sudsRatings;
    }

    public static List<SUDSObject>[] GetSUDSForMode(GameMode mode) {
        return save.sudsRatings[(int)mode];
    }

    [Obsolete ("Do you really need to access the save directly?")] 
    public static Save GetSave() {
        return save;
    }

    public static void UpdateSave(int level, int score, int suds) {
        save.UpdateSaveData(currentMode, level, score, suds);
    }

}

public enum GameMode {
    Spider
}
