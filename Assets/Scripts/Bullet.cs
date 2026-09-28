using UnityEngine;

// A single bullet that flies forward, damages what it hits, and goes back to the pool
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 1;
    [SerializeField] private bool hitsPlayer = false;

    private BulletPool pool;
    private float timer;

    public void SetPool(BulletPool newPool)
    {
        pool = newPool;
    }

    // runs every time the bullet is switched on, so the timer starts fresh
    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            pool.ReturnBullet(this);
        }
    }

    // damages the right kind of target, then goes back to the pool
    void OnTriggerEnter(Collider other)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null)
            return;

        // player bullets only hurt enemies, enemy bullets only hurt the player
        bool targetIsPlayer = target is PlayerHealth;
        if (targetIsPlayer != hitsPlayer)
            return;

        target.TakeDamage(damage);
        pool.ReturnBullet(this);
    }
}