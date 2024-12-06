<template>
  <div class="main-container" style="overflow: hidden">
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

    <b-card bg-variant="light" v-if="isMounted && !isDataReady">
      <b-card style="min-height: 100px">
        <span v-if="errorCode == 404"
          >This
          <b
            >File-Number '{{
              civilFileStore.civilFileInformation.fileNumber
            }}'</b
          >
          doesn't exist in the <b>civil</b> records.</span
        >
        <span v-else-if="errorCode == 200 || errorCode == 204">
          Bad Data in
          <b
            >File-Number '{{
              civilFileStore.civilFileInformation.fileNumber
            }}'</b
          >.</span
        >
        <span v-else-if="errorCode == 403">
          You are not authorized to access this file.
        </span>
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
        <b-col md="3" cols="3" style="overflow: auto">
          <court-files-selector
            v-if="isDataReady && selectedFiles.length > 0"
            :files="selectedFiles"
            @reload-case-details="reloadCaseDetails"
            targetCaseDetails="CivilCaseDetails"
          />
          <civil-side-panel v-if="isDataReady" />
        </b-col>
        <b-col col md="9" cols="9" class="px-0" style="overflow: auto">
          <civil-header-top v-if="isDataReady" />
          <civil-header v-if="isDataReady" />

          <b-row class="ml-0" v-if="showAllDocuments">
            <h2 style="white-space: pre" v-if="isDataReady">
              {{ selectedSideBar }}
            </h2>
            <custom-overlay
              v-if="isDataReady"
              :show="!downloadCompleted"
              style="padding: 0 1rem; margin-left: auto; margin-right: 2rem"
            >
              <b-button
                v-if="enableArchive"
                @click="downloadAllDocuments()"
                size="md"
                variant="info"
                style="padding: 0 1rem; margin-left: auto; margin-right: 2rem"
              >
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
          <civil-provided-documents-view
            v-if="showProvidedDocuments || showAllDocuments"
          />
          <civil-past-appearances v-if="showPastAppearances" />
          <civil-future-appearances v-if="showFutureAppearances" />
          <b-card><br /></b-card>
        </b-col>
      </b-row>
    </b-card>
    <b-modal
      v-if="isMounted"
      v-model="showSealedWarning"
      id="bv-modal-ban"
      hide-header
      hide-footer
    >
      <b-card v-if="isSealed">
        This file has been sealed. Only authorized users are permitted access to
        sealed files.
      </b-card>
      <b-card v-else-if="docIsSealed">
        This File contains one or more Sealed Documents.
      </b-card>
      <!-- <b-button class="mt-3 bg-primary" @click="$bvModal.hide('bv-modal-ban')"
        >Continue</b-button
      > -->
    </b-modal>
  </div>
</template>

