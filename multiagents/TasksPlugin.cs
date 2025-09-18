using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace multiagents;
public class TasksPlugin
{
    [KernelFunction]
    [Description("Returns the complete list of tasks.")]
    public IEnumerable<TaskModel> GetTasks()
    {
        return new List<TaskModel>
        {
            new("Task 1: Implement multi-agent communication protocol", 50),
            new("Task 2: Develop task scheduling algorithm", 75),
            new("Task 3: Create user interface for task management", 100),
            new("Task 4: Integrate with external APIs for data retrieval", 25),
            new("Task 5: Optimize performance of agent interactions", 60),
        };
    }
}

public record TaskModel(string Name, int Progress);