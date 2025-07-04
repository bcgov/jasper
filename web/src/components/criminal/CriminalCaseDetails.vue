<template>
  <div class="main-container" style="overflow: hidden">
    <!-- todo: Extract this out to more generic location -->
    <v-card style="min-height: 40px" v-if="errorCode > 0 && errorCode == 403">
      <span> You are not authorized to access this page. </span>
    </v-card>
    <!------------------------------------------------------->
    <v-row>
      <v-col>
        <court-files-selector
          v-model="fileNumber"
          :files="selectedFiles"
          :courtClass="details.courtClassCd"
        />
      </v-col>
    </v-row>
    <v-container>
      <v-skeleton-loader
        class="my-0"
        type="table"
        :loading="loading || !isMounted"
      >
        <v-row style="display: flex; flex-wrap: nowrap">
          <v-col cols="3" style="overflow-y: auto">
            <CriminalSidePanel
              v-if="isDataReady && details"
              :details="details"
              :adjudicatorRestrictions="adjudicatorRestrictions"
            />
          </v-col>

          <v-col class="px-0" style="overflow: auto">
            <CaseHeader
              :details="details"
              :activityClassCd="details.activityClassCd"
              :fileId="fileId"
            />
            <!-- Comment this out for now as we continue to deprecate it -->
            <!-- 
            <b-row class="ml-0" v-if="showDocuments">
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
                  @click="downloadDocuments()"
                  size="md"
                  variant="info"
                  style="padding: 0 1rem; margin-left: auto; margin-right: 2rem"
                >
                  Download All Documents
                </b-button>
              </custom-overlay>
            </b-row>

            <h2 style="white-space: pre" v-if="!showDocuments && isDataReady">
              {{ selectedSideBar }}
            </h2>

            <criminal-participants v-if="showCaseDetails" />
            <criminal-crown-information v-if="showCaseDetails" />
            <criminal-crown-notes v-if="showCaseDetails"/> Asked to be hidden by Kevin SCV-140.
            <criminal-past-appearances v-if="showPastAppearances" />
            <criminal-future-appearances v-if="showFutureAppearances" />
            <criminal-documents-view v-if="showDocuments" />
            <criminal-witnesses v-if="showWitnesses" />
            <criminal-sentence v-if="showSentenceOrder" />
            <b-card><br /></b-card> -->
          </v-col>
        </v-row>
      </v-skeleton-loader>
    </v-container>
  </div>
</template>

