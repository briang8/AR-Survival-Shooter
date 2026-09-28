using UnityEngine;
using System.Collections.Generic;

// Object pool: makes all the bullets once at the start and reuses them
public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int poolSize = 20;

    private Queue<Bullet> availableBullets = new Queue<Bullet>();

    void Awake()
    {
        // make every bullet now and keep them switched off
        for (int i = 0; i < poolSize; i++)
        {
            Bullet bullet = Instantiate(bulletPrefab, transform);
            bullet.SetPool(this);
            bullet.gameObject.SetActive(false);
            availableBullets.Enqueue(bullet);
        }
    }

    // takes a bullet out of the pool and switches it on at the given spot
    public Bullet GetBullet(Vector3 position, Quaternion rotation)
    {
        // no bullets left, so nothing is fired
        if (availableBullets.Count == 0)
            return null;

        Bullet bullet = availableBullets.Dequeue();
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    // switches the bullet off and puts it back in the pool
    public void ReturnBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        availableBullets.Enqueue(bullet);
    }
}