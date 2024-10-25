<template>
  <b-card v-if="isMounted && userInfo.userType != 'vc'" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal">Case Notes and Comments</h3>
      <hr class="mx-3 bg-light" style="height: 5px;" />
    </div>
    <b-card class="mb-5" v-if="!notesExist">
      <span class="text-muted ml-4"> No notes or comments. </span>
    </b-card>
    <b-card v-if="notesExist" bg-variant="white" no-body class="mx-3 mb-5">
      <b-table :items="civilNotes" :fields="fields" thead-class="d-none" responsive="sm" borderless small striped>
        <template v-slot:cell(notesFieldName)="data">
          <span>
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
import "@store/modules/CivilFileInformation";
import { civilFileInformationType, civilNotesType } from "@/types/civil";
import { UserInfo } from "@/types/common";
const civilState = namespace("CivilFileInformation");
const commonState = namespace("CommonInformation");

@Component
export default class CivilCommentNotes extends Vue {
  @civilState.State
  public civilFileInformation!: civilFileInformationType;

  @commonState.State
  public userInfo!: UserInfo;

  civilNotes: civilNotesType[] = [];
  isMounted = false;
  notesExist = false;

  fields = [
    { key: "notesFieldName", label: "Notes Field Name", tdClass: "border-top" },
    { key: "notesValue", label: "Notes Value", tdClass: "border-top" },
  ];

  public getCivilNotes(): void {
    const data = this.civilFileInformation.detailsData;

    let notesInfo = {} as civilNotesType;
    notesInfo.notesFieldName = "Trial Remark";
    notesInfo.notesValue = data.trialRemarkTxt ? data.trialRemarkTxt : "";
    this.civilNotes.push(notesInfo);
    notesInfo = {} as civilNotesType;

    notesInfo.notesFieldName = "Comment To Judge";
    notesInfo.notesValue = data.commentToJudgeTxt ? data.commentToJudgeTxt : "";
    this.civilNotes.push(notesInfo);
    notesInfo = {} as civilNotesType;

    notesInfo.notesFieldName = "File Comment";
    notesInfo.notesValue = data.fileCommentText ? data.fileCommentText : "";
    this.civilNotes.push(notesInfo);
    if (data.trialRemarkTxt || data.commentToJudgeTxt || data.fileCommentText) {
      this.notesExist = true;
    }

    this.isMounted = true;
  }

  mounted() {
    this.getCivilNotes();
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