<script lang="ts">
  import CaseHeader from '@/components/case-details/common/CaseHeader.vue';
  import CourtFilesSelector from '@/components/case-details/common/CourtFilesSelector.vue';
  import CriminalSidePanel from '@/components/case-details/criminal/CriminalSidePanel.vue';
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import {
    useCommonStore,
    useCourtFileSearchStore,
    useCriminalFileStore,
  } from '@/stores';
  import {
    AdjudicatorRestrictionsInfoType,
    ArchiveInfoType,
    DocumentRequestsInfoType,
  } from '@/types/common';
  import {
    bansInfoType,
    chargesInfoType,
    participantListInfoType,
    ropRequestsInfoType,
  } from '@/types/criminal';
  import {
    criminalFileDetailsType,
    criminalHearingRestrictionType,
    criminalParticipantType,
  } from '@/types/criminal/jsonTypes';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import { getSingleValue } from '@/utils/utils';
  import base64url from 'base64url';
  import {
    computed,
    defineComponent,
    inject,
    onMounted,
    ref,
    watch,
  } from 'vue';
  import { useRoute } from 'vue-router';
  import CustomOverlay from '../CustomOverlay.vue';
  import shared from '../shared';

  enum DecodeCourtLevel {
    'P' = 0,
    'S' = 1,
    'A' = 2,
  }
  enum DecodeCourtClass {
    'A' = 0,
    'Y' = 1,
    'T' = 2,
    'F' = 3,
    'C' = 4,
    'M' = 5,
    'L' = 6,
    'R' = 7,
    'B' = 8,
    'D' = 9,
    'E' = 10,
    'G' = 11,
    'H' = 12,
    'N' = 13,
    'O' = 14,
    'P' = 15,
    'S' = 16,
    'V' = 17,
  }

  export default defineComponent({
    components: {
      CourtFilesSelector,
      CustomOverlay,
      CriminalSidePanel,
      CaseHeader,
    },
    setup() {
      const commonStore = useCommonStore();
      const courtFileSearchStore = useCourtFileSearchStore();
      const criminalFileStore = useCriminalFileStore();
      const route = useRoute();
      //      const router = useRouter();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('HttpService is not available!');
      }

      const participantList = ref<participantListInfoType[]>([]);
      const adjudicatorRestrictionsInfo = ref<
        AdjudicatorRestrictionsInfoType[]
      >([]);
      const bans = ref<bansInfoType[]>([]);
      const details = ref<criminalFileDetailsType>(
        {} as criminalFileDetailsType
      );
      const isDataReady = ref(false);
      const isMounted = ref(false);
      const downloadCompleted = ref(true);
      const errorCode = ref(0);
      const errorText = ref('');
      const loading = ref(false);
      const fileNumber = ref('');
      const fileId = ref('');

      watch(fileNumber, () => {
        reloadCaseDetails();
      });

      const participantJson = ref<criminalParticipantType[]>([]);
      const adjudicatorRestrictionsJson = ref<criminalHearingRestrictionType[]>(
        []
      );

      const sidePanelTitles = [
        'Case Details',
        'Future Appearances',
        'Past Appearances',
        'Witnesses',
        'Documents',
        'Sentence/Order Details',
      ];

      const topTitles = [
        'Case Details',
        'Future Appearances',
        'Past Appearances',
        'Witnesses',
        'Criminal Documents',
        'Criminal Sentences',
      ];

      const statusFields = [
        { key: 'Warrant Issued', abbr: 'W', code: 'warrantYN' },
        { key: 'In Custody', abbr: 'IC', code: 'inCustodyYN' },
        { key: 'Detention Order', abbr: 'DO', code: 'detainedYN' },
        { key: 'Interpreter Required', abbr: 'INT', code: 'interpreterYN' },
      ];

      onMounted(() => {
        loading.value = true;
        const routeFileNumber = getSingleValue(route.params.fileNumber);
        criminalFileStore.criminalFileInformation.fileNumber = routeFileNumber;
        criminalFileStore.updateCriminalFile(
          criminalFileStore.criminalFileInformation
        );
        getFileDetails();
        fileNumber.value = routeFileNumber;
        loading.value = false;
      });

      const getFileDetails = () => {
        errorCode.value = 0;
        httpService
          .get<criminalFileDetailsType>(
            'api/files/criminal/' +
              criminalFileStore.criminalFileInformation.fileNumber
          )
          .then(
            (Response) => Response,
            (err) => {
              // this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //   title: "An error has occured.",
              //   variant: "danger",
              //   autoHideDelay: 10000,
              // });
              errorCode.value = err.status;
              errorText.value = err.statusText;
              console.log(err);
            }
          )
          .then((data) => {
            if (data) {
              criminalFileStore.criminalFileInformation.detailsData = data;
              participantJson.value = data.participant;
              adjudicatorRestrictionsJson.value = data.hearingRestriction;
              const courtLevel = DecodeCourtLevel[data.courtLevelCd];
              const courtClass = DecodeCourtClass[data.courtClassCd];
              ExtractFileInfo();
              //Allow blank participants, it's a real case file 1019 for example on dev.
              criminalFileStore.criminalFileInformation.participantList =
                participantList.value;
              criminalFileStore.criminalFileInformation.adjudicatorRestrictionsInfo =
                adjudicatorRestrictionsInfo.value;
              criminalFileStore.criminalFileInformation.bans = bans.value;
              criminalFileStore.criminalFileInformation.courtLevel = courtLevel;
              criminalFileStore.criminalFileInformation.courtClass = courtClass;
              criminalFileStore.updateCriminalFile(
                criminalFileStore.criminalFileInformation
              );
              details.value = data;
              fileId.value = data.justinNo;
              isDataReady.value = true;
              loading.value = false;
            } else if (errorCode.value == 0) errorCode.value = 200;
            loading.value = false;
            isMounted.value = true;
          });
      };

      const downloadDocuments = () => {
        const fileName = shared.generateFileName(
          CourtDocumentType.CriminalZip,
          {
            location:
              criminalFileStore.criminalFileInformation.detailsData
                .homeLocationAgencyName,
            courtClass:
              criminalFileStore.criminalFileInformation.detailsData
                .courtClassCd,
            courtLevel:
              criminalFileStore.criminalFileInformation.detailsData
                .courtLevelCd,
            fileNumberText:
              criminalFileStore.criminalFileInformation.detailsData
                .fileNumberTxt,
          }
        );
        const documentsToDownload = {
          zipName: fileName,
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
        } as ArchiveInfoType;

        for (const partIndex in participantList.value) {
          const partInfo = participantList.value[partIndex];

          for (const doc of partInfo.documentsJson) {
            if (doc.category != 'rop') {
              if (doc.imageId) {
                const id = doc.imageId;
                const documentRequest = {} as DocumentRequestsInfoType;
                documentRequest.isCriminal = true;
                const documentData: DocumentData = {
                  dateFiled: beautifyDate(doc.issueDate),
                  documentDescription: doc.documentTypeDescription,
                  documentId: id,
                  courtLevel:
                    criminalFileStore.criminalFileInformation.detailsData
                      .courtLevelCd,
                  courtClass:
                    criminalFileStore.criminalFileInformation.detailsData
                      .courtClassCd,
                  fileId: criminalFileStore.criminalFileInformation.fileNumber,
                  fileNumberText:
                    criminalFileStore.criminalFileInformation.detailsData
                      .fileNumberTxt,
                  location:
                    criminalFileStore.criminalFileInformation.detailsData
                      .homeLocationAgencyName,
                };
                documentRequest.pdfFileName = shared.generateFileName(
                  CourtDocumentType.Criminal,
                  documentData
                );
                documentRequest.base64UrlEncodedDocumentId = base64url(id);
                documentRequest.fileId =
                  criminalFileStore.criminalFileInformation.fileNumber;
                documentsToDownload.documentRequests.push(documentRequest);
              }
            } else {
              const ropRequest = {} as ropRequestsInfoType;
              const partId = doc.partId;
              const documentData: DocumentData = {
                courtClass:
                  criminalFileStore.criminalFileInformation.detailsData
                    .courtClassCd,
                courtLevel:
                  criminalFileStore.criminalFileInformation.detailsData
                    .courtLevelCd,
                documentDescription: 'ROP',
                fileNumberText:
                  criminalFileStore.criminalFileInformation.detailsData
                    .fileNumberTxt,
                partId: partId,
                profSeqNo: partInfo.profSeqNo,
                location:
                  criminalFileStore.criminalFileInformation.detailsData
                    .homeLocationAgencyName,
              };
              ropRequest.pdfFileName = shared.generateFileName(
                CourtDocumentType.ROP,
                documentData
              );
              ropRequest.partId = partId;
              ropRequest.profSequenceNumber = partInfo.profSeqNo;
              ropRequest.courtLevelCode =
                criminalFileStore.criminalFileInformation.courtLevel;
              ropRequest.courtClassCode =
                criminalFileStore.criminalFileInformation.courtClass;
              documentsToDownload.ropRequests.push(ropRequest);
            }
          }
        }

        if (
          documentsToDownload.ropRequests.length > 0 ||
          documentsToDownload.documentRequests.length > 0
        ) {
          // const options = {
          //   responseType: 'blob',
          //   headers: {
          //     'Content-Type': 'application/json',
          //   },
          // };
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
              (blob) => {
                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                document.body.appendChild(link);
                link.download = documentsToDownload.zipName;
                link.click();
                setTimeout(() => URL.revokeObjectURL(link.href), 1000);
                downloadCompleted.value = true;
              },
              (err) => {
                // this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
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
        for (const titleInx in sidePanelTitles) {
          if (criminalFileStore.showSections[sidePanelTitles[titleInx]] == true)
            return '  ' + topTitles[titleInx];
        }
        return '';
      });

      const showCaseDetails = computed(() => {
        return (
          criminalFileStore.showSections['Case Details'] && isDataReady.value
        );
      });

      const showDocuments = computed(() => {
        return criminalFileStore.showSections['Documents'] && isDataReady.value;
      });

      const showFutureAppearances = computed(() => {
        return (
          (criminalFileStore.showSections['Case Details'] ||
            criminalFileStore.showSections['Future Appearances']) &&
          isDataReady.value
        );
      });

      const showPastAppearances = computed(() => {
        return (
          (criminalFileStore.showSections['Case Details'] ||
            criminalFileStore.showSections['Past Appearances']) &&
          isDataReady.value
        );
      });

      const showWitnesses = computed(() => {
        return criminalFileStore.showSections['Witnesses'] && isDataReady.value;
      });

      const showSentenceOrder = computed(() => {
        return (
          criminalFileStore.showSections['Sentence/Order Details'] &&
          isDataReady.value
        );
      });

      const ExtractFileInfo = () => {
        for (const partIndex in participantJson.value) {
          const participantInfo = {} as participantListInfoType;
          const jParticipant = participantJson.value[partIndex];
          participantInfo.index = partIndex;
          participantInfo.firstName =
            jParticipant.givenNm.trim().length > 0 ? jParticipant.givenNm : '';
          participantInfo.lastName = jParticipant.lastNm
            ? jParticipant.lastNm
            : jParticipant.orgNm;
          commonStore.updateDisplayName({
            lastName: participantInfo.lastName,
            givenName: participantInfo.firstName,
          });
          participantInfo.name = commonStore.displayName;

          participantInfo.dob = jParticipant.birthDt
            ? new Date(jParticipant.birthDt.split(' ')[0])
                .toUTCString()
                .substr(4, 12)
            : '';
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
          for (const status of statusFields) {
            if (jParticipant[status.code] == 'Y')
              participantInfo.status.push(status);
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
            bans.value.push(banInfo);
          }

          participantInfo.documentsJson = jParticipant.document;
          participantInfo.countsJson = jParticipant.count;

          commonStore.updateDisplayName({
            lastName: jParticipant.counselLastNm
              ? jParticipant.counselLastNm
              : '',
            givenName: jParticipant.counselGivenNm
              ? jParticipant.counselGivenNm
              : '',
          });
          participantInfo.counsel = commonStore.displayName.trim.length
            ? 'JUSTIN: ' + commonStore.displayName
            : '';
          participantInfo.counselDesignationFiled =
            jParticipant.designatedCounselYN;
          participantList.value.push(participantInfo);
        }

        for (const jRestriction of adjudicatorRestrictionsJson.value) {
          const restrictionInfo = {} as AdjudicatorRestrictionsInfoType;
          restrictionInfo.adjRestriction = jRestriction.adjInitialsTxt
            ? jRestriction.hearingRestrictionTypeDsc +
              ': ' +
              jRestriction.adjInitialsTxt
            : jRestriction.hearingRestrictionTypeDsc;
          restrictionInfo.fullName = jRestriction.adjFullNm;
          restrictionInfo.adjudicator = jRestriction.adjInitialsTxt
            ? jRestriction.adjInitialsTxt + ' - ' + jRestriction.adjFullNm
            : jRestriction.adjFullNm;
          restrictionInfo.status = jRestriction.hearingRestrictionTypeDsc + ' ';
          restrictionInfo.appliesTo = jRestriction.partNm
            ? jRestriction.partNm
            : 'All participants on file';
          adjudicatorRestrictionsInfo.value.push(restrictionInfo);
        }
      };

      // const navigateToLandingPage = () => {
      //   router.push({ name: 'Home' });
      // };

      const reloadCaseDetails = () => {
        loading.value = true;
        // Reset the properties to load new case details.
        criminalFileStore.criminalFileInformation.fileNumber = fileNumber.value;
        participantList.value.length = 0;
        bans.value.length = 0;
        adjudicatorRestrictionsInfo.value.length = 0;
        isMounted.value = false;
        isDataReady.value = false;
        getFileDetails();
      };

      return {
        isMounted,
        isDataReady,
        errorCode,
        errorText,
        selectedFiles: courtFileSearchStore.selectedFiles,
        criminalFileInformation: criminalFileStore.criminalFileInformation,
        showDocuments,
        showCaseDetails,
        selectedSideBar,
        showSentenceOrder,
        reloadCaseDetails,
        downloadCompleted,
        enableArchive: commonStore.enableArchive,
        downloadDocuments,
        showPastAppearances,
        showFutureAppearances,
        showWitnesses,
        fileNumber,
        fileId,
        loading,
        details,
        adjudicatorRestrictions: adjudicatorRestrictionsInfo,
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
