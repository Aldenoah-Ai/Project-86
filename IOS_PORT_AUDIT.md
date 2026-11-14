# Project-86 iOS Port — Initial Compatibility Audit

Branch: `ios-port`  
Base: `main` at `90e07c122547c57522214c054c1b6dadae3be817`  
Audit status: preliminary repository inspection; no Unity build or device test has been run.

## Goal

Prepare a faithful iOS adaptation of the existing Juggernaut gameplay. Preserve the original machines, movement, grappling, cannon and secondary weapons, cameras, scavenger interactions, missions, maps, and visual/audio identity wherever the existing systems and third-party licenses permit. This is a port, not a replacement game.

## Confirmed project facts

- Unity Editor version recorded by the project: **6000.4.3f1**.
- The project uses the Unity Input System package **1.19.0**.
- The project uses URP **17.4.0**, Cinemachine **3.1.6**, Netcode for GameObjects **2.11.0**, and Unity Services Multiplayer **2.1.3**.
- The existing `InputManager` translates input callbacks into gameplay events. This is a useful seam for adding touch input without rewriting the Juggernaut controller.
- `MechaController` already contains the existing first-person, cannon, third-person, and free-look camera modes and the core Juggernaut movement controller.
- `Assets/GameInputs.inputactions` currently defines keyboard/mouse bindings for the Juggernaut and other action maps. The inspected Juggernaut map has no touch-screen bindings.
- FMOD is included in the project. The repository tree currently shows FMOD native libraries for Windows, macOS, and Linux, but no iOS FMOD native library directory. FMOD iOS support/library packaging must be verified and supplied before an iOS build.
- Steamworks.NET native plugins in the repository are for Windows, macOS, and Linux. Steamworks must not be treated as an iOS-ready dependency; every runtime use needs to be isolated behind platform checks or replaced with an iOS-compatible service path.
- Multiplayer-related packages and Unity Services/Vivox are present. Their iOS support, authentication/session flow, voice requirements, and current service configuration need a separate validation pass.

## Main workstreams

### 1. Touch control layer
Create a mobile input layer that feeds the existing gameplay events:
- left virtual stick: movement;
- right-side drag: camera look;
- dedicated buttons: primary fire, secondary fire, jump/dash, grapple, reload, zoom, scavenger commands, view change, map/objectives, and pause;
- support simultaneous movement, camera look, and firing;
- safe-area-aware layout, adjustable sensitivity, button sizing, and optional control repositioning.

Keep keyboard/mouse input working on PC. Do not replace or remove existing input actions.

### 2. Platform dependency audit
Search all runtime code for Steamworks, FMOD, desktop-only APIs, file paths, cursor locking, and platform-specific assumptions. Classify each dependency as:
- works on iOS as-is;
- requires an iOS package/native library or configuration;
- requires a platform-specific implementation;
- optional and can be disabled on iOS.

### 3. Rendering and performance
Inspect URP settings, shaders, visual effects, post-processing, textures, shadows, and scene complexity. Establish a scalable iOS quality profile while preserving the original look as far as device performance allows. Do not reduce gameplay features without evidence that a specific device limitation requires it.

### 4. Build and device validation
Check scenes, player settings, signing requirements, permissions, orientation, memory use, thermal behavior, frame pacing, and network behavior. A real iOS build requires Unity's iOS Build Support and a macOS/Xcode environment; repository inspection alone cannot verify a working app.

## First implementation sequence

1. Complete runtime dependency and platform API inventory.
2. Design and implement touch controls as an additive input path.
3. Add an iOS platform configuration without breaking desktop builds.
4. Resolve FMOD and Steamworks platform dependencies.
5. Validate networking and voice support for iOS.
6. Build and test on a real iPhone; tune performance from measured results.

## Guardrails

- Keep all work on `ios-port`; leave `main` untouched.
- Preserve existing gameplay code unless a specific iOS incompatibility requires a change.
- Avoid placeholder machines, substitute gameplay, or a simplified clone.
- Do not claim an iOS build works until it has been built and tested on hardware.
- Record every unverified assumption and every platform-specific change.
