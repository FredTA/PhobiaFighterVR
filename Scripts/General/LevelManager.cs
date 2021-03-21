using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class LevelManager : MonoBehaviour {

    protected UIManager uiManager;

    //TODO update diagram, can't have transform (just copies the reference, doesn't save the original), we need position, rot, scale
    private struct GameObjectState {
        public bool initialActive;
        public GameObject gameObject;
        public Vector3 initialPosition;
        public Vector3 initialScale;
        public Quaternion initialRotation;
    }

    //Why is this static again?
    //So that QuestionsManager can access it 
    //TODO review this... we could instead have QuestionsManager hold a reference to PSM, and access the score that way 
    public static float score = 0;
    protected float timer;

    private bool levelActive = false;
    private GameObject gameObjectsHolder; 
    private List<GameObjectState> gameObjectStates = new List<GameObjectState>();
    public AudioSource voiceover;
    private bool firstActivation = true;

    protected virtual bool PlayCorrectIncorrectSounds { get; set; }

    private Text instructionsText;
    private Text levelEndTitle;
    private Text levelEndSubtitle;

    public virtual void Awake() {
        gameObjectsHolder = gameObject.transform.GetChild(0).gameObject;
        gameObjectsHolder.SetActive(false); //Ensure holders are inactive at first
        uiManager = GameObject.Find("UI").GetComponent<UIManager>();

        Transform canvas = transform.GetChild(0).GetChild(0);
        instructionsText = canvas.GetChild(0).GetComponent<Text>();
        levelEndTitle = canvas.GetChild(1).GetComponent<Text>();
        levelEndSubtitle = canvas.GetChild(2).GetComponent<Text>();
    }

    private void ShowInstructionsText() {
        instructionsText.enabled = true;
        levelEndTitle.enabled = false;
        levelEndSubtitle.enabled = false;
    }

    protected abstract void HandleLevelEnd();

    protected virtual void ShowEndLevelText() {
        instructionsText.enabled = false;
        levelEndTitle.enabled = true;
        levelEndSubtitle.enabled = true;
        GenerateSubtitleAndTitleColour();
    }

    protected virtual void GenerateSubtitleAndTitleColour() {
        string subtitle;
        Color color;

        if (score <= 40) {
            subtitle = "Have another go";
            color = Color.red;
        }
        else if (score <= 60) {
            subtitle = "Nice one!";
            color = new Color(255, 170, 0);
        }
        else if (score <= 80) {
            subtitle = "Great work!";
            color = new Color(170, 255, 0);
        } 
        else {
            subtitle = "Perfect!";
            color = Color.green;
        }

        SetEndLevelSubtitleText(subtitle);
        SetEndLevelTitleColour(color);
    }

    protected virtual void SetEndLevelTitleText(string text) {
        levelEndTitle.text = text;
    }

    protected virtual void SetEndLevelTitleColour(Color color) {
        levelEndTitle.color = color;
    }

    protected virtual void SetEndLevelSubtitleText(string text) {
        levelEndSubtitle.text = text;
    }

    protected virtual void SetEndLevelSubtitleColour(Color color) {
        levelEndSubtitle.color = color;
    }

    public virtual void Start() {

    }

    // Update is called once per frame
    public virtual void Update() {
       
    }

    public void ActivateLevel() {
        levelActive = true;
        score = 0;
        timer = 0;

        ShowInstructionsText();
        gameObjectsHolder.SetActive(true); //Will activate all GameObjects (that start active) in their initial configs
        voiceover.Play();
        //Debug.Log("First activation: " + firstActivation);

        if (firstActivation) {
            SaveGameobjectStates();
            firstActivation = false;
        } else {
            //No need to do this for first activation, as they'll already be setup for the start of the level
            SetupGameobjects();
        }
    }

    //We can add reset level stuff here if we want to, maybe a specific "reset" sound or something 
    public virtual void ResetLevel() {
        ActivateLevel();
    }

    public void DeactivateLevel() {
        levelActive = false;
        gameObjectsHolder.SetActive(false);
        voiceover.Stop();
    }

    private void SaveGameobjectStates() {
        //Go through all GO's in the holder, and save their initial configs 
        foreach (Transform child in gameObjectsHolder.transform) {
            //Level specific UI will be handled seperately
            if (child.gameObject.name == "Canvas") {
                continue;
            }
            GameObjectState goState;
            goState.gameObject = child.gameObject;
            goState.initialActive = child.gameObject.activeSelf;
            goState.initialPosition = child.position;
            goState.initialScale = child.localScale;
            goState.initialRotation = child.rotation;
            gameObjectStates.Add(goState);
        }
    }

    private void SetupGameobjects() {
        //Go through all GO's, return them to their initial states
        foreach (GameObjectState goState in gameObjectStates) {
            goState.gameObject.SetActive(goState.initialActive);
            goState.gameObject.transform.position = goState.initialPosition;
            goState.gameObject.transform.localScale = goState.initialScale;
            goState.gameObject.transform.rotation = goState.initialRotation;

            if (goState.gameObject.TryGetComponent(out Rigidbody rb)) {
                rb.velocity = new Vector3(0, 0, 0);
                rb.angularVelocity = new Vector3(0, 0, 0);
            }
        }
    }

    public virtual void SubmitAnswer(bool correct) {
        if (PlayCorrectIncorrectSounds) {
            if (correct) {
                uiManager.PlayCorrectSound();
            } else {
                uiManager.PlayIncorrectSound();
            }
        }
    }

    public bool IsLevelActive() {
        return levelActive;
    }

    //Takes a transform and returns the LevelManager that manages the object that transform is attached to
    public static LevelManager GetLevelManagerForTransform(Transform transform) {
        Transform t = transform;
        LevelManager levelManager = null;

        while (t.parent != null) {
            if (t.parent.tag == "LevelManagerObject") {
                levelManager = t.parent.gameObject.GetComponent<LevelManager>();
                break;
            } else {
                t = t.parent.transform;
            }
        }

        return levelManager;
    }
}
