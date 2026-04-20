<template>
  <v-menu v-model="menu" offset-y :close-on-content-click="false">
    <!-- activator -->
    <template #activator="{ props: menuProps }">
      <v-btn class="ml-2" v-bind="menuProps" variant="outlined" rounded="pill">
        {{ title }}

        <v-badge v-if="selectedCount > 0" :content="selectedCount" inline />
        <v-icon :icon="mdiChevronDown" />
      </v-btn>
    </template>

    <v-card width="300">
      <!-- search -->
      <v-text-field
        v-if="showSearch"
        v-model="searchQuery"
        density="compact"
        :placeholder="`Search ${title.toLowerCase()}...`"
        :prepend-inner-icon="mdiMagnify"
        hide-details
        clearable
        class="ma-2 search-field"
      />

      <!-- list content provided by parent -->
      <slot :menu="menu" :search-query="searchQuery" />
    </v-card>
  </v-menu>
</template>

<script setup lang="ts">
  import { mdiChevronDown, mdiMagnify } from '@mdi/js';
  import { ref, watch } from 'vue';

  defineProps<{
    title: string;
    selectedCount: number;
    showSearch?: boolean;
  }>();

  const emit = defineEmits<{
    open: [];
    close: [];
  }>();

  const menu = ref(false);
  const searchQuery = ref('');

  watch(menu, (isOpen) => {
    if (isOpen) emit('open');
    else emit('close');
  });
</script>

<style scoped>
  :deep(.v-checkbox-btn .v-label) {
    margin-bottom: 0;
  }

  :deep(.v-list) {
    padding: 0 !important;
  }

  :deep(.v-list-item) {
    min-height: 1rem;
  }

  :deep(.v-list-item-title) {
    font-size: 0.875rem;
  }

  :deep(.v-list-item:hover) {
    background-color: rgba(0, 0, 0, 0.1) !important;
    cursor: pointer;
  }

  :deep(.v-selection-control .v-label) {
    margin-bottom: 0;
    margin-left: 0.25rem;
    font-size: 0.875rem !important;
  }

  :deep(.v-selection-control__wrapper) {
    width: 1.125rem;
    height: 1.125rem;
  }

  :deep(.v-checkbox .v-selection-control) {
    min-height: 0.5rem;
  }

  :deep(.v-checkbox .v-selection-control__input) {
    width: 1.125rem;
    height: 1.125rem;
  }

  :deep(.v-checkbox .v-selection-control__input i) {
    font-size: 1.125rem;
  }

  .search-field :deep(input) {
    min-height: 2rem;
  }

  .search-field :deep(.v-field__input) {
    font-size: 0.75rem;
    padding-top: 0.25rem;
    padding-bottom: 0.25rem;
  }

  .search-field :deep(.v-field__input::placeholder) {
    font-size: 0.75rem;
  }

  :deep(.v-divider) {
    opacity: 50% !important;
  }

  :deep(.v-badge__badge) {
    background-color: var(--bg-blue-800);
    color: var(--text-white-500);
  }
</style>
