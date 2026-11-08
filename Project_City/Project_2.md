# Student 2 — Urban Traffic and Vehicle Simulation
Objective: Simulate realistic vehicle movement and basic traffic behavior.

### Tasks:

- Use the provided vehicle models and movement scripts to create moving traffic.
  - Assets - Our Car - Scripts
  - Camers found on `Pilot_Car`, we have RobotFrontCamera, RobotRearCamera, DemoGroundTruthMapCamera but not attached to vehicles
    - We could attach them and use some identification for lines on the road for stopping/start manuvour 

- Implement predefined driving paths for multiple vehicles.
  - Could do a turn around the block, drive to the end of the street, each vehicle has its own driving path. 4 Vehicles

- Implement basic stopping and starting behavior at intersections.
  - Could be done with a publishers and subscriber/ Serive and client
  - Publish everytime the camera sees the lines and that triggers a stop and either wait 5sec and then go.. or wait until car passes (?)


- Introduce simple traffic rules, such as stopping at designated points or maintaining a safe distance from other vehicles.
    - Again using the cameras/ lidar to detect cars and setting a safe distance between them

- Test traffic movement under different scenarios.
  - Could setup one where the cars are close together to see if they adhere to a safe distance, or where they meet at an intersection to see if they can follow intersection rules, or testing where they follow the given path and adhere to rules (kinda a given), or 

Expected Result: An urban traffic simulation with vehicles following predefined routes and responding to simple traffic conditions.
Note: Existing vehicle models and movement scripts can be reused and modified.

# Where to start:
 how can i visualize in RVIZ? Obtain a camera image but what detection is there for intersections - can use edge detection mixed with color detection of the line or use trigger zones that are in Unit ? Need to add colliders to the cars, are the moving cars Rigid bodies? 


# General Requirements
- Use the provided Unity project and autonomous vehicle model.
- Keep all implementations modular and organized. Name each module and its scripts clearly to simplify evaluation.
- Reuse provided vehicle and pedestrian models where appropriate.
- Avoid unnecessary modifications to unrelated components.
- Ensure that the implemented functionalities work correctly in Unity Play mode.
- Simulated sensor data may be used when real sensor processing or external ROS integration is unavailable.
- Coordinate with other students to ensure compatibility between the modules.

### Student Project Allocation:
| Project | Student |
| ----------- | ----------- |
| Francesco | 1 |
| Stefania | 2 |
| Amine | 3 |
| Riadh | 4 |


### Key Shortcuts
Center the object within your frame:
  - Click the object in the Hierarchy and then hover your mouse in the scene window and click F
  - If you want the camera to track the object while it is moving (for example, when your ROS 2 script is driving the cube around during Play mode), select the object and press Shift + F

