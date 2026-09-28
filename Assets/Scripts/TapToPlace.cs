using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

// Handles placing the game world once when the player taps a detected plane
public class TapToPlace : MonoBehaviour
{
    [SerializeField] private GameObject gameWorldPrefab;
    [SerializeField] private ARRaycastManager raycastManager;

    private bool hasPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        // stop checking taps once we've already placed the world
        if (hasPlaced)
            return;

        Vector2 tapPosition;

        // finger tap on the phone
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapPosition = Input.GetTouch(0).position;
        }
        // mouse click, for testing in the editor
        else if (Input.GetMouseButtonDown(0))
        {
            tapPosition = Input.mousePosition;
        }
        else
        {
            return;
        }

        if (raycastManager.Raycast(tapPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Instantiate(gameWorldPrefab, hitPose.position, hitPose.rotation);
            hasPlaced = true;
        }
    }
}