using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO Sometimes we fall out of the borders (looks like it's when we're walking along the border, somehow), 
//maybe change target dir to centre of table?
public class PathController : MonoBehaviour {
    //---Randomization control---
    private const float BASE_ROTATION_SPEED = 1.2f;
    private const float BASE_SPEED = 0.05f;

    private const float MINIMUM_WAIT_TIME = 0.3f;
    private const float MAXIMUM_WAIT_TIME = 1.2f;
    private const float MINIMUM_WALK_TIME = 2.3f;
    private const float MAXIMUM_WALK_TIME = 5f;

    private const float TURN_ON_THE_SPOT_MIN_SPEED = 0.5f;
    private const float TURN_ON_THE_SPOT_MAX_SPEED = 2.5f;
    private const float MAX_TURN_ON_THE_SPOT_ANGLE = 100;
    private const float MIN_TURN_ON_THE_SPOT_ANGLE = 20;

    private const float MAXIMUM_SPEED_VARIANCE_MULTIPLIER = 5.7f;

    //Walk cycle
    private float randomWalkSpeed;
    private float randomRotateSpeed;
    private float timeOfWalkStart = -1;
    private float walkTime;

    //Turning from walls
    private Vector3 targetDir; 
    private bool turningFromXBorder = false;
    private bool turningFromZBorder = false;
    private Vector3 tableCentrePosition;

    //Spider avoidance
    public int priority;
    private const float MINIMUM_SPIDER_DISTANCE = 0.3f;
    public bool ableToAvoidOtherSpiders = false;
    private bool waitingForOtherSpiderToAvoid = false;
    private GameObject spiderAvoidingUs = null;
    private GameObject spiderWeAreAvoiding = null;
    private GameObject[] spiders;

    //Animation control
    private const float RUNNING_ANIMATION_SPEED_MULTIPLIER = 0.7f;
    private const float TURNING_ANIMATION_SPEED_MULTIPLIER = 0.7f;
    private const float WALKING_ANIMATION_SPEED_MULTIPLIER = 0.7f;
    private Spider animationController;
    private Spider.SpiderAnimations pausedAnimation;
    private float pausedAnimationSpeed;
    private bool animationPaused;

    void Awake() {
        animationController = gameObject.GetComponent<Spider>();
        spiders = GameObject.FindGameObjectsWithTag("SpiderCluster");
        tableCentrePosition = GameObject.FindGameObjectWithTag("TableCentre").transform.position;
    }

    void Start() {
        //If we have this in Awake, the coroutine doesn't continue to exectute after the first yield (why?)
        StartCoroutine("NewWalkingCycle");
    }

    // Update is called once per frame
    void Update() {
        DrawRay(transform.position, transform.forward, Color.green); //forward direction
        HandleSpiderAvoidance(); //TODO Optimization probs don't need to do this every frame, consider coroutine
    }

