<template>
  <b-card v-if="isMounted" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal">Crown Information</h3>
      <hr class="mx-3 bg-light" style="height: 5px;" />
    </div>
    <b-card bg-variant="white" no-body class="mx-3 mb-5">
      <b-table :items="crownInformation" :fields="fields" thead-class="d-none" responsive="sm" borderless small striped>
        <template v-slot:cell(crownInfoValue)="data">
          <span v-if="data.item.crownInfoFieldName == 'Crown Assigned'">
            <b
              v-for="(assignee, index) in data.item.crownInfoValue.split('+')"
              v-bind:key="index"
              style="white-space: pre-line"
              >{{ assignee }} <br
            /></b>
          </span>
          <span v-if="data.item.crownInfoFieldName != 'Crown Assigned'">
            <b> {{ data.value }}</b>
          </span>
        </template>
      </b-table>
    </b-card>
  </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import "@store/modules/CriminalFileInformation";
import "@store/modules/CommonInformation";
import { criminalFileInformationType, criminalCrownInformationInfoType } from "@/types/criminal";
import { InputNamesType } from "@/types/common";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

@Component
export default class CriminalCrownInformation extends Vue {
  @commonState.State
  public displayName!: string;

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  crownInformation: criminalCrownInformationInfoType[] = [];

  isMounted = false;
  fields = [
    { key: "crownInfoFieldName", tdClass: "border-top", label: "Crown Info Field Name" },
    { key: "crownInfoValue", tdClass: "border-top", label: "Crown Info Value" },
  ];

  public getCrownInfo(): void {
    const data = this.criminalFileInformation.detailsData;
    let assignedCrown = "";
    if (data.crown.length > 0) {
      for (const assignee of data.crown) {
        if (assignee.assigned) {
          this.UpdateDisplayName({ lastName: assignee.lastNm, givenName: assignee.givenNm });
          assignedCrown += this.displayName + "+";
        }
      }
      assignedCrown = assignedCrown.substr(0, assignedCrown.length - 1);
    }
    let crownInfo = {} as criminalCrownInformationInfoType;
    crownInfo.crownInfoFieldName = "Crown Assigned";
    crownInfo.crownInfoValue = assignedCrown;
    this.crownInformation.push(crownInfo);
    crownInfo = {} as criminalCrownInformationInfoType;
    crownInfo.crownInfoFieldName = "Crown Time Estimate";
    if (data.crownEstimateLenQty) {
      if (data.crownEstimateLenQty == 1) {
        crownInfo.crownInfoValue = data.crownEstimateLenQty + " " + data.crownEstimateLenDsc.replace("s", "");
      } else {
        crownInfo.crownInfoValue = data.crownEstimateLenQty + " " + data.crownEstimateLenDsc;
      }
    } else {
      crownInfo.crownInfoValue = "";
    }
    this.crownInformation.push(crownInfo);
    crownInfo = {} as criminalCrownInformationInfoType;
    crownInfo.crownInfoFieldName = "Case Age";
    crownInfo.crownInfoValue = data.caseAgeDays ? data.caseAgeDays + " Days" : "";
    this.crownInformation.push(crownInfo);
    crownInfo = {} as criminalCrownInformationInfoType;
    crownInfo.crownInfoFieldName = "Approved By";
    if (data.approvedByAgencyCd) {
      crownInfo.crownInfoValue = data.approvedByPartNm
        ? data.approvedByAgencyCd + " - " + data.approvedByPartNm
        : data.approvedByAgencyCd + " -";
    } else {
      crownInfo.crownInfoValue = "";
    }
    this.crownInformation.push(crownInfo);
    this.isMounted = true;
  }

  mounted() {
    this.getCrownInfo();
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
