<template>
  <div class="pt-4">
    <h5>Parties ({{ parties.length }})</h5>
    <Party v-for="party in parties" :key="party.partyId" :party />
  </div>
</template>
<script setup lang="ts">
  import { partyType } from '@/types/civil/jsonTypes';
  import Party from './Party.vue';

  defineProps<{
    parties: partyType[];
  }>();
</script>
