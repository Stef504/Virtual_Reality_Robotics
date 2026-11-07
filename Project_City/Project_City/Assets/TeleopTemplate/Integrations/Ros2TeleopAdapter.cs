// Install Unity ROS-TCP-Connector and enable TELEOP_ROS in Player > Scripting Define Symbols.
// Keeping all ROS symbols conditional allows the desktop scene to open without ROS packages.
#if TELEOP_ROS
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Sensor;

namespace TeleopLab
{
    public class Ros2TeleopAdapter : MonoBehaviour, ITeleopRosPort
    {
        public TeleopCore core;
        public TeleopDashboard dashboard;
        public Transform robotViewRoot;

        [Header("ROS 2 topics - verify with ros2 topic list/type")]
        public string commandTopic = "/cmd_vel";
        public string odometryTopic = "/odom";
        public string frontCameraTopic = "/sensors/front_camera/color/image_raw";
        public string rearCameraTopic = "/sensors/rear_camera/color/image_raw";
        public string occupancyGridTopic = "/map";
        [Tooltip("Optional. Leave blank if you only have PointCloud2, not LaserScan.")]
        public string laserScanTopic = "";

        [Header("Freshness + rate")]
        public float poseTimeoutSeconds = 1.0f;
        public float imageUpdateHz = 10f;
        public float commandPublishHz = 10f;

        ROSConnection ros;
        float wantedLinear, wantedAngular, nextPublish;
        float lastPose = -9999f, lastFront = -9999f, lastRear = -9999f, lastMap = -9999f, lastLaser = -9999f;
        float nextFrontDecode, nextRearDecode;
        ImageMsg pendingFront, pendingRear;
        OccupancyGridMsg pendingMap;
        LaserScanMsg pendingLaser;
        OdometryMsg pendingOdom;
        Texture2D frontTex, rearTex, mapTex;
        string poseFrame = "", mapFrame = "";
        float poseX, poseY, poseYaw;
        double mapOriginX, mapOriginY, mapYaw;
        float mapResolution;
        int mapWidth, mapHeight;
        readonly object messageLock = new object();
        bool subscriptionsRegistered;

        public bool Ready { get { return enabled && Time.realtimeSinceStartup - lastPose <= poseTimeoutSeconds; } }
        public string Health
        {
            get
            {
                string odom = Ready ? "ODOM OK" : "ODOM MISSING/STALE";
                int cameras = (Time.realtimeSinceStartup-lastFront < 2f ? 1 : 0) + (Time.realtimeSinceStartup-lastRear < 2f ? 1 : 0);
                string map = Time.realtimeSinceStartup-lastMap < 5f ? "MAP OK" : "MAP MISSING";
                return odom + "  |  RGB " + cameras + "/2  |  " + map;
            }
        }

        void OnEnable()
        {
            ros = ROSConnection.GetOrCreateInstance();
            if (!subscriptionsRegistered)
            {
            ros.RegisterPublisher<TwistMsg>(commandTopic);
            ros.Subscribe<OdometryMsg>(odometryTopic, msg => { lock (messageLock) pendingOdom = msg; });
            if (!string.IsNullOrWhiteSpace(frontCameraTopic))
                ros.Subscribe<ImageMsg>(frontCameraTopic, msg => { lock (messageLock) pendingFront = msg; });
            if (!string.IsNullOrWhiteSpace(rearCameraTopic))
                ros.Subscribe<ImageMsg>(rearCameraTopic, msg => { lock (messageLock) pendingRear = msg; });
            if (!string.IsNullOrWhiteSpace(occupancyGridTopic))
                ros.Subscribe<OccupancyGridMsg>(occupancyGridTopic, msg => { lock (messageLock) pendingMap = msg; });
            if (!string.IsNullOrWhiteSpace(laserScanTopic))
                ros.Subscribe<LaserScanMsg>(laserScanTopic, msg => { lock (messageLock) pendingLaser = msg; });
            subscriptionsRegistered = true;
            }
            wantedLinear = wantedAngular = 0;
            nextPublish = 0;
            if (dashboard) { dashboard.SetRosProximityUnavailable(); if (dashboard.mapLabel) dashboard.mapLabel.text = "WAITING FOR OCCUPANCY GRID"; }
        }

        public void SetVelocityCommand(float v, float w)
        {
            wantedLinear = Ready ? v : 0f;
            wantedAngular = Ready ? w : 0f;
        }

