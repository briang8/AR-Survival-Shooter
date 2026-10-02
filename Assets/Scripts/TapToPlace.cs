using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

// Handles placing the game world once when the player taps a detected plane
public class TapToPlace : MonoBehaviour
{
    [SerializeField] private GameObject gameWorldPrefab;
    [SerializeField] private UnityEngine.XR.ARFoundation.ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;
    

    private bool hasPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // hides every detected plane's mesh and stops looking for new ones
    private void HidePlanes()
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }

        planeManager.enabled = false;
    }

    void Update()
    {
        // stop checking taps once we've already placed the world
        if (hasPlaced)
            return;
        
        // taps only place the world while the game is in the placement state
        if (GameManager.Instance == null || !GameManager.Instance.IsPlacing())
            return;

        if (raycastManager == null || gameWorldPrefab == null)
            return;

        Vector2 tapPosition;

        // finger tap on the phone
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        // mouse click, for testing in the editor
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapPosition = Mouse.current.position.ReadValue();
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
            GameManager.Instance.OnWorldPlaced(hitPose.position);
            HidePlanes();
        }
    }
}