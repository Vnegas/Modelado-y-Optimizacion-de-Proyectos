using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Agent;
using System;
using System.Diagnostics.Tracing;
using JetBrains.Annotations;
using UnityEditor;
using Unity.VisualScripting;
using System.Linq;

public class AgentManager : MonoBehaviour
{
    // Atributes
    public static int numberAgentsInSim = 0; int totalAgent = 0;
    public GameObject agentPrefab;
    public int numberOfAgents;
    [SerializeField] public static int pointsBetweenGoals = 10;
    private float stoppingDistance = 0f; // Define una distancia de parada
    private static int numberOfQueues = 3;
    public static int totalPoints = pointsBetweenGoals * numberOfQueues;
    public static List<AgentController> allAgents = new List<AgentController>();
    public static Vector3[] pointsInQueueArray = new Vector3[(pointsBetweenGoals * numberOfQueues)];
    public static int[] arrayBusyPositions = new int[(pointsBetweenGoals * numberOfQueues)];

    // Setters for simulation (input)
    public static float mu = 40f;
    public static float lambda = 10f;
    public static int s = 3;

    // Guardar los puntos donde estaran los servers, maximo 10 servers
    static readonly int maxNumberOfServers = 10;
    public static Vector3[] pointsToServers = new Vector3[maxNumberOfServers];
    public static int[] stateServers = new int[s];

    // Goal points
    GameObject QueueTop;
    GameObject goal;
    GameObject goal3;
    GameObject goal4;
    GameObject goal5;

    // Counters for average
    int cantPayed = 0;
    private List<float> timeEntry = new List<float>();
    private List<float> timeQueue = new List<float>(); private List<float> timeShoping = new List<float>();
    private List<double> timePaying = new List<double>(); private List<float> timeEntryQueue = new List<float>();
    private List<float> timeTotal = new List<float>(); private List<float> timeExit = new List<float>();

    // Stats
    private static int cantAgentActive = 0; private static float averageAgentEnterSim = 0f; // 1
    public static int cantAgentExitSim = 0; private static float averageAgentExitSim = 0f; // 2
    private static int cantAgentShoping = 0; // 3
    private static int cantAgentInQueue = 0; private static float averageAgentInQueue = 0f; // 4
    private static float averageAgentPayed = 0f; // 5
    private static float averageTimeInEntry = 0f; private static float averageTimeInExit = 0f; // 9 y 10
    private static float averageTimeEntryQueue = 0f; // 11
    private static float averageTotalTimeAgent = 0f; // 12
    private static float averageTimeQueue = 0f; private static float averageTimeShoping = 0f; // 13 y 14
    private static double averageTimePaying = 0f; // 15

    float creationTime = 0f;
    [SerializeField] float creationRandom = 0f;

    // Text on HUB
    [SerializeField] Text agentActiveTxt; [SerializeField] Text avgEnterSimTxt; // 1
    [SerializeField] Text agentExitTxt; [SerializeField] Text avgExitSimTxt; // 2
    [SerializeField] Text activeShopingTxt; // 3
    [SerializeField] Text activeQueueTxt; [SerializeField] Text avgActiveQueueTxt; // 4
    [SerializeField] Text avgAgentPayedTxt;
    [SerializeField] Text avgEntryTimeTxt; [SerializeField] Text avgExitTimeTxt; // 9 - 10
    [SerializeField] Text avgEntryQueueTxt; [SerializeField] Text avgAgentTimeSimTxt; // 11 - 12
    [SerializeField] Text avgQueueTimeTxt; [SerializeField] Text avgShopingTimeTxt; //13 - 14
    [SerializeField] Text avgPayTimeTxt; // 15
    float randomNewQueueAgent = 0f;

    void Start() {

        // Crear los puntos entre los goals para la cola 1
        QueueTop = GameObject.Find("QueueTop");
        goal = GameObject.Find("Goal");
        goal3 = GameObject.Find("Goal3");
        goal4 = GameObject.Find("Goal4");
        goal5 = GameObject.Find("Goal5");

        float distance = goal.transform.position.x + (QueueTop.transform.position.x * -1);
        float distanceBetweenAgents = distance / (pointsBetweenGoals - 1);

        // El resto de puntos se establecen de acuerdo a la cantidad de puntos que se eligieron
        // Para la cola con zig zag
        int point = 0;

        PointsLeft(goal, pointsInQueueArray, ref point, distanceBetweenAgents, pointsBetweenGoals);
        PointsRight(goal3, pointsInQueueArray, ref point, distanceBetweenAgents, pointsBetweenGoals * 2);
        PointsRight(goal4, pointsInQueueArray, ref point, distanceBetweenAgents, pointsBetweenGoals * 3);

        // Generar los puntos de posicion para los servers
        Vector3 startPoint = new Vector3(8f, 0.78f, -18f);
        Vector3 endPoint = new Vector3(23f, 0.78f, -18f);
        pointsToServers = GeneratePointsToServers(startPoint, endPoint);

        // Generar el nuevo orden para los puntos: deben ir del centro hacia los lados
        pointsToServers = ChangeOrderServerPoints(pointsToServers);

        // Generar los game objects para los servers en el orden especificado
        GenerateGameObjects(s, pointsToServers);

        creationRandom = UnityEngine.Random.Range(1f, 3f);
        float desviacion = UnityEngine.Random.Range(0f, 10f);
        randomNewQueueAgent = lambda + desviacion;
    }

