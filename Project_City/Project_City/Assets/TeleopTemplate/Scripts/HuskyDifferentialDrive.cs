
using UnityEngine;

public class HuskyDifferentialDrive : MonoBehaviour
{
    [Header("Wheel Articulation Bodies")]
    public ArticulationBody frontLeft;
    public ArticulationBody rearLeft;
    public ArticulationBody frontRight;
    public ArticulationBody rearRight;

    [Header("Robot Geometry - Measure from URDF")]
    public float wheelRadius = 0.165f;
    public float trackWidth = 0.55f;

    [Header("Desktop Test Speeds")]
    public float linearSpeed = 5.0f;
    public float angularSpeed = 6.0f;

    [Header("Wheel Drive")]
    public float driveDamping = 100f;
    public float driveForceLimit = 100f;

    [Header("Wheel Direction Calibration")]
    public float leftWheelSign = 1f;
    public float rightWheelSign = 1f;

    void Start()
    {
        if (!frontLeft || !rearLeft ||
            !frontRight || !rearRight)
        {
            Debug.LogError(
                "Assign all four wheel ArticulationBodies.");
            enabled = false;
            return;
        }

        if (wheelRadius <= 0f || trackWidth <= 0f)
        {
            Debug.LogError("Invalid robot geometry.");
            enabled = false;
            return;
        }

        ConfigureWheel(frontLeft);
        ConfigureWheel(rearLeft);
        ConfigureWheel(frontRight);
        ConfigureWheel(rearRight);
    }

    void ConfigureWheel(ArticulationBody wheel)
    {
        if (wheel.jointType != ArticulationJointType.RevoluteJoint)
        {
            Debug.LogError(
                wheel.name + " must be a revolute joint.");
            enabled = false;
            return;
        }

        ArticulationDrive drive = wheel.xDrive;

        drive.stiffness = 0f;
        drive.damping = driveDamping;
        drive.forceLimit = driveForceLimit;
        drive.targetVelocity = 0f;
        drive.driveType = ArticulationDriveType.Force;

        wheel.xDrive = drive;
    }

    void FixedUpdate()
    {
        float v = 0f;
        float w = 0f;

        // Keyboard input
        if (Input.GetKey(KeyCode.W))
            v += linearSpeed;

        if (Input.GetKey(KeyCode.S))
            v -= linearSpeed;

        if (Input.GetKey(KeyCode.A))
            w += angularSpeed;

        if (Input.GetKey(KeyCode.D))
            w -= angularSpeed;

        // Differential-drive kinematics
        float leftVelocity =
            (v - w * trackWidth * 0.5f) / wheelRadius;

        float rightVelocity =
            (v + w * trackWidth * 0.5f) / wheelRadius;

        leftVelocity *= leftWheelSign;
        rightVelocity *= rightWheelSign;

        SetWheelVelocity(frontLeft, leftVelocity);
        SetWheelVelocity(rearLeft, leftVelocity);

        SetWheelVelocity(frontRight, rightVelocity);
        SetWheelVelocity(rearRight, rightVelocity);
    }

    void SetWheelVelocity(
        ArticulationBody wheel, float velocity)
    {
        ArticulationDrive drive = wheel.xDrive;
        drive.targetVelocity = velocity;
        wheel.xDrive = drive;
    }

    void OnDisable()
    {
        StopWheels();
    }

    void StopWheels()
    {
        if (frontLeft) SetWheelVelocity(frontLeft, 0f);
        if (rearLeft) SetWheelVelocity(rearLeft, 0f);
        if (frontRight) SetWheelVelocity(frontRight, 0f);
        if (rearRight) SetWheelVelocity(rearRight, 0f);
    }
}
