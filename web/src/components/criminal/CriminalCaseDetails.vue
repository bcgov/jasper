<template>
  <div class="main-container" style="overflow:hidden">
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

    <b-card bg-variant="light" v-if="isMounted && !isDataReady">
      <b-card style="min-height: 100px;">
        <span v-if="errorCode == 404">This <b>File-Number '{{ this.criminalFileInformation.fileNumber }}'</b> doesn't
          exist
          in the
          <b>criminal</b> records.
        </span>
        <span v-else-if="errorCode == 200 || errorCode == 204">
          Bad Data in <b>File-Number '{{ this.criminalFileInformation.fileNumber }}'</b> !
        </span>
        <span v-else-if="errorCode == 403"> You are not authorized to access this file. </span>
        <span v-else>
          Server is not responding. <b>({{ errorText }})</b>
        </span>
      </b-card>
      <!-- <b-card>
            <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                Return to Main Page
            </b-button>         
        </b-card> -->
    </b-card>

    <b-row cols="2">
      <b-col md="3" cols="3" style="overflow: auto;">
        <court-files-selector v-if="isDataReady && selectedFiles.length > 0" :files="selectedFiles"
          @reload-case-details="reloadCaseDetails" targetCaseDetails="CriminalCaseDetails" />
        <criminal-side-panel v-if="isDataReady" />
      </b-col>
      <b-col col md="9" cols="9" class="px-0" style="overflow: auto;">
        <criminal-header-top v-if="isDataReady" />
        <criminal-header v-if="isDataReady" />

        <b-row class="ml-0" v-if="showDocuments">
          <h2 style="white-space: pre" v-if="isDataReady">
            {{ selectedSideBar }}
          </h2>
          <custom-overlay v-if="isDataReady" :show="!downloadCompleted"
            style="padding: 0 1rem; margin-left:auto; margin-right:2rem;">
            <b-button v-if="enableArchive" @click="downloadDocuments()" size="md" variant="info"
              style="padding: 0 1rem; margin-left:auto; margin-right:2rem;">
              Download All Documents
            </b-button>
          </custom-overlay>
        </b-row>

        <h2 style="white-space: pre" v-if="!showDocuments && isDataReady">
          {{ selectedSideBar }}
        </h2>

        <criminal-participants v-if="showCaseDetails" />
        <criminal-adjudicator-restrictions v-if="showCaseDetails" />
        <criminal-crown-information v-if="showCaseDetails" />
        <!--<criminal-crown-notes v-if="showCaseDetails"/> Asked to be hidden by Kevin SCV-140.-->
        <criminal-past-appearances v-if="showPastAppearances" />
        <criminal-future-appearances v-if="showFutureAppearances" />
        <criminal-documents-view v-if="showDocuments" />
        <criminal-witnesses v-if="showWitnesses" />
        <criminal-sentence v-if="showSentenceOrder" />
        <b-card><br /></b-card>
      </b-col>
    </b-row>

    <b-modal v-if="isMounted" v-model="banExists" id="bv-modal-ban" hide-header hide-footer>
      <b-card>
        A Ban has been ordered on at least one participant in this case. Please check Ban Details before giving out
        sensitive details.
      </b-card>
      <b-button class="mt-3 bg-primary" @click="$bvModal.hide('bv-modal-ban')">Continue</b-button>
    </b-modal>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-class";
import CriminalDocumentsView from "@components/criminal/CriminalDocumentsView.vue";
import CriminalHeaderTop from "@components/criminal/CriminalHeaderTop.vue";
import CriminalHeader from "@components/criminal/CriminalHeader.vue";
import CriminalSidePanel from "@components/criminal/CriminalSidePanel.vue";
import CriminalParticipants from "@components/criminal/CriminalParticipants.vue";
import CriminalAdjudicatorRestrictions from "@components/criminal/CriminalAdjudicatorRestrictions.vue";
import CriminalCrownInformation from "@components/criminal/CriminalCrownInformation.vue";
import CriminalPastAppearances from "@components/criminal/CriminalPastAppearances.vue";
import CriminalFutureAppearances from "@components/criminal/CriminalFutureAppearances.vue";
import CriminalCrownNotes from "@components/criminal/CriminalCrownNotes.vue";
import CriminalWitnesses from "@components/criminal/CriminalWitnesses.vue";
import CriminalSentence from "@components/criminal/CriminalSentence.vue";
import CourtFilesSelector from "@components/shared/CourtFilesSelector.vue";
import CustomOverlay from "../CustomOverlay.vue";
import {
  bansInfoType,
  chargesInfoType,
  participantListInfoType,
  criminalFileInformationType,
  ropRequestsInfoType,
} from "@/types/criminal";
import {
  InputNamesType,
  AdjudicatorRestrictionsInfoType,
  DocumentRequestsInfoType,
  ArchiveInfoType,
  KeyValueInfo,
} from "@/types/common";
import "@store/modules/CriminalFileInformation";
import "@store/modules/CommonInformation";
import base64url from "base64url";
import shared from "../shared";
import { CourtDocumentType, DocumentData } from "@/types/shared";
import { criminalHearingRestrictionType, criminalParticipantType } from "@/types/criminal/jsonTypes";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");
const courtFileSearchState = namespace('CourtFileSearchInformation');