    //If we're turning, we have priority, the other spider should turn away from us 
    //If both of us are turning, the one with lower priority stops moving
    private void HandleSpiderAvoidance() {
        //Don't need to do this check if we're already waiting for another spider to avoid us, we'll be staying put anyway
        if (!waitingForOtherSpiderToAvoid) {
            //If we're not already avoiding a spider
            if (null == spiderWeAreAvoiding) {
                float closestDistance;
                GameObject closestSpider = FindClosestSpiderThatIsTooClose(out closestDistance);

                //If there is a spider that is too close
                if (null != closestSpider) {
                    //Debug.Log(gameObject.name + " is too close to " + closestSpider.gameObject.name);
                    if (ableToAvoidOtherSpiders) {
                        spiderWeAreAvoiding = closestSpider;
                        //Debug.Log(gameObject.name.ToUpper() + ": Turning away from " + closestSpider.gameObject.name);
                    }
                    else {
                        //Seeing as we're not able to avoid (turn away from) the other spider ourselves (e.g busy turning from the border)
                        //We should just stay put while the other spider moves around us
                        //If neither spider is currently able to avoid, the one with lower priority should stay put, and the other should continue as normal
                        PathController otherController = closestSpider.GetComponent<PathController>();
          
                        //If the other spider is able to avoid us, tell it to avoid us, and we stay put
                        if (otherController.ableToAvoidOtherSpiders) {
                            otherController.SetSpiderToAvoid(gameObject); //If the other spider can avoid us, tell it to do so 
                            SetSpiderToWaitFor(closestSpider); //Also make sure we stay put while that spider moves away
                            Debug.Log(gameObject.name.ToUpper() + ": Waiting for " + closestSpider.gameObject.name + " to avoid us");
                        }
                        //If neither spider can avoid, but we are higher priority, tell the other spider to stay put, and we'll keep walking normally
                        else if (otherController.priority < priority) {
                            otherController.SetSpiderToWaitFor(gameObject);
                            Debug.Log(gameObject.name.ToUpper() + ": Walking while " + closestSpider.gameObject.name + " waits");
                        }
                        //If neither cana void, and we are lower priority, we'll stay put, and the other spider should keep walking normally
                        else {
                            SetSpiderToWaitFor(closestSpider);
                            Debug.Log(gameObject.name.ToUpper() + ": Waiting for " + closestSpider.gameObject.name + " to move away");
                        }
                    }

                }
            }

        }
        //If we are already waiting for a spider to avoid us, 
        //check if it's now far enough away for us to start moving again
        else {
            float distance = (transform.position - spiderAvoidingUs.transform.position).magnitude; 
            if (distance > MINIMUM_SPIDER_DISTANCE) {
                string pausedAnimationName = System.Enum.GetName(typeof(Spider.SpiderAnimations), (int)pausedAnimation);
                waitingForOtherSpiderToAvoid = false;
                string debug = gameObject.name.ToUpper() + ": " + spiderAvoidingUs.gameObject.name + " is out of my way. ";
                if (animationPaused) {
                    animationController.SetAnimation(pausedAnimation);
                    animationController.SetSpeed(pausedAnimationSpeed);
                    debug += "Resuming " + pausedAnimationName;
                }
                Debug.Log(debug);
                spiderAvoidingUs = null;
            }
        }


        if (null != spiderWeAreAvoiding) {    
            AvoidSpider();
            float distance = (transform.position - spiderWeAreAvoiding.transform.position).magnitude;
            if (distance > MINIMUM_SPIDER_DISTANCE) {
                spiderWeAreAvoiding = null;
            }
        }
            
    }

    private GameObject FindClosestSpiderThatIsTooClose(out float closestDistance) {
        //Find the closest spider out of all the ones that are too close (if any)
        GameObject closestSpider = null;
        closestDistance = -1;
        foreach (GameObject spider in spiders) { //TODO put this in a new method
            if (spider == gameObject) {
                continue; //Don't check distance to this spider instance
            }
            float distance = (transform.position - spider.transform.position).magnitude;
            if (distance < MINIMUM_SPIDER_DISTANCE) {
                closestDistance = distance;
                closestSpider = spider;
            }
        }
        return closestSpider;
    }

    private void AvoidSpider() {
        if (!turningFromXBorder && !turningFromZBorder) {
            Vector3 avoidDir = transform.position - spiderWeAreAvoiding.transform.position;
            avoidDir.y = transform.forward.y; //Spider's of different sizes may have different y positions
            DrawRay(transform.position, avoidDir, Color.magenta);
            TurnToTarget(randomRotateSpeed, avoidDir);
        }
    }

    public void SetSpiderToAvoid(GameObject spider) {
        spiderWeAreAvoiding = spider;
    }

    public void SetSpiderToWaitFor(GameObject spider) {
        waitingForOtherSpiderToAvoid = true;
        spiderAvoidingUs = spider;
        HandleAnimationPause();
    }

