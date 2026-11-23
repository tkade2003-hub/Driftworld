# Driftworld

A 3D mobile exploration game: glide between floating islands, hunt creatures, harvest parts, craft gear. The full plan lives in design/ (concept.md is the concept and plan, design/gdd/ holds the mechanic specs, design/market-research/ holds the market brief).

## How builds work here

No machine on this project runs Unity locally. This repo is a Unity project (Unity 6) and every build is a cloud build through GitHub Actions and game-ci:

- Every push to main builds the WebGL lane and publishes it to GitHub Pages. Play the current build from the Pages link in any browser. This lane checks loops, layout, and flow.
- The iOS lane runs on demand from the Actions tab (workflow_dispatch) and produces the Xcode project as a build artifact, on the road to TestFlight. The founder holds an Apple Developer account, so the milestone lane is open.
- Repo secrets the workflow needs: UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD (the founder Unity account). Nothing else.

Build entry points live in Assets/Editor/BuildSetup.cs. The first build creates the main scene in code; Assets/Scripts/Core/Bootstrap.cs builds the greybox world at load, so the repo carries no hand saved scene or art assets for the slice.

## Slice build status

Build order and pipeline rules: design/gdd/slice-1-build-order.md.

- Step 1, MECH-001 drag movement: code written 2026-11-22 (Assets/Scripts/Input/DragInput.cs, Assets/Scripts/Player/PlayerController.cs, Assets/Scripts/UI/TouchRipple.cs, tuning in Assets/Scripts/Tuning/Tuning.cs). Not yet compiled; the first cloud build is the first compile check.
- Step 3, MECH-002 glide and wind lanes: code written the same day inside PlayerController plus Assets/Scripts/World/WindLane.cs, ahead of its slot because movement and glide share the controller; it still gets its own spec pass in build order.
- MECH-008 camera: follow basics written (Assets/Scripts/Camera/FollowCamera.cs); the full framing rules land at step 2 in the build order.

## Founder steps to the first playable build

1. Create the GitHub repo named driftworld (the GitHub App cannot create repos) and say the word; the project gets pushed and the first cloud build runs.
2. Add the three Unity secrets to the repo when prompted; the license comes from a free Unity account.
