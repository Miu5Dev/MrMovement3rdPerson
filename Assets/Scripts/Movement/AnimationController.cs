using System;
using UnityEngine;


[RequireComponent(typeof(PlayerController))]
public class AnimationController : MonoBehaviour    //REMPLACE WITH STATE SYSTEN AFTER
{
    private PlayerController controller;

    [Header("Configurable Settigs")] 
    public Animator animator;
    
    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public void Update()
    {
        if (controller.speedDebug > 0)
        {
            animator.SetBool("moving", true);
        }
        else
        {
            animator.SetBool("moving", false);
        }

        if (controller.GroundedDebug)
        {
            animator.SetBool("InAir", false);
        }
        else
        {
            animator.SetBool("InAir", true);
        }
        
        
    }
}
