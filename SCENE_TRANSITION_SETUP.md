# Scene Transition System - Hollow Knight Style

This system allows players to transition between scenes by walking through specific zones, similar to Hollow Knight's door transitions.

## Files Created

1. **SceneTransitionZone.cs** - Main script that handles the transition zones
2. **TransitionPromptUI.cs** - UI script for showing transition prompts
3. **SceneTransitionSetup.cs** - Helper script for easy setup in the editor
4. **CameraManager.cs** - Ensures camera follows player after transitions

## Quick Setup Guide

### Step 1: Add Scene Transition System to SampleScene

1. Open your **SampleScene** in Unity
2. Create an empty GameObject and name it "Scene Transition Manager"
3. Add the `SceneTransitionSetup` script to this GameObject
4. In the inspector, expand the "Transition Setups" array and add one element:
   - **Scene Name**: "Level2"
   - **Spawn Position**: Set to where the player should spawn in Level2 (e.g., (-10, 0, 0))
   - **Transition Prompt**: "Press E to enter Level2"

### Step 2: Set Up Camera Manager (Important!)

1. Create another empty GameObject and name it "Camera Manager"
2. Add the `CameraManager` script to this GameObject
3. This will ensure the camera always follows the player, especially after scene transitions

### Step 3: Create the UI System

1. With the Scene Transition Manager selected, click **"Create Transition Prompt UI"** in the inspector
2. This will create a Canvas with the transition prompt UI that appears when near transition zones

### Step 4: Create the Transition Zone

1. Click **"Create Transition Zones"** in the inspector
2. This will create a transition zone GameObject with a cyan wireframe box (indicating instant transition)
3. Position this zone where you want the transition to happen (e.g., at the edge of your level, near a door)
4. Adjust the Box Collider size to cover the area where the player should be able to trigger the transition

### Step 5: Set Up Level2 Scene

1. Open your **Level2** scene
2. Repeat steps 1-3 to add the transition system and camera manager
3. Create a transition back to SampleScene:
   - **Scene Name**: "SampleScene"
   - **Spawn Position**: Where the player should spawn when returning
   - **Transition Prompt**: "Press E to return to main area"

### Step 6: Add Both Scenes to Build Settings

1. Go to **File > Build Settings**
2. Add both SampleScene and Level2 to the build if they're not already there
3. Make sure SampleScene is at index 0

## Camera Fix

The **CameraManager** script automatically:
- ✅ Finds the main camera in each scene
- ✅ Adds CameraFollow component if missing
- ✅ Connects the camera to the local player
- ✅ Reconnects if the connection is lost
- ✅ Works with networking/multiplayer

This ensures your camera will always follow the player properly after scene transitions!

## Manual Setup (Alternative)

If you prefer to set up manually:

### Creating a Transition Zone Manually

1. Create an empty GameObject where you want the transition
2. Add a **Box Collider 2D** component and set it as a trigger
3. Add the **SceneTransitionZone** script
4. Configure the script parameters:
   - **Target Scene Name**: "Level2"
   - **Player Spawn Position**: Where the player spawns in the target scene
   - **Use Spawn Position**: Check this box
   - **Instant Transition**: Check this box for automatic teleportation
   - **Transition Delay**: 0.2 seconds (prevents accidental triggers)

### Creating the Camera Manager Manually

1. Create an empty GameObject in each scene
2. Add the **CameraManager** script
3. The script will automatically find and set up the camera

## How It Works

1. **Zone Detection**: When the player enters a transition zone, they automatically start transitioning
2. **Smooth Transition**: The screen fades to black, loads the new scene, then fades back in
3. **Player Positioning**: The player is moved to the specified spawn position in the new scene
4. **Camera Setup**: The camera is automatically reconnected to follow the player
5. **Networking**: Works with Unity Netcode for multiplayer games

## Key Features

- ✅ **Instant transitions** (no button press needed)
- ✅ **Automatic camera follow** after transitions
- ✅ Smooth fade transitions
- ✅ Customizable spawn positions
- ✅ Networking support
- ✅ Easy to set up and configure
- ✅ Hollow Knight-style seamless transitions

## Tips

1. **Zone Placement**: Place transition zones at natural boundaries like doors, cave entrances, or level edges
2. **Spawn Positioning**: Make sure spawn positions are safe and don't place players inside walls
3. **Camera Setup**: The CameraManager will handle camera issues automatically
4. **Multiple Transitions**: You can have multiple transition zones in one scene going to different areas

## Troubleshooting

- **Camera not following**: The CameraManager should fix this automatically. Check console for logs.
- **Transition not triggering**: Make sure the Box Collider 2D is set as a trigger and Instant Transition is checked
- **Player not spawning correctly**: Check that the spawn position is valid and not inside colliders
- **Networking issues**: Make sure both scenes have the same NetworkManager configuration

## Future Enhancements

You can extend this system by:
- Adding sound effects for transitions
- Creating custom transition animations
- Adding visual effects like particles
- Implementing different transition types (vertical, horizontal, etc.)
- Adding conditional transitions based on player progress 