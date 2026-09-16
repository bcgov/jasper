<template>
  <v-select
    :model-value="modelValue"
    @update:model-value="onUpdate"
    multiple
    chips
    closable-chips
    density="compact"
    :placeholder="placeholder"
    hide-details
    :items="items"
    item-title="title"
    item-value="value"
  >
    <template #prepend-item>
      <v-list-item
        class="py-0"
        density="compact"
        :title="getSelectAllTitle()"
        @click="toggleSelectAll"
      >
        <template #prepend>
          <v-checkbox-btn
            :model-value="isAllSelected"
            hide-details
            @click.stop="toggleSelectAll"
          />
        </template>
      </v-list-item>
      <hr class="select-divider" />
    </template>
    <template #item="{ props: itemProps, item }">
      <v-list-item
        v-bind="itemProps"
        density="compact"
        :title="getItemTitle(item.raw)"
      >
        <template #prepend>
          <v-checkbox-btn
            :model-value="isSelected(item.raw.value)"
            hide-details
            @click.stop="toggleValue(item.raw.value)"
          />
        </template>
      </v-list-item>
    </template>
    <template #chip="{ props: chipProps, item }">
      <v-chip
        v-bind="chipProps"
        closable
        size="small"
        @click:close.stop="removeValue(item.raw.value)"
      >
        {{ item.raw.title }}
      </v-chip>
    </template>
  </v-select>
</template>

<script setup lang="ts">
  import { computed } from 'vue';

  type SelectOption = {
    title: string;
    value: string;
    count?: number;
  };

  const props = withDefaults(
    defineProps<{
      modelValue: string[];
      items: SelectOption[];
      selectAllCount?: number;
      selectAllLabel?: string;
      placeholder?: string;
    }>(),
    {
      selectAllLabel: 'Select All',
      placeholder: 'Select options',
    }
  );

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string[]): void;
  }>();

  const isAllSelected = computed<boolean>(() => {
    return (
      props.items.length > 0 && props.modelValue.length === props.items.length
    );
  });

  const onUpdate = (value: string[]) => {
    emit('update:modelValue', value ?? []);
  };

  const getSelectAllTitle = (): string => {
    if (typeof props.selectAllCount === 'number') {
      return `${props.selectAllLabel} (${props.selectAllCount})`;
    }

    return props.selectAllLabel;
  };

  const getItemTitle = (item: SelectOption): string => {
    if (typeof item.count === 'number') {
      return `${item.title} (${item.count})`;
    }

    return item.title;
  };

  const toggleSelectAll = () => {
    if (isAllSelected.value) {
      emit('update:modelValue', []);
      return;
    }

    emit(
      'update:modelValue',
      props.items.map((item) => item.value)
    );
  };

  const removeValue = (value: string) => {
    emit(
      'update:modelValue',
      props.modelValue.filter((selected) => selected !== value)
    );
  };

  const isSelected = (value: string): boolean => {
    return props.modelValue.includes(value);
  };

  const toggleValue = (value: string) => {
    if (isSelected(value)) {
      removeValue(value);
      return;
    }

    emit('update:modelValue', [...props.modelValue, value]);
  };
</script>

<style scoped>
  .select-divider {
    border: 0;
    border-top: 1px solid var(--border-gray-300);
    margin: 4px 0;
  }
</style>
