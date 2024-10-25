<template>
  <div>
    <b-card bg-variant="light" v-if="!isMounted">
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
    <b-card bg-variant="light" v-else no-body>
      <b-card bg-variant="white">
        <b-row cols="2">
          <b-col md="8" cols="8" style="overflow: auto;">
            <b-overlay :show="loadingROP" rounded="sm">
              <div>
                <b-button-group
                  ><h3 class="mx-2 mt-2 font-weight-normal" style="height: 10px;">Charges</h3>
                  <b-button
                    variant="outline-primary text-info"
                    style="border:0px;"
                    class="mt-1"
                    v-b-tooltip.hover.right
                    title="Download Record Of Proceeding"
                    @click="openDocumentsPdf(documentTypeRop)"
                    size="sm"
                  >
                    <b-icon icon="file-earmark-arrow-down" font-scale="2"></b-icon>
                  </b-button>
                </b-button-group>
                <hr class="mb-0 bg-light" style="height: 5px;" />
              </div>
              <b-card v-if="!(appearanceCharges.length > 0)" style="border: white;">
                <span class="text-muted"> No charges. </span>
              </b-card>
              <b-table
                style="max-height: 300px; overflow-y: auto;"
                v-if="appearanceCharges.length > 0"
                :items="appearanceCharges"
                :fields="chargesFields"
                borderless
                striped
                responsive="sm"
              >
                <template v-slot:head="data">
                  <b> {{ data.label }}</b>
                </template>

                <template v-slot:cell(finding)="data">
                  <b-badge
                    variant="secondary"
                    v-if="data.item.finding"
                    v-b-tooltip.hover.left
                    :title="data.item.findingDescription"
                  >
                    {{ data.item.finding }}
                  </b-badge>
                </template>

                <template v-slot:cell(lastResult)="data">
                  <b-badge
                    variant="secondary"
                    v-if="data.item.lastResult"
                    v-b-tooltip.hover.left
                    :title="data.item.lastResultDescription"
                  >
                    {{ data.item.lastResult }}
                  </b-badge>
                </template>
              </b-table>
              <template v-slot:overlay>
                <div style="text-align: center">
                  <loading-spinner />
                  <p id="Downloading-label">Downloading PDF file ...</p>
                </div>
              </template>
            </b-overlay>
          </b-col>
          <b-col col md="4" cols="4" style="overflow: auto;">
            <div>
              <b-button-group>
                <h3 class="mx-2 font-weight-normal" style="margin-top:8px; height:12px">Additional Info</h3>
                <b-button
                  size="sm"
                  style=" font-size:12px; border:0px;"
                  @click="OpenNotes()"
                  variant="outline-primary text-info"
                  v-if="notes.judgeRec.length > 0 || notes.appNote.length > 0"
                  class="mt-1"
                  v-b-tooltip.hover
                  title="Notes"
                >
                  <b-icon icon="chat-square-fill" font-scale="2"></b-icon>
                </b-button>
              </b-button-group>
              <hr class="mb-0 bg-light" style="height: 5px;" />
            </div>
            <b-card v-if="!(appearanceAdditionalInfo.length > 0)" style="border: white;">
              <span class="text-muted"> No additional information. </span>
            </b-card>
            <b-table
              v-if="appearanceAdditionalInfo.length > 0"
              :items="appearanceAdditionalInfo"
              :fields="addInfoFields"
              thead-class="d-none"
              borderless
              responsive="sm"
            >
              <template v-slot:cell(key)="data">
                <b>{{ data.value }}</b>
              </template>
            </b-table>

            <div v-if="appearanceMethods.length > 0">
              <h3 class="mx-2 font-weight-normal">Appearance Methods</h3>
              <hr class="mb-0 bg-light" style="height: 5px;" />
            </div>
            <b-table
              v-if="appearanceMethods.length > 0"
              :items="appearanceMethods"
              :fields="appearanceMethodsField"
              thead-class="d-none"
              borderless
              responsive="sm"
            >
              <template v-for="(field, index) in appearanceMethodsField" v-slot:[`cell(${field.key})`]="data">
                <span v-bind:key="index">
                  <span :class="data.field.cellClass" :style="data.field.cellStyle"
                    ><b>{{ data.item.role }}</b> is appearing by {{ data.item.method }}<br />
                  </span>
                  <span
                    v-if="data.item.phoneNumber.length > 0"
                    :class="data.field.cellClass"
                    :style="data.field.cellStyle"
                    ><b>Phone number: </b>{{ data.item.phoneNumber }}.<br />
                  </span>
                  <span v-if="data.item.instruction.length > 0" class="text" :style="data.field.cellStyle"
                    >{{ data.item.instruction }}
                  </span>
                  <span v-else-if="data.item.instruction.length == 0" class="text-muted" :style="data.field.cellStyle"
                    >No instructions.
                  </span>
                </span>
              </template>
            </b-table>
          </b-col>
        </b-row>
        <b-overlay :show="loadingPdf" rounded="sm">
          <div class="mt-5">
            <b-button-group>
              <h3 class="mx-2 mt-2 font-weight-normal" style="height: 10px;">Appearance Information</h3>
              <b-button
                v-if="informationsFileExists"
                variant="outline-primary text-info"
                style="border:0px;"
                class="mt-1"
                v-b-tooltip.hover.right
                title="Download Information File"
                @click="openDocumentsPdf(documentTypeCriminal)"
                size="sm"
              >
                <b-icon icon="file-earmark-arrow-down" font-scale="2"></b-icon>
              </b-button>
            </b-button-group>
            <hr class="mb-0 bg-light" style="height: 5px;" />
          </div>
          <b-card v-if="!(appearanceMethodDetails.length > 0)" style="border: white;">
            <span class="text-muted"> No appearance information. </span>
          </b-card>
          <b-table
            v-if="appearanceMethodDetails.length > 0"
            style="max-height: 200px; overflow-y: auto;"
            :items="appearanceMethodDetails"
            :fields="appearanceFields"
            borderless
            striped
            responsive="sm"
          >
            <template v-slot:head="data">
              <b> {{ data.label }}</b>
            </template>

            <template v-slot:cell(appearance)="data" :style="data.field.cellStyle">
              <span>{{ data.value }}</span>
              <span><br v-if="data.value.length > 0" />{{ data.item.partyAppearance }}</span>
            </template>
          </b-table>
          <template v-slot:overlay>
            <div style="text-align: center">
              <loading-spinner />
              <p id="Downloading-label">Downloading PDF file ...</p>
            </div>
          </template>
        </b-overlay>
      </b-card>
    </b-card>
    <b-modal v-if="isMounted" v-model="showNotes" id="bv-modal-comment" hide-footer>
      <template v-slot:modal-title>
        <h2 class="mb-0">Notes</h2>
      </template>
      <b-card v-if="notes.judgeRec.length > 0" title="Judge Recommendation" border-variant="white"
        >{{ notes.judgeRec }}
      </b-card>
      <b-card v-if="notes.appNote.length > 0" title="Appearance Note" border-variant="white"
        >{{ notes.appNote }}
      </b-card>
      <b-button class="mt-3 bg-info" @click="$bvModal.hide('bv-modal-comment')">Close</b-button>
    </b-modal>
  </div>
