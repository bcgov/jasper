<template>
  <div>
    <v-text-field
      v-if="showSearch"
      v-model="localSearchQuery"
      density="compact"
      :placeholder="`Search ${title.toLowerCase()}...`"
      hide-details
      clearable
      class="mb-2 search-field"
    />
    <v-checkbox
      :model-value="isAllSelected"
      :indeterminate="isIndeterminate"
      label="Select All"
      hide-details
      density="compact"
      @update:model-value="toggleSelectAll"
    />
    <div class="filter-list-container">
      <v-list class="pa-0">
        <v-list-item v-for="item in displayedItems" :key="item.id" class="pa-0">
          <v-checkbox
            :model-value="selectedItems.includes(item.id)"
            :label="item.label"
            hide-details
            density="compact"
            @update:model-value="toggleItem(item.id)"
          >
          </v-checkbox>
        </v-list-item>
      </v-list>
    </div>
    <v-btn
      v-if="filteredItems.length > previewCount && !localSearchQuery"
      variant="text"
      size="small"
      class="px-0 link-style-btn"
      @click="toggleShowAll"
    >
      {{ showAll ? 'Show Less' : `See all ${title.toLowerCase()}` }}
    </v-btn>
    <div v-if="filteredItems.length === 0" class="text-caption text-grey pa-2">
      No {{ title.toLowerCase() }} found
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, ref } from 'vue';

  interface FilterItemData {
    id: string;
    label: string;
  }

  const props = withDefaults(
    defineProps<{
      title: string;
      items: FilterItemData[];
      modelValue: string[];
      previewCount?: number;
      showSearch?: boolean;
    }>(),
    {
      previewCount: 5,
      showSearch: true,
    }
  );

  const emit = defineEmits<{
    'update:modelValue': [value: string[]];
  }>();

  const showAll = ref(false);
  const localSearchQuery = ref('');

  const selectedItems = computed({
    get: () => props.modelValue,
    set: (value) => emit('update:modelValue', value),
  });

  const filteredItems = computed(() => {
    if (!localSearchQuery.value) {
      return props.items;
    }
    const query = localSearchQuery.value.toLowerCase();
    return props.items.filter((item) =>
      item.label.toLowerCase().includes(query)
    );
  });

  const displayedItems = computed(() => {
    if (localSearchQuery.value) {
      return filteredItems.value;
    }
    return showAll.value
      ? filteredItems.value
      : filteredItems.value.slice(0, props.previewCount);
  });

  const isAllSelected = computed(() => {
    if (filteredItems.value.length === 0) return false;
    return filteredItems.value.every((item) =>
      selectedItems.value.includes(item.id)
    );
  });

  const isIndeterminate = computed(() => {
    const selectedCount = filteredItems.value.filter((item) =>
      selectedItems.value.includes(item.id)
    ).length;
    return selectedCount > 0 && selectedCount < filteredItems.value.length;
  });

  const toggleShowAll = () => {
    showAll.value = !showAll.value;
  };

  const toggleSelectAll = () => {
    if (isAllSelected.value) {
      // Deselect all filtered items
      const filteredIds = filteredItems.value.map((item) => item.id);
      selectedItems.value = selectedItems.value.filter(
        (id) => !filteredIds.includes(id)
      );
    } else {
      // Select all filtered items
      const filteredIds = filteredItems.value.map((item) => item.id);
      const newSelection = [...selectedItems.value];
      filteredIds.forEach((id) => {
        if (!newSelection.includes(id)) {
          newSelection.push(id);
        }
      });
      selectedItems.value = newSelection;
    }
  };

  const toggleItem = (itemId: string) => {
    const newSelection = [...selectedItems.value];
    const index = newSelection.indexOf(itemId);
    if (index > -1) {
      newSelection.splice(index, 1);
    } else {
      newSelection.push(itemId);
    }
    selectedItems.value = newSelection;
  };
</script>

<style scoped>
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

  .link-style-btn {
    text-transform: none;
    letter-spacing: normal;
  }

  .link-style-btn :deep(.v-btn__content) {
    color: var(--text-blue-600);
    text-decoration: underline;
  }

  .link-style-btn:hover :deep(.v-btn__content) {
    color: var(--text-blue-900);
  }

  .filter-list-container {
    max-height: 20rem;
    overflow-y: auto;
  }

  .filter-list-container::-webkit-scrollbar {
    width: 6px;
  }

  .filter-list-container::-webkit-scrollbar-track {
    background: var(--bg-scrollbar-track);
    border-radius: 3px;
  }

  .filter-list-container::-webkit-scrollbar-thumb {
    background: var(--bg-scrollbar-thumb);
    border-radius: 3px;
  }

  .filter-list-container::-webkit-scrollbar-thumb:hover {
    background: var(--bg-scrollbar-thumb-hover);
  }
</style>
