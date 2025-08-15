using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    [System.Serializable]
    public class CodeTask
    {
        public string instruction;
        public string correctAnswer;
        public string startingInput;
    }
    public static TaskManager Instance;

    [Header("All Available Tasks")]
    public List<CodeTask> allTasks = new List<CodeTask>();

    private List<CodeTask> availableTasks = new List<CodeTask>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetTaskPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Make a copy so the original list stays intact
    void ResetTaskPool()
    {
        availableTasks = new List<CodeTask>(allTasks);
    }

    public CodeTask GetUniqueRandomTask()
    {
        if (availableTasks.Count == 0)
        {
            Debug.LogWarning("No more unique tasks available!");
            return null;
        }

        int index = Random.Range(0, availableTasks.Count);
        CodeTask task = availableTasks[index];
        availableTasks.RemoveAt(index); // Remove to prevent reuse
        return task;
    }
}