</template>

<script lang="ts">
import { Component, Vue, Prop } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import shared from "../shared";
import {
  criminalFileInformationType,
  appearanceAdditionalInfoType,
  appearanceMethodDetailsInfoType,
  appearanceChargesInfoType,
  criminalAppearanceInfoType,
  criminalAppearanceMethodsInfoType,
  initiatingDocument,
  criminalAppearanceDetailsInfoType,
  appearanceNotesInfoType,
} from "@/types/criminal";
import "@store/modules/CriminalFileInformation";
import { CourtDocumentType, DocumentData } from "@/types/shared";
import { CourtRoomsJsonInfoType } from "@/types/common";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

@Component
export default class CriminalAppearanceDetails extends Vue {
  @commonState.State
  public courtRoomsAndLocations!: CourtRoomsJsonInfoType[];

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @criminalState.State
  public criminalAppearanceInfo!: criminalAppearanceInfoType;

  appearanceAdditionalInfo: appearanceAdditionalInfoType[] = [];
  appearanceCharges: appearanceChargesInfoType[] = [];
  appearanceMethods: criminalAppearanceMethodsInfoType[] = [];
  appearanceMethodDetails: appearanceMethodDetailsInfoType[] = [];

  documentTypeRop = CourtDocumentType.ROP;
  documentTypeCriminal = CourtDocumentType.Criminal;
  loadingPdf = false;
  loadingROP = false;
  isMounted = false;
  isDataReady = false;
  appearanceDetailsJson;
  sortBy = "date";
  sortDesc = true;
  showNotes = false;
  informationsFileExists = false;
  notes = {} as appearanceNotesInfoType;
  appearanceDetailsInfo = {} as criminalAppearanceDetailsInfoType;
  initiatingDocuments: initiatingDocument[] = [];

