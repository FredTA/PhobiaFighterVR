using UnityEngine;
using System.Collections;
using System;

//Adapted from WDallgraphic's "Wolf Spider Animated" 
//https://assetstore.unity.com/packages/3d/characters/animals/insects/wolf-spider-animated-29330
public class SpiderAnimationController : MonoBehaviour {

    public Animator spider;
    private IEnumerator coroutine;
    private float animationScaleMultiplier;

    void Awake () {
        animationScaleMultiplier = GameObject.Find("DefaultSpider").transform.lossyScale.magnitude / transform.lossyScale.magnitude;
        Debug.Log(gameObject.name + ": " + transform.lossyScale.magnitude + ": " + animationScaleMultiplier);
    }
	
    IEnumerator idle()
    {
        yield return new WaitForSeconds(0.35f);
        spider.SetBool("attack", false);
        spider.SetBool("attack2", false);
        spider.SetBool("idle", true);
        spider.SetBool("hited", false);
    }
    IEnumerator idle2()
    {
        yield return new WaitForSeconds(1.0f);
        spider.SetBool("attack", false);
        spider.SetBool("attack2", false);
        spider.SetBool("idle", true);
        spider.SetBool("turnleft", false);
        spider.SetBool("turnright", false);
    }

    public enum SpiderAnimations {
        idle,
        idle2,
        walking,
        running,
        turnleft,
        turnright,
        hited,
        died, 
        jumping,
        attack,
        attack2
    }

    public void SetAnimation(SpiderAnimations animation) {
        spider.SetBool("idle", false);
        spider.SetBool("walking", false);
        spider.SetBool("running", false);
        spider.SetBool("turnleft", false);
        spider.SetBool("turnright", false);
        spider.SetBool("attack", false);
        spider.SetBool("attack2", false);

        switch (animation) {
            case SpiderAnimations.idle:
                spider.SetBool("idle", true);
                //StartCoroutine("idle");
                break;
            case SpiderAnimations.idle2:
                //StartCoroutine("idle2");
                break;
            case SpiderAnimations.walking:
                spider.SetBool("walking", true);
                break;
            case SpiderAnimations.running:
                spider.SetBool("running", true);
                break;
            case SpiderAnimations.turnleft:
                spider.SetBool("turnleft", true);
                break;
            case SpiderAnimations.turnright:
                spider.SetBool("turnright", true);
                break;
            case SpiderAnimations.hited:
                spider.SetBool("hited", true);
                break;
            case SpiderAnimations.died:
                spider.SetBool("died", true);
                break;
            case SpiderAnimations.jumping:
                spider.SetBool("jumping", true);
                Debug.Log("Should we really be jumping?");
                break;
            case SpiderAnimations.attack:
                spider.SetBool("attack", true);
                break;
            case SpiderAnimations.attack2:
                spider.SetBool("attack2", true);
                break;
        }
        //Debug.Log("Setting anim " + GetCurrentAnimation().ToString());
    }

    public SpiderAnimations GetCurrentAnimation() {
        foreach (SpiderAnimations animation in Enum.GetValues(typeof(SpiderAnimations))) {
            if (spider.GetBool(Enum.GetName(typeof(SpiderAnimations), (int)animation))) {
                return animation;
            }
        }
        return SpiderAnimations.idle;
    }

    public void SetSpeed(float speed) {
        spider.speed = speed * animationScaleMultiplier;
    }

    public float GetSpeed() {
        return spider.speed;
    }

}
