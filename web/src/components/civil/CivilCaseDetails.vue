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
        <span v-if="errorCode == 404">This <b>File-Number '{{ this.civilFileInformation.fileNumber }}'</b> doesn't exist
          in
          the
          <b>civil</b> records.</span>
        <span v-else-if="errorCode == 200 || errorCode == 204">
          Bad Data in <b>File-Number '{{ this.civilFileInformation.fileNumber }}'</b>.</span>
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

    <b-card no-body>
      <b-row cols="2">
        <b-col md="3" cols="3" style="overflow: auto;">
          <court-files-selector v-if="isDataReady && selectedFiles.length > 0" :files="selectedFiles"
            @reload-case-details="reloadCaseDetails" targetCaseDetails="CivilCaseDetails" />
          <civil-side-panel v-if="isDataReady" />
        </b-col>
        <b-col col md="9" cols="9" class="px-0" style="overflow: auto;">
          <civil-header-top v-if="isDataReady" />
          <civil-header v-if="isDataReady" />

          <b-row class="ml-0" v-if="showAllDocuments">
            <h2 style="white-space: pre" v-if="isDataReady">
              {{ selectedSideBar }}
            </h2>
            <custom-overlay v-if="isDataReady" :show="!downloadCompleted"
              style="padding: 0 1rem; margin-left:auto; margin-right:2rem;">
              <b-button v-if="enableArchive" @click="downloadAllDocuments()" size="md" variant="info"
                style="padding: 0 1rem; margin-left:auto; margin-right:2rem;">
                Download All Documents
              </b-button>
            </custom-overlay>
          </b-row>

          <h2 v-if="!showAllDocuments && isDataReady" style="white-space: pre">
            {{ selectedSideBar }}
          </h2>

          <civil-parties v-if="showCaseDetails" />
          <civil-comment-notes v-if="showCaseDetails" />
          <civil-documents-view v-if="showDocuments || showAllDocuments" />
          <civil-provided-documents-view v-if="showProvidedDocuments || showAllDocuments" />
          <civil-past-appearances v-if="showPastAppearances" />
          <civil-future-appearances v-if="showFutureAppearances" />
          <b-card><br /></b-card>
        </b-col>
      </b-row>
    </b-card>
    <b-modal v-if="isMounted" v-model="showSealedWarning" id="bv-modal-ban" hide-header hide-footer>
      <b-card v-if="isSealed">
        This file has been sealed. Only authorized users are permitted access to sealed files.
      </b-card>
      <b-card v-else-if="docIsSealed">
        This File contains one or more Sealed Documents.
      </b-card>
      <b-button class="mt-3 bg-primary" @click="$bvModal.hide('bv-modal-ban')">Continue</b-button>
    </b-modal>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-class";
import * as _ from "underscore";
import CivilDocumentsView from "@components/civil/CivilDocumentsView.vue";
import CivilProvidedDocumentsView from "@components/civil/CivilProvidedDocumentsView.vue";
import CivilPastAppearances from "@components/civil/CivilPastAppearances.vue";
import CivilFutureAppearances from "@components/civil/CivilFutureAppearances.vue";
import CivilAdjudicatorRestrictions from "@components/civil/CivilAdjudicatorRestrictions.vue";
import CivilCommentNotes from "@components/civil/CivilCommentNotes.vue";
import CivilParties from "@components/civil/CivilParties.vue";
import CivilHeaderTop from "@components/civil/CivilHeaderTop.vue";
import CivilHeader from "@components/civil/CivilHeader.vue";
import CivilSidePanel from "@components/civil/CivilSidePanel.vue";
import CourtFilesSelector from "@components/shared/CourtFilesSelector.vue";
import {
  civilFileInformationType,
  partiesInfoType,
  documentsInfoType,
  summaryDocumentsInfoType,
  referenceDocumentsInfoType,
  csrRequestsInfoType,
} from "@/types/civil";
import {
  InputNamesType,
  AdjudicatorRestrictionsInfoType,
  ArchiveInfoType,
  DocumentRequestsInfoType,
  KeyValueInfo,
} from "@/types/common";
import "@store/modules/CommonInformation";
import "@store/modules/CivilFileInformation";
import CustomOverlay from "../CustomOverlay.vue";
import {
  civilDocumentType,
  civilHearingRestrictionType,
  civilReferenceDocumentJsonType,
  partyType,
} from "@/types/civil/jsonTypes";
import base64url from "base64url";
import shared from "../shared";
import { CourtDocumentType, DocumentData } from "@/types/shared";
import { civilShowSectionsType } from '@/types/civil';

