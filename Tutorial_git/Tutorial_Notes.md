# Publisher:

When creating the RosPublisher GameObject this is where we link our script. In the `Insepector` window is where we add our Cube GameObject.

This C# script acts as a bridge between Unity’s frame-based game loop and your ROS 2 environment. Because you are coming from a ROS 2 C++ and Python background, you can think of this script as a lightweight ROS 2 publisher node embedded directly onto a 3D object in Unity.

### 1. Dependencies and Namespaces

```csharp
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.UnityRoboticsDemo;

```

* `UnityEngine`: Gives access to Unity's core engine systems (like `GameObject`, `Transform`, `Time`, and `MonoBehaviour`).
* `Unity.Robotics.ROSTCPConnector`: Imports the TCP networking logic to talk to the `ros_tcp_endpoint` running in your Docker container.
* `RosMessageTypes.UnityRoboticsDemo`: Imports the C# classes that Unity generated from your ROS `.msg` files. This allows you to instantiate `PosRotMsg` natively in C#.

### 2. Class Definition and Variables

```csharp
public class RosPublisherExample : MonoBehaviour

```

By inheriting from `MonoBehaviour`, this class becomes a Unity Component. This is why you were able to drag and drop it onto your empty `RosPublisher` GameObject in the Inspector.

```csharp
ROSConnection ros;
public string topicName = "pos_rot";
public GameObject cube;
public float publishMessageFrequency = 0.5f;
private float timeElapsed;

```

* **Public variables (`topicName`, `cube`, `publishMessageFrequency`):** In Unity, declaring a variable as `public` (or using the `[SerializeField]` attribute) exposes it directly in the Unity Inspector. This allows you to change the ROS topic name, swap the target GameObject, or adjust the publishing rate without having to recompile the C# code.
* **`ROSConnection ros` & `timeElapsed`:** Kept private because they handle the internal state and TCP connection instance.

### 3. Initialization (`Start` Method)

```csharp
void Start()
{
    ros = ROSConnection.GetOrCreateInstance();
    ros.RegisterPublisher<PosRotMsg>(topicName);
}

```

The `Start()` method is executed exactly once when you press Play in the Unity Editor. It acts similarly to the constructor in a `rclcpp::Node`.

* **`GetOrCreateInstance()`:** Uses a singleton pattern to grab the active network connection to your Docker container (the one running on `127.0.0.1`).
* **`RegisterPublisher`:** Sends a handshake over TCP to the ROS 2 endpoint, notifying it that this client will be publishing data of type `PosRotMsg` on the `/pos_rot` topic.

### 4. The Execution Loop (`Update` Method)

```csharp
private void Update()

```

The `Update()` method runs once per rendered frame. Because frame rates fluctuate, you cannot rely on it to execute at a fixed time interval.

```csharp
timeElapsed += Time.deltaTime;
if (timeElapsed > publishMessageFrequency)

```

Instead of using a blocking sleep function (like `rclcpp::sleep_for`), Unity uses a non-blocking accumulator. `Time.deltaTime` measures the fractional seconds elapsed since the last frame. Once the accumulator surpasses `0.5f` (half a second), the publishing logic triggers.

```csharp
cube.transform.rotation = Random.rotation;

```

This physically modifies the cube in the 3D scene, assigning it a completely randomized quaternion rotation.

```csharp
PosRotMsg cubePos = new PosRotMsg(
    cube.transform.position.x,
    // ... y, z ...
    cube.transform.rotation.w
);

```

Here, the script extracts the Cartesian coordinates (`position`) and quaternion data (`rotation`) from the Unity `Transform` component of the assigned cube. It packs this spatial data into the `PosRotMsg` object.

```csharp
ros.Publish(topicName, cubePos);
timeElapsed = 0;

```

Finally, the script serializes the message and pushes it through the TCP socket to your ROS 2 Docker container. The timer is then reset to `0` to begin counting the next 0.5-second interval.


PosRotMsg (The Message Type), the developers created a file named PosRot.msg. When you clicked "Build 2 msgs" earlier, Unity read that file and generated the C# class PosRotMsg. If you write your own ROS 2 package with a file named ArmJointAngles.msg, Unity will generate a C# class named ArmJointAnglesMsg for you to use in your script.

/pos_rot is a general topic name, that you can change even in Unity because its Publish in C#