        void Update()
        {
            if (core == null || core.mode != TeleopSourceMode.Ros2) return;
            OdometryMsg odom; ImageMsg front, rear; OccupancyGridMsg map; LaserScanMsg scan;
            lock (messageLock)
            {
                odom = pendingOdom; pendingOdom = null;
                front = pendingFront; pendingFront = null;
                rear = pendingRear; pendingRear = null;
                map = pendingMap; pendingMap = null;
                scan = pendingLaser; pendingLaser = null;
            }
            if (odom != null) ReadOdom(odom);
            if (front != null && Time.realtimeSinceStartup >= nextFrontDecode)
            {
                if (DecodeImage(front, ref frontTex, true)) lastFront = Time.realtimeSinceStartup;
                nextFrontDecode = Time.realtimeSinceStartup + 1f / Mathf.Max(1, imageUpdateHz);
            }
            if (rear != null && Time.realtimeSinceStartup >= nextRearDecode)
            {
                if (DecodeImage(rear, ref rearTex, false)) lastRear = Time.realtimeSinceStartup;
                nextRearDecode = Time.realtimeSinceStartup + 1f / Mathf.Max(1, imageUpdateHz);
            }
            if (map != null) ReadMap(map);
            if (scan != null) ReadLaser(scan);
            if (dashboard)
            {
                if (Time.realtimeSinceStartup - lastLaser > 1.0f) dashboard.SetRosProximityUnavailable();
                if (Time.realtimeSinceStartup - lastFront > 2f) dashboard.SetCameraTexture(true, null, false);
                if (Time.realtimeSinceStartup - lastRear > 2f) dashboard.SetCameraTexture(false, null, false);
                if (Time.realtimeSinceStartup - lastMap > 5f)
                {
                    dashboard.SetMapTexture(null);
                    if (dashboard.mapLabel) dashboard.mapLabel.text = "ROS MAP MISSING / STALE";
                }
            }
            UpdateMapMarker();

            if (Time.realtimeSinceStartup >= nextPublish)
            {
                PublishVelocity(Ready ? wantedLinear : 0f, Ready ? wantedAngular : 0f);
                nextPublish = Time.realtimeSinceStartup + 1f / Mathf.Max(1f, commandPublishHz);
            }
        }

        void ReadOdom(OdometryMsg msg)
        {
            poseFrame = msg.header.frame_id;
            poseX = (float)msg.pose.pose.position.x;
            poseY = (float)msg.pose.pose.position.y;
            var q = msg.pose.pose.orientation;
            poseYaw = Mathf.Rad2Deg * (float)Math.Atan2(2.0 * (q.w*q.z + q.x*q.y), 1.0 - 2.0*(q.y*q.y + q.z*q.z));
            if (robotViewRoot)
            {
                robotViewRoot.position = new Vector3(-poseY, robotViewRoot.position.y, poseX);
                robotViewRoot.rotation = q.From<FLU>();
            }
            lastPose = Time.realtimeSinceStartup;
        }

