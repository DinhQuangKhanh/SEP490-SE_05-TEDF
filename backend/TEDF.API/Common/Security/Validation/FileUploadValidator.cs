using System.Text.RegularExpressions;

namespace TEDF.API.Common.Security.Validation;

/// <summary>
/// Validates uploaded files: extension whitelist, magic-byte signature,
/// double-extension detection, and size limits.
/// Reusable across all upload endpoints (topic proposals, topic documents, etc.).
/// </summary>
internal static class FileUploadValidator
{
  private static readonly string[] AllowedExtensions =
  [
      ".pdf", ".doc", ".docx", ".xls", ".xlsx",
        ".ppt", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png"
  ];

  private static readonly Dictionary<string, List<byte[]>> Signatures = new(StringComparer.OrdinalIgnoreCase)
  {
    [".pdf"] = ["%PDF"u8.ToArray()],
    [".jpg"] = [[0xFF, 0xD8, 0xFF]],
    [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
    [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
    [".zip"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
    [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],
    [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
    [".pptx"] = [[0x50, 0x4B, 0x03, 0x04]],
    [".doc"] = [[0xD0, 0xCF, 0x11, 0xE0]],
    [".xls"] = [[0xD0, 0xCF, 0x11, 0xE0]],
    [".ppt"] = [[0xD0, 0xCF, 0x11, 0xE0]],
    [".rar"] = [[0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00], [0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00]]
  };

  private static readonly Regex DangerousFileNamePattern =
      new(@"\.(php|phtml|asp|aspx|jsp|exe|dll|bat|cmd|ps1|sh|js)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  public static bool TryValidate(
      IReadOnlyCollection<IFormFile>? attachments,
      long perFileMaxBytes,
      int maxAttachmentCount,
      out string error)
  {
    error = string.Empty;

    if (attachments is null || attachments.Count == 0)
    {
      return true;
    }

    if (attachments.Count > maxAttachmentCount)
    {
      error = $"Chỉ được đính kèm tối đa {maxAttachmentCount} file.";
      return false;
    }

    foreach (var file in attachments)
    {
      if (file.Length <= 0)
      {
        error = "File đính kèm rỗng hoặc không hợp lệ.";
        return false;
      }

      if (file.Length > perFileMaxBytes)
      {
        error = $"File '{file.FileName}' vượt quá dung lượng tối đa {perFileMaxBytes / (1024 * 1024)}MB.";
        return false;
      }

      var fileName = Path.GetFileName(file.FileName);
      var extension = Path.GetExtension(fileName).ToLowerInvariant();

      if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
      {
        error = $"Định dạng file '{fileName}' không được hỗ trợ.";
        return false;
      }

      if (IsDoubleExtensionSuspicious(fileName))
      {
        error = $"Tên file '{fileName}' không hợp lệ.";
        return false;
      }

      if (!HasValidSignature(file, extension))
      {
        error = $"Nội dung file '{fileName}' không khớp với phần mở rộng '{extension}'.";
        return false;
      }
    }

    return true;
  }

  private static bool IsDoubleExtensionSuspicious(string fileName)
  {
    var baseName = Path.GetFileNameWithoutExtension(fileName);
    if (string.IsNullOrWhiteSpace(baseName))
    {
      return true;
    }

    var nestedExtension = Path.GetExtension(baseName);
    if (string.IsNullOrWhiteSpace(nestedExtension))
    {
      return false;
    }

    return DangerousFileNamePattern.IsMatch(nestedExtension);
  }

  private static bool HasValidSignature(IFormFile file, string extension)
  {
    if (!Signatures.TryGetValue(extension, out var signatures) || signatures.Count == 0)
    {
      return true;
    }

    var maxHeaderLength = signatures.Max(s => s.Length);
    var headerBuffer = new byte[maxHeaderLength];

    using var stream = file.OpenReadStream();
    var read = stream.Read(headerBuffer, 0, maxHeaderLength);
    if (read <= 0)
    {
      return false;
    }

    return signatures.Any(signature =>
        read >= signature.Length &&
        headerBuffer.AsSpan(0, signature.Length).SequenceEqual(signature));
  }
}