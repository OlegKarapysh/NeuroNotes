namespace NeuroNotes.TelegramBot.Application.Menus;

public static class ChatStateCommandsMap
{
    private static readonly IReadOnlyDictionary<ChatState, IReadOnlySet<Type>> Map =
        new Dictionary<ChatState, IReadOnlySet<Type>>
        {
            [ChatState.Initial] = new HashSet<Type>
            {
                typeof(ProcessTextMessageCommand),
                typeof(ProcessVoiceMessageCommand)
            },
            [ChatState.HasTranscription] = new HashSet<Type>
            {
                typeof(ProcessTextMessageCommand),
                typeof(CreateNoteCommand),
                typeof(EditTranscriptionCommand)
            },
            [ChatState.AwaitingEditPrompt] = new HashSet<Type>
            {
                typeof(EditTranscriptionCommand),
                typeof(CreateNoteCommand)
            }
        };

    public static bool IsAllowed<TCommand>(ChatState state) => Map[state].Contains(typeof(TCommand));
}
