using Microsoft.CognitiveServices.Speech;

string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
SpeechConfig speechConfig = SpeechConfig.FromSubscription(speechKey, "eastus");        
speechConfig.SpeechSynthesisVoiceName = "es-ES-AbrilNeural";

using var speechSynthesizer = new SpeechSynthesizer(speechConfig);

string ssmlText = @"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='es-ES'>
    <voice name='es-MX-JorgeNeural'>
        <mstts:express-as style='cheerful'>
            ¡Hola! Mi nombre es Jorge y estoy aquí para darte una recomendación.
        </mstts:express-as>

        <p>Si quieres pasar tu examen, debes estudiar los temas de este curso.</p>

        <p>Vera, ¿hay algo más que quieras agregar?</p>
    </voice>
    
    <voice name='es-ES-VeraNeural'>
        <p>Recuerda que este curso es un complemento para estudiar, y no substituye los conocimientos reales que debes tener.</p>
        <p>Finalmente, no olvides descansar un día antes de tu examen.</p>
    </voice>
</speak>
";

SpeechSynthesisResult speechSynthesisResult 
    = await speechSynthesizer.SpeakSsmlAsync(ssmlText);

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