    //Pause the current animation if neccessary
    private void HandleAnimationPause() {
        Spider.SpiderAnimations currentAnimation = animationController.GetCurrentAnimation();
        string currentAnimationName = System.Enum.GetName(typeof(Spider.SpiderAnimations), (int)pausedAnimation);
        //string debug = gameObject.name.ToUpper() + " (" + priority + "): Going to wait for " + spider.gameObject.name + " (" + otherController.priority + "): " + " to move away: ";

        //We don't need to pause the animation if it's idle or turning, as the spider is staying in the same spot anyway
        if (currentAnimation != Spider.SpiderAnimations.idle && currentAnimation != Spider.SpiderAnimations.turnleft &&
            currentAnimation != Spider.SpiderAnimations.turnright) {
            animationPaused = true;
            pausedAnimation = currentAnimation;
            pausedAnimationSpeed = animationController.GetSpeed();

            animationController.SetAnimation(Spider.SpiderAnimations.idle);
            animationController.SetSpeed(1);
            //debug += "Pausing " + currentAnimationName;
        }
        else {
            animationPaused = false;
        }
        //Debug.Log(debug);
    }

    //TODO what about the animation after we finish turning

    //Each walking cycle starts with a wait, then a turn, another wait, and then walking begins
    private IEnumerator NewWalkingCycle() {
        //Debug.Log(gameObject.name + " is starting a new walking cycle...");
        //First wait-----
        ableToAvoidOtherSpiders = false; //Can't avoid other spiders if we're standing still (or turning on the spot)
        animationController.SetAnimation(Spider.SpiderAnimations.idle);
        yield return new WaitForSeconds(RandomWait());
        while (waitingForOtherSpiderToAvoid) {
            yield return null;
        }
        //Debug.Log(gameObject.name + " is done waiting");

        //Turn on the spot-----
        //Debug.Log(gameObject.name + " is turning on the spot");
        float angle;
        do {
            float randomX = Random.Range(-1f, 1f);
            float randomZ = Mathf.Sqrt(1 - Mathf.Pow(randomX, 2));
            if (Random.Range(-1f, 1f) < 0) randomZ = -randomZ;

            targetDir = new Vector3(randomX, transform.forward.y, randomZ);
            angle = SignedAngleBetween(transform.forward, targetDir, transform.up);

        } while (Mathf.Abs(angle) > MAX_TURN_ON_THE_SPOT_ANGLE ||
                 Mathf.Abs(angle) < MIN_TURN_ON_THE_SPOT_ANGLE);
        //TODO Optimize this ^^ not super efficient to just keep rolling the dice until we get a number we like

        if (angle < 0) {
            animationController.SetAnimation(Spider.SpiderAnimations.turnleft);
        } else {
            animationController.SetAnimation(Spider.SpiderAnimations.turnright);
        }

        float turnSpeed = Random.Range(TURN_ON_THE_SPOT_MIN_SPEED, TURN_ON_THE_SPOT_MAX_SPEED);
        while (!SameDirection(targetDir, transform.forward)) {
            TurnToTarget(turnSpeed, targetDir);
            yield return null;
        }

        //TODO Don't stop waiting if we're waiting fro other spider to avoid
        //Second wait---
        animationController.SetAnimation(Spider.SpiderAnimations.idle);
        yield return new WaitForSeconds(RandomWait());
        while (waitingForOtherSpiderToAvoid) {
            yield return null;
        }

        //Walking
        //Debug.Log(gameObject.name + " is starting to walk");
        ableToAvoidOtherSpiders = true;
        targetDir = new Vector3(transform.forward.x, transform.forward.y, transform.forward.z); //Close enough, don't  want to snap when walking starts

        walkTime = Random.Range(MINIMUM_WALK_TIME, MAXIMUM_WALK_TIME); //The time of this walk cycle
        timeOfWalkStart = Time.time;

        float multiplier = Random.Range(1, MAXIMUM_SPEED_VARIANCE_MULTIPLIER);
        randomWalkSpeed = BASE_SPEED * multiplier;
        randomRotateSpeed = BASE_ROTATION_SPEED * multiplier;

        animationController.SetAnimation(Spider.SpiderAnimations.walking);
        animationController.SetSpeed(multiplier * RUNNING_ANIMATION_SPEED_MULTIPLIER); //TODO tweak anim speed
        StartCoroutine("Walk");
    }

