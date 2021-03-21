using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    public GameObject mainUIHolder;
    public GameObject sudsUIHolder;
    public Text levelText;
    public Text scoreText;
    public Text highscoreText;
    public Text sudsPreviewText;
    public Slider sudsSlider;

    public AudioSource correct;
    public AudioSource incorrect;
    private float timerStartTime = -1;
    private const float SCORE_HIGHLIGHT_DURATION = 1;
    private Color scoreDisplayColor;

    private PhobiaSceneManager sceneManager;

    // Start is called before the first frame update
    void Awake() {
        sceneManager = GameObject.FindGameObjectWithTag("SceneManagerObject").GetComponent<PhobiaSceneManager>();
        scoreDisplayColor = scoreText.color;
    }

    // Update is called once per frame
    void Update() {
        if (timerStartTime != -1) {
            if (Time.time > timerStartTime + SCORE_HIGHLIGHT_DURATION) {
                timerStartTime = -1;
                scoreText.color = scoreDisplayColor;
                highscoreText.color = scoreDisplayColor;
            }
        }
    }

    public void UpdateInfo(int level, int highscore) {
        levelText.text = (level+1).ToString(); // +1 as level 0 in the code is level 1 to the player 
        highscoreText.text = highscore.ToString();
    }

    public void UpdateScore(int score) {
        if (score > int.Parse(scoreText.text)) {
            timerStartTime = Time.time;
            scoreText.color = Color.green;

            if (score > int.Parse(highscoreText.text)) {
                highscoreText.color = Color.green;
                highscoreText.text = score.ToString() ;
            }
        }
        scoreText.text = score.ToString();

    }

    public void UpdateSUDSPreview() {
        string sudsSelection = sudsSlider.value.ToString();
        sudsPreviewText.text = sudsSelection;
    }

    //For completing the level, will show suds prompt
    public void CompleteLevel() {
        ToggleSUDSUI(true);
    }

    //For going to the next level, after we've completed the current one 
    public void NextLevel() {
        ToggleSUDSUI(false);
        int sudsSelection = (int)sudsSlider.value;
        sceneManager.NextLevel(sudsSelection);
    }

    //Will reset the level, or send suds and reset level, if suds is active
    public void ResetLevel() {
        if (sudsUIHolder.activeSelf) {
            ToggleSUDSUI(false);
            int sudsSelection = (int)sudsSlider.value;
            sceneManager.ResetLevel(sudsSelection);
        } else {
            sceneManager.ResetLevel();
        }
    }

    public void PreviousLevel() {
        sceneManager.PreviousLevel();
    }


    //TODO Update arch diagram with param bool
    private void ToggleSUDSUI(bool suds) {
        sudsUIHolder.SetActive(suds);
        mainUIHolder.SetActive(!suds);

        //We don't want the level still happening while the suds input is up 
        if (suds) {
            sceneManager.DeactivateCurrentLevel();
        }
    }

    public void IncreaseScore() {
        if (LevelManager.score <= 80) {
            LevelManager.score += 20;
        } else {
            LevelManager.score = 100;
        }
        Debug.Log("Score is " + LevelManager.score);
    }

    public void PlayCorrectSound() {
        correct.Play();
    }

    public void PlayIncorrectSound() {
        incorrect.Play();
    }

}