  addInfoFields = [
    { key: "key", label: "key", sortable: false },
    { key: "value", label: "value", sortable: false },
  ];

  chargesFields = [
    { key: "count", label: "Count", sortable: false, tdClass: "border-top" },
    { key: "criminalCode", label: "Criminal Code", sortable: false, tdClass: "border-top" },
    { key: "description", label: "Description", sortable: false, tdClass: "border-top" },
    { key: "lastResult", label: "LastResult", sortable: false, tdClass: "border-top" },
    { key: "finding", label: "Finding", sortable: false, tdClass: "border-top" },
  ];

  appearanceMethodsField = [{ key: "key", label: "Key", cellClass: "text-danger", cellStyle: "white-space: pre-line" }];

  appearanceFields = [
    { key: "name", label: "Name", sortable: false, tdClass: "border-top", cellStyle: "font-size: 14px;" },
    {
      key: "role",
      label: "Role",
      sortable: false,
      tdClass: "border-top",
      cellStyle: "font-size: 14px; white-space: pre-line;",
    },
    {
      key: "appearance",
      label: "Appearance",
      sortable: false,
      tdClass: "border-top",
      cellStyle: "display: block; font-size: 14px; white-space: initial;",
    },
    {
      key: "attendance",
      label: "Attendance",
      sortable: false,
      tdClass: "border-top",
      cellStyle: "display: block; font-size: 14px; white-space: initial;",
    },
  ];

  mounted() {
    this.getAppearanceInfo();
    this.getAppearanceDetails();
  }

