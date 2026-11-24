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


---


                           ┌──────────────────────────────────────────┐
                           │          UNITY SIMULATION SCENE          │
                           └────────────────────┬─────────────────────┘
                                                │
         ┌──────────────────────────────────────┼──────────────────────────────────────┐
         ▼                                      ▼                                      ▼
[ SceneTeleopDataSource ]              [ Front & Rear Cameras ]               [ Vehicle Control ]
(Physics Raycasts)                              │                             (Obstacle Controller)
         │                                      │                                      ▲
         ▼                                      ▼                                      │
[ ROS 2 /scan Publisher ]            [ Computer Vision Model ]                         │
(Publishes scan arrays)              (Automatic Labeling)                              │
         │                                      │                                      │
         ├──────────────────────────────┐       ▼                                      │
         │                              │  Writes to context.Current.objects           │
         ▼                              │       │                                      │
┌─────────────────┐                     │       ▼                                      │
│ ROS 2 SYSTEM    │                     │  [ SemanticPerceptionModule ]                │
│                 │                     │  (UI Object Name List Panel)                 │
│ /scan Topic     │                     │                                              │
└────────┬────────┘                     ▼                                              │
         │                   [ ObstacleAwarenessModule ]                               │
         │                   (UI Proximity Warning Bars)                               │
         │                                                                             │
         ├─────────────────────────────────────────────────────────────────────────────┘
         │ (Actual Controller: Subscribes to /scan to drive avoidance logic)
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│ TRIAL / TESTING PHASE                                       │
│ Publish /cmd_vel commands to test vehicle control scripts   │
└─────────────────────────────────────────────────────────────┘

---

### Integration Logic:
4 modules, 

Student 1 -> camera module (view raw images) -> they have to implement vehicle movement/ steering control, predefined routes, obstacle detection and avoidance -> the UI for them could be 

Student 2 -> spatial map module (shows a marker of current position within the world) -> create moving traffic , predefined driving paths for multiple vehicles (non-pilot cars), create different environment scenarios, can also do a bounding box on UI -> student 1 can use the global path calculated to define routes of pilot car

Student 3 -> Obstacle awareness module (uses raycasting to detect obstacles on the light/right side of the pilot car) -> their main focus is pedestrian awareness -> their UI is the bar of obstacle closeness they also need to show bounding boxes (which to me makes more sense on the camera module but maybe they ccan alter there to show also the image with bounding box (so adapt the code) and include the bars (an idea)) -> student 2 can create the different walking scenarios (since im already doing that i can do a scenario with people crossing a road)

Student 4 -> Semantic perception module (labels objects and places it within a map) -> assigns labels, enable /disable semantic visualization ayer -> since we all kinda use CV library i assume that, the information gathered (from us) will be linked to the "ObservedObject" names into context.Current.objects so we need to make sure we link it to yours.


### Student 1
- creating a pose publisher since the spatial map already contains it might as well be the one to publish it. This is taken from the sceneTeleopDataSources. With this remember that Unity frame and Ros frames are different Unity Coordinate Space (X Right, Y Up, Z Forward) to ROS Coordinate Space (X Forward, Y Left, Z Up)
    - Using  `geometry_msgs/msg/PoseStamped.msg` which is a compact form of : `std_msgs/msg/Header header`, and `geometry_msgs/msg/Pose pose`. This was the reference coordinate frame and timestamp are sent together. To retrieve the most up to date coordinate frame. 
    - We read from what the sceneTeleopDataSources produces, hence the names
    - Let the user decide topic name 
    - FrameID has to be map because of the way nav2 works, and it uses a global mapping


- I can also alter my ui, add bounding boxes on the top, still use the marker to track its position (locally of course)


- alter the paths taken by the padestrians and other vehicles (i.e changing the router mover script or the street motion animator).
  - Lets set the scene:
  - PARKING SCENE: going from a certain point until the car is parked. ✅
  - Walking Padestrian: Walking across the road, loop
  - More Buildings, moving cars, pedestrians.
  - Moving cars that form traffic

When adding new models to a scene:
- Scale mismatch, axis orientation, physics and raycast interference (make sure raycast masks handle it cleanly)
- Recommened it .fbx, if from Unity Asset Store simply import directlt into open project
- Follow instructions and download and then import
- Drag the new car models into the `Hierarchy Window`. Always use `Preabs` because they come pre-assembled with their materials, colors and textures already applied. 
- The shaders might be different so in `materials` click on one of the items, then in the top menu: `Edit -> Rendering -> Materials -> Converted Selected Built-in Materials to URP ` that way it converts to our shaders

Moving cars that form traffic:
  - We have to modify the `StreetRouteMover.cs` to `StreetRouteMover_Mod1` because we want to implement that the non-controller cars can turn right/left, do a start and stop manuever.
- We also add a `IntersectionZone.cs` that attaches a 3D Box Collider at the intersection that detects when a car enters the zone and tells it how long to stop and which direction to turn. When reached it turns and continues based off the desired travel distance to continue
- In each Moving vehicle ensure that the `Trigger` variable in enabled in the `Box Collider`, and attached the `CarFollowingSensor` and `StreetRouteMover_Mod1`code as a component. From there we can choose the speed, travel distance and where it turns left/right/Continues at an intersection
- In the CarSensorFollwing Script this sends out raycasting so it can detect vehicles in front of it, and this way can commit the start/stop action in the `StreetRouteMover_Mod1`
- When seting the `Vehicle layer` , it is important to choose `Yes, change children` because of how the 3D car model hierarchy is, The Parent GameObject holds the StreetRouteMver and CarFollowSensor while the child GameObject holds the Box Colliders and Mesh
- Also in the CarFollowingSensor make sure that the ray-casting doesnt collide with itself, but do it for each vehicle not the .root (which is the subdivision it is in). `!hit.collider.transform.IsChildOf(transform)`
- The `StreetRouteMover_Mod1` is where we turn the car around and continue to drive back