<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3
        class="mx-4 font-weight-normal"
        v-if="!showSections['Future Appearances']"
      >
        Next Three Future Appearances
      </h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card v-if="!isDataReady && isMounted">
      <span class="text-muted ml-4 mb-5"> No future appearances. </span>
    </b-card>

    <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <b-card
      bg-variant="white"
      v-if="isDataReady"
      style="overflow: auto"
      no-body
      class="mx-3 mb-5"
    >
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
          <b-badge :style="data.field.cellStyle" variant="white">
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(date)="data">
          <span :class="data.field.cellClass" :style="data.field.cellStyle">
            <b-button
              style="transform: translate(-2px, -7px); font-size: 14px"
              size="sm"
              @click="
                OpenDetails(data);
                data.toggleDetails();
              "
              variant="outline-primary border-white  text-info"
              class="mr-2"
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
          <civil-appearance-details />
        </template>

        <template v-slot:cell(reason)="data">
          <b-badge
            :class="data.field.cellClass"
            variant="secondary"
            v-b-tooltip.hover.right
            :title="data.item.reasonDescription"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(result)="data">
          <span
            v-if="data.value"
            :class="data.field.cellClass"
            variant="outline-primary border-white"
            v-b-tooltip.hover.right
            :title="data.item.resultDescription"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </span>
        </template>

        <template v-slot:cell(presider)="data">
          <b-badge
            variant="secondary"
            v-if="data.value"
            :class="data.field.cellClass"
            :style="data.field.cellStyle"
            v-b-tooltip.hover.left
            :title="data.item.judgeFullName"
          >
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
  </b-card>
</template>

