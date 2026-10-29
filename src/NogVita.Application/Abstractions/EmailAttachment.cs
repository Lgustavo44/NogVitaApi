namespace NogVita.Application.Abstractions;

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);
