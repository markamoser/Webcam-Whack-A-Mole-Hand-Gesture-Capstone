# Team-15-Gesture-based-Interactive-Game
#Webcam Whack-A-Mole
## Overview
This is a game developed as a capstone project by four senior computer science students at the University of Nebraksa at Omaha. We are trying to create a proof-of-concept for using a hand gesture captrue by a webcam which can sense the depth of the hands moving towards and away from the sensor. Future applications will further develop the proof-of-concept if successful to create VR like controls without requiring individuals to hold hand controls, specifically to help individuals rehabilitating from Traumatic Brain Injuries.

## Project Progress
We collaborated over Zoom, discord, and used Trello for task organization. The semester project is complete.

We completed five milestones.

## Branches
All demonstrations work in the main branch.

## Release Notes: 4/26/2026
Milestone 1 - we have a couple of libraries which capture the up, down, left, and right movements including MediaPipe Hands and a existing project template of the same library with modifications.
Milestone 2 - Complete now. We have built the whack-a-mole game and incorporated two hands from the MediaPipe Library into Unity.
Milestone 3 - The game now fully works but without the depth tracking capture. We have also demonstrated our depth capture features.
Milestone 4 - Fully functional depth tracking. 
Milestone 5 - All functional requirements inmplemented and game is complete.

## Setup Instructions:

### A. Requirements
- **Operating System:** Windows  
- **Unity Editor Version:** 2021.3.45f2  
  *(Exact version required — available via Unity Hub)*  
- **Hardware:** Webcam with at least 480p resolution *(higher recommended)*  

---

### B. Step 1: Download Source Code

#### Option A — Clone with Git
```bash
git clone https://github.com/markamoser/Webcam-Whack-A-Mole-Hand-Gesture-Capstone.git
````

**Option B — Download as ZIP:**
- Click the green button labeled: `< > Code` and select **Download ZIP**
- Extract the ZIP file after download

---

### C. Step 2: Import the Project in Unity
- Open Unity Hub
- Click **Add** → **Add project from disk**
- Navigate to and select the root folder of the cloned or unzipped project
- Ensure the Editor version is set to **2021.3.45f2**
- Open the project

---

### D. Step 3: Open the Game Scene
- In the Unity Editor, open the `Assets/Scenes` directory in the Project window
- Double-click **GamePrototype** to open the scene

---

### E. Step 4: Build and Run
- Go to **File → Build Settings**
- Ensure **GamePrototype** is listed under **Scenes In Build**
  - If not, click **Add Open Scenes**
- Click **Build** to produce a standalone executable
- Choose an output folder when prompted and wait for the build to complete

You can also run the game by pressing the **Play** button directly in Unity.