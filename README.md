# Deep Reinforcement Learning for Maze Navigation

> **Project**
> <br />
> **Course Unit:** [Topics in Intelligent Robotics 2024/2025](https://sigarra.up.pt/feup/en/UCURR_GERAL.FICHA_UC_VIEW?pv_ocorrencia_id=542590 "Course Link")
> <br />
> **Course:** Master's in Artificial Intelligence (MIA)
> <br />
> **Faculty:** Faculty of Engineering & Faculty of Science, University of Porto
> <br />
> **Report:** [overleaf](https://www.overleaf.com/read/kjjzxfkhwqrh#677a07)
---

## Project Goals

This project extends the classic ball-and-plate control problem by adding maze navigation. The goal was to develop an agent that could guide a ball through generated mazes on a tilting platform. Rather than solving just one static maze, the focus was on creating agents that could generalize across different maze configurations.

**The work progressed through three levels of complexity:**

* **Level 1 – Plate Balancing:** At this initial stage, the agent learned to control the platform to keep the ball stable and prevent it from falling off the plate.
* **Level 2 – Target Navigation:** The task became more complex as the agent was required to move the ball to a specific coordinate on the plate, demonstrating controlled directional movement.
* **Level 3 – Maze Navigation:** The final and most challenging level involved guiding the ball from a designated start point to the maze exit, navigating around walls and obstacles. We explored two main strategies for this task: a Hierarchical approach and an End-to-End approach.


<table>
  <tr>
    <td align="center">
      <img src="docs/baseline1.gif" alt="Level 1: Plate Balancing" width="250">
      <br>
      <em>Level 1: Plate Balancing</em>
    </td>
    <td align="center">
      <img src="docs/baseline2.gif" alt="Level 2: Target Navigation" width="250">
      <br>
      <em>Level 2: Target Navigation</em>
    </td>
    <td align="center">
      <img src="docs/maze.gif" alt="Level 3: Maze Navigation" width="250">
      <br>
      <em>Level 3: Maze Navigation</em>
    </td>
  </tr>
</table>

*Figure 1: The agent's performance across the three levels of complexity: balancing the ball (left), navigating to a target (center), and solving a complete maze (right).*
## Technical Approach

The project's architecture was designed to be modular and extensible, using a **Strategy design pattern** in C# within the Unity environment. This decouples the agent's core logic from its decision-making process, allowing for seamless switching between different algorithmic approaches. The two primary strategies implemented are `HierarchicalStrategy` and `EndToEndStrategy`. Further technical documentation and implementation details are available on the project's [deepWiki](https://deepwiki.com/Minipoloalex/maze-solver-rl).

<p style="text-align:center;">
  <img src="docs/strategyPattern.png" alt="Architectural diagram of the Strategy pattern implementation" width="450">
  <br>
  <small>
    <em>Figure 2: Architecture implementing the Strategy pattern. The main agent delegates control, allowing it to switch between a direct EndToEndStrategy and a HierarchicalStrategy that combines an A planner with a low-level RL controller.*</em>
  </small>
</p>

### Baseline Tasks: Foundational Control

Before tackling the full maze, we established baselines with simpler tasks.  For both, we used a **PPO (Proximal Policy Optimization)** agent.

1.   **Plate Balancing:** The agent's goal was simply to keep the ball on the plate. It received a small positive reward for every step the ball remained on the plate and a penalty for falling. The agent quickly learned an effective stabilization policy, achieving a near-perfect success rate.
2.   **Balance to Target:** This task required the agent to move the ball to a random target coordinate. The reward was inversely proportional to the distance to the target, encouraging the agent to get closer. The agent successfully learned to perform this goal-directed navigation.

### Hierarchical Maze Navigation

 This approach decomposes the problem into two distinct components: a high-level planner and a low-level controller. 

  *  **High-Level Planner (A\* Algorithm):** We used the classic **A\*** search algorithm as the planner (`AStarPlanner`). It operates on the discrete maze structure to compute a guaranteed optimal sequence of waypoints from the start to the exit before the agent begins to move.
  *  **Low-Level Controller (PPO Agent):** A PPO-based reinforcement learning agent (`RLController`) is responsible for the continuous control task of navigating the ball between the waypoints provided by the planner. This agent learns the complex physics-based manipulation through trial and error, focusing only on point-to-point navigation.

The agent's learning is guided by a carefully engineered multi-component reward function:
 $R\_{total} = R\_{dir} + R\_{time} + R\_{waypoint}$

  *  **Directional Reward ($R\_{dir}$):** A shaping reward calculated from the dot product of the ball's velocity and the direction to the target waypoint, encouraging movement in the correct direction. 
  *  **Dynamic Time Penalty ($R\_{time}$):** A small negative reward at each step to incentivize speed. It is scaled based on maze complexity to ensure fairness.
  *  **Waypoint Achievement Reward ($R\_{waypoint}$):** A sparse, positive reward of +1.0 is given for reaching each waypoint, providing a clear signal for sub-goal completion. 

<p style="text-align:center;">
  <img src="docs/hierarchical_reward_flow.png" alt="Flowchart of the reward function for the hierarchical agent" width="450">
  <br>
  <small>
    <em>Figure 3: Flowchart of the hierarchical reward function. Dense rewards guide the agent towards a waypoint, while a large sparse reward is given for reaching it.</em>
  </small>
</p>

### End-to-End Maze Navigation

 This strategy uses a single, monolithic reinforcement learning policy to handle the entire navigation task autonomously. 

  *  **Unified Policy:** The agent learns both high-level planning and low-level control simultaneously.
  * **Advanced Architecture:** This approach requires more sophisticated observations and a more complex network. We used:
      *  A **Convolutional Neural Network (CNN)** to process a 20x20 grid representing the local wall layout around the ball, providing the agent with "spatial vision." 
      *  A **Long Short-Term Memory (LSTM)** unit to give the agent short-term memory, enabling it to better handle the partially observable nature of the environment by tracking its movement history. 
  *  **Observations:** The agent integrates multiple data sources, including its tilt angles, velocity, the difference in grid cells to the goal, its precise position within a cell, and the distance to the goal pre-calculated using a Breadth-First Search (BFS).
  *  **Reward Function:** The reward is primarily driven by reaching the goal (+10) or failing (-1).  To guide its learning, the agent receives a continuous small negative reward based on its distance to the goal, encouraging it to make progress. 

 This approach achieved a notable success rate of 80.4% in testing. 

## Running the Code

**Setup:**

```bash
# Clone the repository
git clone https://github.com/Minipoloalex/maze-solver-rl.git
cd maze-solver-rl
```

**Prerequisites:**

  * Python 3.10.12
  * Unity Hub
  * Unity Editor 6.1

**Running the Project:**

1.  (Optional) Install the required Python packages for training new models: `pip install -r requirements.txt`.  This step is not required to run pre-trained models.
2.   Open the project in the Unity Hub using the specified Unity Editor version. 
3.  Navigate to the `Assets/Scenes` folder and select the desired scene.
4.  The project is pre-configured to run with a trained model. Press the **Play** button in the Unity Editor to start the simulation.

To switch between the **Hierarchical** and **End-to-End** strategies, select the correspondent `agent prefab GameObject` in the scene.


### Tutorial: Switching Agent Strategies

You can easily switch between the `Hierarchical` and `End-to-End` agents in the Unity Editor before running the simulation.

1.  In the **Hierarchy** window, select the `Maze` GameObject. This GameObject contains the `Maze Spawner (Script)` component.
2.  In the **Inspector** window, find the `Maze Spawner` component.
3.  Locate the **Platform Agent Prefab** field. This field determines which agent will be spawned.
4.  In the **Project** window, navigate to the `Assets/Prefabs` folder.
5.  Drag either the `HierarchicalAgent` or `EndToEndAgent` prefab from the `Assets/Prefabs` folder into the **Platform Agent Prefab** slot in the Inspector.
6.  Press **Play** to run the simulation with the selected agent.

<p style="text-align:center;">
  <img src="docs/tutorial_change_agent_prefab.png" alt="Switching agent prefabs in the Unity Inspector" width="700">
  <figcaption>
    <small><em>Figure 4: To change the agent, select the <b>Maze</b> GameObject and drag the desired agent prefab (e.g., <b>HierarchicalAgent</b>) into the <b>Platform Agent Prefab</b> field of the Maze Spawner script.</em></small>
  </figcaption>
</p>

## Tech Stack

Python, C\#, Unity, Unity ML-Agents, PPO

## Team

  * Adriano Machado (202105352)
  * Félix Martins (202108837)
  * Francisco da Ana (202108762)
  *  Martim Iglesias (202005380)
