using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

// Handles placing the game world once when the player taps a detected plane
public class TapToPlace : MonoBehaviour
{
    [SerializeField] private GameObject gameWorldPrefab;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private EnemySpawner enemySpawner;

    private bool hasPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        // stop checking taps once we've already placed the world
        if (hasPlaced)
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
            enemySpawner.BeginSpawning(hitPose.position);
        }
    }
}