enum DecodeCourtLevel {
  "P" = 0,
  "S" = 1,
  "A" = 2,
}
enum DecodeCourtClass {
  "A" = 0,
  "Y" = 1,
  "T" = 2,
  "F" = 3,
  "C" = 4,
  "M" = 5,
  "L" = 6,
  "R" = 7,
  "B" = 8,
  "D" = 9,
  "E" = 10,
  "G" = 11,
  "H" = 12,
  "N" = 13,
  "O" = 14,
  "P" = 15,
  "S" = 16,
  "V" = 17,
}

@Component({
  components: {
    CourtFilesSelector,
    CriminalDocumentsView,
    CriminalSidePanel,
    CriminalHeaderTop,
    CriminalHeader,
    CriminalParticipants,
    CriminalAdjudicatorRestrictions,
    CriminalCrownInformation,
    CriminalPastAppearances,
    CriminalFutureAppearances,
    CriminalCrownNotes,
    CriminalWitnesses,
    CriminalSentence,
    CustomOverlay
  },
})
export default class CriminalCaseDetails extends Vue {
  @criminalState.State
  public showSections;

  @commonState.State
  public displayName!: string;

  @commonState.State
  public enableArchive!: boolean;

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @criminalState.Action
  public UpdateCriminalFile!: (newCriminalFileInformation: criminalFileInformationType) => void;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  @courtFileSearchState.Getter('selectedFiles')
  public selectedFiles!: KeyValueInfo[];

  participantList: participantListInfoType[] = [];
  adjudicatorRestrictionsInfo: AdjudicatorRestrictionsInfoType[] = [];
  bans: bansInfoType[] = [];

  isDataReady = false;
  isMounted = false;
  downloadCompleted = true;
  banExists = false;
  errorCode = 0;
  errorText = "";

  participantJson: criminalParticipantType[] = [];
  adjudicatorRestrictionsJson: criminalHearingRestrictionType[] = [];

  sidePanelTitles = [
    "Case Details",
    "Future Appearances",
    "Past Appearances",
    "Witnesses",
    "Documents",
    "Sentence/Order Details",
  ];

  topTitles = [
    "Case Details",
    "Future Appearances",
    "Past Appearances",
    "Witnesses",
    "Criminal Documents",
    "Criminal Sentences",
  ];

  statusFields = [
    { key: "Warrant Issued", abbr: "W", code: "warrantYN" },
    { key: "In Custody", abbr: "IC", code: "inCustodyYN" },
    { key: "Detention Order", abbr: "DO", code: "detainedYN" },
    { key: "Interpreter Required", abbr: "INT", code: "interpreterYN" },
  ];

  mounted() {
    this.criminalFileInformation.fileNumber = this.$route.params.fileNumber;
    this.UpdateCriminalFile(this.criminalFileInformation);
    this.getFileDetails();
  }

