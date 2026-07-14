namespace NeuroNotes.TelegramBot.Application.Menus;

public static class ChatStateCommandsMap
{
    private static readonly IReadOnlyDictionary<ChatState, IReadOnlySet<Type>> Map =
        new Dictionary<ChatState, IReadOnlySet<Type>>
        {
            [ChatState.Initial] = new HashSet<Type>
            {
                typeof(ProcessTextMessageCommand),
                typeof(ProcessVoiceMessageCommand),
                typeof(ListTagsCommand)
            },
            [ChatState.HasTranscription] = new HashSet<Type>
            {
                typeof(ProcessTextMessageCommand),
                typeof(PreviewNoteCommand),
                typeof(PushNoteToGitHubCommand),
                typeof(EditTranscriptionCommand)
            },
            [ChatState.AwaitingEditPrompt] = new HashSet<Type>
            {
                typeof(EditTranscriptionCommand),
                typeof(PreviewNoteCommand),
                typeof(PushNoteToGitHubCommand)
            },
            [ChatState.PreviewingNote] = new HashSet<Type>
            {
                typeof(ConfirmNoteCommand)
            },
            [ChatState.AwaitingGitHubRepo] = new HashSet<Type>(),
            [ChatState.AwaitingGitHubToken] = new HashSet<Type>
            {
                typeof(ConnectGitHubCommand)
            },
            [ChatState.AwaitingTagName] = new HashSet<Type>
            {
                typeof(AddTagCommand)
            }
        };

    public static bool IsAllowed<TCommand>(ChatState state) => Map[state].Contains(typeof(TCommand));
}