    public static Vector3[] ChangeOrderServerPoints(Vector3[] points) {
        int sumRight = (points.Length / 2) + 1; // 6
        int substractLeft = (points.Length / 2) - 1; // 4

        Vector3[] result = new Vector3[maxNumberOfServers];

        result[0] = points[points.Length / 2]; // En la mitad del result pongo el primero del viejo orden

        for (int i = 1; i < maxNumberOfServers; i++) {
            if (i % 2 == 0) { // par
                if (sumRight < maxNumberOfServers)
                {
                    result[i] = points[sumRight];
                    sumRight += 1;
                }

            } else { // impar
                if (substractLeft >= 0) {
                    result[i] = points[substractLeft];
                    substractLeft -= 1;
                }
            }
        }
        return result;
    }

    public static void GenerateGameObjects(int numberOfServers, Vector3[] pointsToServers) {
        for (int i = 0; i < numberOfServers; i++) {
            // Crear un nuevo GameObject
            GameObject newObject = new GameObject("Server" + i);

            // Asignar una posicion aleatoria
            newObject.transform.position = pointsToServers[i];

            // Asignar el icono con el nombre del server y numero
            var iconContent = EditorGUIUtility.IconContent("CacheServerConnected@2x");
            EditorGUIUtility.SetIconForObject(newObject, (Texture2D)iconContent.image);
        }
    }

    // Genera puntos para ubicar los servers, a partir de un espacio horizontal delimitado
    public static Vector3[] GeneratePointsToServers(Vector3 pointToStart, Vector3 pointToEnd) {
        Vector3[] pointsServers = new Vector3[maxNumberOfServers];
        if (pointToStart.z == pointToEnd.z) {
            // Generar los puntos acorde a la cantidad de servers
            // Point to end debe ser mayor en x que start.
            float distanceBetweenLimits = pointToEnd.x - pointToStart.x;
            float distanceBetweenServers = distanceBetweenLimits / (maxNumberOfServers-1);
            float point = pointToStart.x;
            for (int i = 0; i < maxNumberOfServers; i++) {
                pointsServers[i] = new Vector3(point, pointToStart.y, pointToStart.z);
                point += distanceBetweenServers;
            }
        }

        return pointsServers;
    }

    void PointsRight(GameObject goal1, Vector3[] pointsInQueueArray, ref int point, float distanceBetweenAgents, int pointsBeetween) {
        float nextPosInX = -1;
        if (pointsInQueueArray[point-1] == goal1.transform.position) {
            nextPosInX = goal1.transform.position.x + distanceBetweenAgents;
            
        } else {
            nextPosInX = goal1.transform.position.x;
        }

        for (int iterator = point; point < pointsBeetween; iterator++) {
            pointsInQueueArray[iterator] = new Vector3(nextPosInX,
                goal1.transform.position.y, goal1.transform.position.z);
            nextPosInX += distanceBetweenAgents;
            point++;
        }
    }
    void PointsLeft(GameObject goal1, Vector3[] pointsInQueueArray, ref int point, float distanceBetweenAgents, int pointsBeetween) {
        float nextPosInX = -1;
        if (pointsInQueueArray[point + 1] == goal1.transform.position) {
            nextPosInX = goal1.transform.position.x - distanceBetweenAgents;
 
        } else {
            nextPosInX = goal1.transform.position.x;
        }
        
        for (int iterator = point; iterator < pointsBeetween; iterator++) {
            pointsInQueueArray[iterator] = new Vector3(nextPosInX,
                goal1.transform.position.y, goal1.transform.position.z);
            nextPosInX -= distanceBetweenAgents;
            point++;
        }
    }

    void Update() {
        timeEntry.Add(creationRandom);
        // Create new agent if necessary
        CreateAgent();
        // Control behavior of all agents
        ControlBehavior();
        // Check if agent need to be Recycled
        ReUseAgent();
        // Calculate Average
        CalculateAvg();
        // Update HUB
        UpdateHUB();
        // Check if new agent needed in queue
        GoToQueue();
    }

    public void CreateAgent() {
        Vector3 position = new Vector3(-9.84f, 0.5f, 26.17f);
        // Instantiate a new agents from the prefab and store them in the array
        if (numberAgentsInSim < numberOfAgents) {
            if (creationTime >= creationRandom) {
                GameObject newAgent = Instantiate(agentPrefab, position, Quaternion.identity);
                newAgent.name = "Sphere"; // Set the name of the instantiated GameObject to "Sphere"

                // Access the NavMeshAgent component and set it up if needed
                NavMeshAgent navMeshAgent = newAgent.GetComponent<NavMeshAgent>();
                if (navMeshAgent != null) {
                    // Set the stopping distance
                    navMeshAgent.stoppingDistance = stoppingDistance;
                }

                AgentController agentController = newAgent.GetComponent<AgentController>();
                allAgents.Add(agentController);

                agentController.Start();
                totalAgent++;
                numberAgentsInSim++;
                creationTime = 0;
                creationRandom = UnityEngine.Random.Range(5f, 30f);
            } else {
                creationTime += Time.deltaTime;
            }
        }
    }

