using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
SpeechConfig speechConfig = SpeechConfig.FromSubscription(speechKey, "eastus");        
speechConfig.SpeechRecognitionLanguage = "es-MX";

using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
using SpeechRecognizer speechRecognizer = new(speechConfig, audioConfig);

Console.WriteLine("Usa tu micrófono para hablar...");

SpeechRecognitionResult speechRecognitionResult 
    = await speechRecognizer.RecognizeOnceAsync();

ProcessResult(speechRecognitionResult);

void ProcessResult(SpeechRecognitionResult speechRecognitionResult)
{
    switch (speechRecognitionResult)
    {
        case { Reason: ResultReason.RecognizedSpeech }:
            Console.WriteLine($"Texto: Text={speechRecognitionResult.Text}");
            break;

        case { Reason: ResultReason.NoMatch }:
            Console.WriteLine("No se puede reconocer la voz");
            break;

        case { Reason: ResultReason.Canceled }:
            var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
            Console.WriteLine($"Se cancela. La razón es: {cancellation.Reason}");

            if (cancellation is { Reason: CancellationReason.Error })
            {
                Console.WriteLine($"ErrorCode: {cancellation.ErrorCode}");
                Console.WriteLine($"ErrorDetails: {cancellation.ErrorDetails}");
            }
            break;

        default:
            Console.WriteLine("Resultado no reconocido.");
            break;
    }
}