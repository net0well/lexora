import { documentRules } from '@/api/types/document'

export type UploadRejection = 'format' | 'size' | 'empty'

export function rejectionOf(file: File): UploadRejection | null {
  const name = file.name.toLowerCase()

  if (!documentRules.extensions.some((extension) => name.endsWith(extension))) {
    return 'format'
  }

  if (file.size === 0) {
    return 'empty'
  }

  return file.size > documentRules.maxFileSizeBytes ? 'size' : null
}
