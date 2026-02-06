using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization; // Required for NavMeshAgent

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class HumanoidMover : MonoBehaviour
{
    [Tooltip("The speed at which the character walks.")]
    [SerializeField] public float MoveSpeed = 3.5f;
    
    public List<GameObject> Targets = new List<GameObject>();
    public bool IsRandom;

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    int currentTarget;

    // A hash for the Animator parameter to improve performance.
    private readonly int speedParamHash = Animator.StringToHash("ForwardSpeed");

    void Awake()
    {
        // Get references to the components attached to this GameObject.
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (Targets.Count == 0)
            return;
        
        // Set the NavMeshAgent's speed to our defined move speed.
        navMeshAgent.speed = MoveSpeed;
        if (IsRandom)
            currentTarget = Random.Range(0, Targets.Count);
        else
            currentTarget = 0;
        
    }

    void Update()
    {
        if (Targets.Count == 0)
            return;
        
        // This example uses a mouse click to set the target.
        // You can easily change this to use any target position.
        // if (Input.GetMouseButtonDown(0))
        // {
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     if (Physics.Raycast(ray, out RaycastHit hit))
        //     {
        //         // If the raycast hits a valid point on the NavMesh, move the agent.
        //         MoveTo(hit.point);
        //     }
        // }

        if (Vector3.Distance(transform.position, Targets[currentTarget].transform.position) < 0.4f)
        {
            if (IsRandom)
            {
                int candidate = Random.Range(0, Targets.Count);
                if (candidate == currentTarget && currentTarget == 0)
                    currentTarget++;
                else if (candidate == Targets.Count - 1 && currentTarget == Targets.Count - 1)
                    currentTarget = 0;
                else
                    currentTarget = candidate;
            }
            else
            {
                currentTarget++;
                if  (currentTarget > Targets.Count - 1)
                    currentTarget = 0;
            }
        }
        
        MoveTo(Targets[currentTarget].transform.position);

        // Update the Animator with the current speed of the NavMeshAgent.
        UpdateAnimator();
    }

    /// <summary>
    /// Sets a new destination for the character to move to.
    /// </summary>
    /// <param name="destination">The target world-space location.</param>
    public void MoveTo(Vector3 destination)
    {
        navMeshAgent.SetDestination(destination);
    }

    private void UpdateAnimator()
    {
        // Get the agent's velocity relative to its own coordinate system.
        Vector3 localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);

        // The 'z' component of the local velocity represents forward movement.
        float forwardSpeed = localVelocity.z;

        // Pass this speed to the Animator.
        // The Animator will use this value to blend between Idle and Walk/Run animations.
        animator.SetFloat(speedParamHash, forwardSpeed);
    }
}