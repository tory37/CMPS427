﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DeathAndDecay : Ability
{
    public DeathAndDecay(AttackType attackType, DamageType damageType, float range, float angle, float cooldown, float damageMod, float resourceCost, string id, string readable, GameObject particles)
        : base(attackType, damageType, range, angle, cooldown, damageMod, resourceCost, id, readable, particles)
    {

    }

    public override void AttackHandler(GameObject source, Vector3 AoEPoint, Entity attacker, bool isPlayer)
    {
        // spawn in the parent for this aoe attack
        GameObject parent = new GameObject("ShadowAoeParent");
        parent.transform.position = AoEPoint;

        // do attack "repetition" times with "timeDelta" waiting between each
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().RunCoroutine(DoSpawnAnimation(parent, GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().DeathAndDecaySpawn, 10.0f, isPlayer));

        parent.transform.position += new Vector3(0, 1, 0);

        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().RunCoroutine(DoAttackRepeating(parent, attacker, isPlayer, 20, 0.5f));

        GameObject.Destroy(parent, 11.0f);
    }

    public override List<GameObject> OnAttack(GameObject source, bool isPlayer)
    {
        List<GameObject> enemiesToAttack = new List<GameObject>();

        Vector3 forward = new Vector3();

        // this is a player attack, forward attack vector will be based on cursor position
        if (isPlayer == true)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit target;
            Physics.Raycast(ray, out target, Mathf.Infinity);
            Vector3 vectorToMouse = target.point - source.transform.position;
            forward = new Vector3(vectorToMouse.x, source.transform.forward.y, vectorToMouse.z).normalized;
        }

        int enemyMask = LayerMask.NameToLayer("Enemy");
        int playerMask = LayerMask.NameToLayer("Player");

        Collider[] colliders;

        if (isPlayer == true)
        {
            colliders = Physics.OverlapSphere(source.transform.position, range, 1 << enemyMask);
        }

        else
        {
            colliders = Physics.OverlapSphere(source.transform.position, range, 1 << playerMask);
        }

        Debug.Log("colliders gained: " + colliders.Length);
        foreach (Collider collider in colliders)
        {
            //Debug.Log(collider.ToString());

            // create a vector from the possible enemy to the source.transform

            Vector3 enemyVector = collider.transform.position - source.transform.position;
            Vector3 enemyVector2 = source.transform.position - collider.transform.position;

            // this is an enemy attack, forward attack vector will be based on target position
            if (isPlayer == false)
            {
                forward = enemyVector;
            }

            // if the angle between the forward vector of the attacker and the enemy vector is less than the angle of attack, the enemy is within the attack angle
            if (Vector3.Angle(forward, enemyVector) < angle)
            {

                if (isPlayer == true)
                {
                    Debug.DrawRay(collider.transform.position, enemyVector, Color.green, 0.5f);
                    Debug.DrawRay(collider.transform.position, enemyVector2, Color.red, 0.5f);
                    enemiesToAttack.Add(collider.gameObject);
                }

                else
                {
                    enemiesToAttack.Add(collider.gameObject);
                    
                }
            }
        }

        return enemiesToAttack;
    }

    public override void DoDamage(GameObject source, GameObject target, Entity attacker, Entity defender, bool isPlayer)
    {
        float damageAmt;
        if (isPlayer == true)
        {
            damageAmt = DamageCalc.DamageCalculation(attacker, defender, damageMod);
        }
        else
        {
            damageAmt = DamageCalc.DamageCalculation(attacker, defender, 0);
        }
        Debug.Log("damage: " + damageAmt);

        defender.ModifyHealth(-damageAmt);
    }

    public override void DoPhysics(GameObject source, GameObject target)
    {
        Vector3 relativeVector = (target.transform.position - source.transform.position).normalized;
        float normalizedMagnitude = 5f - Vector3.Distance(target.transform.position, source.transform.position);
        float force = (normalizedMagnitude / (Mathf.Pow(0.4f, 2)));

        target.GetComponent<MovementFSM>().AddForce(relativeVector.normalized * force * 2, 0.2f);
    }

    public IEnumerator DoAttackRepeating(GameObject source, Entity attacker, bool isPlayer, int repetitions, float waitDelta)
    {
        for (int i = 0; i < repetitions; i++)
        {
            List<GameObject> attacked = OnAttack(source, isPlayer);

            if (isPlayer == true)
            {
                Debug.Log(attacked.Count);
                foreach (GameObject enemy in attacked)
                {
                    if (enemy.GetComponent<AIController>().IsResetting() == false
                        && enemy.GetComponent<AIController>().IsDead() == false)
                    {
                        Entity defender = enemy.GetComponent<Entity>();
                        DoDamage(source, enemy, attacker, defender, isPlayer);

                        if (enemy.GetComponent<AIController>().IsInCombat() == false)
                        {
                            enemy.GetComponent<AIController>().BeenAttacked(source);
                        }
                    }


                }
            }

            else
            {
                foreach (GameObject enemy in attacked)
                {
                    Entity defender = enemy.GetComponent<Entity>();
                    DoDamage(source, enemy, attacker, defender, isPlayer);
                }
            }

            yield return new WaitForSeconds(waitDelta);
        }

        yield return null;
    }

    public override IEnumerator DoAnimation(GameObject source, GameObject particlePrefab, float time, bool isPlayer, GameObject target)
    {
        GameObject particles;

        particles = (GameObject)GameObject.Instantiate(particlePrefab, target.transform.position, target.transform.rotation);

        yield return new WaitForSeconds(time);

        ParticleSystem[] particleSystems = particles.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem item in particleSystems)
        {
            item.transform.parent = null;
            item.emissionRate = 0;
            item.enableEmission = false;

        }

        GameObject.Destroy(particles);

        yield return null;
    }

    public IEnumerator DoSpawnAnimation(GameObject source, GameObject particlePrefab, float time, bool isPlayer, GameObject target = null)
    {
        GameObject particles;

        particles = (GameObject)GameObject.Instantiate(particlePrefab, source.transform.position, source.transform.rotation);

        yield return new WaitForSeconds(time);

        ParticleSystem[] particleSystems = particles.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem item in particleSystems)
        {
            item.transform.parent = null;
            item.emissionRate = 0;
            item.enableEmission = false;

        }

        GameObject.Destroy(particles);

        yield return null;
    }

}