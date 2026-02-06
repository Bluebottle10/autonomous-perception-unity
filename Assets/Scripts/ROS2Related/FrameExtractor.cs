using System;
using UnityEngine;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;


public class FrameExtractor : MonoBehaviour 
{

	// Use this for initialization
    public string frameName = "sy_frame";
    
    public TransformStampedMsg Frame;

    private TimeSpan _startTime;

    void Awake ()
	{
        Initialize();
    }

    void Start()
    {
        _startTime = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
    }

    // Update is called once per frame
    void FixedUpdate ()
    {
        //Frame.header.seq++;
        Frame.header = new HeaderMsg();
        Frame.header.stamp = new TimeMsg();
        
        // Frame.header.stamp = ROS.GetTime<Messages.std_msgs.Time>(_startTime + TimeSpan.FromSeconds(UnityEngine.Time.fixedTime));
        Frame.header.stamp.sec = (int)(_startTime.TotalSeconds + Time.fixedDeltaTime);

        //Convert position from Unity Coordinate system to ROS C.S.
        Frame.transform.translation.x = transform.localPosition.z;
        Frame.transform.translation.y = -transform.localPosition.x;
        Frame.transform.translation.z = transform.localPosition.y;

        Quaternion qt = transform.localRotation;

        Frame.transform.rotation.x = -qt.z;
        Frame.transform.rotation.y = qt.x;
        Frame.transform.rotation.z = -qt.y;
        Frame.transform.rotation.w = qt.w;
    }

    public void Initialize(string prefix = "")
    {
        // create frame instance and dependencies
        // Frame = new RosMessageTypes.Geometry.TransformStampedMsg();
        Frame = new TransformStampedMsg();
        Frame.header = new HeaderMsg();
        Frame.transform = new TransformMsg();
        Frame.transform.translation = new Vector3Msg();
        Frame.transform.rotation = new QuaternionMsg();

        if (this.transform.parent != null)
        {
            FrameExtractor parentFrame = this.transform.parent.GetComponent<FrameExtractor>();
            if (parentFrame != null)
            {
                Frame.header.frame_id = parentFrame.Frame != null ? parentFrame.Frame.child_frame_id : parentFrame.frameName;
            }
        }
        else
        {
            // special case, this is the top level starting node so call it map
            Frame.header.frame_id = "map";
        }
        Frame.child_frame_id = !string.IsNullOrEmpty(prefix) ? string.Format("{0}/{1}", prefix, frameName) : frameName;
    }
}
