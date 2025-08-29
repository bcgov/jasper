<template>
  <div>
    <template
      v-for="(group, courtClass) in groupedSelections"
      :key="courtClass"
    >
      <ActionBar
        :selected="group"
        :selectionPrependText="courtClass + ' file/s'"
      >
        <template #default>
          <!-- Hide temporarily until opening Key docs and Judicial Binder is implemented.
          <v-btn
            size="large"
            class="mx-2"
            :prepend-icon="mdiFileDocumentMultipleOutline"
            style="letter-spacing: 0.001rem"
          >
            View document bundle
          </v-btn> -->
          <v-btn
            size="large"
            class="mx-2"
            :prepend-icon="mdiFileDocumentOutline"
            style="letter-spacing: 0.001rem"
            data-testid="view-case-details"
            @click="() => onViewApprCaseDetails(group)"
          >
            View case details
          </v-btn>
        </template>
      </ActionBar>
    </template>
  </div>
</template>

<script setup lang="ts">
  import ActionBar from '@/components/shared/table/ActionBar.vue';
  import { CourtListAppearance } from '@/types/courtlist';
  import { getCourtClassLabel } from '@/utils/utils';
  import { mdiFileDocumentOutline } from '@mdi/js';
  import { computed } from 'vue';

  const props = defineProps<{
    selected: CourtListAppearance[];
  }>();

  const emit =
    defineEmits<
      (e: 'view-case-details', appearances: CourtListAppearance[]) => void
    >();

  const groupedSelections = computed(() => {
    const groups: Record<string, CourtListAppearance[]> = {};
    for (const item of props.selected) {
      const group = getCourtClassLabel(item.courtClassCd);
      if (!groups[group]) {
        groups[group] = [];
      }
      groups[group].push(item);
    }
    return groups;
  });

  const onViewApprCaseDetails = (appearances: CourtListAppearance[]) => {
    emit('view-case-details', appearances);
  };
</script>
