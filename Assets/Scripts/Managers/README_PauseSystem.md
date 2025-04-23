# Pause System Setup Guide

This guide will help you set up the pause system in your Unity game.

## Setup Steps

### 1. Create the Pause Manager GameObject

1. Create a new empty GameObject in your scene
2. Name it "PauseManager"
3. Add the `PauseManager` script to it

### 2. Create the Pause Menu UI

1. Create a new UI Canvas in your scene (Right-click in Hierarchy > UI > Canvas)
2. Add a Panel to the Canvas (Right-click on Canvas > UI > Panel)
3. Name the Panel "PauseMenuPanel"
4. **Important**: Tag the Panel as "PauseMenu" (Select the Panel > Inspector > Tag dropdown > Add Tag > Create new tag "PauseMenu" > Apply the tag)
5. Position the panel in the center of the screen
6. Add the `PauseMenuUI` script to the PauseMenuPanel

### 3. Add Buttons to the Pause Menu

1. Add three buttons to the PauseMenuPanel (Right-click on PauseMenuPanel > UI > Button)
2. Name them "ResumeButton", "MainMenuButton", and "QuitButton"
3. Position them vertically in the center of the panel
4. Set their text to "Resume", "Main Menu", and "Quit" respectively
5. In the Inspector, assign these buttons to the corresponding fields in the PauseMenuUI component

### 4. Connect the PauseManager and PauseMenuUI

1. Select the PauseManager GameObject
2. In the Inspector, assign the PauseMenuPanel to the "Pause Menu Panel" field
3. Set the "Main Menu Scene Name" field to the name of your main menu scene

### 5. Test the Pause System

1. Press the Escape key to toggle the pause menu
2. Verify that:
   - The game pauses (time stops)
   - The pause menu appears
   - The Resume button unpauses the game
   - The Main Menu button loads the main menu scene
   - The Quit button exits the game

### 6. Scene Reloading (Restart)

For the pause system to work correctly when restarting the game:

1. Make sure your PauseMenuPanel has the "PauseMenu" tag in every scene where you want the pause menu to appear
2. If you're using scene reloading for restart, the PauseManager will automatically find the pause menu panel in the new scene

## Customization Options

- **Pause Key**: Change the key used to toggle the pause menu in the PauseManager Inspector
- **Start Paused**: Enable this if you want the game to start in a paused state
- **Main Menu Scene Name**: Set this to the name of your main menu scene
- **Pause Menu Tag**: Change the tag used to find the pause menu panel in each scene

## Integration with Other Systems

To make other systems aware of the pause state:

1. Access the PauseManager through its singleton instance: `PauseManager.Instance`
2. Check if the game is paused: `PauseManager.Instance.IsPaused()`
3. Pause/resume the game: `PauseManager.Instance.PauseGame()` / `PauseManager.Instance.ResumeGame()`

## Troubleshooting

- If the pause menu doesn't appear, check that the PauseMenuPanel is assigned to the PauseManager
- If buttons don't work, verify that they are assigned to the PauseMenuUI component
- If the game doesn't pause, ensure that the PauseManager is in the scene
- If the pause menu doesn't appear after restarting, make sure your PauseMenuPanel has the "PauseMenu" tag 