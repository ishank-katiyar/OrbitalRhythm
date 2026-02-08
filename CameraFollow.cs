using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public OrbitalMovement playerScript;

    [Header("Motion Settings")]
    public float smoothTime = 0.3f;
    public float maxSpeed = 20f;

    [Header("Zoom Settings")]
    public float defaultZoom = 5f;
    public float launchZoom = 7f;
    public float zoomSmoothTime = 0.5f;

    private Vector3 velocity = Vector3.zero;
    private float zoomVelocity = 0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = defaultZoom;

        // ---------------------------------------------------------
        // AUTO-DETECTION LOGIC
        // ---------------------------------------------------------

        // 1. If 'player' Transform is missing, try to find it by Tag
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("CameraFollow: No object with tag 'Player' found! Please tag your player object.");
            }
        }

        // 2. If we have the player transform, try to get the script automatically
        if (player != null && playerScript == null)
        {
            playerScript = player.GetComponent<OrbitalMovement>();

            // If still null, maybe the script is on a child object?
            if (playerScript == null)
            {
                playerScript = player.GetComponentInChildren<OrbitalMovement>();
            }
        }

        // 3. Final Check
        if (playerScript == null)
        {
            Debug.LogError("CameraFollow: Could not find 'OrbitalMovement' script on the Player object!");
        }
    }

    void LateUpdate()
    {
        // Safety Check: If we still don't have the script, do nothing (prevents crashes)
        if (player == null || playerScript == null) return;

        Vector3 targetPos;
        float targetZoom;

        // Explicitly check the state from the script we found
        bool isActuallyOrbiting = (playerScript.currentState == OrbitalMovement.MovementState.Orbiting ||
                                   playerScript.currentState == OrbitalMovement.MovementState.Catching);

        if (isActuallyOrbiting && playerScript.targetNode != null)
        {
            // Focus on the Node
            Transform node = playerScript.targetNode;
            targetPos = new Vector3(node.position.x, node.position.y, -10f);

            // Dynamic Zoom
            float dist = Vector3.Distance(player.position, node.position);
            targetZoom = Mathf.Max(defaultZoom, dist + 2f);
        }
        else
        {
            // Focus on the Player (Launch State)
            targetPos = new Vector3(player.position.x, player.position.y, -10f);
            targetZoom = launchZoom;
        }

        // Smooth Movement
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime, maxSpeed);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
    }
}