using System;
using System.Collections.Generic;
using UnityEngine;

// Main MonoBehaviour class to manage the simulation in Unity
public class FishSystemController : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("The prefab for the fish (e.g., a sphere or capsule).")]
    public GameObject fishPrefab;
    [Tooltip("The number of fish to spawn in the system.")]
    public int fishCount = 100;
    [Tooltip("The dimensions of the cube-shaped area where fish will spawn and be contained.")]
    public Vector3 bounds = new Vector3(50, 50, 50);

    [Header("Boids Behavior")]
    [Tooltip("The target the fish will be attracted to.")]
    public Transform target;
    private float RunningTime;
    [Space(10)]
    public float perceptionRadius = 10.0f;
    public float maxSpeed = 8.0f;
    public float maxForce = 0.5f;
    [Space(10)]
    [Range(0, 10)] public float separationWeight = 5.5f;
    [Range(0, 10)] public float alignmentWeight = 0.5f;
    [Range(0, 10)] public float cohesionWeight = 0.0f; // Kept off as in the original script
    [Range(0, 10)] public float goalWeight = 5.5f;

    private FishSystem fishSystem;
    private List<GameObject> fishGameObjects = new List<GameObject>();

    void Start()
    {
        // Initialize the fish system logic
        fishSystem = new FishSystem(transform.position, fishCount, bounds, this);

        // Create a GameObject for each fish to visualize it
        for (int i = 0; i < fishCount; i++)
        {
            Fish fishData = fishSystem.fishes[i];
            GameObject fishGO = Instantiate(fishPrefab, fishData.position, Quaternion.identity, transform);
            fishGameObjects.Add(fishGO);
        }
    }

    void Update()
    {
        // Ensure the target is set before updating
        if (target == null)
        {
            Debug.LogWarning("Boids target is not set!");
            return;
        }
        RunningTime += Time.deltaTime;

        float A = 50.0f;
        float B = 50.0f;
        float T = RunningTime;


        target.position = new Vector3(A * Mathf.Sin(T), B * Mathf.Sin(T) * Mathf.Cos(T), (A / B) * Mathf.Cos(T) * Mathf.Sin(T) * Mathf.Tan(T));

        // Update the underlying simulation logic
        fishSystem.UpdateSystem(target.position, Time.deltaTime);

        // Update the position and rotation of each fish GameObject
        for (int i = 0; i < fishCount; i++)
        {
            fishGameObjects[i].transform.position = fishSystem.fishes[i].position;
            // Optional: Make the fish look where they are going
            if (fishSystem.fishes[i].velocity != Vector3.zero)
            {
                fishGameObjects[i].transform.rotation = Quaternion.LookRotation(fishSystem.fishes[i].velocity);
            }
        }
    }

    // Draw a gizmo in the editor to visualize the bounds
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(transform.position, bounds);
    }
}


// Represents a single fish with its properties
public class Fish
{
    public Vector3 position;
    public Vector3 velocity;
    private readonly Vector3 bounds;
    private const float FISH_RADIUS = 0.5f; // Visual radius for bounds checks

    public Fish(Vector3 pos, Vector3 vel, Vector3 simulationBounds)
    {
        position = pos;
        velocity = vel;
        bounds = simulationBounds;
    }

    public void UpdatePosition(float dt)
    {
        position += velocity * dt;

        // Keep fish within the specified bounds by reflecting their velocity
        if (Mathf.Abs(position.x) > bounds.x / 2 - FISH_RADIUS)
        {
            position.x = (bounds.x / 2 - FISH_RADIUS) * Mathf.Sign(position.x);
            velocity.x *= -1;
        }
        if (Mathf.Abs(position.y) > bounds.y / 2 - FISH_RADIUS)
        {
            position.y = (bounds.y / 2 - FISH_RADIUS) * Mathf.Sign(position.y);
            velocity.y *= -1;
        }
        if (Mathf.Abs(position.z) > bounds.z / 2 - FISH_RADIUS)
        {
            position.z = (bounds.z / 2 - FISH_RADIUS) * Mathf.Sign(position.z);
            velocity.z *= -1;
        }
    }
}


