using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
SpeechTranslationConfig speechTranslationConfig = SpeechTranslationConfig.FromSubscription(speechKey, "eastus");
speechTranslationConfig.SpeechRecognitionLanguage = "es-MX";
speechTranslationConfig.AddTargetLanguage("en");
speechTranslationConfig.AddTargetLanguage("pt");

using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
using TranslationRecognizer translationRecognizer = new(speechTranslationConfig, audioConfig);

Console.WriteLine("Usa tu micrófono para hablar...");

TranslationRecognitionResult translationRecognitionResult 
    = await translationRecognizer.RecognizeOnceAsync();

ProcessResult(translationRecognitionResult);

void ProcessResult(TranslationRecognitionResult result)
{
    switch (result)
    {
        case { Reason: ResultReason.TranslatedSpeech }:
            result.Translations
                .ToList()
                .ForEach(t => Console.WriteLine($"Traducción a: '{t.Key}' es: {t.Value}"));
            break;

        case { Reason: ResultReason.NoMatch }:
            Console.WriteLine("No se puede reconocer la voz");
            break;

        case { Reason: ResultReason.Canceled }:
            var cancellation = CancellationDetails.FromResult(result);
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