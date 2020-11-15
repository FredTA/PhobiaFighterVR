using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderPathController : MonoBehaviour { //TODO animations need some love

    //---Movement stages ---
    public MovementStage currentMovementStage; //TODO these 2 don't need to be public, it's just good to see in the editor
    public enum MovementStage {
        RandomWait1, 
        TurnOnTheSpot,
        RandomWait2,
        WalkStage
    }

    public WalkStage currentWalkStage = WalkStage.None;
    public enum WalkStage {
        Forward, 
        Turn, 
        Avoid, 
        Wait,
        None
    }

    public BorderPriorities borderPriority = BorderPriorities.None;
    public enum BorderPriorities {
        RightBorder,
        LeftBorder,
        UpBorder,
        DownBorder,
        None
    }

    private const float BASE_TURN_SPEED = 1.6f;
    private const float BASE_SPEED = 0.13f;

    //---Randomization control---
    private const float MINIMUM_WAIT_TIME = 0.3f;
    private const float MAXIMUM_WAIT_TIME = 1.2f;
    private const float MINIMUM_WALK_TIME = 2.3f;
    private const float MAXIMUM_WALK_TIME = 5f;

    private const float TURN_ON_THE_SPOT_MIN_SPEED = 0.9f;
    private const float TURN_ON_THE_SPOT_MAX_SPEED = 2.5f;
    private const float MAX_TURN_ON_THE_SPOT_ANGLE = 100;
    private const float MIN_TURN_ON_THE_SPOT_ANGLE = 35f;

    private const float MAXIMUM_SPEED_VARIANCE_MULTIPLIER = 3.5f;

    //Walk cycle
    private float walkMultiplier;
    private float randomWalkSpeed;
    private float randomRotateSpeed;
    private float movementStageEndTime = -1;
    private Vector3 targetDir;

    //Time spent in the Walk stage not walking forward (waiting, turning, avoiding) shouldn't count towards this time
    private float walkStageDuration;
    private float walkStageElapsedTime;

    //TODO refactor - this should probably all live in the animation controller instead 
    //Animation control
    private const float RUNNING_ANIMATION_SPEED_MULTIPLIER = 0.2f;
    private const float TURNING_ANIMATION_SPEED_MULTIPLIER = 1.25f;
    private const float WALKING_ANIMATION_SPEED_MULTIPLIER = 1.1f * 1.4f;
    private const float IDLE_ANIMATION_SPEED_MULTIPLIER = 0.7f;
    private SpiderAnimationController animationController;

    //Spider avoidance
    public int priority;
    private const float MINIMUM_SPIDER_AVOID_DISTANCE = 0.15f;
    private const float MAXIMUM_SPIDER_AVOID_DISTANCE = 0.25f;
    public bool ableToAvoidOtherSpiders = false;
    private GameObject[] spiders;
    public GameObject spiderBeingAvoided = null;
    private GameObject closestSpider = null;
    private SpiderPathController closestSpiderController;
    private float closestSpiderDistance;
    private float closestSpiderAngle;
    private bool spiderToAvoidIsInSector = false;
    private const float SPIDER_AVOID_SECTOR_LENGTH = 0.3f;
    private const float SPIDER_AVOIDANCE_SECTOR_MIN_ANGLE = 10;
    private const float SPIDER_AVOIDANCE_SECTOR_MAX_ANGLE = 40;
    private const float BASE_AVOID_TURN_SPEED = 0.7f;

    private const float ADDITIONAL_WAIT_TIME = 1f;
    private float timeToLeaveWait = -1f;
    private GameObject spiderWeAreWaitingFor = null;

    private Vector3 tableCentrePosition;
    private const float DIRECTION_ACCEPTANCE_THESHOLD = 0.1F;
    private const float ANGLE_ACCEPTANCE_THRESHOLD = 3;

    //Debugging
    public bool debugEnabled = true;
    private Vector3 debugOffset = new Vector3(0, 0.02f, 0);
    private const float DEBUG_RAY_LENGTH_MULTIPLIER = 0.25f;
    private const float DEBUG_RAY_LENGTH_AVOID_MULTIPLIER = 2f;

    //Table borders
    private float rightBorderX;
    private float leftBorderX;
    private float downBorderZ;
    private float upBorderZ;
    private float borderBoundaryLength;
    private const float TURN_FROM_BORDER_MULTIPLER = 2f;

    void Awake() {
        animationController = gameObject.GetComponent<SpiderAnimationController>();
        spiders = GameObject.FindGameObjectsWithTag("SpiderCluster");
        Vector3 tableCentreObjectPosition = GameObject.FindGameObjectWithTag("TableCentre").transform.position;
        tableCentrePosition = new Vector3(tableCentreObjectPosition.x, transform.position.y, tableCentreObjectPosition.z);

        leftBorderX = GameObject.Find("LeftBorderStart").transform.position.x;
        rightBorderX = GameObject.Find("RightBorderStart").transform.position.x;
        upBorderZ = GameObject.Find("UpBorderStart").transform.position.z;
        downBorderZ = GameObject.Find("DownBorderStart").transform.position.z;
    }

    // Start is called before the first frame update
    void Start() {
        //currentMovementStage = MovementStage.RandomWait1;
        //PrepareRandomWaitStage();
        borderBoundaryLength = GameObject.Find("TableBorders").GetComponent<BorderController>().getBorderDistance(); //must come after BC init

        //todo remove, for debugging
        debugEnabled = true;
        currentMovementStage = MovementStage.WalkStage;
        currentWalkStage = WalkStage.Forward;
        walkStageDuration = 1000;
        randomWalkSpeed = BASE_SPEED * 1f;
        walkMultiplier = 1.5f;
        animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.walking);
        animationController.SetSpeed(walkMultiplier * WALKING_ANIMATION_SPEED_MULTIPLIER);
    }

    // Update is called once per frame
    void FixedUpdate() {
        closestSpider = FindClosestSpider();
        if (null != closestSpider) {
            closestSpiderController = closestSpider.GetComponent<SpiderPathController>();

            //If there's a spider close to use that is turning (meaning it can't avoid us)
            //We should jump straight to the walk stage so that we can start to move out of the way
            if (closestSpiderDistance < MAXIMUM_SPIDER_AVOID_DISTANCE && closestSpiderController.currentWalkStage == WalkStage.Turn
                && currentMovementStage != MovementStage.WalkStage) {
                GoToMovementStage(MovementStage.WalkStage);
            }
        }

        switch (currentMovementStage) {
            case MovementStage.RandomWait1:
                HandleRandomWaitStage();
                break;

            case MovementStage.TurnOnTheSpot:
                HandleTurnOnTheSpotStage();
                break;

            case MovementStage.RandomWait2:
                HandleRandomWaitStage();
                break;

            case MovementStage.WalkStage:
                HandleWalkStage();
                break;
        }
    }

    //To be used for e.g jumping from randomwait 1 to walk 
    private void GoToMovementStage(MovementStage newMovementStage) {
        //Debug.Log(gameObject.name + ": " + currentMovementStage.ToString() + " --> " + newMovementStage.ToString());
        currentMovementStage = newMovementStage;
        switch (newMovementStage) {
            case MovementStage.RandomWait1:
                PrepareRandomWaitStage();
                break;
            case MovementStage.TurnOnTheSpot:
                PrepareTurnOnTheSpotStage();
                break;
            case MovementStage.RandomWait2:
                PrepareRandomWaitStage();
                break;
            case MovementStage.WalkStage:
                PrepareWalkStage();
                break;
        }
    }

    private void NextMovementStage() {
        if (currentMovementStage == MovementStage.WalkStage) {
            GoToMovementStage(MovementStage.RandomWait1);
        } else {
            GoToMovementStage(currentMovementStage + 1);
        }
    }

    private void PrepareRandomWaitStage() {
        float stageDuration = Random.Range(MINIMUM_WAIT_TIME, MAXIMUM_WAIT_TIME);
        movementStageEndTime = Time.time + stageDuration;
        animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.idle);
        animationController.SetSpeed(IDLE_ANIMATION_SPEED_MULTIPLIER);
    }

    private void HandleRandomWaitStage() {
        if (Time.time > movementStageEndTime) {
            NextMovementStage();
        }
    }

    private void PrepareTurnOnTheSpotStage() {
        SetRandomTurnOnTheSpotAngle(out float angle);
        randomRotateSpeed = Random.Range(TURN_ON_THE_SPOT_MIN_SPEED, TURN_ON_THE_SPOT_MAX_SPEED);
        //Debug.Log("Angle: " + angle);
        if (angle < 0) {
            animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.turnleft);
            //Debug.Log("ACW");
        }
        else {
            animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.turnright);
            //Debug.Log("CW");
        }

        float animationMultiplier = randomRotateSpeed / TURN_ON_THE_SPOT_MIN_SPEED;
        animationController.SetSpeed(animationMultiplier * TURNING_ANIMATION_SPEED_MULTIPLIER);
    }

    private void HandleTurnOnTheSpotStage() {
        if (transform.forward != targetDir) {
            TurnToTargetDirection(randomRotateSpeed, targetDir, false);

            if (debugEnabled) {
                Debug.DrawRay(transform.position + debugOffset, transform.forward * DEBUG_RAY_LENGTH_MULTIPLIER, Color.green);
                Debug.DrawRay(transform.position + debugOffset, targetDir * DEBUG_RAY_LENGTH_MULTIPLIER, Color.blue);
            }
        }
        else {
            NextMovementStage();
        }
    }

    private void SetWalkStage(WalkStage newWalkStage) {
        if (currentWalkStage != newWalkStage) {
            if (currentWalkStage == WalkStage.Wait && timeToLeaveWait == -1) {
                //We should wait another few seconds before switching from wait to move
                timeToLeaveWait = Time.time + ADDITIONAL_WAIT_TIME;
            }
            else if (currentWalkStage != WalkStage.Wait ||
              (currentWalkStage == WalkStage.Wait && Time.time > timeToLeaveWait)) {
                //Debug.Log(gameObject.name + ": " + currentWalkStage.ToString() + " --> " + newWalkStage.ToString());
                timeToLeaveWait = -1;
                //Debug.Log("SetWalkStage setting to " + newWalkStage.ToString() + "from " + currentWalkStage);
                currentWalkStage = newWalkStage;
                if (newWalkStage == WalkStage.Wait) {
                    //Debug.Log("setting to idle...");
                    animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.idle);
                    animationController.SetSpeed(IDLE_ANIMATION_SPEED_MULTIPLIER);
                }
                else {
                    animationController.SetSpeed(walkMultiplier * WALKING_ANIMATION_SPEED_MULTIPLIER);
                    animationController.SetAnimation(SpiderAnimationController.SpiderAnimations.walking);
                }
            }
        }
    }

    private void PrepareWalkStage() {
        walkMultiplier = Random.Range(1, MAXIMUM_SPEED_VARIANCE_MULTIPLIER);
        randomWalkSpeed = walkMultiplier * BASE_SPEED;
        animationController.SetSpeed(walkMultiplier * WALKING_ANIMATION_SPEED_MULTIPLIER);

        walkStageElapsedTime = 0;
        walkStageDuration = Random.Range(MINIMUM_WALK_TIME, MAXIMUM_WALK_TIME);
        SetWalkStage(WalkStage.Forward);
    }

    private void HandleWalkStage() {
        float borderProgress = GetBorderProgressAmount();

        //We have to do this first as we'll be checking if we're in the turn stage in the next block
        if (borderProgress != -1) {
            SetWalkStage(WalkStage.Turn);
        }
        else {
            SetBorderPriority(BorderPriorities.None);
        }

        //If there's a spider too close to us
        if (null != closestSpider) {

            switch (currentWalkStage) {
                case WalkStage.Forward:
                    SetWalkStage(WalkStage.Avoid);
                    spiderBeingAvoided = closestSpider;
                    break;
                case WalkStage.Turn:
                    if (closestSpiderController.currentWalkStage == WalkStage.Turn && !DoesThisSpiderHavePriority(closestSpiderController.gameObject)) {
                        spiderWeAreWaitingFor = closestSpider;
                        SetWalkStage(WalkStage.Wait);
                    }
                    else if (borderProgress == -1) {
                        SetWalkStage(WalkStage.Forward);
                    }
                    break;
                case WalkStage.Avoid:
                    if (spiderBeingAvoided != closestSpider && closestSpiderController.currentWalkStage == WalkStage.Turn) {
                        spiderWeAreWaitingFor = closestSpider;
                        SetWalkStage(WalkStage.Wait);
                    }
                    //If the spider is avoiding another spider that isn't us, the one with lower priority waits
                    else if (closestSpiderController.currentWalkStage == WalkStage.Avoid &&
                        closestSpiderController.spiderBeingAvoided != gameObject && priority < closestSpiderController.priority) {
                        spiderWeAreWaitingFor = closestSpider;
                        SetWalkStage(WalkStage.Wait);
                    }
                    break;
                case WalkStage.Wait:
                    if (priority > closestSpiderController.priority) {
                        //If there are three spiders that get too close to eachother, and the highest priority walks way, 
                        //The other two will still be stuck in wait, so we need this check here
                        SetWalkStage(WalkStage.Forward);
                    }
                    break;
            }
        }
        else if (borderProgress == -1) {
            //If there are no spiders too close, and we're not at the border, we can walk forward normally
            SetWalkStage(WalkStage.Forward);
            spiderBeingAvoided = null;
        }

        switch (currentWalkStage) {
            case WalkStage.Forward:
                if (walkStageElapsedTime < walkStageDuration) {
                    transform.position += transform.forward * Time.fixedDeltaTime * randomWalkSpeed;
                    walkStageElapsedTime += Time.fixedDeltaTime;
                }
                else {
                    SetWalkStage(WalkStage.None);
                    NextMovementStage();
                }
                break;

            case WalkStage.Turn:
                Vector3 tableDirection = tableCentrePosition - transform.position;

                float rotateSpeed = BASE_TURN_SPEED * walkMultiplier * TURN_FROM_BORDER_MULTIPLER;
                if (null != closestSpider && closestSpiderDistance < MAXIMUM_SPIDER_AVOID_DISTANCE) { 
                    rotateSpeed = rotateSpeed * (MAXIMUM_SPIDER_AVOID_DISTANCE - closestSpiderDistance) / (MAXIMUM_SPIDER_AVOID_DISTANCE - MINIMUM_SPIDER_AVOID_DISTANCE);
                } else {
                    rotateSpeed = rotateSpeed * borderProgress;
                }
                
                TurnToTargetDirection(rotateSpeed, tableDirection, true);
                transform.position += transform.forward * Time.fixedDeltaTime * randomWalkSpeed;

                if (debugEnabled) {
                    Color debugColorGreen = new Color(0, 1, 0, Mathf.Clamp01(borderProgress));
                    Color debugColorMagenta = new Color(1, 0, 1, Mathf.Clamp01(borderProgress));
                    Debug.DrawRay(transform.position + debugOffset, transform.forward * DEBUG_RAY_LENGTH_MULTIPLIER, debugColorGreen);
                    Debug.DrawRay(transform.position + debugOffset, tableDirection, debugColorMagenta);
                }

                break;

            case WalkStage.Avoid:
                float turnSpeedMultiplier;

                if (spiderToAvoidIsInSector) {
                    turnSpeedMultiplier = (SPIDER_AVOIDANCE_SECTOR_MAX_ANGLE - closestSpiderAngle) / (SPIDER_AVOIDANCE_SECTOR_MAX_ANGLE - SPIDER_AVOIDANCE_SECTOR_MIN_ANGLE);
                }
                else {
                    turnSpeedMultiplier = (MAXIMUM_SPIDER_AVOID_DISTANCE - closestSpiderDistance) / (MAXIMUM_SPIDER_AVOID_DISTANCE - MINIMUM_SPIDER_AVOID_DISTANCE);
                }

                float turnSpeed = BASE_AVOID_TURN_SPEED * walkMultiplier * turnSpeedMultiplier;
                TurnToTargetDirection(turnSpeed, transform.position - closestSpider.transform.position, false);
                transform.position += transform.forward * Time.fixedDeltaTime * randomWalkSpeed;

                if (debugEnabled) {
                    Color debugColorGreen = new Color(0, 1, 0, Mathf.Clamp01(turnSpeedMultiplier));
                    Color debugColorRed = new Color(1, 0, 0, Mathf.Clamp01(turnSpeedMultiplier));
                    Debug.DrawRay(transform.position + debugOffset, transform.forward * DEBUG_RAY_LENGTH_MULTIPLIER, debugColorGreen);
                    Debug.DrawRay(transform.position + debugOffset, (transform.position - closestSpider.transform.position) * DEBUG_RAY_LENGTH_AVOID_MULTIPLIER, debugColorRed);

                    if (gameObject.name == "DefaultSpider") {
                        if (spiderToAvoidIsInSector) {
                            Debug.Log(closestSpider.gameObject.name + " IS IN SECTOR - ANGLE: " + closestSpiderAngle + " MULT: " + turnSpeedMultiplier);
                        }
                        else {
                            Debug.Log(closestSpider.gameObject.name + " IS IN CIRCLE - DISTANCE: " + closestSpiderDistance + " MULT: " + turnSpeedMultiplier);
                        }
                    }
                }
                break;

            case WalkStage.Wait:
                float opacityMultiplier;
                if (timeToLeaveWait == -1) {
                    opacityMultiplier = 1;
                } else { //1 if we've just started waiting 
                    opacityMultiplier = (timeToLeaveWait - Time.time) / ADDITIONAL_WAIT_TIME;
                    //Debug.Log("OM: " + opacityMultiplier);
                }

                if (debugEnabled) {
                    Color debugColorYellow = new Color(1, 0.92f, 0.016f, opacityMultiplier);
                    Debug.DrawLine(transform.position + debugOffset, spiderWeAreWaitingFor.transform.position + debugOffset, debugColorYellow);
                }

                break;
        }
    }

    //Compares the direction of our spider with the one passed in, the one pointing more towards the edge of the table has lower priority 
    private bool DoesThisSpiderHavePriority(GameObject otherSpider) {
        SpiderPathController otherController = otherSpider.GetComponent<SpiderPathController>();
        bool thisSpiderHasPriority = false;

        if (transform.position.z > upBorderZ) {
            thisSpiderHasPriority = transform.forward.z < otherSpider.transform.forward.z;
        }
        else if (transform.position.z < downBorderZ) {
            thisSpiderHasPriority = transform.forward.z > otherSpider.transform.forward.z;
        }
        else if (transform.position.x > rightBorderX) {
            thisSpiderHasPriority = transform.forward.x < otherSpider.transform.forward.x;
        }
        else if (transform.position.x < leftBorderX) {
            thisSpiderHasPriority = transform.forward.x > otherSpider.transform.forward.x;
        }

        //Debug.Log("Our P: " + priority + " Other P " + otherController.priority + ", should we NOW have p? " + thisSpiderHasPriority);

        //Switch the two priorities, only the spider with priority should be the one to do this, if they both execute the switch, we'll be back where we started 
        if (thisSpiderHasPriority && (priority < otherController.priority)) {
            int temp = priority;
            priority = otherController.priority;
            otherController.priority = temp;
        }

        return thisSpiderHasPriority;
    }

    //returns the most direct spider in the sector if there is one, 
    //otherwise returns the closest spider in the circle, if there is one, otherwise returns null

    private GameObject FindClosestSpider() {
        GameObject closestSpider = null;

        GameObject closestSpiderByDistance = FindClosestSpiderByDistance(out float distanceToClosestSpiderByDistance);

        if (distanceToClosestSpiderByDistance < MAXIMUM_SPIDER_AVOID_DISTANCE) {
            closestSpiderDistance = distanceToClosestSpiderByDistance;
            spiderToAvoidIsInSector = false;
            return closestSpiderByDistance;
        } else {
            GameObject closestSpiderByAngle = FindClosestSpiderByAngle(out float distanceToClosestSpiderByAngle, out float angleToClosestSpiderByAngle);

            if (distanceToClosestSpiderByAngle < SPIDER_AVOID_SECTOR_LENGTH) {
                closestSpiderDistance = distanceToClosestSpiderByAngle;
                closestSpiderAngle = angleToClosestSpiderByAngle;
                spiderToAvoidIsInSector = true;
                return closestSpiderByAngle;
            }
        }

        return null;
    }

    private GameObject FindClosestSpiderByAngle(out float closestDistance, out float angleToCloseSpider) {
        GameObject closestSpider = null;
        closestDistance = 10000;
        angleToCloseSpider = 180;
        foreach (GameObject spider in spiders) { 
            if (spider == gameObject) {
                continue; //Don't check distance to this spider instance
            }
            float distance = (transform.position - spider.transform.position).magnitude;
            if (distance < closestDistance) {
                Vector3 directionToCloseSpider = spider.transform.position - transform.position;
                angleToCloseSpider = Vector3.Angle(transform.forward, directionToCloseSpider);

                if (angleToCloseSpider < SPIDER_AVOIDANCE_SECTOR_MAX_ANGLE) {
                    closestDistance = distance;
                    closestSpider = spider;
                }
            }
        }
        return closestSpider;
    }

    private GameObject FindClosestSpiderByDistance(out float closestDistance) {
        GameObject closestSpider = null;
        closestDistance = 10000;
        foreach (GameObject spider in spiders) {
            if (spider == gameObject) {
                continue; //Don't check distance to this spider instance
            }
            float distance = (transform.position - spider.transform.position).magnitude;
            if (distance < closestDistance) {
                closestDistance = distance;
                closestSpider = spider;
            }
        }
        return closestSpider;
    }

    private float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n) {
        // angle in [0,180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        return signed_angle;
    }

    private void SetRandomTurnOnTheSpotAngle(out float angle) {
        do {
            float randomX = Random.Range(-1f, 1f);
            float randomZ = Mathf.Sqrt(1 - Mathf.Pow(randomX, 2));
            if (Random.Range(-1f, 1f) < 0) {
                randomZ = -randomZ;
            }

            targetDir = new Vector3(randomX, transform.forward.y, randomZ);
            angle = SignedAngleBetween(transform.forward, targetDir, transform.up);

        } while (Mathf.Abs(angle) > MAX_TURN_ON_THE_SPOT_ANGLE ||
                 Mathf.Abs(angle) < MIN_TURN_ON_THE_SPOT_ANGLE);
        //TODO Optimize this ^^ not super efficient to just keep rolling the dice until we get a number we like
    }

    private void TurnToTargetDirection(float rotateSpeed, Vector3 target, bool turningFromBorder) {
        //Need to take the y component out, we don't want our elevation to change (flying spiders = bad = oh god please no) 
        Vector3 correctedTarget = new Vector3(target.x, transform.forward.y, target.z);

        Vector3 newDir;
        if (Vector3.Angle(transform.forward, target) > ANGLE_ACCEPTANCE_THRESHOLD) {
            float step = rotateSpeed * Time.deltaTime;

            if (turningFromBorder) {
                correctedTarget = GetBorderTurnDirection();
            }
           
            newDir = Vector3.RotateTowards(transform.forward, correctedTarget, step, 0.0f); 
        } else {
            newDir = correctedTarget;
        }

        Vector3 correctedNewDir = new Vector3(newDir.x, transform.forward.y, newDir.z);
        transform.rotation = Quaternion.LookRotation(newDir);
    }

    //Returns a value between 0 and 1 depending on how far between the start/end borders the spider is
    //If number is negative, spider is within the start borders
    private float GetBorderProgressAmount() {
        float borderProgressZ = -1;
        if (transform.position.z > upBorderZ) {
            borderProgressZ = Mathf.Abs(transform.position.z - upBorderZ) / borderBoundaryLength;
            SetBorderPriority(BorderPriorities.UpBorder);
        } 
        else if (transform.position.z < downBorderZ) {
            borderProgressZ = Mathf.Abs(downBorderZ - transform.position.z) / borderBoundaryLength;
            SetBorderPriority(BorderPriorities.DownBorder);
        }

        float borderProgressX = -1;
        if (transform.position.x > rightBorderX) {
            borderProgressX = Mathf.Abs(transform.position.x - rightBorderX) / borderBoundaryLength;
            SetBorderPriority(BorderPriorities.RightBorder);
        }
        else if (transform.position.x < leftBorderX) {
            borderProgressX = Mathf.Abs(leftBorderX - transform.position.x) / borderBoundaryLength;
            SetBorderPriority(BorderPriorities.LeftBorder);
        }

        if (borderProgressX > borderProgressZ) {
            return borderProgressX;
        } else {
            return borderProgressZ;
        }

    }

    private Vector3 GetBorderTurnDirection() {
        Vector3 tableDirection = tableCentrePosition - transform.position;

        //If we entered the XBorder first..
        if (borderPriority == BorderPriorities.RightBorder || borderPriority == BorderPriorities.LeftBorder) {
            //If we're already pointing back towards the table, we can continue to the original target
            if ((borderPriority == BorderPriorities.RightBorder && transform.forward.x < DIRECTION_ACCEPTANCE_THESHOLD) ||
                (borderPriority == BorderPriorities.LeftBorder && transform.forward.x > -DIRECTION_ACCEPTANCE_THESHOLD)) {
                return tableDirection;
            } else {
                if (transform.forward.z > 0) {
                    return new Vector3(0, 0, 1);
                } else {
                    return new Vector3(0, 0, -1);
                }
            }
        } else {
            //If we're already pointing back towards the table, we can continue to the original target
            if ((borderPriority == BorderPriorities.UpBorder && transform.forward.z < DIRECTION_ACCEPTANCE_THESHOLD) ||
                (borderPriority == BorderPriorities.DownBorder && transform.forward.z > -DIRECTION_ACCEPTANCE_THESHOLD)) {
                return tableDirection;
            } else {
                if (transform.forward.x > 0) {
                    return new Vector3(1, 0, 0);
                } else {
                    return new Vector3(-1, 0, 0);
                }
            }
        }
    }

    private void SetBorderPriority(BorderPriorities p) {
        if (p == BorderPriorities.None || borderPriority == BorderPriorities.None) {
            borderPriority = p;
        } 
    }
}
