REALISTIC STREET DROP-INS -- UNITY 2022.3
=======================================

IMPORTANT -- real asset content is downloaded inside Unity:
This .unitypackage contains working route controllers, an importer/downloader,
and a prefab builder. The photoreal/realistic characters and the detailed
Spectral GT 3D model could not be bundled here because the model-host downloads
are unavailable in the file-building environment. Do NOT expect models to appear
before completing the steps below.

FIRST-TIME SETUP
1. Import RealisticStreetDropIns_Unity2022.unitypackage into your Unity 2022.3 project.
2. For GLB models, open Window > Package Manager > + > Add package by name,
   enter: com.unity.cloud.gltfast
   If this package is unavailable for your editor, try com.atteneder.gltfast.
   Note: installation needs an internet connection.
3. Use Tools > Realistic Street Drop-Ins > 1 - Download High-Detail Models
   (downloading takes time; watch Unity Console for errors).
4. Use Tools > Realistic Street Drop-Ins > 2 - Build or Rebuild Prefabs.
5. Drag prefabs from Assets/RealisticStreetDropIns/Prefabs into any scene.
6. For more detailed traffic coupe variations, FIRST import the previously
   supplied DriveReadyCoupe_Unity2022.unitypackage into the same project.
   The installer detects its prefab and makes two moving + two parked colors.

PREFABS AFTER BUILD
- Traffic_SpectralGT / Parked_SpectralGT: CC0 textured sports car.
- Pedestrian_OldMan_Walking / Pedestrian_OldMan_Standing: CC BY 4.0
  textured human with skeleton, walk and idle animation (prefer GLB for idle).
- Traffic_Coupe_Silver / Traffic_Coupe_Red / Parked_Coupe_Black /
  Parked_Coupe_White: additional sports cars IF DriveReadyCoupe is installed.

HOW MOTION WORKS
- Moving prefabs animate automatically once Play is pressed.
- Select moving prefab instance > Street Route Mover > distance/speed/direction.
- A cyan route line appears in the Scene view when the instance is selected.
- Cars loop by returning to start; pedestrians reverse at endpoints.
- Parked cars and standing characters stay put.
- They are background agents, not navigation-aware. They don't avoid people,
  vehicles, sidewalks or obstacles. Place on a level route unobstructed.
- Routes are straight, and objects stay at the height where you placed them.
- Moving objects have kinematic bodies for simple detection/collisions,
  and DO NOT respond to WASD or the existing DriveReady controller.

MORE DISTINCT PEOPLE
- Download realistic rigged animated FBX/GLB characters from a licensed
  source of your choice, then import them into Unity.
- Select the imported MODEL ASSET in Project view.
- Tools > Realistic Street Drop-Ins > 3 - Make Prefabs from Selected 3D Character.
- A walk clip must exist in the asset for proper leg animation.
- Additional licensed example sources (downloads may require login):
  https://sketchfab.com/3d-models/realistic-woman-walking-animated-d1ce2b8009b8401481408aeecea903d0
  https://sketchfab.com/3d-models/scanned-animated-walking-man-bfbb95c3cb2b4e89a20d3d201b05045f

SOURCES, LICENSES AND REQUIRED CREDIT
"Old Man" by Bake3D (https://bake3d.ai/free-3d-models), CC BY 4.0
https://creativecommons.org/licenses/by/4.0/
Reference FBX: https://bake3d.ai/samples/free/bake3d-person-walk.fbx
Reference GLB: https://bake3d.ai/samples/free/bake3d-person-animated.glb
The Spectral GT RS was released by its creator into public domain CC0 1.0:
https://github.com/JaronKBragg7337/spectral-gt-rs
https://creativecommons.org/publicdomain/zero/1.0/
Car models from DriveReadyCoupe are from the original previously provided user package.
Other Sketchfab models have their own CC BY credit requirements.

LIMITATIONS / TEST STATUS
The Unity .unitypackage is structurally checked, but not Play-mode tested in
Unity itself. The external source hosts, GLB importer, rig animation mapping,
render pipeline and download availability should be verified in your editor.

This package intentionally avoids low-poly placeholders. If an external
model cannot be obtained, its prefabs will not be generated rather than
silently replacing it with blocky assets.
