using UnityEngine;

public class OrbitalMovement : MonoBehaviour
{
    public Transform targetNode;
    public float rotationSpeed = 100f;
    public float launchSpeed = 10f;

    public AudioSource jumpAudio;

    [Header("Rhythm Settings")]
    public float bpm = 75f;
    public float pulseSize = 1.2f;
    public Transform nodeTransform;

    private float nextBeatTime;
    private Vector3 originalScale;

    [Header("Rewards")]
    [ColorUsage(true, true)]
    public Color perfectColor = Color.yellow;
    public float speedBoostMultiplier = 1.5f; // Slightly reduced so they don't fly off screen

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("Catch Physics")]
    public float gravityStrength = 4f;      // Controls how hard we pull to the center (Lower = softer)
    public float velocityBlendSpeed = 2f;   // Controls how fast we align to the orbit (Lower = wider curve)
    public float targetRadius = 3f;

    private Vector3 velocity;
    private float orbitDirection = 1f;

    public enum MovementState { Orbiting, Launching, Catching }
    public MovementState currentState = MovementState.Orbiting;

    public bool IsOrbiting => currentState == MovementState.Orbiting || currentState == MovementState.Catching;
    public float detectionRadius = 3f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        originalScale = nodeTransform.localScale;
        nextBeatTime = (float)AudioSettings.dspTime + (60f / bpm);
    }

    void Update()
    {
        HandleInput();

        switch (currentState)
        {
            case MovementState.Orbiting:
                Orbit();
                break;
            case MovementState.Launching:
                Launch();
                CheckForNearbyNodes();
                break;
            case MovementState.Catching:
                HandleCatching();
                break;
        }

        HandlePulse();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && IsOrbiting)
        {
            currentState = MovementState.Launching;
            jumpAudio.Play();

            Vector3 directionFromNode = (transform.position - targetNode.position).normalized;

            // Tangent Launch Logic
            Vector3 tangentDir = Vector3.Cross(Vector3.forward, directionFromNode) * orbitDirection;

            float timeToNextBeat = Mathf.Abs((float)AudioSettings.dspTime - nextBeatTime);
            float timeSinceLastBeat = Mathf.Abs((float)AudioSettings.dspTime - (nextBeatTime - (60f / bpm)));
            float closestBeatOffset = Mathf.Min(timeToNextBeat, timeSinceLastBeat);

            float finalSpeed = (closestBeatOffset < 0.3f) ? (launchSpeed * speedBoostMultiplier) : launchSpeed;

            if (closestBeatOffset < 0.3f) spriteRenderer.color = perfectColor;

            velocity = tangentDir * finalSpeed;
            transform.right = velocity.normalized;
        }
    }

    void Orbit()
    {
        transform.RotateAround(targetNode.position, Vector3.forward, rotationSpeed * orbitDirection * Time.deltaTime);
    }

    void Launch()
    {
        transform.position += velocity * Time.deltaTime;
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, originalColor, Time.deltaTime * 2f);
    }

    void CheckForNearbyNodes()
    {
        Collider2D[] nearbyNodes = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (Collider2D col in nearbyNodes)
        {
            if (col.CompareTag("Node") && col.transform != targetNode)
            {
                targetNode = col.transform;

                // 1. Get the direction from the node to us
                Vector3 directionFromNode = (transform.position - targetNode.position).normalized;

                // 2. Calculate the two potential tangents
                Vector3 tangentCCW = Vector3.Cross(Vector3.forward, directionFromNode); // Anti-clockwise
                Vector3 tangentCW = -tangentCCW; // Clockwise

                // 3. Use Dot Product to see which tangent aligns with our current velocity
                float dotCCW = Vector3.Dot(velocity.normalized, tangentCCW);

                // If dot product is higher for CCW, we go CCW (1), else CW (-1)
                orbitDirection = (dotCCW > 0) ? 1f : -1f;

                currentState = MovementState.Catching;
                break;
            }
        }
    }

    void HandleCatching()
    {
        Vector3 directionToNode = (targetNode.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetNode.position);

        // 1. Calculate Target State
        float targetLinearSpeed = (rotationSpeed * Mathf.PI * targetRadius) / 180f;
        Vector3 targetTangent = Vector3.Cross(Vector3.forward, directionToNode) * orbitDirection;
        Vector3 perfectOrbitalVelocity = targetTangent * targetLinearSpeed;

        // 2. Dynamic Blend Speed: The further away, the "floatier" it is
        float distanceError = distance - targetRadius;
        float dynamicBlend = Mathf.Lerp(velocityBlendSpeed * 2f, velocityBlendSpeed, Mathf.Abs(distanceError) / 3f);

        // 3. Blend Velocity
        velocity = Vector3.Lerp(velocity, perfectOrbitalVelocity, Time.deltaTime * dynamicBlend);

        // 4. Smooth Gravity: Pull harder the more "off" the distance is
        // This creates a nice springy feel
        Vector3 gravityCorrection = directionToNode * (distanceError * gravityStrength);
        velocity += gravityCorrection * Time.deltaTime;

        // 5. Apply Movement
        transform.position += velocity * Time.deltaTime;

        // 6. Visual Smoothness: Slow the rotation of the sprite so it doesn't "snap" its nose
        Vector3 moveDir = velocity.normalized;
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(moveDir, Vector3.forward));
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        // 7. Lock in
        if (Mathf.Abs(distanceError) < 0.05f)
        {
            currentState = MovementState.Orbiting;
            transform.position = targetNode.position - directionToNode * targetRadius;
        }
    }

    void HandlePulse()
    {
        if (AudioSettings.dspTime >= nextBeatTime)
        {
            nextBeatTime += (60f / bpm);
            nodeTransform.localScale = originalScale * pulseSize;
        }
        nodeTransform.localScale = Vector3.Lerp(nodeTransform.localScale, originalScale, Time.deltaTime * 5f);
    }
}