const maximumCommentLength = 10_000
const rawHtmlPattern = /<\/?[a-z][^>]*>/iu
const unsafeUriPattern = /(?:^|[\s(])(?:data:|file:|[a-z]:[\\/])/iu
const markdownImagePattern = /!\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)/giu
const markdownLinkPattern = /(?<!!)\[[^\]]+\]\(([^)\s]+)(?:\s+"[^"]*")?\)/giu

export function commentLength(value: string): number {
  return Array.from(value).length
}

export function validateCommentMarkdown(value: string): string | undefined {
  if (!value.trim()) return 'required'
  if (commentLength(value) > maximumCommentLength) return 'too_long'
  if (rawHtmlPattern.test(value) || unsafeUriPattern.test(value)) return 'unsafe_markdown'

  for (const match of value.matchAll(markdownImagePattern)) {
    if (!isAuthorizedAttachment(match[1] ?? '')) return 'unsafe_markdown'
  }
  for (const match of value.matchAll(markdownLinkPattern)) {
    const target = match[1] ?? ''
    if (!isHttpUrl(target) && !isAuthorizedAttachment(target)) return 'unsafe_markdown'
  }

  return undefined
}

function isHttpUrl(value: string): boolean {
  return /^https?:\/\//iu.test(value)
}

function isAuthorizedAttachment(value: string): boolean {
  return /^\/api\/v1\/.+\/attachments\/.+\/content(?:[?#].*)?$/iu.test(value)
}
