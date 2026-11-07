```
This is an installation guide for Unity and ROS integration. The other steps also include the necessary packages for the Assignment of Virtual Reality for Robotics. 
```

## ROS Integration:
The steps up to `Install Unity Robotics Demo` (excluding) are used to just setup the connecting bridge between Unity and the ROS2 Docker container. 

The steps included in the `Install Unity Robotics Demo` are used when following the tutorials offered to familiarize yourself with ROS and Unity integration. 

Following the tutorial on: https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/ros_unity_integration/setup.md#ros2-environment 

You don't have to have your projects in the same folder as their, as its just a setup for the ROS Docker. Create the new Unity Project (3D)

## For the Assignment: 
Follow `Getting Started` section on https://github.com/subhransu10/Autonomous_Driving_in_an_Urban_Environment/tree/main 

### Personal Usage
I followed a tutorial and am using the docker. Everything was done on Windows. 

The Docker environment includes the demo packages. The `colcon build` command must run inside the terminal where the Docker container is running.

To Run Docker Container:
Go to the folder where you cloned the git, and type the following commands:

```
 docker run -it --rm -p 10000:10000 foxy /bin/bash
bash
```

The 
```
/home/dev_ws# 
bash
```
Is the correct root folder to run this command. 

### Attempting the Publisher Tutorial
https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/ros_unity_integration/publisher.md

Changing from Microsft Visual Studio to Visual Studio Code:

- Window -> Porject manager
- Project Manager -> in the Unity Registry -> search Visual Studio editor (unblock it)
- Edit -> Preferences 
- Preferences -> External Tools
- External Tools -> External Script editor -> Visual Studio Code

In Visual Studio Code download the `Unity` extention from Microsoft