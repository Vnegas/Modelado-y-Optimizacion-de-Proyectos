using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PositionInLine {
    public class PositionInLineController : MonoBehaviour
    {
        public enum StateGoal
        {
            occupied,
            unoccupied
        }
        public static StateGoal currentStateGoalPoint;
        public static Vector3 nextPointInQueue;
        public static GameObject GoalPoint;
        public static bool newLine = false;
        public static bool pairLine = false;
        public static bool[] ocupied;

        // Start is called before the first frame update
        public static void Start()
        {
            GoalPoint = GameObject.Find("Goal");

            // Asignar a nextPoint la posicion del goal (se ira actualizando, por eso "next")
            nextPointInQueue = new Vector3(0f,0f,0f);
            nextPointInQueue.x = GoalPoint.transform.position.x;
            nextPointInQueue.y = GoalPoint.transform.position.y;
            nextPointInQueue.z = GoalPoint.transform.position.z;
            // Debug.Log("PositionInLineController: Point: " + nextPointInQueue);
            
            // Marcar como desocupado el goal point al inicio (de momento no necesario)
            currentStateGoalPoint = StateGoal.unoccupied;
        }

        public static void SetOcupiedSize(int size)
        {
            ocupied = new bool[size];
        }

        public static bool TrySetOcupied(int index)
        {
            if (ocupied[index])
            {
                return false;
            }
            else
            {
                ocupied[index] = true;
                return true;
            }
        }

        public static void SetFree(int index)
        {
            ocupied[index] = false;
        }
    }
}
