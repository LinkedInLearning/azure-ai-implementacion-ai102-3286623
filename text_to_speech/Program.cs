using Microsoft.CognitiveServices.Speech;

string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
SpeechConfig speechConfig = SpeechConfig.FromSubscription(speechKey, "eastus");        
speechConfig.SpeechSynthesisVoiceName = "es-ES-AbrilNeural";

using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
Console.WriteLine("Escribe el texto:");
var text = Console.ReadLine();
SpeechSynthesisResult speechSynthesisResult 
    = await speechSynthesizer.SpeakTextAsync(text);

ProcessResult(speechSynthesisResult);


void ProcessResult(SpeechSynthesisResult speechSynthesisResult)
{
    switch (speechSynthesisResult)
    {
        case { Reason: ResultReason.SynthesizingAudioCompleted }:
            Console.WriteLine("Finalizado");
            break;

        case { Reason: ResultReason.Canceled } cancellationResult:
            var cancellationDetails = SpeechSynthesisCancellationDetails.FromResult(cancellationResult);
            Console.WriteLine($"Cancelado. La razón es: {cancellationDetails.Reason}");

            if (cancellationDetails is { Reason: CancellationReason.Error })
            {
                Console.WriteLine($"ErrorCode={cancellationDetails.ErrorCode}");
                Console.WriteLine($"ErrorDetails={cancellationDetails.ErrorDetails}");
            }
            break;

        default:
            Console.WriteLine("Operación no reconocida.");
            break;
    }
}