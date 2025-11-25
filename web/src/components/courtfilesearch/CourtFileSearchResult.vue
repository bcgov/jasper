<template>
  <div>
    <v-row>
      <v-col>
        <span>
          <strong>
            {{ searchResults.length ? resultsText : '&nbsp;' }}
          </strong>
        </span>
      </v-col>
    </v-row>
    <v-banner bg-color="#efedf5">
      <h3 class="px-2 py-2">Search Results</h3>
    </v-banner>
    <v-skeleton-loader type="table" :loading="isSearching">
      <v-data-table-virtual
        v-model="selectedItems"
        show-select
        :group-by
        :items-per-page="maxItemsPerPage"
        :headers
        :sort-by="sortBy"
        return-object
        :items="searchResults"
        :item-value="idSelector"
      >
        <template
          v-slot:group-header="{ item, columns, isGroupOpen, toggleGroup }"
        >
          <tr>
            <td class="pa-0" style="height: 1rem" :colspan="columns.length">
              <v-banner
                :class="
                  bannerClasses[getCourtClassLabel(item.value)] ||
                  'table-banner'
                "
                :ref="
                  () => {
                    if (!isGroupOpen(item)) toggleGroup(item);
                  }
                "
              >
                {{
                  isCriminal
                    ? 'Criminal - ' + getClass(item.value)
                    : getClass(item.value)
                }}
              </v-banner>
            </td>
          </tr>
        </template>
        <template #item.sealStatusCd="{ item }">
          <span v-if="item.sealStatusCd != 'NA'" class="sealed">(Sealed)</span>
        </template>
        <template #item.participant="{ item }">
          <a href="#" @click.prevent="emitFilesViewed(item)">
            {{ [...new Set(item.participant.map((p) => p.fullNm))].join('; ') }}
          </a>
        </template>
      </v-data-table-virtual>
    </v-skeleton-loader>
    <ActionBar :selected="selectedItems" @clicked="handleViewFilesClick">
      <v-btn
        size="large"
        class="mx-2"
        :prepend-icon="mdiFileDocumentOutline"
        style="letter-spacing: 0.001rem"
        @click="handleViewFilesClick"
      >
        View case details
      </v-btn>
    </ActionBar>
  </div>
</template>

<script setup lang="ts">
  import ActionBar from '@/components/shared/table/ActionBar.vue';
  import { bannerClasses } from '@/constants/bannerClasses';
  import { beautifyDate } from '@/filters';
  import { KeyValueInfo, LookupCode } from '@/types/common';
  import { FileDetail } from '@/types/courtFileSearch';
  import { LocationInfo } from '@/types/courtlist';
  import { getCourtClassLabel } from '@/utils/utils';
  import { mdiFileDocumentOutline } from '@mdi/js';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    courtRooms: LocationInfo[];
    searchResults: FileDetail[];
    classes: LookupCode[];
    isSearching: boolean;
    isCriminal: boolean;
    isSearchResultsOver: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'add-selected', file: FileDetail): void;
    (e: 'remove-selected', idSelector: string, id: string): void;
    (e: 'clear-selected'): void;
    (e: 'files-viewed', files: KeyValueInfo[]): void;
  }>();

  // The reason we use 103 instead of 100 is due to the ability to have up to 3 group headers in the table
  const maxItemsPerPage = 103;
  const selectedItems = defineModel<FileDetail[]>();
  const resultsText = computed(
    () =>
      `${props.searchResults.length ? props.searchResults.length + ' results' : ''}`
  );

  const idSelector = computed(() =>
    props.isCriminal ? 'mdocJustinNo' : 'physicalFileId'
  );
  const sortBy = ref([{ key: 'nextApprDt', order: 'desc' }] as const);

  const groupBy = ref([
    {
      key: 'courtClassCd',
      order: 'desc' as const,
    },
  ]);
  const fullHeaders = ref([
    { key: 'data-table-group' },
    {
      title: 'File #',
      key: 'fileNumberTxt',
      value: (item: FileDetail) => `${getFormattedFileNumber(item)}`,
    },
    {
      title: 'Accused / Parties',
      key: 'participant',
    },
    {
      title: 'Class',
      key: 'courtClassCd',
      value: (item: FileDetail) => `${getClass(item.courtClassCd)}`,
    },
    {
      title: 'Location',
      key: 'location',
      width: '15%',
      value: (item: FileDetail) => `${getLocation(item.fileHomeAgencyId)}`,
    },
    {
      title: 'Charges',
      key: 'charges',
      value: (item: FileDetail) => {
        const uniqueCharges = [
          ...new Set(
            item.participant.flatMap((p) => p.charge).map((c) => c.sectionTxt)
          ),
        ];
        const firstCharge = uniqueCharges.length > 0 ? uniqueCharges[0] : '';
        return uniqueCharges.length > 1
          ? `${firstCharge} + [${uniqueCharges.length - 1}]`
          : firstCharge;
      },
    },
    {
      title: 'Next appearance',
      key: 'nextApprDt',
      value: (item: FileDetail) => `${beautifyDate(item.nextApprDt)}`,
      sortRaw: (a: FileDetail, b: FileDetail) => {
        const aTime = a.nextApprDt
          ? new Date(a.nextApprDt).getTime()
          : Number.MIN_VALUE;
        const bTime = b.nextApprDt
          ? new Date(b.nextApprDt).getTime()
          : Number.MIN_VALUE;
        return aTime - bTime;
      },
    },
    { title: 'OW', key: 'warrantyYN' },
    { title: 'IC', key: 'inCustodyYN' },
    {
      title: '',
      key: 'sealStatusCd',
    },
  ]);

  const headers = computed(() => {
    return props.isCriminal
      ? fullHeaders.value
      : fullHeaders.value.filter(
          (hdr) => !['charges', 'warrantyYN', 'inCustodyYN'].includes(hdr.key)
        );
  });

  const getFormattedFileNumber = (detail: FileDetail) =>
    `${detail.ticketSeriesTxt ?? ''}${detail.fileNumberTxt}${detail.mdocSeqNo ? '-' + detail.mdocSeqNo : ''}${detail.mdocRefTypeCd ? '-' + detail.mdocRefTypeCd : ''}`;
  

  const getLocation = (fileHomeAgencyId: string) =>
    props.courtRooms.find((room) => room.code === fileHomeAgencyId)?.name || '';
  const getClass = (courtClassCd: string) =>
    props.classes.find((lookup) => lookup.code === courtClassCd)?.shortDesc ||
    '';

  const handleViewFilesClick = () => emitFilesViewed(selectedItems.value ?? []);

  function emitFilesViewed(details: FileDetail | FileDetail[]) {
    const items = Array.isArray(details) ? details : [details];
    const files = items.map((c) => ({
      key: c[idSelector.value],
      value: getFormattedFileNumber(c),
    })) as KeyValueInfo[];
    emit('files-viewed', files);
  }
</script>
<style scoped>
  .sealed {
    color: var(--text-red-500);
    text-transform: uppercase;
    font-weight: bold;
  }
</style>
