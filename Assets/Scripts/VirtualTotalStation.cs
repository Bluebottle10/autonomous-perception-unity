//============================================================================================
//
// Purpose: This class is responsible for setting the angles for the any virtual total station
//
//============================================================================================

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
public class VirtualTotalStation : MonoBehaviour
{
    [Tooltip("Used for testing the Heading angle for the Body.")]
    public float targetHeading;
    
    [Tooltip("Used for testing the Pitch angle for the Lens.")]
    public float targetPitch;

    [Tooltip("Health value between 0 and 100.")]
    public HingeJoint body;

    [Tooltip("Health value between 0 and 100.")]
    public HingeJoint lens;

    public bool debug;

    private JointSpring domeSpring;
    private JointSpring lensSpring;
    private HingeJoint domeHinge;
    private HingeJoint lensHinge;

    private float dSpringValue;
    private float dDamperValue;
    private float lSpringValue;
    private float lDamperValue;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        domeHinge = body.GetComponent<HingeJoint>();
        domeSpring = domeHinge.spring;
        lensHinge = lens.GetComponent<HingeJoint>();
        lensSpring = lensHinge.spring;

        if (debug && domeHinge != null && lensHinge != null)
            Debug.Log("TOTAL STATION INITIALIZED");
    }

    void Start () 
    {
		
	}
	

	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetTargetRotation(targetPitch, targetHeading);
        }
		
	}

    public void Reset()
    {
        dSpringValue = domeHinge.spring.spring;
        dDamperValue = domeHinge.spring.damper;
        lSpringValue = lensHinge.spring.spring;
        lDamperValue = lensHinge.spring.damper;

        domeSpring.spring = 2000;
        domeSpring.damper = 200;
        domeHinge.spring = domeSpring;

        lensSpring.spring = 2000;
        lensSpring.damper = 200;
        lensHinge.spring = lensSpring;
    }

    public void SetTargetRotation(float targetPitch, float targetHeading)
    {
        domeSpring = domeHinge.spring;
        domeSpring.targetPosition = Utils.NormalizeDegrees(targetHeading);
        domeHinge.spring = domeSpring;
        lensSpring = lensHinge.spring;
        lensSpring.targetPosition = Utils.NormalizeDegrees(targetPitch, -90);
        lensHinge.spring = lensSpring;
    }
}
