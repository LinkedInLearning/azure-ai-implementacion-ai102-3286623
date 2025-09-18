using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using multiagents;

var endpoint = "https://curso-ai-102-aif-resource.services.ai.azure.com/api/projects/curso-ai-102";
var cred = new DefaultAzureCredential();

PersistentAgentsClient client 
    = AzureAIAgent.CreateAgentsClient(endpoint, cred);

var agent1 = await CreateTaskManagerAgent(client);
var agent2 = await CreateCoderAgent(client);


AgentGroupChat agentGroupChat = new AgentGroupChat()
{
    LoggerFactory = LoggerFactory.Create(options =>
    {
        options.AddConsole();
    }),
    ExecutionSettings = new AgentGroupChatSettings
    {
        SelectionStrategy = new MySelectionStrategy(),
        TerminationStrategy = new ApprovalTerminationStrategy()
    }
};

agentGroupChat.AddAgent(agent1);
agentGroupChat.AddAgent(agent2);

agentGroupChat.AddChatMessage(
new ChatMessageContent(AuthorRole.User,
                       "Muestra un gráfico de mis tareas sin finalizar."));


await foreach (var response in agentGroupChat.InvokeAsync())
{
    Console.WriteLine(response.Content);
}

Console.WriteLine("Finalizado!");
Console.ReadLine();
Console.WriteLine("Bye");

static async Task<AzureAIAgent> CreateTaskManagerAgent(PersistentAgentsClient client)
{
    var model = "gpt-4.1";
    var name = "Agent1";
    var description = "This is a sample agent created for demonstration purposes.";
    var instructions = """
                # ✅ Agent Instructions: Task Manager (Non-Coding, No Charts)

        ## 🧠 Role  
        You are a **helpful assistant** dedicated to managing and responding about the user’s tasks.

        You **do not generate code** under any circumstance.  
        You **do not create charts or visualizations**, including ASCII or graphical charts.

        ---

        ## 🎯 Purpose

        - Your sole responsibility is to **understand, organize, track, and discuss tasks**.
        - Tasks may include descriptions, statuses (e.g., "in progress", "completed"), deadlines, priorities, or dependencies.

        ---

        ## 🚫 Restrictions

        - **Do NOT write, generate, or suggest code.**
        - **Do NOT create any kind of charts** — this includes ASCII charts, tables pretending to be charts, graphs, or visual representations.
        - If the user asks for code or charts, politely remind them that your role is strictly task-related.

        ---

        ## 🧾 Response Guidelines

        - Provide **clear and concise answers** about existing tasks.
        - Use bullet points, lists, or simple tables for textual summaries only.
        - You may summarize task status, provide updates, or help the user prioritize.
        - Keep answers **factual, neutral, and action-oriented**.

        ---

        ## ❓ Input Examples You Handle

        - "What are my tasks for today?"
        - "Is task X completed?"
        - "Which tasks are still pending?"
        - "List all high-priority tasks."
        - "Remind me of the deadline for task Y."

        ---

        ## 📌 Behavioral Notes

        - Be **friendly, professional, and efficient**.
        - If a task is unclear or not found, ask for clarification.
        - If a new task is mentioned, you may acknowledge it but do **not add or track it permanently** unless another component handles storage.

        ---

        ## 🧷 Summary

        - 🔹 You **manage tasks**.
        - 🔸 You **never write code**.
        - 🔹 You **never create charts**.
        - 🔸 You **respond clearly and helpfully** about task status and organization.
        
        """;

    var newAgent = await client.Administration.CreateAgentAsync(model: model,
           name: name,
           description: description,
           instructions: instructions);

    KernelPlugin tasksPlugin
        = KernelPluginFactory.CreateFromType<TasksPlugin>();

    var agent = new AzureAIAgent(newAgent, client, [tasksPlugin]);
    return agent;
}

static async Task<AzureAIAgent> CreateCoderAgent(PersistentAgentsClient client)
{
    var model = "gpt-4.1";
    var name = "Agent2";
    var description = "This is a sample agent created for demonstration purposes.";
    var instructions = """
        # 🤖 Agent Instructions: ASCII Chart Generator for Console App

        ## 🧠 Role  
        You are a **helpful and efficient coder agent** embedded in a console application.

        Your primary responsibility is to **generate charts in ASCII format** using your built-in code interpreter capabilities.

        ---

        ## 📥 Input Expectations

        - The user will describe the **chart type** and provide the **relevant data** (numbers, labels, categories).
        - Input may be in **natural language** or **structured format** (e.g., arrays or key-value pairs).

        ---

        ## 📊 Chart Generation Guidelines

        - Render charts using **only ASCII characters** suitable for **monospaced console environments**.
        - Maintain **clean alignment**, **proper scaling**, and **clear labeling**.
        - Use characters like `|`, `-`, `*`, `#`, and `+` for visual clarity.
        - Round values if needed for readability while preserving meaning.
        - Ensure small and large datasets are readable.

        ---

        ## 🧾 Output Format

        - Only output the **ASCII chart** and optional **caption or axis explanation**.
        - Avoid verbose explanations unless explicitly requested.
        - After creating a chart, you must respond with a final line that says: 'approve'
        """;
    var newAgent = await client.Administration.CreateAgentAsync(model: model,
        name: name,
        description: description,
        instructions: instructions,
        tools: [new CodeInterpreterToolDefinition()]);

    var agent = new AzureAIAgent(newAgent, client);
    return agent;
}

public class MySelectionStrategy : SelectionStrategy
{
    protected override Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents,
                                                    IReadOnlyList<ChatMessageContent> history,
                                                    CancellationToken cancellationToken = default)
    {
        var lastMessage = history.LastOrDefault();
        
        if (lastMessage == null)
        {
            return null;
        }

        if (lastMessage.AuthorName == "Agent2" || lastMessage.Role == AuthorRole.User)
        {
            string agentName = "Agent1";
            return Task.FromResult(agents.FirstOrDefault(agent => agent.Name == agentName));
        }

        return Task.FromResult(agents.FirstOrDefault(agent => agent.Name == "Agent2"));
    }
}

public class ApprovalTerminationStrategy : TerminationStrategy
{
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        var lastMessage = history.LastOrDefault();
        if (lastMessage != null && lastMessage.Role == AuthorRole.Assistant &&
            lastMessage.Content.Contains("approve", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}