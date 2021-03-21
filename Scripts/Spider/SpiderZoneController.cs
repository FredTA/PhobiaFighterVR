using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderZoneController : MonoBehaviour 
{ 
    private const float OPACITY_MULTIPIPLIER = 2f;
    private const float SCALE_MULTIPLIER = 1.1f;

    private Material material;
    private Color baseColour;
    private Color highlightColour;
    private int numberOfSpidersInZone;

    private Vector3 baseScale;
    private Vector3 highlightScale;

    private List<GameObject> spidersInZone = new List<GameObject>();

    // Start is called before the first frame update
    void Start() {
        material = GetComponent<Renderer>().material;
        baseColour = material.color;
        highlightColour = new Color(baseColour.r, baseColour.g, baseColour.g, (baseColour.a * OPACITY_MULTIPIPLIER));

        baseScale = transform.localScale;
        highlightScale = baseScale * SCALE_MULTIPLIER;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "SpiderCluster") {
            numberOfSpidersInZone++;
            material.color = highlightColour;
            transform.localScale = highlightScale;
            spidersInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.tag == "SpiderCluster") {
            //In case there is still a second spider in the box
            //Only want to de-highlight if there are no spiders
            RemoveSpiderFromZone(other.gameObject);
        }
    }

    void RemoveSpiderFromZone(GameObject spider) {
        spidersInZone.Remove(spider);
        numberOfSpidersInZone--;

        if (numberOfSpidersInZone == 0) {
            material.color = baseColour;
            transform.localScale = baseScale;
        }
    }

    // TODO shouldn't really modify the list as we iterate through it 
    void Update() {
        if (spidersInZone.Count > 0) {
            if (!spidersInZone[0].activeSelf) {
                RemoveSpiderFromZone(spidersInZone[0]);

                if (spidersInZone.Count > 0) {
                    if (!spidersInZone[0].activeSelf) {
                        RemoveSpiderFromZone(spidersInZone[0]);
                    }
                }
            }
        }
    }
}
