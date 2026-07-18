namespace NeuroNotes.Platform.Public;

/// <summary>Encrypts/decrypts bot tokens at rest (SEC-002) — backed by ASP.NET Core Data Protection.</summary>
public interface ITokenProtector
{
    byte[] Protect(string plaintextToken);

    string Unprotect(byte[] protectedToken);
}