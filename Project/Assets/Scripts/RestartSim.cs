using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using Agent;
using UnityEngine.AI;

public class Restart : MonoBehaviour {
    [SerializeField] TMP_InputField lambda;
    [SerializeField] public float lambdaSim = AgentManager.lambda;
    [SerializeField] TMP_InputField mu;
    [SerializeField] public float muSim = AgentManager.mu;
    [SerializeField] TMP_InputField s;
    [SerializeField] public int sSim = AgentManager.s;
    public void RestartGame() {
        bool restart = false; bool restartServer = false;
        float lmbdFloat = float.Parse(lambda.text);
        float muFloat = float.Parse(mu.text);
        int sInt = int.Parse(s.text);
        
        lambdaSim = AgentManager.lambda;
        muSim = AgentManager.mu;
        sSim = AgentManager.s;

        if (AgentManager.lambda != lmbdFloat) { // If input has changed
            // Update simulation setters
            AgentManager.lambda = lmbdFloat;
            lambdaSim = AgentManager.lambda;
            // Flag to restart simulation
            restart = true;
        }

        if (AgentManager.mu != muFloat) { // If input has changed
            // Update simulation setters
            AgentManager.mu = muFloat;
            muSim = AgentManager.mu;
            // Flag to restart simulation
            restart = true;
        }

        if (AgentManager.s != sInt) { // If input has changed
            // Update simulation setters
            AgentManager.s = sInt;
            sSim = AgentManager.s;
            // Flag to restart simulation
            restart = true;
            restartServer = true;
        } else if (sInt > 10) {
            Debug.Log("'S' no puede ser mayor a 10");
        }

        if (restart) {
            // Restart simulation for queue/paying Agents
            foreach (AgentController agent in AgentManager.allAgents) {
                agent.randomTimeToPay = AgentController.GenerateMu();

                if (agent.currentState.Equals(AgentController.StateAgent.inQueue)
                        || agent.currentState.Equals(AgentController.StateAgent.paying)) {
                    // Deactivate agent and send it back to start
                    agent.Deactivate();
                }
            }

            // Modify quantity of servers if needed
            if (restartServer) {
                Vector3 startPoint = new Vector3(8f, 0.78f, -18f);
                Vector3 endPoint = new Vector3(23f, 0.78f, -18f);
                AgentManager.pointsToServers = AgentManager.GeneratePointsToServers(startPoint, endPoint);

                // Generar el nuevo orden para los puntos: deben ir del centro hacia los lados
                AgentManager.pointsToServers = AgentManager.ChangeOrderServerPoints(AgentManager.pointsToServers);

                // Generar los game objects para los servers en el orden especificado
                AgentManager.GenerateGameObjects(AgentManager.s, AgentManager.pointsToServers);

                // Resize array of server occupied
                System.Array.Resize(ref AgentManager.stateServers, sInt);
            }
        }
    }
}