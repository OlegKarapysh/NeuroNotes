using FluentResults;

namespace NeuroNotes.AudioProcessing.Application.Interfaces;

public interface ISpeechRecognizer
{
    Task<Result<string>> RecognizeSpeech(Stream speech, CancellationToken cancellationToken = default);
}