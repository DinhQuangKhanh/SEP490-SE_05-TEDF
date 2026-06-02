// ── File Upload Constants & Validation ──────────────────────────────────────
// Shared between TopicPoolDetailPage (ProposalModal) and RegisterTopicModal.
// Mirrors backend rules in FileUploadValidator.cs + ProposeTopicToPoolEndpoint.

export const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024   // 10 MB per file
export const MAX_TOTAL_SIZE_BYTES = 25 * 1024 * 1024   // 25 MB total
export const MAX_ATTACHMENTS = 5

export const ACCEPTED_TYPES = [
  ".pdf", ".doc", ".docx", ".xls", ".xlsx",
  ".ppt", ".pptx", ".zip", ".rar",
  ".jpg", ".jpeg", ".png",
]

export const DANGEROUS_EXTENSIONS = [
  "php", "phtml", "asp", "aspx", "jsp",
  "exe", "dll", "bat", "cmd", "ps1", "sh", "js",
]

export function isSuspiciousDoubleExtension(fileName: string): boolean {
  const parts = fileName.toLowerCase().split(".").filter(Boolean)
  if (parts.length < 3) return false
  const nestedExt = parts[parts.length - 2]
  return DANGEROUS_EXTENSIONS.includes(nestedExt)
}

export function formatFileSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  return `${Math.ceil(bytes / 1024)} KB`
}

export function validateFiles(
  current: File[],
  incoming: File[],
): { accepted: File[]; rejected: string[] } {
  const accepted: File[] = []
  const rejected: string[] = []

  const currentTotal = current.reduce((sum, f) => sum + f.size, 0)
  let acceptedTotal = 0

  incoming.forEach((file) => {
    if (current.length + accepted.length >= MAX_ATTACHMENTS) {
      rejected.push(`Không thể thêm '${file.name}': tối đa ${MAX_ATTACHMENTS} file.`)
      return
    }

    const extension = `.${file.name.split(".").pop()?.toLowerCase() ?? ""}`
    if (!ACCEPTED_TYPES.includes(extension)) {
      rejected.push(`'${file.name}' không đúng định dạng cho phép.`)
      return
    }

    if (isSuspiciousDoubleExtension(file.name)) {
      rejected.push(`'${file.name}' có tên file không an toàn (double extension).`)
      return
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      rejected.push(`'${file.name}' vượt quá ${formatFileSize(MAX_FILE_SIZE_BYTES)}.`)
      return
    }

    const nextTotal = currentTotal + acceptedTotal + file.size
    if (nextTotal > MAX_TOTAL_SIZE_BYTES) {
      rejected.push(`Tổng dung lượng vượt quá ${formatFileSize(MAX_TOTAL_SIZE_BYTES)} khi thêm '${file.name}'.`)
      return
    }

    accepted.push(file)
    acceptedTotal += file.size
  })

  return { accepted, rejected }
}
