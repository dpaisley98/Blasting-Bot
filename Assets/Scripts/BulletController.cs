using System;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float lifetime = 3f;
    public int maxCollisions;
    public bool explodeOnTouch = true;
    public float bounciness;
    public bool useGravity;
    public Rigidbody rigidBody;
    public GameObject explosion;
    public LayerMask enemy;
    public int explosionDamage;
    public float explosionRange, explosionForce;

    int collisions;
    PhysicMaterial physicMaterial;

    void Start()
    {
        Setup();
    }

    void Setup()
    {
        physicMaterial = new PhysicMaterial();
        physicMaterial.bounciness = bounciness;
        physicMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        physicMaterial.bounceCombine = PhysicMaterialCombine.Maximum;

        GetComponent<SphereCollider>().material = physicMaterial;
        rigidBody.useGravity = useGravity;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (collisions > maxCollisions | lifetime <= 0)
            Explode();
    }

    private void Explode()
    {
        if (explosion != null)
            Instantiate(explosion, transform.position, Quaternion.identity);
        
        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, enemy);
        foreach (Collider enemy in enemies) 
        {
            enemy.GetComponentInChildren<Rigidbody>().AddExplosionForce(explosionForce, transform.position, explosionRange); 
            //GameObject.Destroy(enemy.gameObject);
        }

        Invoke("Delay", 0.05f);
    }

    void Delay() 
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisions++;
        if (collision.collider.CompareTag("Enemy") && explodeOnTouch)
            Explode();
    }
}
