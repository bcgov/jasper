<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal" v-if="!showSections['Future Appearances']">Next Three Future Appearances</h3>
      <hr class="mx-3 bg-light" style="height: 5px;" />
    </div>

    <b-card v-if="!isDataReady && isMounted" no-body>
      <span class="text-muted ml-4 mb-5"> No future appearances. </span>
    </b-card>

    <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px;" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <b-card bg-variant="white" v-if="isDataReady" no-body class="mx-3 mb-5" style="overflow:auto">
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
        <template v-for="(field, index) in fields" v-slot:[`head(${field.key})`]="data">
          <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
        </template>

        <template v-slot:cell()="data">
          <b-badge style="font-weight: normal; font-size: 16px; padding-top:12px" variant="white">
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(sate)="data">
          <span :class="data.field.cellClass" style="display: inline-flex;">
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
              <b-icon-caret-right-fill v-if="!data.item['_showDetails']"></b-icon-caret-right-fill>
              <b-icon-caret-down-fill v-if="data.item['_showDetails']"></b-icon-caret-down-fill>
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
          <b-badge variant="white" :style="data.field.cellStyle" class="mt-2"> {{ data.value }} </b-badge>
        </template>

        <template v-slot:cell(status)="data">
          <b :class="data.item.statusStyle" :style="data.field.cellStyle"> {{ data.value }} </b>
        </template>
      </b-table>
    </b-card>
  </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import * as _ from "underscore";
import CriminalAppearanceDetails from "@components/criminal/CriminalAppearanceDetails.vue";
import "@store/modules/CommonInformation";
import { criminalAppearancesListType, criminalAppearanceInfoType, criminalFileInformationType } from "@/types/criminal";
import { InputNamesType, DurationType } from "@/types/common";
import { criminalApprDetailType } from "@/types/criminal/jsonTypes";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

enum appearanceStatus {
  UNCF = "Unconfirmed",
  CNCL = "Canceled",
  SCHD = "Scheduled",
}

@Component({
  components: {
    CriminalAppearanceDetails,
  },
})
export default class CriminalFutureAppearances extends Vue {
  @criminalState.State
  public showSections;

  @commonState.State
  public displayName!: string;

  @commonState.State
  public duration;

  @commonState.State
  public time;

  @commonState.State
  public statusStyle;

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @criminalState.State
  public criminalAppearanceInfo!: criminalAppearanceInfoType;

  @criminalState.Action
  public UpdateCriminalAppearanceInfo!: (newCriminalAppearanceInfo: criminalAppearanceInfoType) => void;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  @commonState.Action
  public UpdateDuration!: (duration: DurationType) => void;

  @commonState.Action
  public UpdateTime!: (time: string) => void;

  @commonState.Action
  public UpdateStatusStyle!: (statusStyle: string) => void;

  futureAppearancesList: criminalAppearancesListType[] = [];
  isMounted = false;
  isDataReady = false;
  futureAppearancesJson: criminalApprDetailType[] = [];
  sortBy = "date";
  sortDesc = true;

  fields = [
    {
      key: "date",
      label: "Date",
      sortable: true,
      tdClass: "border-top",
      headerStyle: "text-primary",
      cellStyle: "transform: translate(0,-7px); font-size:16px",
      cellClass: "text-info mt-2 d-inline-flex",
    },
    {
      key: "reason",
      label: "Reason",
      sortable: true,
      tdClass: "border-top",
      headerStyle: "text-primary",
      cellStyle: "margin-top: 10px; font-size: 14px;",
    },
    { key: "time", label: "Time", sortable: false, tdClass: "border-top", headerStyle: "text" },
    { key: "duration", label: "Duration", sortable: false, tdClass: "border-top", headerStyle: "text" },
    { key: "location", label: "Location", sortable: true, tdClass: "border-top", headerStyle: "text-primary" },
    { key: "room", label: "Room", sortable: false, tdClass: "border-top", headerStyle: "text" },
    {
      key: "accused",
      label: "Accused",
      sortable: true,
      tdClass: "border-top",
      headerStyle: "text-primary",
      cellStyle: "font-size: 16px;",
    },
    {
      key: "status",
      label: "Status",
      sortable: true,
      tdClass: "border-top",
      headerStyle: "text-primary",
      cellStyle: "font-weight: normal; font-size: 16px; width:110px",
    },
  ];

  mounted() {
    this.getFutureAppearances();
  }

