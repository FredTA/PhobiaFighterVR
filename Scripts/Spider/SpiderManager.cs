using UnityEngine;


//Handles everything related to gripping the spider (disabling pathing, rootmotion, scoring when putting in zones)
public class SpiderManager : MonoBehaviour
{
    private SpiderPathController pathController;
    protected SpiderAnimationController animationController;
    protected OVRGrabbable grabbable;
    public bool flatOnTable = false; //should be private, public just to see what's happening in the editor 
    public float forwardY;
    private const float MAXIMUM_ANGLE_TO_FLAT_FOR_TABLE_CONTACT = 0.001f;

    private const float MINIMUM_GRIP_TRIGGER_AMOUNT = 0.2f;

    private bool waitingForFlatTableContact = false;

    //TODO fix hacky workaround, why doesn't overriding onGrabbed etc work
    public bool grabBegun;
    public bool grabEnded;
    public bool lastIsGrabbed;

    //---Saving the spawn position
    public Vector3 initialPosition;
    public Quaternion initialRotation;

    //How skittish the spider is, affects animation in hands and vibration intensity 
    private const float MIN_SKITTISH_MULTIPLIER = 0.1f;
    private float skittishMultiplier;
    private const float BASE_CONTROLLER_VIBRATION = 0.55f;
    private const float MIN_CONTROLLER_VIBRATION = 0.25f;
    private float controllerVibration;
    private OVRInput.Controller grabbingController;

    // Start is called before the first frame update
    protected void Awake() {
        grabbable = GetComponent<OVRGrabbable>();
        pathController = gameObject.GetComponent<SpiderPathController>();
        animationController = gameObject.GetComponent<SpiderAnimationController>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    protected void OnEnable() {
        PrepForSpawn();
    }

    public virtual void PrepForSpawn() {
        animationController.Disable();
        pathController.enabled = false;
        flatOnTable = false;

        skittishMultiplier = Random.Range(MIN_SKITTISH_MULTIPLIER, 1);
        animationController.UpdateIdleSpeed(skittishMultiplier);
        controllerVibration = BASE_CONTROLLER_VIBRATION * skittishMultiplier;
        if (controllerVibration < MIN_CONTROLLER_VIBRATION) {
            controllerVibration = MIN_CONTROLLER_VIBRATION;
        }
    }

    private void Respawn() {
        PrepForSpawn();

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
    }

    public void OnCollisionEnter(Collision coll) {
        if (coll.gameObject.name == "Floor") {
            Respawn();
        }
    }

    public void OnCollisionStay(Collision coll) {
        if (coll.gameObject.name == "Table" && !flatOnTable) {
            //If Flat on the table (pointing up)
            if (Vector3.Angle(transform.up, Vector3.up) == 0) {
                //We want to cancel any velocity so we don't bounce around
                GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
                GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);

                flatOnTable = true;
            }
            //If upside down on table
            else if (Vector3.Angle(transform.up, Vector3.down) == 0) {
                //So the spider wriggles when left on its back
                animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.idle);
                animationController.ToggleRootMotion(true);
            }
        }
    }

    public void OnCollisionExit(Collision coll) {
        if (coll.gameObject.name == "Table") {
            flatOnTable = false;
        }
    }

    // Update is called once per frame
    protected void Update() {

        //TODO should this live in a different file?
        OVRGrabber grabber = grabbable.grabbedBy;
        if (null != grabber) {
            Debug.Log("GRAB " + grabber.getController());
            grabbingController = grabber.getController();
            OVRInput.SetControllerVibration(0.3f, controllerVibration, grabbingController);
        }

        if (grabbable.isGrabbed && !lastIsGrabbed) {
            grabBegun = true;
        }
        else if (!grabbable.isGrabbed && lastIsGrabbed) {
            grabEnded = true;
        }

        lastIsGrabbed = grabbable.isGrabbed;

        //If the spider is on the on the table, is flat, and isn't already activiated 
        if (flatOnTable) {
            if (!pathController.isActiveAndEnabled) {
                if (!animationController.IsAnimatorEnabled()) {
                    animationController.Enable();
                }
                
                animationController.ToggleIdleHeld(false);
                animationController.ToggleRootMotion(true);
                pathController.enabled = true;
            }
        }
        else { 
            //If we just picked up the spider
            if (grabBegun) {
                animationController.ToggleIdleHeld(true);
                animationController.ToggleRootMotion(true); // can be true while grabbed
                pathController.enabled = false;
            } 
            //If we just let go of the spider
            else if (grabEnded) {
                animationController.SetFallingSpeed();
                animationController.ToggleRootMotion(false);

                if (null != grabbingController) {
                    OVRInput.SetControllerVibration(0, 0, grabbingController);
                }
            }
        }

        forwardY = transform.forward.y;
    }

    public void LateUpdate() {
        grabBegun = grabEnded = false;
    }

}
