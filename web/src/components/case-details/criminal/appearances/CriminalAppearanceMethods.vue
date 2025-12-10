<template>
  <div v-if="appearanceMethods?.length">
    <span v-for="method in appearanceMethods">
      {{ `${method.roleTypeDsc} appearing by ${method.appearanceMethodDesc}` }}
    </span>
  </div>
  <div v-else>
    <span>No appearance methods.</span>
  </div>
</template>

<script setup lang="ts">
  import { CriminalAppearanceMethod } from '@/types/criminal/jsonTypes';

  defineProps<{
    appearanceMethods: CriminalAppearanceMethod[] | undefined;
  }>();
</script>
