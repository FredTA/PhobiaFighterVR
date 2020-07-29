using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class Save {
    public int[] unlockedLevels; //An array because we know how many modes there are 
    public int[][] highscores; //An array of arrays, because we know how many modes there are, but not how many levels
    public List<SUDSObject>[][] sudsRatings; //An array of arrays of lists, same as above, but each element is a list for multiple suds

    private const string SAVE_FILE_NAME = "savefile.save";

    public Save() {
        LoadSaveData();
    }

    private void LoadSaveData() {
        Debug.Log("Looking for save " + Application.persistentDataPath + "/" + SAVE_FILE_NAME);
        if (File.Exists(Application.persistentDataPath + "/" + SAVE_FILE_NAME)) {
            Debug.Log("Found save file, loading...");
            //Open the file, deserialize the byte stream, and leave it as a Save object
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/" + SAVE_FILE_NAME, FileMode.Open);
            Save loadedData = (Save)bf.Deserialize(file);
            file.Close();

            unlockedLevels = loadedData.unlockedLevels;
            highscores = loadedData.highscores;
            sudsRatings = loadedData.sudsRatings;
        }
        else {
            Debug.Log("No save file found");
            InitalizeSaveGame();
        }
    }

    //Instantiates our arrays. Must be updated as new game modes are added
    private void InitalizeSaveGame() {
        int numberOfPhobiaModes = Enum.GetValues(typeof(GameMode)).Cast<int>().Max() + 1;

        unlockedLevels = new int[numberOfPhobiaModes];
        for (int i = 0; i < unlockedLevels.Length; i++) {
            unlockedLevels[i] = 0; //Start with level 0 (level 1 to the player) unlocked for each mode
        }

        highscores = new int[numberOfPhobiaModes][]; // An array of int lists, we initialized the array, but not the int lists
                                                     
        sudsRatings = new List<SUDSObject>[numberOfPhobiaModes][]; //An array of arrays of suds lists

        //Add new gamemodes here
        foreach (GameMode mode in Enum.GetValues(typeof(GameMode))) {
            int numberOfLevels = 0;
            switch (mode) {
                case GameMode.Spider:
                    numberOfLevels = SpiderSceneManager.numberOfLevels;
                    break;
            }

            highscores[(int)mode] = new int[numberOfLevels];
            sudsRatings[(int)mode] = new List<SUDSObject>[numberOfLevels];
            for (int i = 0; i < numberOfLevels; i++) {
                highscores[(int)mode][i] = 0; //Set all highscores as 0
                sudsRatings[(int)mode][i] = new List<SUDSObject>(); //Instantiate SUDS lists
            }
        }
    }

    public void UpdateSaveData(GameMode mode, int level, int score, int suds) {
        unlockedLevels[(int)mode] = level + 1;

        //Debug.Log("highscores: modes = " + highscores.Length + " S levels: " + highscores[(int)mode].Length);
        //Debug.Log("We are trying to access level number " + level);

        //Only update highscore if its higher than the current highscore
        if (highscores[(int)mode][level] < score) { 
            highscores[(int)mode][level] = score;
        }
        
        SUDSObject newSUDS = new SUDSObject();
        newSUDS.date = DateTime.Now;
        newSUDS.rating = suds;
        sudsRatings[(int)mode][level].Add(newSUDS);

        WriteSaveData();
    }

    private void WriteSaveData() {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/" + SAVE_FILE_NAME);
        bf.Serialize(file, this);
        file.Close();

        Debug.Log("System State Saved to " + Application.persistentDataPath + "/" + SAVE_FILE_NAME);
    }

}

[Serializable]
public struct SUDSObject {
    public DateTime date;
    public int rating;
}