// Manages the entire collection of fish and their interactions
public class FishSystem
{
    public List<Fish> fishes = new List<Fish>();
    private readonly int count;
    private readonly Vector3 bounds;
    private readonly FishSystemController controller; // Reference to MonoBehaviour for parameters

    public FishSystem(Vector3 center, int fishCount, Vector3 simulationBounds, FishSystemController controller)
    {
        this.count = fishCount;
        this.bounds = simulationBounds;
        this.controller = controller;

        // Spawn fish within a UnityEngine.Random sphere inside the bounds
        for (int i = 0; i < this.count; i++)
        {
            Vector3 pos = center + UnityEngine.Random.insideUnitSphere * 20;
            Vector3 vel = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(3.0f, 8.0f);
            fishes.Add(new Fish(pos, vel, this.bounds));
        }
    }

    public void UpdateSystem(Vector3 targetPosition, float dt)
    {
        // Boids Logic (Separation, Alignment, Cohesion, Goal)
        UpdateBoidsBehavior(targetPosition, dt);

        // Hard Collision Handling
        HandleCollisions(dt);

        // Update final positions for all fish
        foreach (var fish in fishes)
        {
            fish.UpdatePosition(dt);
        }
    }

    private void UpdateBoidsBehavior(Vector3 targetPosition, float dt)
    {
        foreach (var fish in fishes)
        {
            Vector3 steerSeparation = Vector3.zero;
            Vector3 steerAlignment = Vector3.zero;
            Vector3 steerCohesion = Vector3.zero; // Not used in original script
            int total = 0;

            // Find neighbors
            foreach (var other in fishes)
            {
                if (fish == other) continue;

                float dist = Vector3.Distance(fish.position, other.position);
                if (dist > 0 && dist < controller.perceptionRadius)
                {
                    // Separation: Steer away from neighbors
                    Vector3 diff = fish.position - other.position;
                    steerSeparation += diff.normalized / dist; // Weight by distance

                    // Alignment: Steer towards the average velocity of neighbors
                    steerAlignment += other.velocity;
                    
                    total++;
                }
            }
            
            if (total > 0)
            {
                steerSeparation /= total;
                steerAlignment /= total;

                // Normalize and apply max speed/force
                steerAlignment = steerAlignment.normalized * controller.maxSpeed;
                steerAlignment = Vector3.ClampMagnitude(steerAlignment - fish.velocity, controller.maxForce);
            }

            // Goal Attraction: Steer towards the target
            Vector3 steerGoal = targetPosition - fish.position;
            steerGoal = steerGoal.normalized * controller.maxSpeed;
            steerGoal = Vector3.ClampMagnitude(steerGoal - fish.velocity, controller.maxForce);

            // Apply weighted forces as acceleration
            Vector3 acceleration = Vector3.zero;
            acceleration += steerSeparation * controller.separationWeight;
            acceleration += steerAlignment * controller.alignmentWeight;
            acceleration += steerGoal * controller.goalWeight;
            
            // Add wander noise
            acceleration += UnityEngine.Random.insideUnitSphere * 0.1f;

            // Apply acceleration to velocity
            fish.velocity += acceleration * dt;

            // Apply damping and cap speed
            fish.velocity *= 0.98f;
            fish.velocity = Vector3.ClampMagnitude(fish.velocity, controller.maxSpeed);
        }
    }
    
    private void HandleCollisions(float dt)
    {
        Vector3[] forces = new Vector3[count];
        const float MIN_DIST = 5.25f; // Diameter for collision
        const float REPULSION_STRENGTH = 80.0f;

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                Fish fish1 = fishes[i];
                Fish fish2 = fishes[j];

                Vector3 diff = fish1.position - fish2.position;
                float distance = diff.magnitude;

                if (distance > 0 && distance < MIN_DIST)
                {
                    float overlap = MIN_DIST - distance;
                    Vector3 repulsion = diff.normalized * overlap * REPULSION_STRENGTH;
                    
                    forces[i] += repulsion;
                    forces[j] -= repulsion;
                }
            }
        }

        // Apply forces to velocities
        for (int i = 0; i < count; i++)
        {
            fishes[i].velocity += forces[i] * dt;
        }
    }
}