const civilState = namespace("CivilFileInformation");
const commonState = namespace("CommonInformation");
const selectedCourtFilesState = namespace('CourtFileSearchInformation');

@Component({
  components: {
    CivilAdjudicatorRestrictions,
    CivilCommentNotes,
    CivilDocumentsView,
    CivilProvidedDocumentsView,
    CivilPastAppearances,
    CivilFutureAppearances,
    CivilParties,
    CivilSidePanel,
    CivilHeaderTop,
    CivilHeader,
    CourtFilesSelector,
    CustomOverlay,
  },
})
export default class CivilCaseDetails extends Vue {
  @civilState.State
  public showSections;

  @commonState.State
  public displayName!: string;

  @commonState.State
  public enableArchive!: boolean;

  @civilState.State
  public civilFileInformation!: civilFileInformationType;

  @civilState.Action
  public UpdateCivilFile!: (newCivilFileInformation: civilFileInformationType) => void;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  @civilState.Action
  public UpdateHasNonParty!: (newHasNonParty: boolean) => void;

  @civilState.Action
  public UpdateShowSections!: (newShowSections: civilShowSectionsType) => void

  @selectedCourtFilesState.Getter('selectedFiles')
  public selectedFiles!: KeyValueInfo[];

  leftPartiesInfo: partiesInfoType[] = [];
  rightPartiesInfo: partiesInfoType[] = [];
  adjudicatorRestrictionsInfo: AdjudicatorRestrictionsInfoType[] = [];
  documentsInfo: documentsInfoType[] = [];
  providedDocumentsInfo: referenceDocumentsInfoType[] = [];
  summaryDocumentsInfo: summaryDocumentsInfoType[] = [];

  isDataReady = false;
  isMounted = false;
  downloadCompleted = true;
  isSealed = false;
  docIsSealed = false;
  showSealedWarning = false;
  errorCode = 0;
  errorText = "";
  partiesJson: partyType[] = [];
  adjudicatorRestrictionsJson: civilHearingRestrictionType[] = [];
  documentsDetailsJson: civilDocumentType[] = [];
  providedDocumentsDetailsJson: civilReferenceDocumentJsonType[] = [];
  categories: string[] = [];
  providedDocumentCategories: string[] = [];
  sidePanelTitles = [
    "Case Details",
    "Future Appearances",
    "Past Appearances",
    "All Documents",
    "Documents",
    "Provided Documents",
  ];

  mounted() {
    this.civilFileInformation.fileNumber = this.$route.params.fileNumber;
    this.UpdateCivilFile(this.civilFileInformation);
    this.getFileDetails();

    this.navigateToSection(this.$route.params.section);
  }

  public navigateToSection(section): void {
    if (section) {
      const sections = this.showSections;
      for (const item of this.sidePanelTitles) {
        if (item == section)
          sections[item] = true;
        else
          sections[item] = false;
      }
      this.UpdateShowSections(sections);
    }
  }

  public getFileDetails(): void {
    this.errorCode = 0;
    this.$http
      .get("api/files/civil/" + this.civilFileInformation.fileNumber)
      .then(
        (Response) => Response.json(),
        (err) => {
          this.errorCode = err.status;
          this.errorText = err.statusText;
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          console.log(err);
        }
      )
      .then((data) => {
        if (data) {
          this.civilFileInformation.detailsData = data;
          this.partiesJson = data.party;
          this.adjudicatorRestrictionsJson = data.hearingRestriction;
          this.documentsDetailsJson = data.document;
          this.providedDocumentsDetailsJson = data.referenceDocument;
          if (data.sealedYN == "Y") {
            this.isSealed = true;
          }
          this.ExtractCaseInfo();
          if (
            this.adjudicatorRestrictionsInfo.length > 0 ||
            this.leftPartiesInfo.length > 0 ||
            this.rightPartiesInfo.length > 0 ||
            this.documentsInfo.length > 0 ||
            this.summaryDocumentsInfo.length > 0
          ) {
            this.civilFileInformation.leftPartiesInfo = this.leftPartiesInfo;
            this.civilFileInformation.rightPartiesInfo = this.rightPartiesInfo;
            this.civilFileInformation.isSealed = this.isSealed;
            this.civilFileInformation.adjudicatorRestrictionsInfo = this.adjudicatorRestrictionsInfo;
            this.civilFileInformation.documentsInfo = this.documentsInfo;
            this.civilFileInformation.summaryDocumentsInfo = this.summaryDocumentsInfo;
            this.civilFileInformation.referenceDocumentInfo = this.providedDocumentsInfo;
            this.civilFileInformation.categories = this.categories;
            this.civilFileInformation.providedDocumentCategories = this.providedDocumentCategories;
            this.UpdateCivilFile(this.civilFileInformation);
            if (this.isSealed || this.docIsSealed) {
              this.showSealedWarning = true;
            }
            this.isDataReady = true;
          } else this.errorCode = 200;
        } else if (this.errorCode == 0) this.errorCode = 200;
        this.isMounted = true;
      });
  }

