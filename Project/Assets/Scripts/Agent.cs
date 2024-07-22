using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PositionInLine;
using static Agent.AgentController;
using static UnityEditor.PlayerSettings;
using Unity.VisualScripting;
// using Simulation;

namespace Agent
{
    public class AgentController : MonoBehaviour
    {
        // Estados
        public enum StateAgent
        {
            entry,
            walking,
            inQueue,
            paying,
            afk,
            reUse
        }

        // Server elegido
        public int numberOfServerChoosed = -1;
        // Estado actual
        public StateAgent currentState;
        NavMeshAgent Sphere;
        // Guarda el tiempo que tarda revisando estantes
        float randomTimeChecking;
        // Guarda el tiempo que ocupa para revisar estantes
        float timeElapsedChecking = 0f;
        public float timerPaying = 0f;
        public float randomTimeToPay = 0f;
        public float randomTimeInQueue;
        int randomInterest;
        GameObject entry;
        // Puntos de interes
        GameObject[] interests;

        // Render
        Renderer[] render;

        public bool hasPayed = false; public float exitTimer = 0f;
        public float entryQueueTimer = 0f; bool hasCalculateTimeQ = false;
        public float timeInQueue = 0f; public float timeInShop = 0f;
        public float totalTimeSim = 0f;
        bool reuse = false; float waitRandomTimer = 0f;

        public void Start() {
            currentState = StateAgent.entry;
            Sphere = GetComponent<NavMeshAgent>();

            randomTimeChecking = Random.Range(2f, 10f);

            // La clase compartida debe saber donde esta el goal inicial, queda en nextPointInQueue
            PositionInLineController.Start();
            entry = GameObject.Find("EntryPoint");
            randomTimeInQueue = Random.Range(30f,60f);
            randomTimeToPay = GenerateMu();

            // Encuentra todos los objetos con la etiqueta "Interest"
            interests = GameObject.FindGameObjectsWithTag("Interests");

            // Inicializa el vector de posiciones ocupadas
            PositionInLineController.SetOcupiedSize(interests.Length + 1);
            
            // Obtén todos los componentes de renderizado en el objeto
            render = GetComponentsInChildren<Renderer>();
            waitRandomTimer = Random.Range(5f, 30f);
        }

        // Update is called once per frame
        public void StateController() {
            switch (currentState) {
                case StateAgent.entry:
                    EntryHandler();
                    break;
                case StateAgent.walking:
                    totalTimeSim += Time.deltaTime;
                    WalkingHandler();
                    break;
                case StateAgent.inQueue:
                    totalTimeSim += Time.deltaTime;
                    InQueueHandler();
                    break;
                case StateAgent.paying:
                    totalTimeSim += Time.deltaTime;
                    Paying();
                    break;
                case StateAgent.afk:
                    break;
                case StateAgent.reUse:
                    ReuseHandler();
                    break;
            }
        }

        // Set estados
        public void SetState( StateAgent newState ) {
            this.currentState = newState;
        }

        public static float GenerateMu() {
            // Generar desviacion estandar
            float desviacion = Random.Range(0f,4f);

            // Escalar y trasladar para obtener la distribución normal deseada
            return AgentManager.mu + desviacion;
        }
        
        float waitElapsed = 0f;
        public void EntryHandler() {
            if (reuse) {
                Vector3 warpPos = new Vector3(-9.84f, 0.5f, 30.17f);
                Sphere.Warp(warpPos);
                ClearVariables();
                reuse = false;
            }

            if (isDeactiveCheck) {
                if (waitElapsed >= waitRandomTimer) {
                    isDeactiveCheck = false;
                    waitRandomTimer = Random.Range(5f, 30f);
                } else {
                    waitElapsed += Time.deltaTime;
                }
            } else {
                Sphere.SetDestination(entry.transform.position);
                if (!Sphere.pathPending && Sphere.remainingDistance <= 3) {
                    totalTimeSim += Time.deltaTime;
                    Vector3 centerPos = new Vector3(4.72f, 0.5f, 5.98f);
                    Sphere.SetDestination(centerPos);
                    SetState(StateAgent.walking);
                }
            }
        }
        
        bool firstWalk = true;
        public void WalkingHandler() {
            timeInShop += Time.deltaTime;
            if (firstWalk) {
                firstWalk = false;
                bool found = false;
                while (!found) {
                    randomInterest = Random.Range(0, interests.Length);
                    if (PositionInLineController.TrySetOcupied(randomInterest)) {
                        found = true;
                    }
                }
                Sphere.SetDestination(interests[randomInterest].transform.position); 
            } else {
                if (timeElapsedChecking >= randomTimeChecking) {
                    PositionInLineController.SetFree(randomInterest);
                    timeElapsedChecking = 0f;
                    randomTimeChecking = Random.Range(2f, 10f);

                    bool found = false;
                    while (!found) {
                        randomInterest = Random.Range(0, interests.Length);
                        if (PositionInLineController.TrySetOcupied(randomInterest)) {
                            found = true;
                        }
                    }
                    Sphere.SetDestination(interests[randomInterest].transform.position); 
                } else {
                    timeElapsedChecking += Time.deltaTime;
                }
            }
        }

