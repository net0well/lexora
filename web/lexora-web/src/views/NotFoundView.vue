<script setup lang="ts">
import Button from 'primevue/button'
</script>

<template>
  <section class="page page--centered">
    <h1>404</h1>
    <RouterLink v-slot="{ navigate }" to="/" custom>
      <Button label="Home" icon="pi pi-home" @click="navigate" />
    </RouterLink>
  </section>
</template>
