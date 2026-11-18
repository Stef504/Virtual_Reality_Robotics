### The 5 Core Jobs of SceneTeleopDataSource1: 

**1. Robot Pose Tracking**
- Reads the position, forward direction vector, and yaw angle from robotPoseFrame or huskyRoot.   
- Sets dst.poseAvailable = true and updates robotWorldPosition and robotYawDegrees.   
- This supplies the numerical coordinates shown in SpatialMapModule.   

**2. Camera Stream Feeding**
- Grabs the targetTexture (RenderTexture) assigned to frontCamera, rearCamera, and topDownCamera.   
- Allows optional manual texture overrides (frontTextureOverride, etc.) if using direct camera feeds or streamed video.   
- This supplies the visual images rendered in CameraModule and SpatialMapModule.  

**3. Top-Down Map Marker Projection**
- Converts the robot's 3D world position (pose.position) into 2D Viewport UV coordinates ($0.0$ to $1.0$) using topDownCamera.WorldToViewportPoint().   
- Projects a point slightly ahead of the robot to calculate its heading angle (mapMarkerDegrees).   
- This is how SpatialMapModule knows exact percentage offsets to place and rotate the UI map marker on screen.   

**4. Built-in Obstacle Raycasting (Simulated Proximity Sensor)**
- Shoots multiple Unity physics raycasts (Physics.RaycastAll) out to the left and right based on customizable angles (leftRayAnglesDegrees, rightRayAnglesDegrees.
- Automatically ignores collisions with the robot itself (hit.collider.transform.IsChildOf(huskyRoot)).   
- Calculates the minimum distance to the closest hit in meters and sets dst.leftDistanceMeters and dst.rightDistanceMeters.   
- This is where ObstacleAwarenessModule gets its warning distance numbers without needing external LiDAR or ROS 2 streams!  

**5. Semantic Object Scanning**
- Scans the scene periodically (every refreshLabelsSeconds) to find GameObjects with a SemanticSceneLabel component attached.   
- Packages each labeled object's name, world position, and movement status (manuallyMarkedMoving) into dst.objects.   
- This supplies the list of dynamic/static obstacles displayed in SemanticPerceptionModule. 



                                 ┌──────────────────────────────────────────┐
                                 │                UNITY SIM                 │
                                 └────────────────────┬─────────────────────┘
                                                      │
                       ┌──────────────────────────────┴──────────────────────────────┐
                       ▼                                                             ▼
         [ SceneTeleopDataSource ]                                     [ Camera / Video Feeds ]
         (Physics Raycasting)                                             │
                       │                                                             ▼
                       ├──────────────────────────────┐                [ Computer Vision (CV) ]
                       │                              │                (YOLO / OpenCV Model)
                       ▼                              ▼                              │
         [ Obstacle ROS Publisher ]     [ TeleopStudentContext ] ◄───────────────────┘
         (Publishes /scan to ROS 2)     (Populates UI Modules)
                       │                              │
                       ▼                              ▼
          ┌─────────────────────────┐   ┌───────────────────────────┐
          │     ROS 2 SYSTEM        │   │    UNITY UI FRONTEND      │
          │                         │   │                           │
          │  1. Costmap Layer       │   │ • Obstacle Awareness Bar  │
          │  2. Nav2 Global Planner │   │ • Semantic Object Labels  │
          │  3. Nav2 Local Controller│  │ • Front/Rear Camera View  │
          └────────────┬────────────┘   └───────────────────────────┘
                       │
                       ▼ (/cmd_vel)
         [ Unity Nav2 Subscriber ]
         (Drives StreetRouteMover / Car Controller)