        int GetMyPosition() {
            for (int i = AgentManager.totalPoints - 1; i >= 0; i--) {
                if (AgentManager.arrayBusyPositions[i] == 1) {
                    return i + 1;
                }
            }
            return 0;
        }

        bool havePos = false;
        bool firstTime = true;
        [SerializeField] int pos = 0;
        public void InQueueHandler() {
            timeInQueue += Time.deltaTime;
            if (!hasCalculateTimeQ) {
                entryQueueTimer = Time.deltaTime;
            } else {
                hasCalculateTimeQ = true;
            }

            if (firstTime == true) {
                pos = GetMyPosition();
                Sphere.SetDestination(AgentManager.pointsInQueueArray[pos]); // Se le da un punto para que se mueva hacia ahi
                AgentManager.arrayBusyPositions[pos] = 1;
                firstTime = false;
                havePos = true;
            }

            if (havePos == false) {
                if (pos > 0) {
                    AgentManager.arrayBusyPositions[pos] = 0;
                    pos--;
                    AgentManager.arrayBusyPositions[pos] = 1;
                }
                havePos = true;
                Sphere.SetDestination(AgentManager.pointsInQueueArray[pos]); // Se le da un punto para que se mueva hacia ahi
            }

            if (havePos == true && pos > 0) {
                if (AgentManager.arrayBusyPositions[pos - 1] == 0) {
                    havePos = false;
                }
            }

            // Ahora: Si estoy al frente y llegue a mi punto al frente de la fila
            // no hago nada mas que esperar a que algun server se desocupe para ir
            if (Sphere.remainingDistance <= 2 && pos == 0) { // pos = 0 => al frente de la fila
                Vector3 myServerChoosed = ChooseMyServer();
                if (myServerChoosed != Vector3.zero) { // Si es dif de 0,0,0 es que habia uno desocupado
                    AgentManager.arrayBusyPositions[pos] = 0; // Indico que mi pos la dejo desocupada antes de irme

                    Sphere.SetDestination(myServerChoosed);
                    SetState(StateAgent.paying);
                }
            }
        }

        public bool checkPayed = false;
        public void Paying() {
            if (timerPaying >= randomTimeToPay) {
                hasPayed = true;
                // Tiempo que permanezco pagando
                randomTimeToPay = GenerateMu(); // variable MU
                AgentManager.stateServers[this.numberOfServerChoosed] = 0;
                Sphere.SetDestination(GameObject.Find("OOB").transform.position); // Me voy fuera de camara
                ++AgentManager.cantAgentExitSim;
                SetState(StateAgent.afk);
            } else {
                timerPaying += Time.deltaTime;
            }
        }

        public bool checkExit = false;
        bool isDeactiveCheck = false;
        public void ReuseHandler() {
            exitTimer = totalTimeSim;
            reuse = true;
            if (!Sphere.pathPending && Sphere.remainingDistance <= 2) {
                if (isDeactive == true) {
                    foreach (Renderer renderer in render) {
                        renderer.enabled = true;
                    }
                    isDeactive = false;
                    isDeactiveCheck = true;
                }
                SetState(StateAgent.entry);
            }
        }

        public void ClearVariables() {
            hasPayed = false; exitTimer = 0f; entryQueueTimer = 0f;
            hasCalculateTimeQ = false; timeInQueue = 0f;
            timeInShop = 0f; havePos = false;
            totalTimeSim = 0f; firstWalk = true;
            timerPaying = 0f;
            numberOfServerChoosed = -1; firstTime = true; pos = 0;
            checkPayed = false; checkExit = false;
        }

        Vector3 ChooseMyServer() {
            Vector3 positionOfServerChoosed = Vector3.zero;
            for (int i = 0; i < AgentManager.s; i++) {
                if (AgentManager.stateServers[i] == 0) { // Significa que esta desocupado
                    AgentManager.stateServers[i] = 1;
                    positionOfServerChoosed = AgentManager.pointsToServers[i];
                    numberOfServerChoosed = i;
                    break;
                }
            }
            return positionOfServerChoosed;
        }
        
        public bool isDeactive = false;
        public void Deactivate() {
            // Clear where Agent was
            if (havePos) {
                AgentManager.arrayBusyPositions[pos] = 0;
            }
            if (numberOfServerChoosed >= 0) {
                AgentManager.stateServers[this.numberOfServerChoosed] = 0;
            }
            // Set flag
            isDeactive = true;
            // Make render components invisible
            foreach (Renderer renderer in render) {
                renderer.enabled = false;
            }
        }
    }
}



