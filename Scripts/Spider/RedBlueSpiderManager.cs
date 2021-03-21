using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//correct: https://freesound.org/people/Bertrof/sounds/131660/
//Wrong https://freesound.org/people/ertfelda/sounds/243700/

public class RedBlueSpiderManager : SpiderManager
{
    LevelManager levelManager;

    public Material blueSpiderMat;
    public Material redSpiderMat;
    private new SkinnedMeshRenderer renderer;

    public bool redSpider;
    private bool inRedZone = false;
    private bool inBlueZone = false;

    // Start is called before the first frame update
    new void Start() {
        levelManager = LevelManager.GetLevelManagerForTransform(transform);
    }

     new void Awake() {
        renderer = transform.Find("Spider").GetComponent<SkinnedMeshRenderer>();

        base.Awake();
    }

    public override void PrepForSpawn() {
        base.PrepForSpawn();

        inBlueZone = false;
        inRedZone = false;

        Random.seed = System.DateTime.Now.Millisecond * (int)(transform.position.magnitude * 100);

        if (Random.Range(0, 2) == 0) {
            redSpider = true;
            renderer.material = redSpiderMat;
        }
        else {
            redSpider = false;
            renderer.material = blueSpiderMat;
        }
    }

    public new void OnEnable() {
        base.OnEnable();
    }

    public void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.name == "BlueZone") {
            inBlueZone = true;
        }
        else if (coll.gameObject.name == "RedZone") {
            inRedZone = true;
        }

    }

    public void OnTriggerExit(Collider coll) {
        if (coll.gameObject.name == "BlueZone") {
            inBlueZone = false;
        }
        else if (coll.gameObject.name == "RedZone") {
            inRedZone = false;
        }
    }

    // Update is called once per frame
    new void LateUpdate() {

        if (!grabbable.isGrabbed) {
            if (inBlueZone) {
                levelManager.SubmitAnswer(!redSpider);
                gameObject.SetActive(false);
            }
            else if (inRedZone) {
                levelManager.SubmitAnswer(redSpider);
                gameObject.SetActive(false);
            }
        }

        base.LateUpdate();
    }
}
