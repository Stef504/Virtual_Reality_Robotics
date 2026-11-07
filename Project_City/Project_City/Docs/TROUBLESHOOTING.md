# Troubleshooting (Unity 2022.3)

**Tools menu missing:** Put `.cs` files under `Assets/TeleopStudentModules/` and let Unity recompile. `TeleopStudentInstaller.cs` must remain in its `Editor` directory. Open Window > General > Console for red errors.

**Existing UI still shows `STANDALONE | DEMO DATA`:** That text belongs to the OLD `TeleopDashboard` and is outside this package. Disable the old `BaselineUI_Canvas` to see only the new `UNITY SIMULATION` panel. If keeping the original Canvas, edit that old text logic separately.

**Husky vibrates or camera moves while robot doesn't:** Check that the old `TeleopCore` is not re-enabling `DemoRobotDriver`; physical Husky must not be parented to `MyRobot`. Leave wheel articulation scripts alone. This package never drives wheels or moves transforms.

**Camera frame says N/A:** Check actual front/rear Camera components and `Target Texture` RenderTextures. If they use a special render path, reference an existing RenderTexture via the data source `... Texture Override`. The UI never calls Camera.Render or changes camera poses.

**Map N/A:** Assign `DemoGroundTruthMapCamera` (or your own top-down camera), with RenderTexture. The robot marker is only computed if the image comes from exactly that camera and it has a valid forward projection. If supplying an arbitrary map image, use the external source to supply properly calibrated normalized UV. Nothing is secretly SLAM.

**No proximity:** Inspect Scene source's robot pose reference and `enableRaycastProximity`. The rays are horizontal fan probes; adjust `sensorHeight`, `obstacleLayers`, and `maximumRangeMeters` for your environment and colliders. Ignore hits on the Husky hierarchy. This is a training signal, not collision avoidance safety control.

**No semantic objects:** Add `SemanticSceneLabel` to desired existing scene props, OR run Tools > Teleop Students > Add OPTIONAL Manual Sample Markers. The module intentionally does not invent detections from camera images.

**Buttons don't respond:** Only one EventSystem should be present. If using Input System only, replace `StandaloneInputModule` with a correctly configured `InputSystemUIInputModule` after installing the Input System package. This package needs mouse UI events, not HTC Vive.

**Panel size / overlap:** The UI uses a reference 1600x900 scaled Canvas. Adjust Canvas ordering and panel anchors in your scene or prefab. It's a sample UX, not a finished visual design.

**Data from ROS missing:** External adapter has no ROS dependency. You must integrate your existing ROS–TCP callback code yourself. See `INTEGRATION_CONTRACT.md`.