  public getFileDetails(): void {
    this.errorCode = 0;
    this.$http
      .get("api/files/criminal/" + this.criminalFileInformation.fileNumber)
      .then(
        (Response) => Response.json(),
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          this.errorCode = err.status;
          this.errorText = err.statusText;
          console.log(err);
        }
      )
      .then((data) => {
        if (data) {
          this.criminalFileInformation.detailsData = data;
          this.participantJson = data.participant;
          this.adjudicatorRestrictionsJson = data.hearingRestriction;
          const courtLevel = DecodeCourtLevel[data.courtLevelCd];
          const courtClass = DecodeCourtClass[data.courtClassCd];
          this.ExtractFileInfo();
          //Allow blank participants, it's a real case file 1019 for example on dev.
          this.criminalFileInformation.participantList = this.participantList;
          this.criminalFileInformation.adjudicatorRestrictionsInfo = this.adjudicatorRestrictionsInfo;
          if (this.bans.length > 0) {
            this.banExists = true;
          }
          this.criminalFileInformation.bans = this.bans;
          this.criminalFileInformation.courtLevel = courtLevel;
          this.criminalFileInformation.courtClass = courtClass;
          this.UpdateCriminalFile(this.criminalFileInformation);
          this.isDataReady = true;
        } else if (this.errorCode == 0) this.errorCode = 200;

        this.isMounted = true;
      });
  }

  public downloadDocuments() {
    const fileName = shared.generateFileName(CourtDocumentType.CriminalZip, {
      location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
      courtClass: this.criminalFileInformation.detailsData.courtClassCd,
      courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
      fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
    });
    const documentsToDownload = {
      zipName: fileName,
      csrRequests: [],
      documentRequests: [],
      ropRequests: [],
    } as ArchiveInfoType;

    for (const partIndex in this.participantList) {
      const partInfo = this.participantList[partIndex];

      for (const doc of partInfo.documentsJson) {
        if (doc.category != "rop") {
          if (doc.imageId) {
            const id = doc.imageId;
            const documentRequest = {} as DocumentRequestsInfoType;
            documentRequest.isCriminal = true;
            const documentData: DocumentData = {
              dateFiled: Vue.filter("beautify_date")(doc.issueDate),
              documentDescription: doc.documentTypeDescription,
              documentId: id,
              courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
              courtClass: this.criminalFileInformation.detailsData.courtClassCd,
              fileId: this.criminalFileInformation.fileNumber,
              fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
              location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
            };
            documentRequest.pdfFileName = shared.generateFileName(CourtDocumentType.Criminal, documentData);
            documentRequest.base64UrlEncodedDocumentId = base64url(id);
            documentRequest.fileId = this.criminalFileInformation.fileNumber;
            documentsToDownload.documentRequests.push(documentRequest);
          }
        } else {
          const ropRequest = {} as ropRequestsInfoType;
          const partId = doc.partId;
          const documentData: DocumentData = {
            courtClass: this.criminalFileInformation.detailsData.courtClassCd,
            courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
            documentDescription: "ROP",
            fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
            partId: partId,
            profSeqNo: partInfo.profSeqNo,
            location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
          };
          ropRequest.pdfFileName = shared.generateFileName(CourtDocumentType.ROP, documentData);
          ropRequest.partId = partId;
          ropRequest.profSequenceNumber = partInfo.profSeqNo;
          ropRequest.courtLevelCode = this.criminalFileInformation.courtLevel;
          ropRequest.courtClassCode = this.criminalFileInformation.courtClass;
          documentsToDownload.ropRequests.push(ropRequest);
        }
      }
    }

    if (documentsToDownload.ropRequests.length > 0 || documentsToDownload.documentRequests.length > 0) {
      const options = {
        responseType: "blob",
        headers: {
          "Content-Type": "application/json",
        },
      };
      this.downloadCompleted = false;
      this.$http.post("api/files/archive", documentsToDownload, options).then(
        (response) => {
          const blob = response.data;
          const link = document.createElement("a");
          link.href = URL.createObjectURL(blob);
          document.body.appendChild(link);
          link.download = documentsToDownload.zipName;
          link.click();
          setTimeout(() => URL.revokeObjectURL(link.href), 1000);
          this.downloadCompleted = true;
        },
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          console.log(err);
          this.downloadCompleted = true;
        }
      );
    }
  }

  get selectedSideBar() {
    for (const titleInx in this.sidePanelTitles) {
      if (this.showSections[this.sidePanelTitles[titleInx]] == true) return "  " + this.topTitles[titleInx];
    }
    return "";
  }

  get showCaseDetails() {
    return this.showSections["Case Details"] && this.isDataReady;
  }

  get showDocuments() {
    return this.showSections["Documents"] && this.isDataReady;
  }

  get showFutureAppearances() {
    return (this.showSections["Case Details"] || this.showSections["Future Appearances"]) && this.isDataReady;
  }

  get showPastAppearances() {
    return (this.showSections["Case Details"] || this.showSections["Past Appearances"]) && this.isDataReady;
  }

  get showWitnesses() {
    return this.showSections["Witnesses"] && this.isDataReady;
  }

  get showSentenceOrder() {
    return this.showSections["Sentence/Order Details"] && this.isDataReady;
  }

  public ExtractFileInfo(): void {
    for (const partIndex in this.participantJson) {
      const participantInfo = {} as participantListInfoType;
      const jParticipant = this.participantJson[partIndex];
      participantInfo.index = partIndex;
      participantInfo.firstName = jParticipant.givenNm.trim().length > 0 ? jParticipant.givenNm : "";
      participantInfo.lastName = jParticipant.lastNm ? jParticipant.lastNm : jParticipant.orgNm;
      this.UpdateDisplayName({ lastName: participantInfo.lastName, givenName: participantInfo.firstName });
      participantInfo.name = this.displayName;

      participantInfo.dob = jParticipant.birthDt
        ? new Date(jParticipant.birthDt.split(" ")[0]).toUTCString().substr(4, 12)
        : "";
      participantInfo.partId = jParticipant.partId;
      participantInfo.profSeqNo = jParticipant.profSeqNo;
      participantInfo.charges = [];
      const charges: chargesInfoType[] = [];
      for (const charge of jParticipant.charge) {
        const chargeInfo = {} as chargesInfoType;
        chargeInfo.description = charge.sectionDscTxt;
        chargeInfo.code = charge.sectionTxt;
        charges.push(chargeInfo);
      }
      participantInfo.charges = charges;

      participantInfo.status = [];
      for (const status of this.statusFields) {
        if (jParticipant[status.code] == "Y") participantInfo.status.push(status);
      }

      for (const ban of jParticipant.ban) {
        const banInfo = {} as bansInfoType;
        banInfo.banParticipant = participantInfo.name;
        banInfo.banType = ban.banTypeDescription;
        banInfo.orderDate = ban.banOrderedDate;
        banInfo.act = ban.banTypeAct;
        banInfo.sect = ban.banTypeSection;
        banInfo.sub = ban.banTypeSubSection;
        banInfo.description = ban.banStatuteId;
        banInfo.comment = ban.banCommentText;
        this.bans.push(banInfo);
      }

      participantInfo.documentsJson = jParticipant.document;
      participantInfo.countsJson = jParticipant.count;

      this.UpdateDisplayName({
        lastName: jParticipant.counselLastNm ? jParticipant.counselLastNm : "",
        givenName: jParticipant.counselGivenNm ? jParticipant.counselGivenNm : "",
      });
      participantInfo.counsel = this.displayName.trim.length ? "JUSTIN: " + this.displayName : "";
      participantInfo.counselDesignationFiled = jParticipant.designatedCounselYN;
      this.participantList.push(participantInfo);
    }

    for (const jRestriction of this.adjudicatorRestrictionsJson) {
      const restrictionInfo = {} as AdjudicatorRestrictionsInfoType;
      restrictionInfo.adjRestriction = jRestriction.adjInitialsTxt
        ? jRestriction.hearingRestrictionTypeDsc + ": " + jRestriction.adjInitialsTxt
        : jRestriction.hearingRestrictionTypeDsc;
      restrictionInfo.fullName = jRestriction.adjFullNm;
      restrictionInfo.adjudicator = jRestriction.adjInitialsTxt
        ? jRestriction.adjInitialsTxt + " - " + jRestriction.adjFullNm
        : jRestriction.adjFullNm;
      restrictionInfo.status = jRestriction.hearingRestrictionTypeDsc + " ";
      restrictionInfo.appliesTo = jRestriction.partNm ? jRestriction.partNm : "All participants on file";
      this.adjudicatorRestrictionsInfo.push(restrictionInfo);
    }
  }

  public navigateToLandingPage() {
    this.$router.push({ name: "Home" });
  }

  private reloadCaseDetails(): void {
    // Reset the properties to load new case details.
    this.criminalFileInformation.fileNumber = this.$route.params.fileNumber;
    this.participantList.length = 0;
    this.bans.length = 0;
    this.adjudicatorRestrictionsInfo.length = 0;
    this.isMounted = false;
    this.isDataReady = false;
    this.getFileDetails();
  }
}
</script>

<style scoped>
.card {
  border: white;
}

body {
  overflow-x: hidden;
}
</style>