  public getFutureAppearances(): void {
    const data = this.criminalFileInformation.detailsData;
    this.futureAppearancesJson = data.appearances.apprDetail;
    this.ExtractFutureAppearancesInfo();
    if (this.futureAppearancesList.length) {
      this.isDataReady = true;
    }
    this.isMounted = true;
  }

  public ExtractFutureAppearancesInfo(): void {
    const currentDate = new Date();

    for (const appIndex in this.futureAppearancesJson) {
      const appInfo = {} as criminalAppearancesListType;
      const jApp = this.futureAppearancesJson[appIndex];

      appInfo.index = appIndex;
      appInfo.date = jApp.appearanceDt.split(" ")[0];
      if (new Date(appInfo.date) < currentDate) continue;
      appInfo.formattedDate = Vue.filter("beautify_date")(appInfo.date);
      appInfo.time = this.getTime(jApp.appearanceTm.split(" ")[1].substr(0, 5));
      appInfo.reason = jApp.appearanceReasonCd;
      appInfo.reasonDescription = jApp.appearanceReasonDsc ? jApp.appearanceReasonDsc : "";

      appInfo.duration = this.getDuration(jApp.estimatedTimeHour, jApp.estimatedTimeMin);
      appInfo.location = jApp.courtLocation ? jApp.courtLocation : "";
      appInfo.room = jApp.courtRoomCd;

      appInfo.firstName = jApp.givenNm ? jApp.givenNm : "";
      appInfo.lastName = jApp.lastNm ? jApp.lastNm : jApp.orgNm;
      appInfo.accused = this.getNameOfParticipant(appInfo.lastName, appInfo.firstName);
      appInfo.status = jApp.appearanceStatusCd ? appearanceStatus[jApp.appearanceStatusCd] : "";
      appInfo.statusStyle = this.getStatusStyle(appInfo.status);
      appInfo.presider = jApp.judgeInitials ? jApp.judgeInitials : "";
      appInfo.judgeFullName = jApp.judgeInitials ? jApp.judgeFullNm : "";

      appInfo.appearanceId = jApp.appearanceId;
      appInfo.partId = jApp.partId;
      appInfo.supplementalEquipment = jApp.supplementalEquipmentTxt;
      appInfo.securityRestriction = jApp.securityRestrictionTxt;
      appInfo.outOfTownJudge = jApp.outOfTownJudgeTxt;
      appInfo.profSeqNo = jApp.profSeqNo;
      this.futureAppearancesList.push(appInfo);
    }
  }

  public getStatusStyle(status) {
    this.UpdateStatusStyle(status);
    return this.statusStyle;
  }

  public getNameOfParticipant(lastName, givenName) {
    this.UpdateDisplayName({ lastName: lastName, givenName: givenName });
    return this.displayName;
  }

  public getTime(time) {
    this.UpdateTime(time);
    return this.time;
  }

  public getDuration(hr, min) {
    this.UpdateDuration({ hr: hr, min: min });
    return this.duration;
  }

  public OpenDetails(data) {
    if (!data.detailsShowing) {
      this.criminalAppearanceInfo.fileNo = this.criminalFileInformation.fileNumber;
      this.criminalAppearanceInfo.courtLevel = this.criminalFileInformation.courtLevel;
      this.criminalAppearanceInfo.courtClass = this.criminalFileInformation.courtClass;
      this.criminalAppearanceInfo.date = data.item.formattedDate;
      this.criminalAppearanceInfo.appearanceId = data.item.appearanceId;
      this.criminalAppearanceInfo.partId = data.item.partId;
      this.criminalAppearanceInfo.supplementalEquipmentTxt = data.item.supplementalEquipment;
      this.criminalAppearanceInfo.securityRestrictionTxt = data.item.securityRestriction;
      this.criminalAppearanceInfo.outOfTownJudgeTxt = data.item.outOfTownJudge;
      this.criminalAppearanceInfo.profSeqNo = data.item.profSeqNo;
      this.UpdateCriminalAppearanceInfo(this.criminalAppearanceInfo);
    }
  }

  public sortChanged() {
    this.SortedFutureAppearances.forEach((item) => {
      this.$set(item, "_showDetails", false);
    });
  }

  get SortedFutureAppearances() {
    if (this.showSections["Future Appearances"]) {
      return this.futureAppearancesList;
    } else {
      return _.sortBy(this.futureAppearancesList, "date")
        .reverse()
        .slice(0, 3);
    }
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
