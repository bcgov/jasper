<template>
  <div class="pt-4">
    <h5>Children ({{ children.length }})</h5>
    <Child v-for="child in children" :key="child.partyId" :child />
  </div>
</template>
<script setup lang="ts">
  import { partyType } from '@/types/civil/jsonTypes';
  import Child from './Child.vue';

  defineProps<{
    children: partyType[];
  }>();
</script>
