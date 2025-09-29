using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public GameObject boidPrefab; // Le préfabriqué de votre boid
    public int numberOfBoids = 50;
    private Boid[] boids;

    void Start()
    {
        boids = new Boid[numberOfBoids];
        for (int i = 0; i < numberOfBoids; i++)
        {
            Vector3 randomPosition = new Vector3(Random.Range(-10f, 10f), Random.Range(-5f, 5f), 0);
            GameObject boidGO = Instantiate(boidPrefab, randomPosition, Quaternion.identity);
            boids[i] = boidGO.GetComponent<Boid>();
        }
    }

    void Update()
    {
        foreach (Boid boid in boids)
        {
            boid.UpdateBoid(boids);
        }
    }
}
