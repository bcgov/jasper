<template>
  <FilterDropdownBase
    :title="title"
    :selected-count="selectedItems.length"
    :show-search="showSearch"
    @open="onMenuOpen"
    v-slot="{ searchQuery }"
  >
    <!-- selected list -->
    <v-list max-height="300" v-if="showSelected">
      <v-list-item
        v-for="item in selectedItemObjects"
        :key="item.value"
        :title="item.text"
        @click="toggleItem(item.value)"
      >
        <template #prepend>
          <v-checkbox-btn
            :model-value="selectedItems.includes(item.value)"
            hide-details
            @click.stop
            @update:model-value="toggleItem(item.value)"
          />
        </template>
      </v-list-item>
    </v-list>

    <v-divider
      v-if="showSelectAll"
      :thickness="3"
      class="my-1"
      color="grey-darken-2"
    />

    <!-- all list -->
    <v-list max-height="300">
      <v-list-item
        title="Select All"
        @click="selectAll(searchQuery)"
        v-if="showSelectAll"
      >
        <template #prepend>
          <v-checkbox-btn
            :model-value="
              filteredItems(searchQuery).every((item) =>
                selectedItems.includes(item.value)
              )
            "
            hide-details
            @click.stop
            @update:model-value="selectAll(searchQuery)"
          />
        </template>
      </v-list-item>
      <v-list-item
        v-for="item in filteredItems(searchQuery)"
        :key="item.value"
        :title="item.text"
        @click="toggleItem(item.value)"
      >
        <template #prepend>
          <v-checkbox-btn
            :model-value="selectedItems.includes(item.value)"
            hide-details
            @click.stop
            @update:model-value="toggleItem(item.value)"
          />
        </template>
      </v-list-item>
    </v-list>
  </FilterDropdownBase>
</template>

<script setup lang="ts">
  import { TextValue } from '@/types/TextValue';
  import { ref } from 'vue';
  import FilterDropdownBase from './FilterDropdownBase.vue';

  const props = withDefaults(
    defineProps<{
      title: string;
      items: TextValue[];
      showSearch?: boolean;
      showSelected?: boolean;
      showSelectAll?: boolean;
    }>(),
    {
      showSearch: true,
      showSelected: true,
      showSelectAll: true,
    }
  );

  const selectedItems = defineModel<string[]>({ default: [] });

  const selectedItemObjects = ref<TextValue[]>([]);

  const onMenuOpen = () => {
    selectedItemObjects.value = props.items.filter((item) =>
      selectedItems.value.includes(item.value)
    );
  };

  const filteredItems = (searchQuery: string): TextValue[] => {
    if (!props.showSelected) return props.items;

    const unselected = props.items.filter(
      (item) => !selectedItemObjects.value.some((s) => s.value === item.value)
    );
    if (!searchQuery) return unselected;
    const query = searchQuery.toLowerCase();
    return unselected.filter((item) => item.text.toLowerCase().includes(query));
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

  const selectAll = (searchQuery: string) => {
    const visible = filteredItems(searchQuery);
    const allSelected = visible.every((item) =>
      selectedItems.value.includes(item.value)
    );
    if (allSelected) {
      selectedItems.value = [];
      selectedItemObjects.value = [];
    } else {
      const newSelection = [
        ...selectedItems.value,
        ...visible.map((item) => item.value),
      ];
      selectedItems.value = [...new Set(newSelection)];
      selectedItemObjects.value = props.items.filter((item) =>
        selectedItems.value.includes(item.value)
      );
    }
  };
</script>