<script lang="ts">
  import CivilCommentNotes from '@/components/civil/CivilCommentNotes.vue';
  import CivilDocumentsView from '@/components/civil/CivilDocumentsView.vue';
  import CivilFutureAppearances from '@/components/civil/CivilFutureAppearances.vue';
  import CivilHeader from '@/components/civil/CivilHeader.vue';
  import CivilHeaderTop from '@/components/civil/CivilHeaderTop.vue';
  import CivilParties from '@/components/civil/CivilParties.vue';
  import CivilPastAppearances from '@/components/civil/CivilPastAppearances.vue';
  import CivilProvidedDocumentsView from '@/components/civil/CivilProvidedDocumentsView.vue';
  import CivilSidePanel from '@/components/civil/CivilSidePanel.vue';
  import CourtFilesSelector from '@/components/shared/CourtFilesSelector.vue';
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import {
    useCivilFileStore,
    useCommonStore,
    useCourtFileSearchStore,
  } from '@/stores';
  import {
    csrRequestsInfoType,
    documentsInfoType,
    partiesInfoType,
    referenceDocumentsInfoType,
    summaryDocumentsInfoType,
  } from '@/types/civil';
  import {
    civilDocumentType,
    civilFileDetailsType,
    civilHearingRestrictionType,
    civilReferenceDocumentJsonType,
    partyType,
  } from '@/types/civil/jsonTypes';
  import {
    AdjudicatorRestrictionsInfoType,
    ArchiveInfoType,
    DocumentRequestsInfoType,
  } from '@/types/common';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import { getSingleValue } from '@/utils/utils';
  import base64url from 'base64url';
  import _ from 'underscore';
  import { computed, defineComponent, inject, onMounted, ref } from 'vue';
  import { useRoute } from 'vue-router';
  import CustomOverlay from '../CustomOverlay.vue';
  import shared from '../shared';

  export default defineComponent({
    components: {
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
    setup() {
      const civilFileStore = useCivilFileStore();
      const commonStore = useCommonStore();
      const courtFileSearchStore = useCourtFileSearchStore();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('Service is undefined.');
      }

      const route = useRoute();

      let leftPartiesInfo: partiesInfoType[] = [];
      let rightPartiesInfo: partiesInfoType[] = [];
      const adjudicatorRestrictionsInfo = ref<
        AdjudicatorRestrictionsInfoType[]
      >([]);
      const documentsInfo = ref<documentsInfoType[]>([]);
      const providedDocumentsInfo = ref<referenceDocumentsInfoType[]>([]);
      const summaryDocumentsInfo = ref<summaryDocumentsInfoType[]>([]);

      const isDataReady = ref(false);
      const isMounted = ref(false);
      const downloadCompleted = ref(true);
      const isSealed = ref(false);
      const docIsSealed = ref(false);
      const showSealedWarning = ref(false);
      const errorCode = ref(0);
      const errorText = ref('');

      const partiesJson = ref<partyType[]>([]);
      const adjudicatorRestrictionsJson = ref<civilHearingRestrictionType[]>(
        []
      );
      let documentsDetailsJson: civilDocumentType[] = [];
      let providedDocumentsDetailsJson: civilReferenceDocumentJsonType[] = [];
      const categories = ref<string[]>([]);
      const providedDocumentCategories = ref<string[]>([]);
      const sidePanelTitles = ref([
        'Case Details',
        'Future Appearances',
        'Past Appearances',
        'All Documents',
        'Documents',
        'Provided Documents',
      ]);

      onMounted(() => {
        civilFileStore.civilFileInformation.fileNumber = getSingleValue(
          route.params.fileNumber
        );
        civilFileStore.updateCivilFile(civilFileStore.civilFileInformation);
        getFileDetails();
        navigateToSection(route.params.section);
      });

      const navigateToSection = (section) => {
        if (section) {
          const sections = civilFileStore.showSections;
          for (const item of sidePanelTitles.value) {
            if (item == section) sections[item] = true;
            else sections[item] = false;
          }
          civilFileStore.updateShowSections(sections);
        }
      };

      const getFileDetails = () => {
        errorCode.value = 0;
        httpService
          .get<civilFileDetailsType>(
            'api/files/civil/' + civilFileStore.civilFileInformation.fileNumber
          )
          .then(
            (Response) => Response,
            (err) => {
              errorCode.value = err.status;
              errorText.value = err.statusText;
              // $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //   title: "An error has occured.",
              //   variant: "danger",
              //   autoHideDelay: 10000,
              // });
              console.log(err);
            }
          )
          .then((data) => {
            if (data) {
              civilFileStore.civilFileInformation.detailsData = data;
              partiesJson.value = data.party;
              adjudicatorRestrictionsJson.value = data.hearingRestriction;
              documentsDetailsJson = [...data.document];
              providedDocumentsDetailsJson = [...data.referenceDocument];
              if (data.sealedYN == 'Y') {
                isSealed.value = true;
              }
              ExtractCaseInfo();
              if (
                adjudicatorRestrictionsInfo.value.length > 0 ||
                leftPartiesInfo.length > 0 ||
                rightPartiesInfo.length > 0 ||
                documentsInfo.value.length > 0 ||
                summaryDocumentsInfo.value.length > 0
              ) {
                civilFileStore.civilFileInformation.leftPartiesInfo =
                  leftPartiesInfo;
                civilFileStore.civilFileInformation.rightPartiesInfo =
                  rightPartiesInfo;
                civilFileStore.civilFileInformation.isSealed = isSealed.value;
                civilFileStore.civilFileInformation.adjudicatorRestrictionsInfo =
                  adjudicatorRestrictionsInfo.value;
                civilFileStore.civilFileInformation.documentsInfo =
                  documentsInfo.value;
                civilFileStore.civilFileInformation.summaryDocumentsInfo =
                  summaryDocumentsInfo.value;
                civilFileStore.civilFileInformation.referenceDocumentInfo =
                  providedDocumentsInfo.value;
                civilFileStore.civilFileInformation.categories =
                  categories.value;
                civilFileStore.civilFileInformation.providedDocumentCategories =
                  providedDocumentCategories.value;
                civilFileStore.updateCivilFile(
                  civilFileStore.civilFileInformation
                );
                if (isSealed.value || docIsSealed.value) {
                  showSealedWarning.value = true;
                }
                isDataReady.value = true;
              } else errorCode.value = 200;
            } else if (errorCode.value == 0) errorCode.value = 200;
            isMounted.value = true;
          });
      };

      const downloadAllDocuments = () => {
        const fileName = shared.generateFileName(CourtDocumentType.CivilZip, {
          location:
            civilFileStore.civilFileInformation.detailsData
              .homeLocationAgencyName,
          courtLevel:
            civilFileStore.civilFileInformation.detailsData.courtLevelCd,
          fileNumberText:
            civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
        });

        const documentsToDownload = {
          zipName: fileName,
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
          vcCivilFileId: civilFileStore.civilFileInformation.fileNumber,
        } as ArchiveInfoType;
        for (const doc of providedDocumentsInfo.value) {
          if (doc.isEnabled) {
            const id = doc.objectGuid;
            const documentRequest = {} as DocumentRequestsInfoType;
            documentRequest.isCriminal = false;
            const documentData: DocumentData = {
              appearanceDate: beautifyDate(doc.appearanceDate),
              courtLevel:
                civilFileStore.civilFileInformation.detailsData.courtLevelCd,
              documentDescription: doc.descriptionText,
              documentId: id,
              fileId: civilFileStore.civilFileInformation.fileNumber,
              fileNumberText:
                civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
              location:
                civilFileStore.civilFileInformation.detailsData
                  .homeLocationAgencyName,
              partyName: doc.partyName.toString(),
            };
            documentRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.ProvidedCivil,
              documentData
            );
            documentRequest.base64UrlEncodedDocumentId = base64url(id);
            documentRequest.fileId =
              civilFileStore.civilFileInformation.fileNumber;
            documentsToDownload.documentRequests.push(documentRequest);
          }
        }

        for (const doc of documentsInfo.value) {
          if (doc.isEnabled) {
            const id = doc.documentId;
            const documentRequest = {} as DocumentRequestsInfoType;
            documentRequest.isCriminal = false;
            const documentData: DocumentData = {
              courtLevel:
                civilFileStore.civilFileInformation.detailsData.courtLevelCd,
              dateFiled: beautifyDate(doc.dateFiled),
              documentDescription: doc.documentType,
              documentId: id,
              fileId: civilFileStore.civilFileInformation.fileNumber,
              fileNumberText:
                civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
              location:
                civilFileStore.civilFileInformation.detailsData
                  .homeLocationAgencyName,
            };
            documentRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.Civil,
              documentData
            );
            documentRequest.base64UrlEncodedDocumentId = base64url(id);
            documentRequest.fileId =
              civilFileStore.civilFileInformation.fileNumber;
            documentsToDownload.documentRequests.push(documentRequest);
          }
        }

        for (const doc of summaryDocumentsInfo.value) {
          if (doc.isEnabled) {
            const id = doc['Appearance ID'];
            const csrRequest = {} as csrRequestsInfoType;
            csrRequest.appearanceId = id;
            const documentData: DocumentData = {
              appearanceDate: beautifyDate(doc.appearanceDate),
              appearanceId: id,
              courtLevel:
                civilFileStore.civilFileInformation.detailsData.courtLevelCd,
              documentDescription: doc.documentType,
              fileNumberText:
                civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
              fileId: civilFileStore.civilFileInformation.fileNumber,
              location:
                civilFileStore.civilFileInformation.detailsData
                  .homeLocationAgencyName,
            };
            csrRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.CSR,
              documentData
            );
            documentsToDownload.csrRequests.push(csrRequest);
          }
        }

        if (
          documentsToDownload.csrRequests.length > 0 ||
          documentsToDownload.documentRequests.length > 0
        ) {
          downloadCompleted.value = false;
          httpService
            .post<Blob>(
              'api/files/archive',
              documentsToDownload,
              {
                'Content-Type': 'application/json',
              },
              'blob'
            )
            .then(
              (data) => {
                const blob = data;
                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                document.body.appendChild(link);
                link.download = documentsToDownload.zipName;
                link.click();
                setTimeout(() => URL.revokeObjectURL(link.href), 1000);
                downloadCompleted.value = true;
              },
              (err) => {
                // $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
                //   title: "An error has occured.",
                //   variant: "danger",
                //   autoHideDelay: 10000,
                // });
                console.log(err);
                downloadCompleted.value = true;
              }
            );
        }
      };

      const selectedSideBar = computed(() => {
        for (const title of sidePanelTitles.value) {
          if (civilFileStore.showSections[title] == true) return title;
        }
        return '';
      });

      const showCaseDetails = computed(() => {
        return civilFileStore.showSections['Case Details'] && isDataReady.value;
      });

      const showFutureAppearances = computed(() => {
        return (
          (civilFileStore.showSections['Case Details'] ||
            civilFileStore.showSections['Future Appearances']) &&
          isDataReady.value
        );
      });

      const showPastAppearances = computed(() => {
        return (
          (civilFileStore.showSections['Case Details'] ||
            civilFileStore.showSections['Past Appearances']) &&
          isDataReady.value
        );
      });

      const showProvidedDocuments = computed(() => {
        return (
          (civilFileStore.showSections['Case Details'] ||
            civilFileStore.showSections['Provided Documents']) &&
          isDataReady.value
        );
      });

      const showDocuments = computed(() => {
        return (
          (civilFileStore.showSections['Case Details'] ||
            civilFileStore.showSections['Documents']) &&
          isDataReady.value
        );
      });

      const showAllDocuments = computed(() => {
        return (
          (civilFileStore.showSections['Case Details'] ||
            civilFileStore.showSections['All Documents']) &&
          isDataReady.value
        );
      });

      const ExtractCaseInfo = () => {
        let partyIndex = 0;
        for (const jParty of partiesJson.value) {
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
          partyInfo.firstName = jParty.givenNm ? jParty.givenNm : '';
          partyInfo.lastName = jParty.lastNm ? jParty.lastNm : jParty.orgNm;
          commonStore.updateDisplayName({
            lastName: partyInfo.lastName,
            givenName: partyInfo.firstName,
          });
          partyInfo.name = commonStore.displayName;
          partyInfo.id = jParty.partyId;
          partyInfo.index = partyIndex;
          partyIndex = partyIndex + 1;
          if (partyInfo.leftRight == 'R') {
            rightPartiesInfo.push(partyInfo);
          } else {
            leftPartiesInfo.push(partyInfo);
          }
        }
        leftPartiesInfo = [...SortParties(leftPartiesInfo)];
        rightPartiesInfo = [...SortParties(rightPartiesInfo)];

        for (const jRestriction of adjudicatorRestrictionsJson.value) {
          const restrictionInfo = {} as AdjudicatorRestrictionsInfoType;
          restrictionInfo.adjRestriction = jRestriction.adjInitialsTxt
            ? jRestriction.hearingRestrictionTypeDsc +
              ': ' +
              jRestriction.adjInitialsTxt
            : jRestriction.hearingRestrictionTypeDsc;
          restrictionInfo.adjudicator = jRestriction.adjInitialsTxt
            ? jRestriction.adjInitialsTxt + ' - ' + jRestriction.adjFullNm
            : jRestriction.adjFullNm;
          restrictionInfo.fullName = jRestriction.adjFullNm;
          restrictionInfo.status = jRestriction.hearingRestrictionTypeDsc + ' ';
          restrictionInfo.appliesTo = jRestriction.applyToNm
            ? jRestriction.applyToNm
            : 'All Documents';

          adjudicatorRestrictionsInfo.value.push(restrictionInfo);
        }

        for (const docIndex in documentsDetailsJson) {
          const jDoc = documentsDetailsJson[docIndex];
          if (jDoc.documentTypeCd != 'CSR') {
            const docInfo = {} as documentsInfoType;
            docInfo.index = docIndex;
            docInfo.seq = jDoc.fileSeqNo;
            docInfo.documentType = jDoc.documentTypeDescription;
            docInfo.concluded = jDoc.concludedYn;
            if (
              categories.value.indexOf('CONCLUDED') < 0 &&
              docInfo.concluded.toUpperCase() == 'Y'
            )
              categories.value.push('CONCLUDED');
            docInfo.nextAppearanceDate = jDoc.nextAppearanceDt
              ? beautifyDate(jDoc.nextAppearanceDt)
              : '';
            if (
              docInfo.nextAppearanceDate.length > 0 &&
              categories.value.indexOf('SCHEDULED') < 0
            )
              categories.value.push('SCHEDULED');

            docInfo.category = jDoc.category ? jDoc.category : '';
            if (
              categories.value.indexOf(docInfo.category) < 0 &&
              docInfo.category.length > 0
            )
              categories.value.push(docInfo.category);

            docInfo.swornBy = jDoc.swornByNm ? jDoc.swornByNm : '';
            docInfo.affNo = jDoc.affidavitNo ? jDoc.affidavitNo : '';

            docInfo.act = [];
            if (jDoc.documentSupport && jDoc.documentSupport.length > 0) {
              for (const act of jDoc.documentSupport) {
                docInfo.act.push({ code: act.actCd, description: act.actDsc });
              }
            }
            if (jDoc.sealedYN == 'Y') {
              docIsSealed.value = true;
              docInfo.sealed = true;
            } else {
              docInfo.sealed = false;
            }
            docInfo.documentId = jDoc.civilDocumentId;
            docInfo.pdfAvail = jDoc.imageId ? true : false;
            if (docInfo.documentType.toUpperCase() == 'ORDER') {
              docInfo.dateFiled = jDoc.DateGranted
                ? jDoc.DateGranted.split(' ')[0]
                : '';
            } else {
              docInfo.dateFiled = jDoc.filedDt
                ? jDoc.filedDt.split(' ')[0]
                : '';
            }
            docInfo.issues = [];
            if (jDoc.issue && jDoc.issue.length > 0) {
              for (const issue of jDoc.issue) {
                docInfo.issues.push(issue.issueDsc);
              }
            }
            docInfo.comment = jDoc.commentTxt ? jDoc.commentTxt : '';
            docInfo.filedByName = [];
            if (jDoc.filedBy && jDoc.filedBy[0] && jDoc.filedBy.length > 0) {
              for (const filed of jDoc.filedBy) {
                if (filed.roleTypeCode) {
                  docInfo.filedByName.push(
                    filed.filedByName + ' (' + filed.roleTypeCode + ')'
                  );
                } else {
                  docInfo.filedByName.push(filed.filedByName);
                }
              }
            }
            docInfo.orderMadeDate = jDoc.DateGranted
              ? beautifyDate(jDoc.DateGranted)
              : '';
            docInfo.isChecked = false;
            docInfo.isEnabled = docInfo.pdfAvail;

            documentsInfo.value.push(docInfo);
          } else {
            const docInfo = {} as summaryDocumentsInfoType;
            docInfo.index = docIndex;
            docInfo.documentType = 'CourtSummary';
            docInfo.appearanceDate = jDoc.lastAppearanceDt.split(' ')[0];
            docInfo.appearanceId = jDoc.imageId;
            docInfo.pdfAvail = jDoc.imageId ? true : false;
            docInfo.isChecked = false;
            docInfo.isEnabled = docInfo.pdfAvail;
            summaryDocumentsInfo.value.push(docInfo);
          }
        }
        civilFileStore.updateHasNonParty(false);
        for (const providedDocIndex in providedDocumentsDetailsJson) {
          const jDoc = providedDocumentsDetailsJson[providedDocIndex];
          const providedDocInfo = {} as referenceDocumentsInfoType;
          providedDocInfo.appearanceId = jDoc.AppearanceId;

          providedDocInfo.partyId = [];
          providedDocInfo.partyName = [];
          providedDocInfo.nonPartyName = [];
          for (const refDocInterestIndex in jDoc.ReferenceDocumentInterest) {
            const refDocInterest =
              jDoc.ReferenceDocumentInterest[refDocInterestIndex];
            if (refDocInterest.PartyId)
              providedDocInfo.partyId.push(refDocInterest.PartyId);
            if (refDocInterest.PartyName)
              providedDocInfo.partyName.push(refDocInterest.PartyName);
            if (refDocInterest.NonPartyName)
              providedDocInfo.nonPartyName.push(refDocInterest.NonPartyName);
          }
          if (providedDocInfo.nonPartyName.length > 0) {
            civilFileStore.updateHasNonParty(true);
          }
          providedDocInfo.appearanceDate = jDoc.AppearanceDate;
          providedDocInfo.descriptionText = jDoc.DescriptionText;
          // providedDocInfo.enterDtm = jDoc.EnterDtm;
          providedDocInfo.referenceDocumentTypeDsc =
            jDoc.ReferenceDocumentTypeDsc;
          providedDocInfo.objectGuid = jDoc.ObjectGuid;
          providedDocInfo.isChecked = false;
          providedDocInfo.isEnabled = jDoc.ObjectGuid ? true : false;
          if (
            providedDocumentCategories.value.indexOf(
              providedDocInfo.referenceDocumentTypeDsc
            ) < 0 &&
            providedDocInfo.referenceDocumentTypeDsc.length > 0
          ) {
            providedDocumentCategories.value.push(
              providedDocInfo.referenceDocumentTypeDsc
            );
          }
          providedDocumentsInfo.value.push(providedDocInfo);
        }
      };

      const SortParties = (partiesList) => {
        return _.sortBy(partiesList, (party: partiesInfoType) => {
          return party.lastName ? party.lastName.toUpperCase() : '';
        });
      };

      // const navigateToLandingPage = () => {
      //   router.push({ name: 'Home' });
      // };

      const reloadCaseDetails = () => {
        // Reset the properties to load new case details.
        civilFileStore.civilFileInformation.fileNumber = getSingleValue(
          route.params.fileNumber
        );
        isMounted.value = false;
        isDataReady.value = false;
        partiesJson.value.length = 0;
        adjudicatorRestrictionsJson.value.length = 0;
        documentsDetailsJson.length = 0;
        providedDocumentsDetailsJson.length = 0;
        categories.value.length = 0;
        providedDocumentCategories.value.length = 0;
        leftPartiesInfo.length = 0;
        rightPartiesInfo.length = 0;
        adjudicatorRestrictionsInfo.value.length = 0;
        documentsInfo.value.length = 0;
        providedDocumentsInfo.value.length = 0;
        summaryDocumentsInfo.value.length = 0;

        getFileDetails();
      };

      return {
        isMounted,
        isDataReady,
        errorCode,
        errorText,
        selectedFiles: courtFileSearchStore.selectedFiles,
        reloadCaseDetails,
        showAllDocuments,
        selectedSideBar,
        civilFileStore,
        downloadCompleted,
        enableArchive: commonStore.enableArchive,
        downloadAllDocuments,
        showCaseDetails,
        showDocuments,
        showProvidedDocuments,
        showPastAppearances,
        showFutureAppearances,
        showSealedWarning,
        isSealed,
        docIsSealed,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }

  body {
    overflow-x: hidden;
  }
</style>