    private float RandomWait() {
        float waitTime = Random.Range(MINIMUM_WAIT_TIME, MAXIMUM_WAIT_TIME);
        animationController.SetSpeed(1);
        animationController.SetAnimation(Spider.SpiderAnimations.idle);
        //Debug.Log(gameObject.name.ToUpper() + ": Waiting for " + waitTime + " secs");
        return waitTime;
    }

    private float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n) {
        // angle in [0,180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        return signed_angle;
    }

    //Walks the spider towards the target
    private IEnumerator Walk() {
        while (Time.time - timeOfWalkStart < walkTime) {
            if (!waitingForOtherSpiderToAvoid) {
                if (turningFromXBorder || turningFromZBorder) {
                    TurnToTarget(randomRotateSpeed, targetDir);
                }
                transform.position += transform.forward * Time.deltaTime * randomWalkSpeed;
            } else {
                //If we're waiting for the other spider to move around us, we shouldn't move
                walkTime += Time.deltaTime;
            }
            yield return null;
        }

        //Don't want to start a new cycle if we're currently turning from the border or avoiding a spider
        if (turningFromXBorder || turningFromZBorder || spiderWeAreAvoiding != null) {
            walkTime += 1.5f;
            StartCoroutine("Walk");
        }
        //If we're finished waiting 
        else {
            StartCoroutine("NewWalkingCycle");
        }
    }

    private void TurnToTarget(float turnSpeed, Vector3 target) {
        float step = turnSpeed * Time.deltaTime;
        Vector3 newDir = Vector3.RotateTowards(transform.forward, target, step, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDir);
        DrawRay(transform.position, target, Color.blue);
    }

    public bool SameDirection(Vector3 v1, Vector3 v2) {
        return (Mathf.Abs(v1.x - v2.x) < 0.01f && Mathf.Abs(v1.z - v2.z) < 0.01f);
    }

    //Turning towards the centre of the table rather than flipping x and/or z - not yet working
    //private void OnTriggerEnter(Collider other) {
    //    if (other.gameObject.tag == "ZBorder" || other.gameObject.tag == "XBorder") {
    //        ableToAvoidOtherSpiders = false;
            
    //        targetDir = tableCentrePosition - transform.position;
    //        targetDir.y = transform.forward.y;
    //    }

    //    if (other.gameObject.tag == "ZBorder") {
    //        turningFromZBorder = true;
    //    }
    //    if (other.gameObject.tag == "XBorder") {
    //        turningFromXBorder = true;
    //    }
    //}

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "ZBorder") {
            ableToAvoidOtherSpiders = false;
            turningFromZBorder = true;
            if (!turningFromXBorder) {
                targetDir = new Vector3(transform.forward.x, transform.forward.y, -transform.forward.z);
            }
            else {
                targetDir = new Vector3(targetDir.x, targetDir.y, -targetDir.z);
            }
        }
        else if (other.gameObject.tag == "XBorder") {
            ableToAvoidOtherSpiders = false;
            turningFromXBorder = true;
            if (!turningFromZBorder) {
                targetDir = new Vector3(-transform.forward.x, transform.forward.y, transform.forward.z);
            }
            else {
                targetDir = new Vector3(-targetDir.x, targetDir.y, targetDir.z);
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        string debug = "";
        if (other.gameObject.tag == "ZBorder") {
            turningFromZBorder = false;
            debug += "Left Z border";
        }
        else if (other.gameObject.tag == "XBorder") {
            turningFromXBorder = false;
            debug += "Left X border";
        }

        if (!turningFromZBorder && !turningFromXBorder) {
            ableToAvoidOtherSpiders = true;
            debug += " - away from both borders";
        }

        Debug.Log(debug);
    }

    //normal debug.drawray draws ray under table so it's pretty hard to see, move it up a little
    private const float DEBUG_RAY_OFFSET = 0.03f;
    private void DrawRay(Vector3 pos, Vector3 dir, Color col) {
        Vector3 newPos = new Vector3(pos.x, pos.y + DEBUG_RAY_OFFSET, pos.z);
        Debug.DrawRay(newPos, dir, col);
    }
}