<script lang="ts">
  import CivilAppearanceDetails from '@/components/civil/CivilAppearanceDetails.vue';
  import { beautifyDate } from '@/filters';
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import { civilAppearancesListType } from '@/types/civil';
  import { civilApprDetailType } from '@/types/civil/jsonTypes';
  import * as _ from 'underscore';
  import { computed, defineComponent, onMounted, ref } from 'vue';

  enum appearanceStatus {
    UNCF = 'Unconfirmed',
    CNCL = 'Canceled',
    SCHD = 'Scheduled',
  }

  export default defineComponent({
    components: {
      CivilAppearanceDetails,
    },
    setup() {
      const commonStore = useCommonStore();
      const civilFileStore = useCivilFileStore();

      const isMounted = ref(false);
      const isDataReady = ref(false);

      const futureAppearancesList = ref<civilAppearancesListType[]>([]);
      const futureAppearancesJson = ref<civilApprDetailType[]>([]);
      const sortBy = ref('date');
      const sortDesc = ref(true);

      const getTableFieldSettings = () => {
        const commonStyles = {
          tdClass: 'border-top',
          cellStyle: 'font-size: 14px;',
        };

        const headerStyles = {
          textPrimary: 'text-primary',
          text: 'text',
        };

        const cellClasses = {
          badge: 'badge badge-secondary mt-2',
          text: 'text',
          inlineText: 'text-info mt-2 d-inline-flex',
        };

        return [
          {
            key: 'date',
            label: 'Date',
            sortable: true,
            ...commonStyles,
            headerStyle: headerStyles.textPrimary,
            cellClass: cellClasses.inlineText,
            cellStyle: `${commonStyles.cellStyle} display: inline-flex;`,
          },
          {
            key: 'reason',
            label: 'Reason',
            sortable: true,
            ...commonStyles,
            headerStyle: headerStyles.textPrimary,
            cellClass: cellClasses.badge,
            cellStyle: `${commonStyles.cellStyle} text-align:left;`,
          },
          {
            key: 'documentType',
            label: 'Document Type',
            sortable: false,
            ...commonStyles,
            headerStyle: headerStyles.text,
            cellClass: cellClasses.text,
            cellStyle: `${commonStyles.cellStyle} font-weight: normal; padding-top:12px;`,
          },
          {
            key: 'result',
            label: 'Result',
            sortable: true,
            ...commonStyles,
            headerStyle: headerStyles.textPrimary,
            cellClass: cellClasses.badge,
          },
          {
            key: 'time',
            label: 'Time',
            sortable: false,
            ...commonStyles,
            headerStyle: headerStyles.text,
            cellClass: cellClasses.text,
            cellStyle: `${commonStyles.cellStyle} font-weight: normal; padding-top:12px;`,
          },
          {
            key: 'duration',
            label: 'Duration',
            sortable: false,
            ...commonStyles,
            headerStyle: headerStyles.text,
            cellClass: cellClasses.text,
            cellStyle: `${commonStyles.cellStyle} font-weight: normal; padding-top:12px;`,
          },
          {
            key: 'location',
            label: 'Location',
            sortable: true,
            ...commonStyles,
            headerStyle: headerStyles.textPrimary,
            cellClass: cellClasses.text,
            cellStyle: `${commonStyles.cellStyle} font-weight: normal; padding-top:12px;`,
          },
          {
            key: 'room',
            label: 'Room',
            sortable: false,
            ...commonStyles,
            headerStyle: headerStyles.text,
            cellClass: cellClasses.text,
            cellStyle: `${commonStyles.cellStyle} font-weight: normal; padding-top:12px;`,
          },
          {
            key: 'presider',
            label: 'Presider',
            sortable: true,
            ...commonStyles,
            headerStyle: headerStyles.textPrimary,
            cellClass: cellClasses.badge,
          },
          {
            key: 'status',
            label: 'Status',
            sortable: true,
            ...commonStyles,
            headerStyle: headerStyles.textPrimary,
            cellClass: cellClasses.badge,
          },
        ];
      };

      const fields = ref(getTableFieldSettings());

      onMounted(() => {
        getFutureAppearances();
      });

      const getFutureAppearances = () => {
        const data = civilFileStore.civilFileInformation.detailsData;
        futureAppearancesJson.value = data.appearances.apprDetail;
        ExtractFutureAppearancesInfo();
        if (futureAppearancesList.value.length) {
          isDataReady.value = true;
        }

        isMounted.value = true;
      };

      const ExtractFutureAppearancesInfo = () => {
        const currentDate = new Date();
        for (const appIndex in futureAppearancesJson.value) {
          const appInfo = {} as civilAppearancesListType;
          const jApp = futureAppearancesJson.value[appIndex];

          appInfo.index = appIndex;
          appInfo.date = jApp.appearanceDt.split(' ')[0];
          if (new Date(appInfo.date) < currentDate) continue;
          appInfo.formattedDate = beautifyDate(appInfo.date);
          appInfo.documentType = jApp.documentTypeDsc
            ? jApp.documentTypeDsc
            : '';
          appInfo.result = jApp.appearanceResultCd;
          appInfo.resultDescription = jApp.appearanceResultDsc
            ? jApp.appearanceResultDsc
            : '';
          appInfo.time = getTime(jApp.appearanceTm.split(' ')[1].substr(0, 5));
          appInfo.reason = jApp.appearanceReasonCd;
          appInfo.reasonDescription = jApp.appearanceReasonDsc
            ? jApp.appearanceReasonDsc
            : '';
          appInfo.duration = getDuration(
            jApp.estimatedTimeHour,
            jApp.estimatedTimeMin
          );
          appInfo.location = jApp.courtLocation ? jApp.courtLocation : '';
          appInfo.room = jApp.courtRoomCd;
          appInfo.status = jApp.appearanceStatusCd
            ? appearanceStatus[jApp.appearanceStatusCd]
            : '';
          appInfo.statusStyle = getStatusStyle(appInfo.status);
          appInfo.presider = jApp.judgeInitials ? jApp.judgeInitials : '';
          appInfo.judgeFullName = jApp.judgeInitials ? jApp.judgeFullNm : '';
          appInfo.appearanceId = jApp.appearanceId;
          appInfo.supplementalEquipment = jApp.supplementalEquipmentTxt;
          appInfo.securityRestriction = jApp.securityRestrictionTxt;
          appInfo.outOfTownJudge = jApp.outOfTownJudgeTxt;

          futureAppearancesList.value.push(appInfo);
        }
      };

      const getStatusStyle = (status) => {
        commonStore.updateStatusStyle(status);
        return commonStore.statusStyle;
      };

      const getTime = (time) => {
        commonStore.updateTime(time);
        return commonStore.time;
      };

      const getDuration = (hr, min) => {
        commonStore.updateDuration({ hr: hr, min: min });
        return commonStore.duration;
      };

      const OpenDetails = (data) => {
        if (!data.detailsShowing) {
          civilFileStore.civilAppearanceInfo.fileNo =
            civilFileStore.civilFileInformation.fileNumber;
          civilFileStore.civilAppearanceInfo.date = data.item.formattedDate;
          civilFileStore.civilAppearanceInfo.appearanceId =
            data.item.appearanceId;
          civilFileStore.civilAppearanceInfo.supplementalEquipmentTxt =
            data.item.supplementalEquipment;
          civilFileStore.civilAppearanceInfo.securityRestrictionTxt =
            data.item.securityRestriction;
          civilFileStore.civilAppearanceInfo.outOfTownJudgeTxt =
            data.item.outOfTownJudge;

          civilFileStore.updateCivilAppearanceInfo(
            civilFileStore.civilAppearanceInfo
          );
        }
      };

      const sortChanged = () => {
        SortedFutureAppearances.value.forEach((item) => {
          item['_showDetails'] = false;
        });
      };

      const SortedFutureAppearances = computed(
        (): civilAppearancesListType[] => {
          if (civilFileStore.showSections['Future Appearances']) {
            return futureAppearancesList.value;
          } else {
            return _.sortBy(futureAppearancesList.value, 'date')
              .reverse()
              .slice(0, 3);
          }
        }
      );

      return {
        showSections: civilFileStore.showSections,
        isDataReady,
        isMounted,
        SortedFutureAppearances,
        fields,
        sortBy,
        sortDesc,
        sortChanged,
        OpenDetails,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
