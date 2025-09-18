using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string deploymentName = config["DeploymentName"];
string endpoint = config["Endpoint"];
string apiKey = config["ApiKey"];

var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
             .Plugins.AddFromType<TimePlugin>()
                     .AddFromType<ProcessPlugin>()
                     .AddFromType<UserProfilePlugin>()
             .Services.AddLogging(logging =>
             {
                 logging.AddConsole();
                 logging.SetMinimumLevel(LogLevel.Trace);
             }).AddSingleton<IFunctionInvocationFilter, MyFunctionInvocationFilter>()
               .AddSingleton<IAutoFunctionInvocationFilter, MyAutoFunctionInvocationFilter>()
               .AddSingleton<IPromptRenderFilter, EmailBlockerPromptRenderFilter>();

var kernel = kernelBuilder.Build();

var promptTemplate = """
        Eres un agente muy útil.

        #Usuario actual
        {{UserProfilePlugin.GetUserProfile $user_id}}

        #Histórico de mensajes
        {{$history}}

        Assistant:
    """;

var settings = new AzureOpenAIPromptExecutionSettings() 
{ 
    Temperature = 1f,
    MaxTokens = 1000,
    SetNewMaxCompletionTokensEnabled = true,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new List<Message>();

while (true)
{
    Console.Write("Prompt: ");
    var message = Console.ReadLine();

    history.Add(new Message("User", message));

    var kernelArgs = new KernelArguments(settings)
    {
        { "history", history.AsString() },
        { "user_id", 25 }
    };

    var result = await kernel.InvokePromptAsync(promptTemplate,
                                                kernelArgs);

    var resultContent = result.GetValue<string>();

    history.Add(new Message("Assistant", resultContent));
    
    Console.WriteLine($"\n{resultContent}\n");

}

static class HistoryExtensions
{
    public static string AsString(this IEnumerable<Message> history)
    {
        return string.Join("\n", history
               .TakeLast(10)
               .Select(t => $"{t.Role}: {t.Content.Replace("\n", " ").Trim()}"));
    }
}

public record Message(string Role, string Content);


public class ProcessPlugin
{
    [KernelFunction]
    [Description("Regresa la información de todos los procesos de la máquina.")]
    public IEnumerable<string> GetProcesses()
    {
        return Process.GetProcesses().Select(p => $"Id:{p.Id} ProcessName:{p.ProcessName}");
    }
}

public class UserProfilePlugin
{
    [KernelFunction]
    [Description("Regresa el perfil completo del usuario especificado.")]
    public UserProfile GetUserProfile([Description("El identificador único del usuario.")] int id)
    {
        return new UserProfile(id, "Rodrigo Díaz Concha", "SK Course Authors");
    }
}

public record UserProfile(int Id, string FullName, string Department)
{
    public override string ToString()
    {
        return $"Id: {Id}\n Nombre completo: {FullName}\n Departamento: {Department}\n";
    }
}

public class MyFunctionInvocationFilter : IFunctionInvocationFilter
{
    public Task OnFunctionInvocationAsync(FunctionInvocationContext context, 
                                          Func<FunctionInvocationContext, Task> next)
    {
        return next(context);
    }
}

public class MyAutoFunctionInvocationFilter : IAutoFunctionInvocationFilter
{
    public Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, 
                                              Func<AutoFunctionInvocationContext, Task> next)
    {
        return next(context);
    }
}

public class EmailBlockerPromptRenderFilter : IPromptRenderFilter
{
    private readonly Regex EmailRegex = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task OnPromptRenderAsync(PromptRenderContext context, 
                                    Func<PromptRenderContext, Task> next)
    {
        await next(context);
        //Ya se renderizó

        if (string.IsNullOrWhiteSpace(context.RenderedPrompt))
        {
            return;
        }

        context.RenderedPrompt = EmailRegex.Replace(context.RenderedPrompt, "[CORREO ELIMINADO]");
    }
}