import DOMPurify from 'dompurify'
import MarkdownIt from 'markdown-it'

const markdown = new MarkdownIt({ html: false, linkify: true, breaks: true }).disable('image')

// A private instance: the link hook below must not change how other code sanitizes.
const purifier = DOMPurify(window)

purifier.addHook('afterSanitizeAttributes', (node) => {
  if (node.tagName === 'A') {
    node.setAttribute('target', '_blank')
    node.setAttribute('rel', 'noopener noreferrer')
  }
})

// The answer can carry text injected by a document: an image or any element that loads a URL on its
// own would leak data without a click, so only text, lists, emphasis, code and plain links survive.
export function renderMarkdown(source: string): string {
  return purifier.sanitize(markdown.render(source), {
    FORBID_TAGS: [
      'img',
      'picture',
      'source',
      'video',
      'audio',
      'iframe',
      'object',
      'embed',
      'svg',
      'math',
      'style',
      'link',
      'meta',
      'form',
      'input',
      'button',
      'textarea',
    ],
    FORBID_ATTR: ['style', 'srcset', 'background', 'poster'],
    ALLOWED_URI_REGEXP: /^(?:https?:|mailto:)/i,
    ADD_ATTR: ['target'],
  })
}
