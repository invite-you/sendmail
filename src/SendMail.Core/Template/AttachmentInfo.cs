namespace SendMail.Core.Template;

public sealed record AttachmentInfo(string Path, long LengthBytes, bool Exists);

