<template>
  <!-- todo: Extract this out to more generic location -->
  <v-overlay :opacity="0.333" v-model="isLoading" />

  <v-card style="min-height: 40px" v-if="errorCode > 0 && errorCode == 403">
    <span> You are not authorized to access this page. </span>
  </v-card>
  <!------------------------------------------------------->
  <banner title="Court list" color="#183a4a" bgColor="#b4e6ff">
    <template #left v-if="appliedDate"
      >:
      <v-icon :icon="mdiChevronLeft" @click="addDay(-1)" />
      <b>{{ shortHandDate }}</b>
      <v-icon :icon="mdiChevronRight" @click="addDay(1)" />
    </template>
    <template #right>
      <v-btn @click="showDropdown = !showDropdown">
        Search by Courtroom & Date
      </v-btn>
    </template>
  </banner>

  <v-expand-transition>
    <v-card v-show="showDropdown" height="100" elevation="7">
      <court-list-search
        v-model:date="selectedDate"
        v-model:isSearching="searchingRequest"
        v-model:showDropdown="showDropdown"
        v-model:appliedDate="appliedDate"
        @courtListSearched="populateCardTablePairings"
      />
    </v-card>
  </v-expand-transition>
  <v-container>
    <v-skeleton-loader class="my-1" type="table" :loading="searchingRequest">
      <court-list-table-search
        v-if="cardTablePairings.length"
        v-model:filesFilter="selectedFilesFilter"
        v-model:AMPMFilter="selectedAMPMFilter"
        v-model:search="search"
        :isFuture
      />
      <template
        v-for="pairing in filteredTablePairings"
        :key="pairing.card.courtListLocationID"
      >
        <div class="w-100">
          <court-list-card :cardInfo="pairing.card" />
          <court-list-table
            :search="search"
            :data="pairing.tableData"
            :notes-by-appearance-id="notesByAppearanceId"
            :highlight-appearance-id="activeHighlightAppearanceId"
            :launcher-hovered="launcherHovered"
            @open-notes="openNotesDrawer"
          />
        </div>
      </template>
      <court-list-table-search-dialog
        v-model:showDialog="showDialog"
        :on-generate="onGenerateClick"
      />
    </v-skeleton-loader>
    <div
      v-if="
        !searchingRequest && !cardTablePairings.length && searchResultMessage
      "
    >
      <p>{{ searchResultMessage }}</p>
    </div>
  </v-container>

  <v-btn
    v-if="hasAnyNotes && !showNotesDrawer"
    class="notes-launcher"
    color="primary"
    size="large"
    icon
    rounded="circle"
    elevation="8"
    @click="
      () => {
        showNotesDrawer = true;
        selectedNoteAppearance = null;
      }
    "
    @mouseenter="launcherHovered = true"
    @mouseleave="launcherHovered = false"
  >
    <v-badge :content="totalNotesCount" color="error" floating rounded="lg">
      <v-icon :icon="mdiNoteTextOutline" rounded="lg" />
    </v-badge>
  </v-btn>

  <v-navigation-drawer
    v-if="hasAnyNotes"
    v-model="showNotesDrawer"
    location="left"
    permanent
    width="400"
    class="notes-drawer"
  >
    <template #prepend>
      <div class="drawer-prepend">
        <v-btn
          :icon="mdiClose"
          variant="text"
          color="primary"
          @click.stop="showNotesDrawer = false"
        />
        <div class="drawer-prepend-copy">
          <div class="text-subtitle-2 font-weight-bold">Notes</div>
          <div class="text-caption text-medium-emphasis">
            {{ totalNotesCount }} total
          </div>
        </div>
      </div>
    </template>

    <template #default>
      <div class="drawer-body">
        <!-- <v-sheet class="drawer-hero ma-3 pa-4" rounded="xl" color="primary">
          <div class="d-flex align-center justify-space-between ga-3">
            <div>
              <div class="text-overline text-white">Court List</div>
              <div class="text-h6 text-white font-weight-bold">Notes</div>
              <div class="text-body-2 text-white">Browse, highlight, and edit notes.</div>
            </div>
            <v-chip color="white" variant="flat">
              {{ totalNotesCount }}
            </v-chip>
          </div>
        </v-sheet> -->

        <div class="notes-panels-wrap">
          <v-expansion-panels variant="accordion" v-model="openPanels">
            <v-expansion-panel rounded="xl">
              <v-expansion-panel-title>
                Notes ({{ totalNotesCount }})
              </v-expansion-panel-title>
              <v-expansion-panel-text>
                <v-list density="compact">
                  <template
                    v-for="group in groupedNotes"
                    :key="group.fileNumber"
                  >
                    <div class="note-group-header">{{ group.fileNumber }}</div>
                    <v-list-item
                      v-for="entry in group.notes"
                      :key="entry.key"
                      :class="[
                        'mb-2',
                        'note-list-item',
                        {
                          'note-list-item--active':
                            entry.appearanceId ===
                            selectedNoteAppearance?.appearanceId,
                        },
                      ]"
                      rounded="lg"
                      @click="focusNoteEntry(entry.appearanceId)"
                      @mouseenter="hoveredAppearanceId = entry.appearanceId"
                      @mouseleave="hoveredAppearanceId = null"
                    >
                      <v-list-item-title class="text-body-2 text-wrap">
                        {{ entry.text }}
                      </v-list-item-title>
                      <v-list-item-subtitle class="text-wrap">
                        {{ entry.context }}
                      </v-list-item-subtitle>
                    </v-list-item>
                  </template>
                </v-list>
              </v-expansion-panel-text>
            </v-expansion-panel>
          </v-expansion-panels>
        </div>

        <v-card
          flat
          class="mx-3 mb-3 pa-4 context-card"
          rounded="xl"
          v-if="selectedNoteContext"
        >
          <template v-if="selectedNoteContext">
            <div class="text-body-1 font-weight-bold mb-3">
              {{ selectedNoteContext.primaryName }}
            </div>
            <v-row dense>
              <v-col cols="6">
                <div class="text-caption text-medium-emphasis">File #</div>
                <div class="text-body-2">
                  {{ selectedNoteContext.fileNumber }}
                </div>
              </v-col>
              <v-col cols="6">
                <div class="text-caption text-medium-emphasis">
                  Appearance #
                </div>
                <div class="text-body-2">
                  {{ selectedNoteContext.appearanceNumber }}
                </div>
              </v-col>
              <v-col cols="6">
                <div class="text-caption text-medium-emphasis">Date</div>
                <div class="text-body-2">{{ selectedNoteContext.date }}</div>
              </v-col>
              <v-col cols="6">
                <div class="text-caption text-medium-emphasis">Time</div>
                <div class="text-body-2">{{ selectedNoteContext.time }}</div>
              </v-col>
              <v-col cols="6">
                <div class="text-caption text-medium-emphasis">Room</div>
                <div class="text-body-2">{{ selectedNoteContext.room }}</div>
              </v-col>
              <v-col cols="6">
                <div class="text-caption text-medium-emphasis">Reason</div>
                <div class="text-body-2">{{ selectedNoteContext.reason }}</div>
              </v-col>
              <v-col cols="12">
                <div class="d-flex flex-wrap ga-2 mt-2">
                  <v-chip size="small" color="primary" variant="tonal">
                    {{ selectedNoteContext.division }}
                  </v-chip>
                  <v-chip size="small" color="secondary" variant="tonal">
                    {{ selectedNoteContext.status }}
                  </v-chip>
                  <v-chip
                    v-for="(chip, i) in currentChips"
                    :key="i"
                    size="small"
                    color="success"
                    variant="tonal"
                    closable
                    @click:close="removeChip(i)"
                  >
                    {{ chip }}
                  </v-chip>
                  <div class="d-flex align-center ga-1 chip-add-row">
                    <v-text-field
                      v-model="newChipText"
                      density="compact"
                      variant="outlined"
                      hide-details
                      placeholder="Add tag..."
                      class="chip-input"
                      @keydown.enter.prevent="addChip"
                    />
                    <v-btn
                      size="x-small"
                      icon
                      color="primary"
                      variant="tonal"
                      :disabled="!newChipText.trim()"
                      @click="addChip"
                    >
                      <v-icon :icon="mdiPlus" />
                    </v-btn>
                  </div>
                </div>
              </v-col>
            </v-row>
          </template>
        </v-card>

        <v-container class="overflow-y-auto px-3 pt-0 notes-scroll-area">
          <v-card
            v-for="note in currentNotes"
            :key="note.id"
            class="mb-3 note-card"
            rounded="xl"
            elevation="0"
          >
            <v-card-text>
              <template v-if="editingNoteId === note.id">
                <v-textarea
                  v-model="editingNoteText"
                  label="Edit note"
                  auto-grow
                  rows="2"
                  class="mb-2"
                />

                <div class="d-flex ga-2 justify-end">
                  <v-btn size="small" variant="tonal" @click="cancelEditNote">
                    Cancel
                  </v-btn>
                  <v-btn size="small" color="primary" @click="saveEditedNote">
                    Save
                  </v-btn>
                </div>
              </template>

              <template v-else>
                <div class="d-flex align-center justify-space-between mb-1">
                  <div class="text-caption text-grey">
                    {{ note.date }}
                  </div>
                  <div class="d-flex ga-1">
                    <v-btn
                      size="x-small"
                      variant="text"
                      icon
                      @click="startEditNote(note.id)"
                    >
                      <v-icon :icon="mdiPencilOutline" />
                    </v-btn>
                    <v-btn
                      size="x-small"
                      variant="text"
                      icon
                      @click="deleteNote(note.id)"
                    >
                      <v-icon :icon="mdiDeleteOutline" />
                    </v-btn>
                  </div>
                </div>
                <div class="text-body-2 note-html">{{ note.text }}</div>
              </template>
            </v-card-text>
          </v-card>
        </v-container>

        <v-divider class="mx-3" />

        <v-card class="ma-3 pa-3 add-note-card" rounded="xl" elevation="0">
          <v-textarea
            v-model="newNote"
            label="Add a note"
            auto-grow
            rows="2"
            variant="outlined"
            hide-details="auto"
            :disabled="!selectedNoteAppearance"
          />

          <v-btn
            block
            color="primary"
            class="mt-2"
            :disabled="!selectedNoteAppearance"
            @click="saveNote"
          >
            Save Note
          </v-btn>
        </v-card>
      </div>
    </template>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
  import shared from '@/components/shared';
  import { CourtListService } from '@/services';
  import { ApiResponse } from '@/types/ApiResponse';
  import { DivisionEnum } from '@/types/common';
  import {
    CourtListAppearance,
    CourtListCardInfo,
    CourtListSearchResult,
    CourtRoomDetail,
  } from '@/types/courtlist';
  import { DocumentRequestType } from '@/types/shared';
  import {
    formatDateInstanceToDDMMMYYYY,
    parseDDMMMYYYYToDate,
  } from '@/utils/dateUtils';
  import { parseQueryStringToString } from '@/utils/utils';
  import {
    mdiClose,
    mdiChevronLeft,
    mdiChevronRight,
    mdiDeleteOutline,
    mdiNoteTextOutline,
    mdiPencilOutline,
    mdiPlus,
  } from '@mdi/js';
  import { computed, inject, provide, ref, watch } from 'vue';
  interface NoteItem {
    id: string;
    text: string;
    author: string;
    date: string;
  }

  import { useRoute, useRouter } from 'vue-router';
  import CourtListSearch from './CourtListSearch.vue';
  import CourtListTable from './CourtListTable.vue';
  import CourtListTableSearch from './CourtListTableSearch.vue';

  const route = useRoute();
  const router = useRouter();
  const errorCode = ref(0);
  const searchingRequest = ref(false);
  const isLoading = ref(false);
  const selectedDate = ref(
    parseDDMMMYYYYToDate(parseQueryStringToString(route.query.date)) ??
      new Date()
  );
  const searchResultMessage = ref<string>('');
  const appliedDate = ref<Date | null>(null);
  const showDropdown = ref(false);
  const search = ref('');
  const launcherHovered = ref(false);
  const selectedFilesFilter = ref();
  const selectedAMPMFilter = ref<string | null>(null);
  const documentUrls = ref<string[]>([]);
  const showNotesDrawer = ref(false);
  const hoveredAppearanceId = ref<string | null>(null);
  const openPanels = ref<number[]>([]);
  const selectedNoteAppearance = ref<CourtListAppearance | null>(null);
  const newNote = ref('');
  const editingNoteId = ref<string | null>(null);
  const editingNoteText = ref('');
  const notesByAppearanceId = ref<Record<string, NoteItem[]>>({});
  const customChipsByAppearanceId = ref<Record<string, string[]>>({});
  const newChipText = ref('');
  const cardTablePairings = ref<
    {
      card: CourtListCardInfo;
      table: CourtListAppearance[];
    }[]
  >([]);
  const filesFilterMap: {
    [key: string]: (appearance: CourtListAppearance) => boolean;
  } = {
    Complete: (appearance: CourtListAppearance) => !!appearance.isComplete,
    Cancelled: (appearance: CourtListAppearance) =>
      appearance.appearanceStatusCd === 'CNCL',
    'To be called': (appearance: CourtListAppearance) =>
      appearance.appearanceStatusCd === 'SCHD',
  };
  const showDialog = ref(false);
  // We ignore the time portion of the date for comparison as we only care about the day
  const isFuture = computed(
    () =>
      appliedDate.value !== null &&
      appliedDate.value.setHours(0, 0, 0, 0) >= new Date().setHours(0, 0, 0, 0)
  );

  const filterByAMPM = (table: CourtListAppearance[]) =>
    selectedAMPMFilter.value
      ? table.filter((appearance: CourtListAppearance) =>
          appearance.appearanceTm.includes(selectedAMPMFilter.value || '')
        )
      : table;

  const filterByFiles = (table: CourtListAppearance[]) =>
    selectedFilesFilter.value
      ? table.filter(filesFilterMap[selectedFilesFilter.value])
      : table;

  const filteredTablePairings = computed<
    {
      card: CourtListCardInfo;
      tableData: CourtListAppearance[];
    }[]
  >(() => {
    return cardTablePairings.value.map((pairing) => ({
      ...pairing,
      tableData: filterByFiles(filterByAMPM(pairing.table)),
    }));
  });

  const shortHandDate = computed(() =>
    appliedDate.value
      ? appliedDate.value.toLocaleDateString('en-US', {
          weekday: 'long',
          day: '2-digit',
          month: 'long',
          year: 'numeric',
        })
      : ''
  );

  const selectedNoteContext = computed(() => {
    const appearance = selectedNoteAppearance.value;
    if (!appearance) {
      return null;
    }

    const divisionMap: Record<string, string> = {
      R: 'Criminal',
      I: 'Civil',
      F: 'Family',
      S: 'Small Claims',
      Y: 'Youth',
      T: 'Tickets',
    };

    const primaryName =
      appearance.accusedNm || appearance.styleOfCause || 'Selected appearance';

    const fileNumber =
      appearance.courtFileNumber || appearance.aslCourtFileNumber;

    return {
      primaryName,
      fileNumber: fileNumber || '-',
      appearanceNumber: appearance.appearanceSequenceNumber || '-',
      date: appearance.appearanceDt || '-',
      time: appearance.appearanceTm || '-',
      room: appearance.courtRoomCd || '-',
      reason:
        appearance.appearanceReasonDsc || appearance.appearanceReasonCd || '-',
      division:
        divisionMap[appearance.courtDivisionCd] || appearance.courtDivisionCd,
      status:
        appearance.appearanceStatusDsc || appearance.appearanceStatusCd || '-',
    };
  });

  const currentNotes = computed(() => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    if (!appearanceId) {
      return [] as NoteItem[];
    }

    return notesByAppearanceId.value[appearanceId] ?? [];
  });

  const currentChips = computed(() => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    if (!appearanceId) return [];
    return customChipsByAppearanceId.value[appearanceId] ?? [];
  });

  const addChip = () => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    const text = newChipText.value.trim();
    if (!appearanceId || !text) return;
    customChipsByAppearanceId.value[appearanceId] = [
      ...(customChipsByAppearanceId.value[appearanceId] ?? []),
      text,
    ];
    newChipText.value = '';
  };

  const removeChip = (index: number) => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    if (!appearanceId) return;
    customChipsByAppearanceId.value[appearanceId] =
      customChipsByAppearanceId.value[appearanceId].filter(
        (_, i) => i !== index
      );
  };

  const activeHighlightAppearanceId = computed(
    () =>
      hoveredAppearanceId.value ||
      selectedNoteAppearance.value?.appearanceId ||
      null
  );

  const appearancesById = computed(() => {
    const map: Record<string, CourtListAppearance> = {};
    for (const pairing of cardTablePairings.value) {
      for (const appearance of pairing.table) {
        map[appearance.appearanceId] = appearance;
      }
    }
    return map;
  });

  const allNotes = computed(() => {
    return Object.entries(notesByAppearanceId.value).flatMap(
      ([appearanceId, notes]) => {
        const appearance = appearancesById.value[appearanceId];
        const context =
          appearance?.accusedNm ||
          appearance?.styleOfCause ||
          appearance?.courtFileNumber ||
          appearanceId;

        return notes.map((note) => ({
          ...note,
          key: `${appearanceId}-${note.id}`,
          appearanceId,
          context,
          fileNumber:
            appearance?.courtFileNumber ||
            appearance?.aslCourtFileNumber ||
            'No File #',
        }));
      }
    );
  });

  const groupedNotes = computed(() => {
    const groups = new Map<string, typeof allNotes.value>();
    for (const note of allNotes.value) {
      const fileNum = note.fileNumber;
      if (!groups.has(fileNum)) {
        groups.set(fileNum, []);
      }
      groups.get(fileNum)!.push(note);
    }
    return Array.from(groups.entries()).map(([fileNumber, notes]) => ({
      fileNumber,
      notes,
    }));
  });

  const totalNotesCount = computed(() => allNotes.value.length);

  const hasAnyNotes = computed(() => totalNotesCount.value > 0);

  const createNoteId = () =>
    `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 9)}`;

  watch(selectedDate, (newValue) => {
    if (!route.query.date) {
      return;
    }

    router.replace({
      query: {
        ...route.query,
        date: formatDateInstanceToDDMMMYYYY(newValue),
      },
    });
  });

  watch(showNotesDrawer, (isOpen) => {
    if (!isOpen) {
      hoveredAppearanceId.value = null;
      selectedNoteAppearance.value = null;
      editingNoteId.value = null;
      editingNoteText.value = '';
      launcherHovered.value = false;
    }
  });

  const courtListService = inject<CourtListService>('courtListService');
  if (!courtListService) {
    throw new Error('Service(s) is undefined.');
  }

  const determineAMPM = (courtRoomDetails: CourtRoomDetail): string => {
    if (courtRoomDetails.isAM === 'Y' && courtRoomDetails.isPM !== 'Y') {
      return 'AM';
    }
    if (courtRoomDetails.isPM === 'Y' && courtRoomDetails.isAM !== 'Y') {
      return 'PM';
    }
    return 'AM/PM';
  };

  const buildDummyNotes = (
    appearance: CourtListAppearance,
    index: number
  ): NoteItem[] => {
    const notes: NoteItem[] = [];

    if (appearance.scheduleNoteTxt) {
      notes.push({
        id: createNoteId(),
        text: appearance.scheduleNoteTxt,
        author: 'System',
        date: 'Apr 10',
      });
    }

    if (!appearance.scheduleNoteTxt && index % 3 === 0) {
      notes.push({
        id: createNoteId(),
        text: 'Follow up with counsel before the appearance.',
        author: 'Judge Traill',
        date: 'Apr 09',
      });
    }

    if (index % 5 === 0) {
      notes.push({
        id: createNoteId(),
        text: 'Verify submitted documents are complete.',
        author: 'Judge Traill',
        date: 'Apr 08',
      });
    }

    return notes;
  };

  const populateCardTablePairings = (
    resp?: ApiResponse<CourtListSearchResult>
  ) => {
    searchResultMessage.value = '';
    cardTablePairings.value = [];
    notesByAppearanceId.value = {};
    if (!resp) {
      return;
    }

    if (!resp.succeeded) {
      searchResultMessage.value =
        resp.errors?.[0] ??
        'An error occurred while searching. Please try again.';
      return;
    }

    const items = resp.payload?.items;
    if (resp.succeeded && (!items || items.length === 0)) {
      searchResultMessage.value = 'No activities.';
      return;
    }

    const data = resp.payload;
    let appearanceIndex = 0;
    for (const courtList of data.items) {
      const courtRoomDetails = courtList.courtRoomDetails[0];
      if (!courtRoomDetails) {
        return;
      }
      const adjudicatorDetails = courtRoomDetails.adjudicatorDetails[0];
      const card = {} as CourtListCardInfo;
      card.fileCount = courtList.appearances.length;
      card.activity = courtList.activityDsc;
      card.presider = adjudicatorDetails?.adjudicatorNm;
      card.courtListRoom = courtRoomDetails.courtRoomCd;
      card.courtListLocationID = courtList.locationId;
      card.courtListLocation = courtList.locationNm;
      card.amPM = adjudicatorDetails?.amPm || determineAMPM(courtRoomDetails);

      for (const appearance of courtList.appearances) {
        notesByAppearanceId.value[appearance.appearanceId] = buildDummyNotes(
          appearance,
          appearanceIndex
        );
        appearanceIndex += 1;
      }

      cardTablePairings.value.push({ card, table: courtList.appearances });
    }

    // We always want AM pairings to appear before PM pairings
    cardTablePairings.value.sort((a, b) =>
      a.card.amPM?.localeCompare(b.card.amPM)
    );
  };

  const addDay = (days: number) => {
    if (appliedDate.value) {
      appliedDate.value = new Date(
        appliedDate.value.setDate(appliedDate.value.getDate() + days)
      );
      selectedDate.value = appliedDate.value;
    }
  };

  provide('menuClicked', () => {
    showDialog.value = true;
  });

  const openNotesDrawer = (appearance: CourtListAppearance) => {
    selectedNoteAppearance.value = appearance;
    showNotesDrawer.value = true;
    hoveredAppearanceId.value = appearance.appearanceId;
    editingNoteId.value = null;
    editingNoteText.value = '';
    newNote.value = '';

    const appearanceId = appearance.appearanceId;
    if (!(appearanceId in notesByAppearanceId.value)) {
      notesByAppearanceId.value[appearanceId] = [];
    }
  };

  const focusNoteEntry = (appearanceId: string) => {
    const appearance = appearancesById.value[appearanceId];
    if (!appearance) {
      return;
    }

    selectedNoteAppearance.value = appearance;
    hoveredAppearanceId.value = appearanceId;
    editingNoteId.value = null;
    editingNoteText.value = '';
    openPanels.value = [];
  };

  const startEditNote = async (noteId: string) => {
    const note = currentNotes.value.find((n) => n.id === noteId);
    if (!note) {
      return;
    }

    editingNoteId.value = noteId;
    editingNoteText.value = note.text;
  };

  const cancelEditNote = () => {
    editingNoteId.value = null;
    editingNoteText.value = '';
  };

  const saveEditedNote = () => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    if (!appearanceId || !editingNoteId.value) {
      return;
    }

    const updatedText = editingNoteText.value.trim();
    if (!updatedText) {
      return;
    }

    notesByAppearanceId.value[appearanceId] = notesByAppearanceId.value[
      appearanceId
    ].map((note) =>
      note.id === editingNoteId.value
        ? {
            ...note,
            text: updatedText,
            date: new Date().toLocaleString(),
          }
        : note
    );

    cancelEditNote();
  };

  const deleteNote = (noteId: string) => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    if (!appearanceId) {
      return;
    }

    notesByAppearanceId.value[appearanceId] = notesByAppearanceId.value[
      appearanceId
    ].filter((note) => note.id !== noteId);

    if (editingNoteId.value === noteId) {
      cancelEditNote();
    }
  };

  const saveNote = () => {
    const appearanceId = selectedNoteAppearance.value?.appearanceId;
    const noteText = newNote.value.trim();

    if (!appearanceId || !noteText) {
      return;
    }

    notesByAppearanceId.value[appearanceId] = [
      {
        id: createNoteId(),
        text: noteText,
        author: 'You',
        date: new Date().toLocaleString(),
      },
      ...(notesByAppearanceId.value[appearanceId] ?? []),
    ];

    newNote.value = '';
  };

  const onGenerateClick = (reportType: 'Daily' | 'Additions') => {
    documentUrls.value = [];
    // Prepare unique combinations to generate report(s)
    const uniqueMap = new Map<
      string,
      {
        locationId: number;
        locationName: string;
        date: string;
        division: string;
        class: string;
        courtRoom: string;
      }
    >();
    for (const element of cardTablePairings.value) {
      for (const data of element.table) {
        const obj = {
          locationId: element.card.courtListLocationID,
          locationName: element.card.courtListLocation,
          date: data.appearanceDt,
          division: data.courtDivisionCd,
          class: data.courtClassCd,
          courtRoom: data.courtRoomCd,
        };

        const key = `${obj.locationId}|${obj.locationName}|${obj.date}|${obj.division}|${obj.class}|${obj.courtRoom}`;
        uniqueMap.set(key, obj);
      }
    }
    const documents: Array<{
      documentType: DocumentRequestType;
      documentData: Record<string, any>;
      groupKeyTwo: string;
      documentName: any;
      groupKeyOne: string;
    }> = [];
    for (const value of uniqueMap.values()) {
      const documentData: Record<string, any> = {
        courtDivisionCd: value.division,
        courtClass: value.class,
        date: value.date,
        locationId: value.locationId,
        roomCode: value.courtRoom,
      };
      if (value.division === DivisionEnum.R) {
        documentData.reportType = reportType;
        documentData.isCriminal = true;
      } else if (value.division === DivisionEnum.I) {
        documentData.additionsList = reportType === 'Additions' ? 'Y' : 'N';
      }

      documents.push({
        documentType: DocumentRequestType.Report,
        documentData,
        groupKeyTwo: value.courtRoom,
        documentName: reportType,
        groupKeyOne: value.locationName,
      });
    }
    shared.openMergedDocuments(documents);
  };
