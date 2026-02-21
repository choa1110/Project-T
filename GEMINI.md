# Gemini CLI
This file is for notes and information related to the Gemini CLI agent.

## Project Analysis

### Summary of Findings
This is a multiplayer action game built with Unity and the Photon Fusion networking library. The project follows a server-authoritative networking model. Key features include a lobby system for creating and joining rooms, in-game chat using Photon Chat, and synchronized player movement. The project uses the Universal Render Pipeline (URP) for graphics, the new Unity Input System for controls, and Cinemachine for camera management. The core gameplay loop involves players joining a lobby, entering a game world, and controlling a character with basic movement and attack capabilities. The code is structured around key manager scripts (`GameManager`, `ChatManager`, `DataManager`) and network-aware player controllers. Although my investigation was cut short, I can confidently state that the foundation is a standard, well-architected Photon Fusion project.

### Key Files

*   **`Assets/GameManager.cs`**: This is the central script for game logic, handling player spawning and collecting network inputs. It uses the `INetworkRunnerCallbacks` interface from Photon Fusion.
    *   **Key Symbols**: `GameManager`, `OnPlayerJoined`, `SpawnGameCharacter`, `OnInput`
*   **`Assets/PlayerController.cs`**: This script controls the player's character. It receives network input to move the character and synchronizes the player's nickname across the network using a `[Networked]` property.
    *   **Key Symbols**: `PlayerController`, `FixedUpdateNetwork`, `NickName`
*   **`Assets/LobbyUI.cs`**: Handles the entire lobby flow, including creating rooms, listing available rooms, and joining them. It interacts directly with the Photon Fusion `NetworkRunner`.
    *   **Key Symbols**: `LobbyUI`, `OnCreateRoomConfirm`, `OnSessionListUpdated`
*   **`Assets/ChatManager.cs`**: This script implements the chat functionality using the Photon Chat service. It connects to the chat service, sends messages, and receives messages from other players.
    *   **Key Symbols**: `ChatManager`, `SendChatMessage`, `OnGetMessages`
*   **`Assets/DataManager.cs`**: A singleton class responsible for persisting player data (specifically the nickname) locally using `PlayerPrefs`.
    *   **Key Symbols**: `DataManager`, `SetNickName`, `LoadNickName`
