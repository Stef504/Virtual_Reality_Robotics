DRIVE READY COUPE -- UNITY 2022.3
================================

THIS IS AN ORIGINAL HIGH-DETAIL PROCEDURAL COUPE, not the earlier BMW and not a downloaded photorealistic model. It includes separate wheel meshes and uses deliberately simple, STABLE ARCADE driving physics (no WheelColliders to misalign).

IMPORT THE .UNITYPACKAGE
1. Unity 2022.3: Assets > Import Package > Custom Package... > DriveReadyCoupe_Unity2022.unitypackage
2. Click Import and wait until Unity finishes compiling.
3. Under Assets > DriveReadyCoupe > Prefabs, find DriveReadyCoupe.prefab. It is generated automatically on import.
4. Drag DriveReadyCoupe.prefab onto your road. Its root scale must stay (1,1,1). Ensure the road has a BoxCollider/MeshCollider/TerrainCollider (and Is Trigger is disabled).
5. Press Play, click the Game tab to focus it, use W/S/A/D.

IF PREFAB DOES NOT APPEAR
Tools > Drive Ready Coupe > Build or Rebuild Prefab.
Wait until compilation finishes and try that command again.

CONTROLS
W / Up     Drive forward
S / Down   Reverse
A / Left   Steer left
D / Right  Steer right
Space      Brake
R          Reset to the placed starting position

OPTIONAL FOLLOW CAMERA
Select your existing Camera (do not create an extra Audio Listener).
Add Component > DriveReadyOptionalCamera, drag DriveReadyCoupe_WASD into Target.
Or just use your existing camera as before.

WHAT'S INCLUDED
* 13 OBJ sources: many tens of thousands of polygons, smoothly curved body, separate high-resolution tire/rim details.
* Auto-made HDR-compatible URP Lit (or Built-in Standard) materials.
* Single Rigidbody + BoxCollider + WASD physics controller.
* Four independent steer/roll wheel pivots.
* One-click/rebuild Editor builder and optional camera follow script.
* No external plugins, custom Input packages or network downloads required.

NOTES
* Car heading is +Z, Y is up, root Scale=1.
* This is a stable arcade controller, not a realistic suspension simulator. Wheels animate but use no WheelColliders; physics body is a box.
* Ground needs a non-trigger collider; roads that are only textures/meshes without colliders cannot support any Rigidbody.
* If your project uses the Input System-only setting, the script supports it when that Unity package is installed; default Legacy input also works.
* Root prefab created in the Unity Editor; it is not precompiled Unity prefab YAML, so allow editor scripts to compile first.
* The assembly has been verified for file structure and rendered outside Unity, but CANNOT be play-tested in Unity in this environment. If Unity shows import/compiler errors, send a screenshot and the package can be patched.
* Body colour can be changed via Assets/DriveReadyCoupe/Materials/Gloss_Yellow.mat after first build.

LICENSING
Original geometry generated for this package and scripts may be used and edited in your own projects. No third-party car model or copyrighted brand assets are included. No attribution is required.
