using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class GPSReporter : MonoBehaviour
{
    public Transform target;
    public float DeltaTime = 0.1f;

    private CameraCapture _capture;
    int count = 0;
    private float _time = 0;
    
    Dictionary<int, string> _map = new Dictionary<int, string>();
    
    // reference
    double lat = 37.08650396057173;
    double lon = -76.38087990000001;
    double alt = 0;
    UnityToGPSConverter _converter = new UnityToGPSConverter();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        transform.LookAt(target.position + new Vector3(0, 1f, 0));
        // // local coordinate
        // var pos = transform.position;
        // double e = -pos.z;
        // double n = pos.x;
        // double u = pos.y;
        //
        // // camera conversion
        // var conversion = EnuToLlaConverter.EnuToLla(e, n, u, lat, lon, alt);
        // Debug.Log($"Camera @: {conversion.latitude},{conversion.longitude},{conversion.altitude}");
        //
        // // object conversion
        // pos = target.position;
        // e = -pos.z;
        // n = pos.x;
        // u = pos.y;
        //
        // conversion = EnuToLlaConverter.EnuToLla(e, n, u, lat, lon, alt);
        // Debug.Log($"Person @: {conversion.latitude},{conversion.longitude},{conversion.altitude}," +
        //           $"{e},{n},{u}");
        
        
        var gps_data = _converter.UnityToGPS(target.position);
        Debug.Log($"lat: {gps_data.lat}, lon: {gps_data.lon}, alt: {gps_data.alt}");

        _capture = GetComponent<CameraCapture>();

        _time = Time.fixedTime;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target.position + new Vector3(0, 1f, 0f) );
        if (Time.fixedTime - _time > DeltaTime)
        {
            // Self.LookAt(target.position + new Vector3(0, 1f, 0f) );
        
            // image capture
            var img_path = Path.Combine(@"D:\Sandbox\Data\Images\GPS", $"{count}.jpg");
            _capture.SnapImage(img_path);
            var euler = transform.eulerAngles;
            // gps conversion
            var pos = transform.position;
            var gps = _converter.UnityToGPS(transform.position);
            // float bottom = Vector3.Distance(pos, target.position);
            // float top = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(target.position.x, target.position.z));
            // float ang = Mathf.Rad2Deg * Mathf.Acos(top / bottom);
            _map[count] = $"{gps.lat},{gps.lon},{gps.alt},{pos.x},{pos.y},{pos.z},{euler.x},{90 - euler.y}";
            // double e = -pos.z;
            // double n = pos.x;
            // double u = pos.y;
            // var conversion = EnuToLlaConverter.EnuToLla(e, n, u, lat, lon, alt);
            // // Debug.Log($"{count},{conversion.latitude},{conversion.longitude},{conversion.altitude}");
            // _map[count] = $"{conversion.latitude},{conversion.longitude},{conversion.altitude}," +
            //               $"{e},{n},{u},{euler.x},{90.0 - euler.y}";
        
            count++;
            _time = Time.fixedTime;
        }

    }

    void OnApplicationQuit()
    {
        Debug.Log("Application Quit");
        var csv_path = Path.Combine(@"D:\Sandbox\Data\Images\GPS", $"gps.csv");
        string jsonString = JsonConvert.SerializeObject(_map, Formatting.Indented);
        File.WriteAllText(csv_path, jsonString);
    }
}
