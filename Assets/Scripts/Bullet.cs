using UnityEngine;

// A single bullet that flies forward and goes back to the pool when its time is up
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;

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
}