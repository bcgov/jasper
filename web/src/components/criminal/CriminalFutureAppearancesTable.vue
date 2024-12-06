<template>
  <b-card bg-variant="white" no-body class="mx-3 mb-5" style="overflow: auto">
    <b-table
      :items="SortedFutureAppearances"
      :fields="fields"
      :sort-by.sync="sortBy"
      :sort-desc.sync="sortDesc"
      :no-sort-reset="true"
      sort-icon-left
      borderless
      @sort-changed="sortChanged"
      small
      responsive="sm"
    >
      <template
        v-for="(field, index) in fields"
        v-slot:[`head(${field.key})`]="data"
      >
        <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
      </template>

      <template v-slot:cell()="data">
        <b-badge
          style="font-weight: normal; font-size: 16px; padding-top: 12px"
          variant="white"
        >
          {{ data.value }}
        </b-badge>
      </template>

      <template v-slot:cell(sate)="data">
        <span :class="data.field.cellClass" style="display: inline-flex">
          <b-button
            :style="data.field.cellStyle"
            size="sm"
            @click="
              OpenDetails(data);
              data.toggleDetails();
            "
            variant="outline-primary border-white text-info"
            class="mr-2 mt-1"
          >
            <b-icon-caret-right-fill
              v-if="!data.item['_showDetails']"
            ></b-icon-caret-right-fill>
            <b-icon-caret-down-fill
              v-if="data.item['_showDetails']"
            ></b-icon-caret-down-fill>
            {{ data.item.formattedDate }}
          </b-button>
        </span>
      </template>
      <template v-slot:row-details>
        <b-card>
          <criminal-appearance-details />
        </b-card>
      </template>

      <template v-slot:cell(reason)="data">
        <b-badge
          variant="secondary"
          v-b-tooltip.hover.right
          :title="data.item.reasonDescription"
          :style="data.field.cellStyle"
        >
          {{ data.value }}
        </b-badge>
      </template>

      <template v-slot:cell(accused)="data">
        <b-badge variant="white" :style="data.field.cellStyle" class="mt-2">
          {{ data.value }}
        </b-badge>
      </template>

      <template v-slot:cell(status)="data">
        <b :class="data.item.statusStyle" :style="data.field.cellStyle">
          {{ data.value }}
        </b>
      </template>
    </b-table>
  </b-card>
</template>
<script lang="ts">
  import { useCriminalFileStore } from '@/stores';
  import { criminalAppearancesListType } from '@/types/criminal';
  import { defineComponent, PropType, ref } from 'vue';
  import CriminalAppearanceDetails from './CriminalAppearanceDetails.vue';

  export default defineComponent({
    components: {
      CriminalAppearanceDetails,
    },
    props: {
      SortedFutureAppearances: {
        type: Array as PropType<criminalAppearancesListType[]>,
        default: () => [],
      },
    },
    setup(props) {
      const sortBy = ref('date');
      const sortDesc = ref(true);
      const criminalFileStore = useCriminalFileStore();

      const baseField = {
        tdClass: 'border-top',
        headerStyle: 'text',
      };

      const fieldConfigs = [
        {
          key: 'date',
          label: 'Date',
          sortable: true,
          additionalStyles: {
            headerStyle: 'text-primary',
            cellStyle: 'transform: translate(0,-7px); font-size:16px',
            cellClass: 'text-info mt-2 d-inline-flex',
          },
        },
        {
          key: 'reason',
          label: 'Reason',
          sortable: true,
          additionalStyles: {
            headerStyle: 'text-primary',
            cellStyle: 'margin-top: 10px; font-size: 14px;',
          },
        },
        { key: 'time', label: 'Time', sortable: false },
        { key: 'duration', label: 'Duration', sortable: false },
        {
          key: 'location',
          label: 'Location',
          sortable: true,
          additionalStyles: { headerStyle: 'text-primary' },
        },
        { key: 'room', label: 'Room', sortable: false },
        {
          key: 'accused',
          label: 'Accused',
          sortable: true,
          additionalStyles: {
            headerStyle: 'text-primary',
            cellStyle: 'font-size: 16px;',
          },
        },
        {
          key: 'status',
          label: 'Status',
          sortable: true,
          additionalStyles: {
            headerStyle: 'text-primary',
            cellStyle: 'font-weight: normal; font-size: 16px; width:110px',
          },
        },
      ];

      const fields = fieldConfigs.map(
        ({ key, label, sortable, additionalStyles = {} }) => ({
          ...baseField,
          key,
          label,
          sortable,
          ...additionalStyles,
        })
      );

      const sortChanged = () => {
        props.SortedFutureAppearances.forEach((item) => {
          item['_showDetails'] = false;
        });
      };

      const OpenDetails = (data) => {
        if (!data.detailsShowing) {
          const { criminalFileInformation, criminalAppearanceInfo } =
            criminalFileStore;
          const { item } = data;

          const mappings = {
            fileNo: criminalFileInformation.fileNumber,
            courtLevel: criminalFileInformation.courtLevel,
            courtClass: criminalFileInformation.courtClass,
            date: item.formattedDate,
            appearanceId: item.appearanceId,
            partId: item.partId,
            supplementalEquipmentTxt: item.supplementalEquipment,
            securityRestrictionTxt: item.securityRestriction,
            outOfTownJudgeTxt: item.outOfTownJudge,
            profSeqNo: item.profSeqNo,
          };

          Object.keys(mappings).forEach((key) => {
            criminalAppearanceInfo[key] = mappings[key];
          });

          criminalFileStore.updateCriminalAppearanceInfo(
            criminalAppearanceInfo
          );
        }
      };

      return {
        fields,
        sortBy,
        sortDesc,
        sortChanged,
        OpenDetails,
      };
    },
  });
</script>
