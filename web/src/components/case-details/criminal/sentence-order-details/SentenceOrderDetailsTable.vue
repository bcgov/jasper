<template>
  <v-data-table
    :headers
    :group-by
    :items="data"
    no-data-text="No sentences"
    :show-select="false"
    hide-default-footer
    hide-pagination
    :sort-by="sortBy"
  >
    <template v-slot:group-header="{ item, columns, isGroupOpen, toggleGroup }">
      <tr>
        <td class="pa-0" style="height: 1rem" :colspan="columns.length">
          <v-banner
            class="table-banner"
            :ref="
              () => {
                if (!isGroupOpen(item)) toggleGroup(item);
              }
            "
          >
            {{ item.value }}
          </v-banner>
        </td>
      </tr>
    </template>
    <template #item="{ item }">
      <tr>
        <td class="text-no-wrap">
          {{ formatDateToDDMMMYYYY(item.appearanceDate) }}
        </td>
        <td>{{ item.countNumber }}</td>
        <td>
          <b>{{ item.sectionTxt }}</b> - {{ item.sectionDscTxt }}
        </td>
        <td>
          <v-tooltip :text="item.findingDsc" location="top">
            <template v-slot:activator="{ props }">
              <span v-bind="props" class="has-tooltip">{{ item.finding }}</span>
            </template>
          </v-tooltip>
        </td>
        <td>
          <div class="d-flex flex-column mt-2">
            <span v-for="{ sentenceTypeDesc } in item.sentence" class="mb-3">{{
              sentenceTypeDesc
            }}</span>
          </div>
        </td>
        <td class="text-no-wrap">
          <div class="d-flex flex-column mt-2">
            <span
              v-for="{ sentTermPeriodQty, sentTermCd } in item.sentence"
              class="mb-3"
              >{{
                sentTermPeriodQty
                  ? `${sentTermPeriodQty} ${sentTermCd}`
                  : '\u00A0'
              }}</span
            >
          </div>
        </td>
        <td>
          <div class="d-flex flex-column mt-2">
            <span v-for="{ sentMonetaryAmt } in item.sentence" class="mb-3">{{
              sentMonetaryAmt ?? '\u00A0'
            }}</span>
          </div>
        </td>
        <td class="text-no-wrap">
          <div class="d-flex flex-column mt-2">
            <span v-for="{ sentDueTtpDt } in item.sentence" class="mb-3">{{
              sentDueTtpDt ? formatDateToDDMMMYYYY(sentDueTtpDt) : '\u00A0'
            }}</span>
          </div>
        </td>
        <td class="text-no-wrap">
          <div class="d-flex flex-column mt-2">
            <span v-for="{ sentEffectiveDt } in item.sentence" class="mb-3">{{
              sentEffectiveDt
                ? formatDateToDDMMMYYYY(sentEffectiveDt)
                : '\u00A0'
            }}</span>
          </div>
        </td>
      </tr>
    </template>
  </v-data-table>
</template>
<script setup lang="ts">
  import { criminalParticipantType } from '@/types/criminal/jsonTypes';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { formatToFullName } from '@/utils/utils';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    participants: criminalParticipantType[];
  }>();

  const data = computed(() =>
    props.participants.flatMap((p) => {
      const { lastNm, givenNm, orgNm } = p;
      return p.count.map((count) => ({
        ...count,
        fullName: lastNm ? formatToFullName(lastNm, givenNm) : orgNm,
      }));
    })
  );

  const sortBy = ref([{ key: 'countNumber', order: 'asc' }] as const);
  const groupBy = ref([{ key: 'fullName', order: 'asc' as const }]);
  const headers = [
    { title: 'DATE', key: 'appearanceDate' },
    { title: 'COUNT', key: 'countNumber' },
    { title: 'CHARGE/ISSUE', key: 'sectionTxt' },
    { title: 'FINDING', key: 'finding' },
    { title: 'SENTENCE/DISPOSITION TYPE', key: 'sentences', sortable: false },
    { title: 'TERM', key: 'terms', sortable: false },
    { title: 'AMOUNT', key: 'amount', sortable: false },
    { title: 'DUE DATE', key: 'dueDate', sortable: false },
    { title: 'EFFECTIVE DATE', key: 'effectiveDate', sortable: false },
  ];
</script>
<style scoped>
  /* Hides the group-by column */
  .v-data-table :deep(th:first-child),
  .v-data-table :deep(tbody .v-data-table__tr td:first-child) {
    display: none;
  }
</style>