        bool DecodeImage(ImageMsg msg, ref Texture2D tex, bool isFront)
        {
            int width = (int)msg.width, height = (int)msg.height, step = (int)msg.step;
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096 || msg.data == null) return false;
            string encoding = msg.encoding.ToLowerInvariant();
            int channels = encoding == "rgb8" || encoding == "bgr8" ? 3 :
                           encoding == "rgba8" || encoding == "bgra8" ? 4 :
                           encoding == "mono8" ? 1 : 0;
            if (channels == 0 || step < width * channels || msg.data.Length < step * height)
            {
                Debug.LogWarning("Unsupported ROS Image encoding/stride: " + msg.encoding);
                return false;
            }
            if (!tex || tex.width != width || tex.height != height)
            {
                if (tex) Destroy(tex);
                tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
            }
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = y * step + x * channels;
                byte r = msg.data[i], g = channels > 1 ? msg.data[i+1] : r, b = channels > 1 ? msg.data[i+2] : r;
                byte a = channels == 4 ? msg.data[i+3] : (byte)255;
                if (encoding == "bgr8" || encoding == "bgra8") { byte tmp = r; r = b; b = tmp; }
                pixels[y * width + x] = new Color32(r, g, b, a);
            }
            tex.SetPixels32(pixels); tex.Apply(false);
            if (dashboard) dashboard.SetCameraTexture(isFront, tex, true); // ROS rows run top-to-bottom
            return true;
        }

        void ReadMap(OccupancyGridMsg msg)
        {
            int width = (int)msg.info.width, height = (int)msg.info.height;
            if (width <= 0 || height <= 0 || width > 2048 || height > 2048 || msg.data == null || msg.data.Length < width * height) return;
            if (!mapTex || mapTex.width != width || mapTex.height != height)
            {
                if (mapTex) Destroy(mapTex);
                mapTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                mapTex.filterMode = FilterMode.Point;
                mapTex.wrapMode = TextureWrapMode.Clamp;
            }
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                int p = Convert.ToInt32(msg.data[i]);
                // ROS convention: -1 unknown, 0 free, 100 occupied.
                byte v = p < 0 ? (byte)95 : (byte)Mathf.RoundToInt(Mathf.Lerp(233, 35, Mathf.Clamp01(p / 100f)));
                pixels[i] = p < 0 ? new Color32(77, 88, 105, 255) : new Color32(v, v, v, 255);
            }
            mapTex.SetPixels32(pixels); mapTex.Apply(false);
            mapFrame = msg.header.frame_id;
            mapOriginX = msg.info.origin.position.x;
            mapOriginY = msg.info.origin.position.y;
            var q = msg.info.origin.orientation;
            mapYaw = Math.Atan2(2.0*(q.w*q.z+q.x*q.y), 1.0-2.0*(q.y*q.y+q.z*q.z));
            mapResolution = msg.info.resolution;
            mapWidth = width; mapHeight = height;
            lastMap = Time.realtimeSinceStartup;
            if (dashboard)
            {
                dashboard.SetMapTexture(mapTex);
                if (dashboard.mapLabel) dashboard.mapLabel.text = "ROS OCCUPANCY GRID  |  " + mapFrame;
            }
        }

        void UpdateMapMarker()
        {
            if (!dashboard) return;
            // Never combine an odom-frame pose with a map-frame grid without map->odom TF.
            bool valid = Ready && Time.realtimeSinceStartup - lastMap <= 5f && lastMap > 0 && mapResolution > 0 && mapWidth > 0 && mapHeight > 0
                         && !string.IsNullOrEmpty(mapFrame) && poseFrame == mapFrame;
            if (!valid)
            {
                dashboard.SetMapMarker(false, 0, 0, 0);
                if (Ready && Time.realtimeSinceStartup - lastMap < 5f && poseFrame != mapFrame
                    && dashboard.mapLabel)
                    dashboard.mapLabel.text = "ROS GRID  |  POSE/MAP FRAME MISMATCH (NO TF)";
                return;
            }
            double dx = poseX - mapOriginX, dy = poseY - mapOriginY;
            double c = Math.Cos(mapYaw), s = Math.Sin(mapYaw);
            float u = (float)(( c*dx + s*dy) / (mapResolution*mapWidth));
            float v = (float)((-s*dx + c*dy) / (mapResolution*mapHeight));
            dashboard.SetMapMarker(u >= 0f && u <= 1f && v >= 0f && v <= 1f,
                u, v, poseYaw - Mathf.Rad2Deg*(float)mapYaw);
        }

        void ReadLaser(LaserScanMsg msg)
        {
            float left = float.PositiveInfinity, right = float.PositiveInfinity;
            for (int i = 0; i < msg.ranges.Length; i++)
            {
                float dist = msg.ranges[i];
                if (float.IsNaN(dist) || float.IsInfinity(dist) || dist < msg.range_min || dist > msg.range_max) continue;
                float angleDeg = Mathf.Rad2Deg * (msg.angle_min + i * msg.angle_increment);
                if (angleDeg > 5 && angleDeg < 100) left = Mathf.Min(left, dist);
                if (angleDeg < -5 && angleDeg > -100) right = Mathf.Min(right, dist);
            }
            lastLaser = Time.realtimeSinceStartup;
            if (dashboard) dashboard.SetRosProximity(left, right, Urgency(left), Urgency(right));
        }
        float Urgency(float dist) { return dist <= .50f ? 1f : dist >= .85f ? 0f : (.85f-dist)/(.85f-.50f); }

        void PublishVelocity(float v, float w)
        {
            if (ros == null) return;
            try
            {
                var msg = new TwistMsg();
                msg.linear = new Vector3Msg(v, 0, 0);
                msg.angular = new Vector3Msg(0, 0, w);
                ros.Publish(commandTopic, msg);
            }
            catch (Exception e) { Debug.LogWarning("ROS velocity publish: " + e.Message); }
        }
        void OnDisable()
        {
            wantedLinear = wantedAngular = 0;
            PublishVelocity(0, 0); // best effort; does NOT replace a ROS-side timeout watchdog
            if (dashboard) dashboard.SetMapMarker(false, 0, 0, 0);
        }
        void OnDestroy() { if (frontTex) Destroy(frontTex); if (rearTex) Destroy(rearTex); if (mapTex) Destroy(mapTex); }
    }
}
#endif
