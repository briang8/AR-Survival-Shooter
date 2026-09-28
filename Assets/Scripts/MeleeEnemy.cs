using UnityEngine;

// Melee enemy: walks straight at the player and stops when it is close
public class MeleeEnemy : Enemy
{
    [SerializeField] private float attackRange = 0.7f;

    protected override void Act()
    {
        FacePlayer();

        if (DistanceToPlayer() > attackRange)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }
}