</script>

<style scoped>
  :deep(.notes-drawer) {
    overflow: hidden;
    border-right: 1px solid rgba(var(--v-border-color), 0.08);
    background: linear-gradient(
      180deg,
      rgba(var(--v-theme-surface), 1) 0%,
      rgba(var(--v-theme-surface-variant), 0.25) 100%
    );
  }

  :deep(.notes-drawer .v-navigation-drawer__content) {
    display: flex;
    flex-direction: column;
    height: 100%;
  }

  .notes-launcher {
    position: fixed;
    left: 20px;
    bottom: 20px;
    z-index: 1005;
  }

  .drawer-prepend {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 0.75rem 0.25rem;
  }

  .drawer-prepend-copy {
    min-width: 0;
  }

  .drawer-body {
    display: flex;
    flex-direction: column;
    min-height: 0;
    height: 100%;
  }

  .notes-panels-wrap {
    margin: 0 12px 12px;
  }

  .drawer-hero {
    box-shadow: 0 10px 28px rgba(var(--v-theme-primary), 0.18);
  }

  .context-card,
  .add-note-card,
  .note-card {
    border: 1px solid rgba(var(--v-border-color), 0.08);
    background: rgba(var(--v-theme-surface), 0.95);
  }

  .note-list-item {
    border: 1px solid rgba(var(--v-border-color), 0.08);
    background: rgba(var(--v-theme-surface), 0.85);
    transition:
      transform 0.15s ease,
      box-shadow 0.15s ease,
      background 0.15s ease;
  }

  .note-list-item:hover {
    transform: translateY(-1px);
    background: rgba(var(--v-theme-primary), 0.06);
    box-shadow: 0 6px 18px rgba(0, 0, 0, 0.06);
  }

  .note-list-item--active {
    background: rgba(var(--v-theme-primary), 0.12) !important;
    border-color: rgba(var(--v-theme-primary), 0.3) !important;
  }

  .note-list-item--active:hover {
    background: rgba(var(--v-theme-primary), 0.18) !important;
  }

  .note-group-header {
    font-size: 0.75rem;
    font-weight: 600;
    color: rgba(var(--v-theme-on-surface), 0.6);
    text-transform: uppercase;
    letter-spacing: 0.5px;
    padding: 0rem 0.5rem 0.5rem;
  }

  .notes-scroll-area {
    flex: 1;
    min-height: 0;
  }

  .note-html {
    white-space: pre-wrap;
  }

  .chip-add-row {
    align-items: center;
  }

  .chip-input {
    width: 100px;
    font-size: 0.75rem;
  }

  :deep(.chip-input .v-field__input) {
    min-height: 0;
    padding-top: 4px;
    padding-bottom: 4px;
    font-size: 0.75rem;
  }
</style>
