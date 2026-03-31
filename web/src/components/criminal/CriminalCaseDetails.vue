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
        :loading="loading || !isMounted"
      >
        <v-row style="display: flex; flex-wrap: nowrap">
          <v-col cols="3" style="overflow-y: auto">
            <CriminalSidePanel
              v-if="isDataReady && details"
              :details="details"
              :summaryDetails="summaryDetails"
              :adjudicatorRestrictions="adjudicatorRestrictions"
              :loadingStates="{
                summary: sectionLoading.overview,
                participants: sectionLoading.participants,
                restrictions: sectionLoading.restrictions,
              }"
            />
          </v-col>

          <v-col class="px-0" style="overflow: auto">
            <CaseHeader
              :details="details"
              :activityClassCd="details.activityClassCd"
              :fileId="fileId"
              :transcripts="transcripts"
              :loadingStates="{
                participants: sectionLoading.participants,
                appearances: sectionLoading.appearances,
              }"
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
  import { DarsService, TranscriptDocument } from '@/services/DarsService';
  import { FilesService } from '@/services/FilesService';
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
    criminalAppearancesType,
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
      CriminalSidePanel,
      CaseHeader,
    },
    setup() {
      const commonStore = useCommonStore();
      const courtFileSearchStore = useCourtFileSearchStore();
      const criminalFileStore = useCriminalFileStore();
      const route = useRoute();
      //      const router = useRouter();
      const filesService = inject<FilesService>('filesService');
      const httpService = inject<HttpService>('httpService');
      const darsService = inject<DarsService>('darsService');

      if (!httpService || !darsService || !filesService) {
        throw new Error('Service is not available!');
      }

      const createEmptyAppearances = (): criminalAppearancesType => ({
        responseCd: '',
        responseMessageTxt: '',
        futureRecCount: '',
        historyRecCount: '',
        apprDetail: [],
        additionalProperties: {},
        additionalProp1: {},
        additionalProp2: {},
        additionalProp3: {},
      });

      const createEmptyDetails = (): criminalFileDetailsType =>
        ({
          responseCd: '',
          responseMessageTxt: '',
          fileNumberTxt: '',
          courtClassCd: '',
          courtClassDescription: '',
          courtLevelCd: '',
          courtLevelDescription: '',
          appearances: createEmptyAppearances(),
          activityClassCd: '',
          activityClassDesc: '',
          homeLocationAgenId: '',
          homeLocationAgencyName: '',
          homeLocationAgencyCode: '',
          homeLocationRegionName: '',
          justinNo: '',
          currentEstimateLenQty: '',
          currentEstimateLenUnit: '',
          initialEstimateLenQty: '',
          initialEstimateLenUnit: '',
          trialStartDt: '',
          mdocSubCategoryDsc: '',
          mdocCcn: '',
          assignedPartNm: '',
          approvedByAgencyCd: '',
          approvedByPartNm: '',
          approvalCrownAgencyTypeCd: '',
          crownEstimateLenQty: 0,
          crownEstimateLenDsc: '',
          crownEstimateLenUnit: '',
          caseAgeDays: '',
          indictableYN: '',
          complexityTypeCd: '',
          assignmentTypeDsc: '',
          trialRemark: [],
          participant: [],
          witness: [],
          crown: [],
          hearingRestriction: [],
        }) as criminalFileDetailsType;

      const participantList = ref<participantListInfoType[]>([]);
      const adjudicatorRestrictionsInfo = ref<
        AdjudicatorRestrictionsInfoType[]
      >([]);
      const bans = ref<bansInfoType[]>([]);
      const details = ref<criminalFileDetailsType>(createEmptyDetails());
      const summaryDetails = ref<criminalFileDetailsType>(createEmptyDetails());
      const isDataReady = ref(false);
      const isMounted = ref(false);
      const downloadCompleted = ref(true);
      const errorCode = ref(0);
      const errorText = ref('');
      const loading = ref(false);
      const sectionLoading = ref({
        overview: false,
        participants: false,
        appearances: false,
        restrictions: false,
        transcripts: false,
      });
      const activeLoadId = ref(0);
      const fileNumber = ref('');
      const fileId = ref('');
      const transcripts = ref<TranscriptDocument[]>([]);

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

      type AppearancePartIdPair = {
        appearanceId: string;
        partId: string;
      };

      const buildTranscriptToParticipantMappings = (
        transcripts: TranscriptDocument[],
        appearanceToPartId: Map<string, string>
      ): Map<string, AppearancePartIdPair[]> => {
        const transcriptToParticipants = new Map<
          string,
          AppearancePartIdPair[]
        >();

        transcripts.forEach((transcript) => {
          const transcriptId = `${transcript.orderId}-${transcript.id}`;
          const pairs: AppearancePartIdPair[] = [];

          transcript.appearances.forEach((appearance) => {
            const appearanceId = String(appearance.justinAppearanceId);
            const partId = appearanceToPartId.get(appearanceId);
            if (partId) {
              pairs.push({ appearanceId, partId });
            }
          });

          if (pairs.length > 0) {
            transcriptToParticipants.set(transcriptId, pairs);
          }
        });

        return transcriptToParticipants;
      };

      const addTranscriptsToParticipants = (
        transcriptToParticipants: Map<string, AppearancePartIdPair[]>,
        transcripts: TranscriptDocument[],
        participantMap: Map<string, participantListInfoType>
      ) => {
        transcriptToParticipants.forEach((pairs, transcriptId) => {
          const transcript = transcripts.find(
            (t) => `${t.orderId}-${t.id}` === transcriptId
          );

          if (!transcript) return;

          const processedParticipants = new Set<string>();

          pairs.forEach(({ appearanceId, partId }) => {
            if (processedParticipants.has(partId)) return;

            const participant = participantMap.get(partId);
            const matchedAppearance = transcript.appearances.find(
              (a) => String(a.justinAppearanceId) === appearanceId
            );

            if (participant && matchedAppearance) {
              const transcriptDoc = {
                partId,
                category: 'Transcript',
                documentTypeDescription: `Transcript - ${transcript.description}`,
                hasFutureAppearance: false,
                docmClassification: 'Transcript',
                docmId: transcript.id.toString(),
                issueDate: matchedAppearance.appearanceDt.split('T')[0],
                docmFormId: '',
                docmFormDsc: `Transcript - ${transcript.description}`,
                docmDispositionDsc: '',
                docmDispositionDate: '',
                imageId: transcript.id.toString(),
                documentPageCount: String(transcript.pagesComplete),
                additionalProperties: {},
                additionalProp1: {},
                additionalProp2: {},
                additionalProp3: {},
                transcriptOrderId: transcript.orderId.toString(),
                transcriptDocumentId: transcript.id.toString(),
                transcriptAppearanceId: appearanceId,
              } as any;

              const criminalParticipant = participantJson.value.find(
                (p) => p.partId === partId
              );
              if (criminalParticipant) {
                criminalParticipant.document.push(transcriptDoc);
              }
              processedParticipants.add(partId);
            }
          });
        });
      };

      onMounted(() => {
        const routeFileNumber = getSingleValue(route.params.fileNumber);
        criminalFileStore.criminalFileInformation.fileNumber = routeFileNumber;
        criminalFileStore.updateCriminalFile(
          criminalFileStore.criminalFileInformation
        );
        fileNumber.value = routeFileNumber;
      });

      const updateCriminalFileStore = () => {
        criminalFileStore.criminalFileInformation.fileNumber = fileNumber.value;
        criminalFileStore.criminalFileInformation.detailsData = details.value;
        criminalFileStore.criminalFileInformation.participantList =
          participantList.value;
        criminalFileStore.criminalFileInformation.adjudicatorRestrictionsInfo =
          adjudicatorRestrictionsInfo.value;
        criminalFileStore.criminalFileInformation.bans = bans.value;
        criminalFileStore.criminalFileInformation.courtLevel =
          details.value.courtLevelCd
            ? DecodeCourtLevel[details.value.courtLevelCd]
            : '';
        criminalFileStore.criminalFileInformation.courtClass =
          details.value.courtClassCd
            ? DecodeCourtClass[details.value.courtClassCd]
            : '';
        criminalFileStore.updateCriminalFile(
          criminalFileStore.criminalFileInformation
        );
      };

      const buildParticipantInfo = () => {
        participantList.value = [];
        bans.value = [];

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
          participantInfo.documentsJson = jParticipant.document ?? [];
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
      };

      const buildAdjudicatorRestrictions = () => {
        adjudicatorRestrictionsInfo.value = [];

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

      const syncTranscriptsToParticipants = () => {
        if (!participantJson.value.length) {
          details.value.participant = [];
          return;
        }

        buildParticipantInfo();

        participantJson.value.forEach((participant) => {
          participant.document = (participant.document ?? []).filter(
            (document) => document.category !== 'Transcript'
          );
        });

        if (
          details.value.appearances?.apprDetail?.length &&
          transcripts.value.length
        ) {
          const appearanceToPartId = new Map<string, string>();
          details.value.appearances.apprDetail.forEach((appr) => {
            appearanceToPartId.set(appr.appearanceId, appr.partId);
          });

          const transcriptToParticipants =
            buildTranscriptToParticipantMappings(
              transcripts.value,
              appearanceToPartId
            );

          const participantMap = new Map(
            participantList.value.map((participant) => [participant.partId, participant])
          );

          addTranscriptsToParticipants(
            transcriptToParticipants,
            transcripts.value,
            participantMap
          );
        }

        details.value.participant = [...participantJson.value];
        buildParticipantInfo();
      };

      const isCurrentLoad = (loadId: number) => activeLoadId.value === loadId;

      const loadOverview = async (loadId: number) => {
        sectionLoading.value.overview = true;
        loading.value = true;

        try {
          const overview = await filesService.criminalFileOverview(fileNumber.value);

          if (!isCurrentLoad(loadId)) {
            return null;
          }

          details.value = {
            ...createEmptyDetails(),
            ...overview,
            appearances: createEmptyAppearances(),
            participant: overview.participant ?? [],
            crown: overview.crown ?? [],
            witness: overview.witness ?? [],
            hearingRestriction: [],
          } as criminalFileDetailsType;

          summaryDetails.value = {
            ...details.value,
            appearances: {
              ...details.value.appearances,
              apprDetail: [...(details.value.appearances?.apprDetail ?? [])],
            },
            participant: [...(details.value.participant ?? [])],
            crown: [...(details.value.crown ?? [])],
            witness: [...(details.value.witness ?? [])],
            hearingRestriction: [...(details.value.hearingRestriction ?? [])],
          } as criminalFileDetailsType;

          fileId.value = details.value.justinNo;
          updateCriminalFileStore();
          isDataReady.value = true;

          return details.value;
        } catch (err: any) {
          if (isCurrentLoad(loadId)) {
            errorCode.value = err?.status ?? 500;
            errorText.value = err?.statusText ?? 'An error has occurred.';
            console.log(err);
          }

          return null;
        } finally {
          if (isCurrentLoad(loadId)) {
            sectionLoading.value.overview = false;
            loading.value = false;
            isMounted.value = true;
          }
        }
      };

      const loadAppearances = async (loadId: number) => {
        sectionLoading.value.appearances = true;

        try {
          const appearances = await filesService.criminalFileAppearances(
            fileNumber.value
          );

          if (!isCurrentLoad(loadId)) {
            return;
          }

          details.value.appearances = appearances ?? createEmptyAppearances();
          syncTranscriptsToParticipants();
          updateCriminalFileStore();
        } catch (err) {
          if (isCurrentLoad(loadId)) {
            console.error('Error loading criminal appearances:', err);
          }
        } finally {
          if (isCurrentLoad(loadId)) {
            sectionLoading.value.appearances = false;
          }
        }
      };

      const loadParticipants = async (loadId: number) => {
        sectionLoading.value.participants = true;

        try {
          const participants = await filesService.criminalFileParticipants(
            fileNumber.value
          );

          if (!isCurrentLoad(loadId)) {
            return;
          }

          participantJson.value = participants ?? [];
          syncTranscriptsToParticipants();
          updateCriminalFileStore();
        } catch (err) {
          if (isCurrentLoad(loadId)) {
            console.error('Error loading criminal participants:', err);
          }
        } finally {
          if (isCurrentLoad(loadId)) {
            sectionLoading.value.participants = false;
          }
        }
      };

      const loadHearingRestrictions = async (loadId: number) => {
        sectionLoading.value.restrictions = true;

        try {
          const hearingRestrictions =
            await filesService.criminalFileHearingRestrictions(fileNumber.value);

          if (!isCurrentLoad(loadId)) {
            return;
          }

          adjudicatorRestrictionsJson.value = hearingRestrictions ?? [];
          details.value.hearingRestriction = hearingRestrictions ?? [];
          buildAdjudicatorRestrictions();
          updateCriminalFileStore();
        } catch (err) {
          if (isCurrentLoad(loadId)) {
            console.error('Error loading criminal hearing restrictions:', err);
          }
        } finally {
          if (isCurrentLoad(loadId)) {
            sectionLoading.value.restrictions = false;
          }
        }
      };

      const loadTranscripts = async (justinNo: string, loadId: number) => {
        sectionLoading.value.transcripts = true;

        try {
          const transcriptsData = await darsService
            .getTranscripts(undefined, justinNo)
            .catch((error) => {
              console.error('Error loading transcripts:', error);
              return [];
            });

          if (!isCurrentLoad(loadId)) {
            return;
          }

          transcripts.value = transcriptsData;
          syncTranscriptsToParticipants();
          updateCriminalFileStore();
        } finally {
          if (isCurrentLoad(loadId)) {
            sectionLoading.value.transcripts = false;
          }
        }
      };

      const getFileDetails = async () => {
        errorCode.value = 0;
        const loadId = ++activeLoadId.value;

        details.value = createEmptyDetails();
        participantJson.value = [];
        adjudicatorRestrictionsJson.value = [];
        participantList.value = [];
        bans.value = [];
        adjudicatorRestrictionsInfo.value = [];
        transcripts.value = [];
        fileId.value = '';
        summaryDetails.value = createEmptyDetails();
        isMounted.value = false;
        isDataReady.value = false;

        const overview = await loadOverview(loadId);

        if (!overview) {
          if (isCurrentLoad(loadId) && errorCode.value == 0) {
            errorCode.value = 200;
          }
          return;
        }

        void loadParticipants(loadId);
        void loadAppearances(loadId);
        void loadHearingRestrictions(loadId);
        void loadTranscripts(overview.justinNo, loadId);
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

      // const navigateToLandingPage = () => {
      //   router.push({ name: 'Home' });
      // };

      const reloadCaseDetails = () => {
        criminalFileStore.criminalFileInformation.fileNumber = fileNumber.value;
        void getFileDetails();
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
        sectionLoading,
        details,
        summaryDetails,
        adjudicatorRestrictions: adjudicatorRestrictionsInfo,
        transcripts,
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
