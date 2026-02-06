# Autonomous Perception Unity

Unity 6 sensor simulation platform for autonomous robot/drone perception research, integrated with ROS2 via the Unity Robotics Hub TCP Connector.

This project provides realistic sensor simulations (LiDAR, radar, cameras) and publishes sensor data to ROS2 for real-time perception processing. Designed to work with the [ROS2 perception pipeline](https://github.com/Bluebottle10/autonomous-perception-ros2).

## Architecture

```
Unity 6 Simulation
├── Sensors
│   ├── RGB Camera ──────────── /camera/image_raw ──┐
│   ├── Depth Camera ────────── /camera/depth ──────┤
│   ├── Generic LiDAR ──────── /scan_cloud ─────────┤  ROS TCP
│   │   (50+ models)                                 ├── Connector ──► ROS2
│   └── Generic Radar ──────── (future)              │
├── Navigation                                       │
│   ├── Odometry Publisher ──── /odom ──────────────┤
│   ├── TF Publisher ────────── /tf ────────────────┤
│   └── Static TF Publisher ── /tf ─────────────────┘
└── Control
    └── Vehicle Controller ◄── /cmd_vel ◄────────────── ROS2
```

## Custom Components

### ROS2 Publishers (Assets/Scripts/ROS2Related/)

| Script | Topic | Message Type | Description |
|--------|-------|-------------|-------------|
| `RosCameraPublisher.cs` | `/camera/image_raw` | `sensor_msgs/Image` | RGB camera capture at configurable resolution |
| `DepthCameraPublisher.cs` | `/camera/depth` | `sensor_msgs/Image` | Depth via custom shader extraction |
| `OdometryPublisher.cs` | `/odom` + `/tf` | `nav_msgs/Odometry` | Position/rotation with Unity→ROS coordinate conversion |
| `LidarStaticTfPublisher.cs` | `/tf` | `tf2_msgs/TFMessage` | Static transform for sensor frames |

### ROS2 Subscribers

| Script | Topic | Message Type | Description |
|--------|-------|-------------|-------------|
| `RosVehicleController.cs` | `/cmd_vel` | `geometry_msgs/Twist` | Drives Unity vehicle from ROS2 velocity commands |
| `RosSplineAnimatorController.cs` | `/yolo/stop_signal` | `std_msgs/Bool` | Pauses/resumes spline animation on detection |

### Sensor Simulations (Assets/Scripts/Sensors/)

| Script | Description |
|--------|-------------|
| `GenericLidarSensor.cs` | GPU-accelerated LiDAR with 50+ pre-configured models |
| `GenericLidarRenderer.cs` | PointCloud2 publisher with ROS2 integration |
| `GenericRadarSensor.cs` | Radar simulation with material-based RCS modeling |
| `GenericCamera.cs` | Generic camera sensor interface |
| `SemanticSegmentation.cs` | Real-time semantic segmentation rendering |
| `SegmentationId.cs` | Per-object segmentation class tagging |
| `ScreenBoundingBox.cs` | 2D bounding box computation |

### Supported LiDAR Models

The `GenericLidarSensor` supports 50+ pre-configured LiDAR models:

**Velodyne:** Puck (VLP-16), PuckHiRes, HDL-32E, UltraPuck (VLP-32C), HDL-64E, Alpha Puck (VLS-128)

**Ouster:** OS0-32/64/128, OS1-32/64/128, OS2-32/64/128 (each with 512/1024/2048 horizontal resolution)

**Others:** Quanergy M8, VelaBit, Hokuyo, Leddar Pixell

### Custom Shaders (Assets/Shaders/)

| Shader | Purpose |
|--------|---------|
| `ExtractDepth.shader` | Extracts camera depth buffer for ROS2 publishing |
| `DepthShader.shader` | LiDAR depth rendering |
| `SceneSegmentation.shader` | Semantic class coloring |
| `fisheyeSim.shader` | Fisheye lens distortion |
| `pointCloud.shader` | Point cloud visualization |
| `BBX.shader` | Bounding box rendering |

### GPU Compute Shaders (Assets/ComputeShaders/)

| Shader | Purpose |
|--------|---------|
| `DepthScannerShader.compute` | GPU depth-to-point-cloud conversion for LiDAR |
| `RangeScannerShader.compute` | Range data computation |
| `DepthScannerShaderRadar.compute` | Radar-specific depth processing |
| `BoundingBoxShader.compute` | GPU bounding box computation |

## Prerequisites

- **Unity:** Unity 6 (6000.3.x or later)
- **Packages:** Unity Robotics Hub (ROS TCP Connector + URDF Importer) - installed automatically via manifest
- **ROS2:** [autonomous-perception-ros2](https://github.com/Bluebottle10/autonomous-perception-ros2) running on Ubuntu 24.04 / WSL2

### Required Asset Store Packages (not included in repo)

The following packages are excluded from the repository due to licensing. Import them from the Unity Asset Store:

- NWH Vehicle Physics 2 (vehicle dynamics)
- Gaia Pro (terrain generation)
- Complete Vehicle Pack (vehicle models)
- Suburb Neighborhood House Pack (environment)
- Realistic Drone (drone physics)

The project will work without these assets, but scenes will have missing references. The core scripts, shaders, and ROS2 integration work independently.

## Installation

```bash
# 1. Clone the repository
git clone https://github.com/Bluebottle10/autonomous-perception-unity.git

# 2. Open in Unity Hub
#    - Add the cloned folder as a project
#    - Unity will import and compile

# 3. Install ROS TCP Connector (should be automatic via Packages/manifest.json)
#    If not: Window → Package Manager → + → Add from Git URL:
#    https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector

# 4. Configure ROS connection
#    - In Unity: Robotics → ROS Settings
#    - Set ROS IP Address to your WSL2/Linux IP
#    - Default port: 10000
```

## Usage

1. Start the ROS TCP Endpoint in WSL2:
   ```bash
   ros2 run ros_tcp_endpoint default_server_endpoint
   ```

2. In Unity, open one of the sensor research scenes (e.g., `Assets/Scenes/AutonomousDriving.unity`)

3. Press Play - sensor data will stream to ROS2

4. Launch the ROS2 perception pipeline:
   ```bash
   ros2 launch perception_bringup full_pipeline.launch.py
   ```

## Coordinate Conversion

Unity uses a **left-handed** coordinate system (Z-forward, Y-up), while ROS2 uses **right-handed** (X-forward, Z-up). The conversion is handled automatically in `OdometryPublisher.cs`:

```
Unity → ROS2:
  Position: (X, Y, Z) → (Z, -X, Y)
  Rotation: (-Qz, Qx, -Qy, Qw)
```

## Scenes

| Scene | Description |
|-------|-------------|
| `AutonomousDriving` | Full autonomous vehicle with all sensors |
| `lidar_testing` | LiDAR sensor calibration and testing |
| `RadarResearch` | Radar sensor development |
| `PubSubScene` | ROS2 publisher/subscriber testing |
| `TFScene` | Transform frame debugging |
| `ObstacleDetection` | Obstacle detection pipeline |

## YouTube Tutorial Series

This project is part of an educational YouTube series on autonomous perception:

1. **Video 1:** Setting Up ROS2 Jazzy + TensorRT for Autonomous Perception
2. **Video 2:** Implementing Real-Time Perception: YOLO v11 + Semantic Segmentation
3. **Video 3:** From Pixels to Navigation: Semantic Costmap Pipeline

## Related Repository

- **ROS2 Pipeline:** [autonomous-perception-ros2](https://github.com/Bluebottle10/autonomous-perception-ros2) - ROS2 Jazzy perception nodes (YOLO, PIDNet, Costmap, Fusion)

## License

MIT License
