using UnityEngine;

// anything that can take damage (enemies and the player) uses this
public interface IDamageable
{
    void TakeDamage(int amount);
}