    public void ReUseAgent() {
        foreach (AgentController agent in allAgents) {
            // If agent is state afk or deactivated (reset simulation)
            if (agent.currentState.Equals(AgentController.StateAgent.afk) ||
                    agent.isDeactive == true) {
                totalAgent++;
                agent.currentState = AgentController.StateAgent.reUse;
                creationTime = 0;
                creationRandom = UnityEngine.Random.Range(5f, 30f);
            } else {
                creationTime += Time.deltaTime;
            }
        }
    }

    public void ControlBehavior() {
        //Reset Counters
        cantAgentShoping = 0; cantAgentInQueue = 0;
        cantAgentActive = 0;

        foreach (AgentController agent in allAgents) {
            // Update Agent State
            agent.StateController();

            if (agent.hasPayed && !agent.checkPayed) {
                agent.checkPayed = true;
                ++cantPayed;
                timePaying.Add(agent.timerPaying);
            }

            if (agent.currentState.Equals(AgentController.StateAgent.walking)) {
                ++cantAgentActive;
                ++cantAgentShoping;
                timeShoping.Add(agent.timeInShop);
            }

            if (agent.currentState.Equals(AgentController.StateAgent.inQueue)) {
                ++cantAgentActive;
                ++cantAgentInQueue;
                timeQueue.Add(agent.timeInQueue);
            }

            if (agent.currentState.Equals(AgentController.StateAgent.reUse)) {
                if (!agent.checkExit) {
                    agent.checkExit = true;
                    --cantAgentActive;
                    timeExit.Add(agent.exitTimer);
                }
            }
            timeTotal.Add(agent.totalTimeSim);
        }
    }

    public void CalculateAvg() {
        averageAgentEnterSim = (float) cantAgentActive / totalAgent;
        averageAgentExitSim = (float) cantAgentExitSim / totalAgent;
        averageAgentInQueue = (float) cantAgentInQueue / cantAgentActive;
        averageAgentPayed = (float) cantPayed / totalAgent;
        if (timeEntry.Count > 0) {
            averageTimeInEntry = timeEntry.Average();
        }
        if (timeExit.Count > 0) {
            averageTimeInExit = timeExit.Average();
        }
        if (timeEntryQueue.Count > 0) {
            averageTimeEntryQueue = timeEntryQueue.Average();
        }
        if (timeTotal.Count > 0) {
            averageTotalTimeAgent = timeTotal.Average();
        }
        if (timeQueue.Count > 0) {
            averageTimeQueue = timeQueue.Average();
        }
        if (timeShoping.Count > 0) {
            averageTimeShoping = timeShoping.Average();
        }
        if (timePaying.Count > 0) {
            averageTimePaying = timePaying.Average();
        }
    }

    public void UpdateHUB() {
        agentActiveTxt.text = "" + cantAgentActive;
        avgEnterSimTxt.text = "" + averageAgentEnterSim;
        agentExitTxt.text = "" + cantAgentExitSim;
        avgExitSimTxt.text = "" + averageAgentExitSim;
        activeShopingTxt.text = "" + cantAgentShoping;
        activeQueueTxt.text = "" + cantAgentInQueue;
        avgActiveQueueTxt.text = "" + averageAgentInQueue;
        avgAgentPayedTxt.text = "" + averageAgentPayed;
        avgEntryTimeTxt.text = "" + averageTimeInEntry;
        avgExitTimeTxt.text = "" + averageTimeInExit;
        avgEntryQueueTxt.text = "" + averageTimeEntryQueue;
        avgAgentTimeSimTxt.text = "" + averageTotalTimeAgent;
        avgQueueTimeTxt.text = "" + averageTimeQueue;
        avgShopingTimeTxt.text = "" + averageTimeShoping;
        avgPayTimeTxt.text = "" + averageTimePaying;
    }

    [SerializeField] float timerNewAgentInQueue = 0f;
    public void GoToQueue() {
        bool isAgent = false;
        foreach (AgentController agent in allAgents) {
            if (agent.currentState.Equals(AgentController.StateAgent.walking)) {
                isAgent = true;
                break; // **
            }
        }
        if (isAgent) {
            if (timerNewAgentInQueue >= randomNewQueueAgent) {
                timeEntryQueue.Add(timerNewAgentInQueue);
                int randomPos;
                bool found = false;
                while (!found) {
                    randomPos = UnityEngine.Random.Range(0, allAgents.Count());
                    if (allAgents[randomPos].currentState.Equals(AgentController.StateAgent.walking)) {
                        found = true;
                        allAgents[randomPos].currentState = AgentController.StateAgent.inQueue;
                        timerNewAgentInQueue = 0f;
                    }
                }
                float desviacion = UnityEngine.Random.Range(0f, 10f);
                randomNewQueueAgent = lambda + desviacion;
            } else {
                timerNewAgentInQueue += Time.deltaTime;
            }
        }
    }
}
