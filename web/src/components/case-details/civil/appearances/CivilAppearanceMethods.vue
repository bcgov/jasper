<template>
  <div v-if="appearanceMethod?.length">
    <span v-for="method in appearanceMethod">
      <div>
        {{
          `${method.roleTypeDesc} appearing by ${method.appearanceMethodDesc}`
        }}
      </div>
    </span>
  </div>
</template>

<script setup lang="ts">
  defineProps<{
    appearanceMethod: any[];
  }>();
</script>
