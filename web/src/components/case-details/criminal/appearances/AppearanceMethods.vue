<template>
  <div v-if="appearanceMethods?.length">
    <span v-for="method in appearanceMethods">
      <div>
        {{
          `${method.roleTypeDsc} appearing by ${method.appearanceMethodDesc}`
        }}
      </div>
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
