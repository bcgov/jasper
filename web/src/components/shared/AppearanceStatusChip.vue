<template>
  <v-chip rounded="rounded-sm" :color="colors[status]">
    {{ appearanceStatus[status] }}
  </v-chip>
</template>

<script setup lang="ts">
  defineProps<{ status: string }>();

  enum appearanceStatus {
    SCHD = 'Scheduled',
    CNCL = 'Canceled',
    UNCF = 'Unconfirmed',
  }
  const colors: { [key: string]: string } = {
    SCHD: '#195b96',
    CNCL: '#d82a2f',
    UNCF: '#2e8540',
  };
</script>