  @Prop() tagcasename;
  public getAppearanceDetails(): void {
    this.$http
      .get(
        "api/files/criminal/" +
          this.criminalAppearanceInfo.fileNo +
          "/appearance-detail/" +
          this.criminalAppearanceInfo.appearanceId +
          "/" +
          this.criminalAppearanceInfo.partId
      )
      .then(
        (Response) => Response.json(),
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          console.log(err);
          window.alert("bad data!");
        }
      )
      .then((data) => {
        if (data) {
          this.appearanceDetailsJson = data;
          this.ExtractAppearanceDetailsInfo();
          const element = document.getElementById(this.tagcasename);
          if (element != null)
            setTimeout(() => {
              element.scrollIntoView();
            }, 100);
        } else {
          window.alert("bad data!");
        }
        this.isMounted = true;
      });
  }

  public getAppearanceInfo() {
    this.appearanceDetailsInfo.supplementalEquipment = this.criminalAppearanceInfo.supplementalEquipmentTxt
      ? this.criminalAppearanceInfo.supplementalEquipmentTxt
      : "";
    this.appearanceDetailsInfo.securityRestriction = this.criminalAppearanceInfo.securityRestrictionTxt
      ? this.criminalAppearanceInfo.securityRestrictionTxt
      : "";
    this.appearanceDetailsInfo.outOfTownJudge = this.criminalAppearanceInfo.outOfTownJudgeTxt
      ? this.criminalAppearanceInfo.outOfTownJudgeTxt
      : "";

    for (const info in this.appearanceDetailsInfo) {
      if (this.appearanceDetailsInfo[info].length > 0)
        this.appearanceAdditionalInfo.push({ key: info, value: this.appearanceDetailsInfo[info] });
    }
  }

  public ExtractAppearanceDetailsInfo() {
    const judgeRec = this.appearanceDetailsJson.judgesRecommendation
      ? this.appearanceDetailsJson.judgesRecommendation
      : "";
    const appNote = this.appearanceDetailsJson.appearanceNote ? this.appearanceDetailsJson.appearanceNote : "";
    this.notes = { judgeRec: judgeRec, appNote: appNote };
    if (this.appearanceDetailsJson.initiatingDocuments && this.appearanceDetailsJson.initiatingDocuments.length > 0) {
      this.initiatingDocuments.push(this.appearanceDetailsJson.initiatingDocuments[0]);
      this.informationsFileExists = true;
    }

    for (const charge of this.appearanceDetailsJson.charges) {
      const chargeInfo = {} as appearanceChargesInfoType;
      chargeInfo.count = charge.printSeqNo;

      chargeInfo.criminalCode = charge.statuteSectionDsc;
      chargeInfo.description = charge.statuteDsc;

      chargeInfo.lastResult = charge.appearanceResultCd;
      chargeInfo.lastResultDescription = charge.appearanceResultDesc;

      chargeInfo.finding = charge.findingCd;
      chargeInfo.findingDescription = charge.findingDsc;

      this.appearanceCharges.push(chargeInfo);
    }

    for (const appearanceMethod of this.appearanceDetailsJson.appearanceMethods) {
      const methodInfo = {} as criminalAppearanceMethodsInfoType;
      methodInfo.role = appearanceMethod.roleTypeDsc;
      methodInfo.method = appearanceMethod.appearanceMethodDesc;
      methodInfo.instruction = appearanceMethod.instructionTxt ? appearanceMethod.instructionTxt : "";
      methodInfo.phoneNumber = appearanceMethod.phoneNumberTxt ? appearanceMethod.phoneNumberTxt : "";
      this.appearanceMethods.push(methodInfo);
    }

    if (this.appearanceDetailsJson.accused) {
      const accusedJson = this.appearanceDetailsJson.accused;
      const accused = {
        name: accusedJson.fullName,
        role: "Accused",
        attendance: accusedJson.attendanceMethodDesc ? accusedJson.attendanceMethodDesc : "",
        appearance: accusedJson.appearanceMethodDesc ? accusedJson.appearanceMethodDesc : "",
        partyAppearance: accusedJson.partyAppearanceMethodDesc ? accusedJson.partyAppearanceMethodDesc : "",
      };
      this.appearanceMethodDetails.push(accused);
    }

    if (this.appearanceDetailsJson.prosecutor) {
      const prosecutorJson = this.appearanceDetailsJson.prosecutor;
      const prosecutor = {
        name: prosecutorJson.fullName,
        role: "Prosecutor",
        attendance: prosecutorJson.attendanceMethodDesc ? prosecutorJson.attendanceMethodDesc : "",
        appearance: prosecutorJson.appearanceMethodDesc ? prosecutorJson.appearanceMethodDesc : "",
        partyAppearance: prosecutorJson.partyAppearanceMethodDesc ? prosecutorJson.partyAppearanceMethodDesc : "",
      };
      this.appearanceMethodDetails.push(prosecutor);
    }

    if (this.appearanceDetailsJson.adjudicator) {
      const adjudicatorJson = this.appearanceDetailsJson.adjudicator;
      const adjudicator = {
        name: adjudicatorJson.fullName,
        role: "Adjudicator",
        attendance: adjudicatorJson.attendanceMethodDesc ? adjudicatorJson.attendanceMethodDesc : "",
        appearance: adjudicatorJson.appearanceMethodDesc ? adjudicatorJson.appearanceMethodDesc : "",
        partyAppearance: adjudicatorJson.partyAppearanceMethodDesc ? adjudicatorJson.partyAppearanceMethodDesc : "",
      };
      this.appearanceMethodDetails.push(adjudicator);
    }

    if (this.appearanceDetailsJson.justinCounsel) {
      const counselJson = this.appearanceDetailsJson.justinCounsel;
      const counsel = {
        name: counselJson.fullName,
        role: "Counsel",
        attendance: counselJson.attendanceMethodDesc ? counselJson.attendanceMethodDesc : "",
        appearance: counselJson.appearanceMethodDesc ? counselJson.appearanceMethodDesc : "",
        partyAppearance: counselJson.partyAppearanceMethodDesc ? counselJson.partyAppearanceMethodDesc : "",
      };
      this.appearanceMethodDetails.push(counsel);
    }
  }

  public OpenNotes() {
    this.showNotes = true;
  }

  public openDocumentsPdf(courtDocumentType) {
    this.loadingPdf = true;
    const location = this.courtRoomsAndLocations.filter((location) => {
      return location.locationId == this.appearanceDetailsJson?.agencyId;
    })[0]?.name;
    const documentData: DocumentData = {
      courtLevel: this.criminalAppearanceInfo.courtLevel,
      courtClass: this.criminalAppearanceInfo.courtClass,
      dateFiled: Vue.filter("beautify_date")(this.appearanceDetailsJson?.appearanceDt),
      documentDescription: courtDocumentType == CourtDocumentType.ROP ? "ROP" : "Information",
      documentId: "", //this.initiatingDocuments[0].imageId,
      fileId: this.criminalAppearanceInfo.fileNo,
      fileNumberText: this.appearanceDetailsJson?.fileNumberTxt,
      partId: this.criminalAppearanceInfo.partId,
      profSeqNo: this.criminalAppearanceInfo.profSeqNo,
      location: location ? location : "",
    };
    shared.openDocumentsPdf(courtDocumentType, documentData);
    this.loadingPdf = false;
  }
}
</script>
