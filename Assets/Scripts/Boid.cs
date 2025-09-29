using UnityEngine;

public class Boid : MonoBehaviour
{
    public Vector3 position;
    public Vector3 velocity;

    public float maxSpeed = 5f;
    public float perceptionRadius = 2.5f;

    [SerializeField] int cohesionWeight;
    [SerializeField] int separationWeight;
    [SerializeField] int alignmentWeight;

    void Start()
    {
        position = transform.position;
        velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
    }

    public void UpdateBoid(Boid[] allBoids)
    {
        Boid[] neighbors = GetNeighbors(allBoids);

        // Calculer chaque force
        Vector3 cohesionForce = CalculateCohesion(neighbors) * cohesionWeight; // Ajouter les poids
        Vector3 separationForce = CalculateSeparation(neighbors) * separationWeight;
        Vector3 alignmentForce = CalculateAlignment(neighbors) * alignmentWeight;

        // Calculer l'accélération totale
        Vector3 acceleration = cohesionForce + separationForce + alignmentForce;

        // Mettre à jour la vitesse et la position
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed); // Limiter la vitesse
        position += velocity * Time.deltaTime;

        // Mise à jour de la partie visuelle (GameObject)
        transform.position = position;
        if (velocity != Vector3.zero)
        {
            transform.forward = velocity.normalized;
        }
    }


    private Boid[] GetNeighbors(Boid[] allBoids)
    {
        System.Collections.Generic.List<Boid> neighbors = new System.Collections.Generic.List<Boid>();
        foreach (Boid otherBoid in allBoids)
        {
            if (otherBoid == this) continue;

            float distance = Vector3.Distance(this.position, otherBoid.position);
            if (distance <= perceptionRadius)
            {
                neighbors.Add(otherBoid);
            }
        }
        return neighbors.ToArray();
    }

    // Règle 1: Cohésion
    private Vector3 CalculateCohesion(Boid[] neighbors)
    {
        if (neighbors.Length == 0) return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (Boid neighbor in neighbors)
        {
            centerOfMass += neighbor.position;
        }
        centerOfMass /= neighbors.Length;

        return (centerOfMass - this.position);
    }

    // Règle 2: Séparation
    private Vector3 CalculateSeparation(Boid[] neighbors)
    {
        if (neighbors.Length == 0) return Vector3.zero;

        Vector3 separationVector = Vector3.zero;
        foreach (Boid neighbor in neighbors)
        {
            Vector3 direction = this.position - neighbor.position;
            separationVector += direction.normalized / direction.magnitude;
        }
        separationVector /= neighbors.Length;

        return separationVector;
    }

    // Règle 3: Alignement
    private Vector3 CalculateAlignment(Boid[] neighbors)
    {
        if (neighbors.Length == 0) return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;
        foreach (Boid neighbor in neighbors)
        {
            averageVelocity += neighbor.velocity;
        }
        averageVelocity /= neighbors.Length;

        return averageVelocity;
    }
}
