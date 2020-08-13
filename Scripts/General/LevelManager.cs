using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LevelManager : MonoBehaviour {

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


    public virtual void Awake() {
        gameObjectsHolder = gameObject.transform.GetChild(0).gameObject;
        gameObjectsHolder.SetActive(false); //Ensure holders are inactive at first
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
    public void ResetLevel() {
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

    public bool IsLevelActive() {
        return levelActive;
    }
}
