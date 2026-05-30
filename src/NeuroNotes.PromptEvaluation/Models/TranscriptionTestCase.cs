namespace NeuroNotes.PromptEvaluation.Models;

public sealed record TranscriptionTestCase(string Name, string AudioFilePath, string ReferenceText);
