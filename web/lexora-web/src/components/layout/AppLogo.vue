<script setup lang="ts">
const { size = '2rem' } = defineProps<{ size?: string }>()
</script>

<template>
  <svg
    class="app-logo"
    :width="size"
    :height="size"
    viewBox="0 0 32 32"
    role="img"
    aria-hidden="true"
    focusable="false"
  >
    <rect width="32" height="32" rx="7" fill="var(--p-primary-700, #0f766e)" />
    <path d="M10 7h4v14h9v4H10z" fill="#ffffff" />
  </svg>
</template>
