<script setup lang="ts">
import Button from 'primevue/button'
</script>

<template>
  <section class="page page--centered">
    <h1>{{ $t('notFound.title') }}</h1>
    <p>{{ $t('notFound.description') }}</p>
    <RouterLink v-slot="{ navigate }" to="/" custom>
      <Button :label="$t('notFound.home')" icon="pi pi-home" @click="navigate" />
    </RouterLink>
  </section>
</template>