  public downloadAllDocuments() {
    const fileName = shared.generateFileName(CourtDocumentType.CivilZip, {
      location: this.civilFileInformation.detailsData.homeLocationAgencyName,
      courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
      fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
    });

    const documentsToDownload = {
      zipName: fileName,
      csrRequests: [],
      documentRequests: [],
      ropRequests: [],
      vcCivilFileId: this.civilFileInformation.fileNumber,
    } as ArchiveInfoType;
    for (const doc of this.providedDocumentsInfo) {
      if (doc.isEnabled) {
        const id = doc.objectGuid;
        const documentRequest = {} as DocumentRequestsInfoType;
        documentRequest.isCriminal = false;
        const documentData: DocumentData = {
          appearanceDate: Vue.filter("beautify_date")(doc.appearanceDate),
          courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
          documentDescription: doc.descriptionText,
          documentId: id,
          fileId: this.civilFileInformation.fileNumber,
          fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
          location: this.civilFileInformation.detailsData.homeLocationAgencyName,
          partyName: doc.partyName.toString(),
        };
        documentRequest.pdfFileName = shared.generateFileName(CourtDocumentType.ProvidedCivil, documentData);
        documentRequest.base64UrlEncodedDocumentId = base64url(id);
        documentRequest.fileId = this.civilFileInformation.fileNumber;
        documentsToDownload.documentRequests.push(documentRequest);
      }
    }

    for (const doc of this.documentsInfo) {
      if (doc.isEnabled) {
        const id = doc.documentId;
        const documentRequest = {} as DocumentRequestsInfoType;
        documentRequest.isCriminal = false;
        const documentData: DocumentData = {
          courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
          dateFiled: Vue.filter("beautify_date")(doc.dateFiled),
          documentDescription: doc.documentType,
          documentId: id,
          fileId: this.civilFileInformation.fileNumber,
          fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
          location: this.civilFileInformation.detailsData.homeLocationAgencyName,
        };
        documentRequest.pdfFileName = shared.generateFileName(CourtDocumentType.Civil, documentData);
        documentRequest.base64UrlEncodedDocumentId = base64url(id);
        documentRequest.fileId = this.civilFileInformation.fileNumber;
        documentsToDownload.documentRequests.push(documentRequest);
      }
    }

    for (const doc of this.summaryDocumentsInfo) {
      if (doc.isEnabled) {
        const id = doc["Appearance ID"];
        const csrRequest = {} as csrRequestsInfoType;
        csrRequest.appearanceId = id;
        const documentData: DocumentData = {
          appearanceDate: Vue.filter("beautify_date")(doc.appearanceDate),
          appearanceId: id,
          courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
          documentDescription: doc.documentType,
          fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
          fileId: this.civilFileInformation.fileNumber,
          location: this.civilFileInformation.detailsData.homeLocationAgencyName,
        };
        csrRequest.pdfFileName = shared.generateFileName(CourtDocumentType.CSR, documentData);
        documentsToDownload.csrRequests.push(csrRequest);
      }
    }

    if (documentsToDownload.csrRequests.length > 0 || documentsToDownload.documentRequests.length > 0) {
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
    for (const title of this.sidePanelTitles) {
      if (this.showSections[title] == true) return title;
    }
    return "";
  }

  get showCaseDetails() {
    return this.showSections["Case Details"] && this.isDataReady;
  }

  get showFutureAppearances() {
    return (this.showSections["Case Details"] || this.showSections["Future Appearances"]) && this.isDataReady;
  }

  get showPastAppearances() {
    return (this.showSections["Case Details"] || this.showSections["Past Appearances"]) && this.isDataReady;
  }

  get showProvidedDocuments() {
    return (this.showSections["Case Details"] || this.showSections["Provided Documents"]) && this.isDataReady;
  }

  get showDocuments() {
    return (this.showSections["Case Details"] || this.showSections["Documents"]) && this.isDataReady;
  }

  get showAllDocuments() {
    return (this.showSections["Case Details"] || this.showSections["All Documents"]) && this.isDataReady;
  }

  public ExtractCaseInfo(): void {
    let partyIndex = 0;
    for (const jParty of this.partiesJson) {
      const partyInfo = {} as partiesInfoType;
      partyInfo.partyId = jParty.partyId;
      partyInfo.role = jParty.roleTypeDescription;
      if (jParty.counsel.length > 0) {
        partyInfo.counsel = [];
        for (const couns of jParty.counsel) {
          partyInfo.counsel.push(couns.fullNm);
        }
      } else {
        partyInfo.counsel = [];
      }
      partyInfo.leftRight = jParty.leftRightCd;
      partyInfo.firstName = jParty.givenNm ? jParty.givenNm : "";
      partyInfo.lastName = jParty.lastNm ? jParty.lastNm : jParty.orgNm;
      this.UpdateDisplayName({ lastName: partyInfo.lastName, givenName: partyInfo.firstName });
      partyInfo.name = this.displayName;
      partyInfo.id = jParty.partyId;
      partyInfo.index = partyIndex;
      partyIndex = partyIndex + 1;
      if (partyInfo.leftRight == "R") {
        this.rightPartiesInfo.push(partyInfo);
      } else {
        this.leftPartiesInfo.push(partyInfo);
      }
    }
    this.leftPartiesInfo = this.SortParties(this.leftPartiesInfo);
    this.rightPartiesInfo = this.SortParties(this.rightPartiesInfo);

    for (const jRestriction of this.adjudicatorRestrictionsJson) {
      const restrictionInfo = {} as AdjudicatorRestrictionsInfoType;
      restrictionInfo.adjRestriction = jRestriction.adjInitialsTxt
        ? jRestriction.hearingRestrictionTypeDsc + ": " + jRestriction.adjInitialsTxt
        : jRestriction.hearingRestrictionTypeDsc;
      restrictionInfo.adjudicator = jRestriction.adjInitialsTxt
        ? jRestriction.adjInitialsTxt + " - " + jRestriction.adjFullNm
        : jRestriction.adjFullNm;
      restrictionInfo.fullName = jRestriction.adjFullNm;
      restrictionInfo.status = jRestriction.hearingRestrictionTypeDsc + " ";
      restrictionInfo.appliesTo = jRestriction.applyToNm ? jRestriction.applyToNm : "All Documents";

      this.adjudicatorRestrictionsInfo.push(restrictionInfo);
    }

    for (const docIndex in this.documentsDetailsJson) {
      const jDoc = this.documentsDetailsJson[docIndex];
      if (jDoc.documentTypeCd != "CSR") {
        const docInfo = {} as documentsInfoType;
        docInfo.index = docIndex;
        docInfo.seq = jDoc.fileSeqNo;
        docInfo.documentType = jDoc.documentTypeDescription;
        docInfo.concluded = jDoc.concludedYn;
        if (this.categories.indexOf("CONCLUDED") < 0 && docInfo.concluded.toUpperCase() == "Y")
          this.categories.push("CONCLUDED");
        docInfo.nextAppearanceDate = jDoc.nextAppearanceDt ? Vue.filter("beautify_date")(jDoc.nextAppearanceDt) : "";
        if (docInfo.nextAppearanceDate.length > 0 && this.categories.indexOf("SCHEDULED") < 0)
          this.categories.push("SCHEDULED");

        docInfo.category = jDoc.category ? jDoc.category : "";
        if (this.categories.indexOf(docInfo.category) < 0 && docInfo.category.length > 0)
          this.categories.push(docInfo.category);

        docInfo.swornBy = jDoc.swornByNm ? jDoc.swornByNm : "";
        docInfo.affNo = jDoc.affidavitNo ? jDoc.affidavitNo : "";

        docInfo.act = [];
        if (jDoc.documentSupport && jDoc.documentSupport.length > 0) {
          for (const act of jDoc.documentSupport) {
            docInfo.act.push({ code: act.actCd, description: act.actDsc });
          }
        }
        if (jDoc.sealedYN == "Y") {
          this.docIsSealed = true;
          docInfo.sealed = true;
        } else {
          docInfo.sealed = false;
        }
        docInfo.documentId = jDoc.civilDocumentId;
        docInfo.pdfAvail = jDoc.imageId ? true : false;
        if (docInfo.documentType.toUpperCase() == "ORDER") {
          docInfo.dateFiled = jDoc.DateGranted ? jDoc.DateGranted.split(" ")[0] : "";
        } else {
          docInfo.dateFiled = jDoc.filedDt ? jDoc.filedDt.split(" ")[0] : "";
        }
        docInfo.issues = [];
        if (jDoc.issue && jDoc.issue.length > 0) {
          for (const issue of jDoc.issue) {
            docInfo.issues.push(issue.issueDsc);
          }
        }
        docInfo.comment = jDoc.commentTxt ? jDoc.commentTxt : "";
        docInfo.filedByName = [];
        if (jDoc.filedBy && jDoc.filedBy[0] && jDoc.filedBy.length > 0) {
          for (const filed of jDoc.filedBy) {
            if (filed.roleTypeCode) {
              docInfo.filedByName.push(filed.filedByName + " (" + filed.roleTypeCode + ")");
            } else {
              docInfo.filedByName.push(filed.filedByName);
            }
          }
        }
        docInfo.orderMadeDate = jDoc.DateGranted ? Vue.filter("beautify_date")(jDoc.DateGranted) : "";
        docInfo.isChecked = false;
        docInfo.isEnabled = docInfo.pdfAvail;

        this.documentsInfo.push(docInfo);
      } else {
        const docInfo = {} as summaryDocumentsInfoType;
        docInfo.index = docIndex;
        docInfo.documentType = "CourtSummary";
        docInfo.appearanceDate = jDoc.lastAppearanceDt.split(" ")[0];
        docInfo.appearanceId = jDoc.imageId;
        docInfo.pdfAvail = jDoc.imageId ? true : false;
        docInfo.isChecked = false;
        docInfo.isEnabled = docInfo.pdfAvail;
        this.summaryDocumentsInfo.push(docInfo);
      }
    }
    this.UpdateHasNonParty(false);
    for (const providedDocIndex in this.providedDocumentsDetailsJson) {
      const jDoc = this.providedDocumentsDetailsJson[providedDocIndex];
      const providedDocInfo = {} as referenceDocumentsInfoType;
      providedDocInfo.appearanceId = jDoc.AppearanceId;

      providedDocInfo.partyId = [];
      providedDocInfo.partyName = [];
      providedDocInfo.nonPartyName = [];
      for (const refDocInterestIndex in jDoc.ReferenceDocumentInterest) {
        const refDocInterest = jDoc.ReferenceDocumentInterest[refDocInterestIndex];
        if (refDocInterest.PartyId) providedDocInfo.partyId.push(refDocInterest.PartyId);
        if (refDocInterest.PartyName) providedDocInfo.partyName.push(refDocInterest.PartyName);
        if (refDocInterest.NonPartyName) providedDocInfo.nonPartyName.push(refDocInterest.NonPartyName);
      }
      if (providedDocInfo.nonPartyName.length > 0) {
        this.UpdateHasNonParty(true);
      }
      providedDocInfo.appearanceDate = jDoc.AppearanceDate;
      providedDocInfo.descriptionText = jDoc.DescriptionText;
      // providedDocInfo.enterDtm = jDoc.EnterDtm;
      providedDocInfo.referenceDocumentTypeDsc = jDoc.ReferenceDocumentTypeDsc;
      providedDocInfo.objectGuid = jDoc.ObjectGuid;
      providedDocInfo.isChecked = false;
      providedDocInfo.isEnabled = jDoc.ObjectGuid ? true : false;
      if (
        this.providedDocumentCategories.indexOf(providedDocInfo.referenceDocumentTypeDsc) < 0 &&
        providedDocInfo.referenceDocumentTypeDsc.length > 0
      ) {
        this.providedDocumentCategories.push(providedDocInfo.referenceDocumentTypeDsc);
      }
      this.providedDocumentsInfo.push(providedDocInfo);
    }
  }

  public SortParties(partiesList) {
    return _.sortBy(partiesList, (party: partiesInfoType) => {
      return party.lastName ? party.lastName.toUpperCase() : "";
    });
  }

  public navigateToLandingPage() {
    this.$router.push({ name: "Home" });
  }

  private reloadCaseDetails(): void {
    // Reset the properties to load new case details.
    this.civilFileInformation.fileNumber = this.$route.params.fileNumber;
    this.isMounted = false;
    this.isDataReady = false;
    this.partiesJson.length = 0;
    this.adjudicatorRestrictionsJson.length = 0;
    this.documentsDetailsJson.length = 0;
    this.providedDocumentsDetailsJson.length = 0;
    this.categories.length = 0;
    this.providedDocumentCategories.length = 0;
    this.leftPartiesInfo.length = 0;
    this.rightPartiesInfo.length = 0;
    this.adjudicatorRestrictionsInfo.length = 0;
    this.documentsInfo.length = 0;
    this.providedDocumentsInfo.length = 0;
    this.summaryDocumentsInfo.length = 0;

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
