import type { AppLocale } from './index'

const portuguese = {
  accept: 'Sim',
  reject: 'Não',
  choose: 'Escolher',
  upload: 'Enviar',
  cancel: 'Cancelar',
  clear: 'Limpar',
  apply: 'Aplicar',
  emptyMessage: 'Nenhum registro encontrado',
  emptyFilterMessage: 'Nenhum resultado encontrado',
  emptySearchMessage: 'Nenhum resultado encontrado',
  emptySelectionMessage: 'Nenhum item selecionado',
  searchMessage: '{0} resultados disponíveis',
  selectionMessage: '{0} itens selecionados',
  pending: 'Pendente',
  completed: 'Concluído',
  passwordPrompt: 'Digite uma senha',
  weak: 'Fraca',
  medium: 'Média',
  strong: 'Forte',
  fileSizeTypes: ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
}

const english = {
  accept: 'Yes',
  reject: 'No',
  choose: 'Choose',
  upload: 'Upload',
  cancel: 'Cancel',
  clear: 'Clear',
  apply: 'Apply',
  emptyMessage: 'No records found',
  emptyFilterMessage: 'No results found',
  emptySearchMessage: 'No results found',
  emptySelectionMessage: 'No selected item',
  searchMessage: '{0} results are available',
  selectionMessage: '{0} items selected',
  pending: 'Pending',
  completed: 'Completed',
  passwordPrompt: 'Enter a password',
  weak: 'Weak',
  medium: 'Medium',
  strong: 'Strong',
  fileSizeTypes: ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
}

export const primeVueLocales: Record<AppLocale, typeof english> = {
  'pt-BR': portuguese,
  en: english,
}
