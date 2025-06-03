# Stormlight Echoes

A multiplayer action game set in the world of Roshar, inspired by the Stormlight Archive series. Players take on the role of characters with unique abilities, engaging in combat and exploration in a networked environment.

## Features

### Player Mechanics
- **Movement System**
  - Smooth horizontal movement
  - Double jump capability
  - Dash ability
  - Ground detection with coyote time
  - Jump buffering for responsive controls

### Combat System
- **Attack System**
  - Directional attacks (side, up, down)
  - Attack hitboxes with visual feedback
  - Recoil mechanics on successful hits
  - Damage flash effects
  - Time slow effect on taking damage

### Spellcasting
- **Spell System**
  - Multiple spell types (side, up, down)
  - Mana management
  - Spell cooldowns
  - Healing spells
  - Unlockable abilities

### Networking
- **Multiplayer Features**
  - Networked player movement
  - Synchronized combat
  - Server authority for critical actions
  - Player spawning system
  - Network variable synchronization

### Technical Features
- **Animation System**
  - State-based animations
  - Networked animation triggers
  - Visual effects for actions
  - Damage feedback animations

- **Audio System**
  - Footstep sounds
  - Combat sound effects
  - Jump and attack audio feedback

## Development Setup

### Prerequisites
- Unity 2022.3 or later
- Netcode for GameObjects package
- Basic understanding of Unity networking

### Installation
1. Clone the repository
2. Open the project in Unity
3. Install required packages through Package Manager
4. Open the main scene

### Configuration
- Set up your network settings in the NetworkManager
- Configure player spawn points
- Adjust player controller parameters in the inspector

## Controls

### Movement
- A/D or Left/Right Arrow: Move horizontally
- Space: Jump
- Double tap Space: Double jump (when unlocked)
- Shift: Dash (when unlocked)

### Combat
- Left Click: Attack
- Right Click (hold): Heal
- Q: Cast side spell
- W: Cast up spell
- S: Cast down spell

## Development Notes

### Network Architecture
- Uses Unity's Netcode for GameObjects
- Server-authoritative design
- Client-side prediction for movement
- Network variables for state synchronization

### Player Controller
The `PlayerController` script handles:
- Movement physics
- Combat mechanics
- Spell casting
- Health and mana management
- Network synchronization
- Animation state management

### Performance Considerations
- Efficient network variable usage
- Optimized physics calculations
- State-based animation system
- Object pooling for effects

## Contributing

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Inspired by the Stormlight Archive series by Brandon Sanderson
- Built with Unity and Netcode for GameObjects
- Special thanks to the Unity community for networking resources

## Contact

For questions or collaboration, please open an issue in the repository.

---
*Note: This project is a fan creation and is not officially affiliated with the Stormlight Archive series or its creators.* 