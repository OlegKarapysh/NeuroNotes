namespace NeuroNotes.Platform.Infrastructure.Security;

public sealed class DataProtectionTokenProtector : ITokenProtector
{
    private const string Purpose = "NeuroNotes.Platform.BotToken.v1";

    private readonly IDataProtector _protector;

    public DataProtectionTokenProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public byte[] Protect(string plaintextToken) =>
        _protector.Protect(Encoding.UTF8.GetBytes(plaintextToken));

    public string Unprotect(byte[] protectedToken) =>
        Encoding.UTF8.GetString(_protector.Unprotect(protectedToken));
}