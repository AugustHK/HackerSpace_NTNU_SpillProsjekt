﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class UnigolemController : MonoBehaviour
{
    [SerializeField]
    private Transform model;

    [SerializeField]
    private float tiltAngleSide = 30f;

    [SerializeField]
    private float tiltAngleFront = 30f;

    [SerializeField]
    private Animator anim;

    [SerializeField]
    private float damage;

    [SerializeField]
    private float detonationDistance = 2f;

    private Transform baseTransform;
    private NavMeshAgent agent;
    private HealthLogic healthLogic;

    private Vector3 lastForward;

    private static readonly int speedAnimatorParam = Animator.StringToHash("Speed");

    void Awake()
    {
        baseTransform = BaseController.Singleton.transform;
        agent = GetComponent<NavMeshAgent>();
        healthLogic = GetComponent<HealthLogic>();
        healthLogic.onDeath += Die;
    }

    void Start()
    {
        lastForward = transform.forward;
        agent.SetDestination(baseTransform.position);
    }

    void OnDestroy()
    {
        GetComponent<HealthLogic>().onDeath -= Die;
    }

    private void Die(DamageInfo damageInfo)
    {
        Destroy(gameObject);
    }

    void Update()
    {
        float angularSpeed = Vector3.Cross(lastForward, transform.forward).y / Time.deltaTime;
        lastForward = transform.forward;

        float sidewaysAngle = -tiltAngleSide * angularSpeed;
        float speed = agent.velocity.magnitude / agent.speed;
        anim.SetFloat(speedAnimatorParam, speed);
        float forwardAngle = tiltAngleFront * speed;

        model.localRotation = Quaternion.Euler(forwardAngle, 0, sidewaysAngle);

        if (Vector3.Distance(transform.position, baseTransform.position) < detonationDistance)
            Detonate();
    }

    private void Detonate()
    {
        baseTransform.GetComponent<HealthLogic>()?.DealDamage(damage);
        Destroy(gameObject);
    }
}