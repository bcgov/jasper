<template>
  <v-skeleton-loader
    class="my-3"
    v-if="isBinderLoading"
    type="table"
    :loading="isBinderLoading"
  ></v-skeleton-loader>
  <div
    class="mb-5"
    data-testid="jb-container"
    v-if="!isBinderLoading && binderDocuments.length !== 0"
  >
    <v-card class="my-3" color="var(--bg-gray-500)" elevation="0">
      <v-card-text>
        <v-row align="center" no-gutters>
          <v-col class="text-h5" cols="6">Judicial binder</v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-alert :class="['ml-3', courtClassCdStyle]" border="start">
      <template #prepend>
        <v-icon :icon="mdiNotebookOutline" />
      </template>
      <template #text>
        To remove documents from your judicial binder, click the ellipsis icon
        on the document you want to remove, then select “Remove from binder”. To
        reorder documents click and drag the<v-icon :icon="mdiDragVertical" />
        icon.
      </template>
    </v-alert>
    <div class="d-flex justify-end my-3">
      <v-btn-secondary class="mr-3" text="View Binder"></v-btn-secondary>
      <ConfirmButton
        buttonText="Delete Judicial Binder"
        infoText="Are you sure you want to delete your Judicial Binder? This will not delete any documents."
        confirmText="Yes, delete"
        :confirmAction="deleteBinder"
      />
    </div>
    <div class="jb-table overflow-y-auto">
      <v-table :headers="headers" :items="binderDocuments">
        <template v-slot:default>
          <thead>
            <tr>
              <th
                id="table-header"
                v-for="header in headers"
                :key="header.value"
              >
                {{ header.title }}
              </th>
            </tr>
          </thead>
          <draggable
            v-model="draggableItems"
            item-key="civilDocumentId"
            tag="tbody"
            handle=".handle"
            :component-data="{
              tag: 'ul',
              type: 'transition-group',
              name: !drag ? 'flip-list' : null,
            }"
            v-bind="dragOptions"
            @change="dropped"
            @start="drag = true"
            @end="drag = false"
          >
            <template #item="{ element }">
              <tr>
                <!-- Handle column -->
                <td>
                  <v-icon
                    style="cursor: move"
                    class="handle"
                    :icon="mdiDragVertical"
                  />
                </td>
                <!-- SequenceNumber column -->
                <td>
                  {{ element.fileSeqNo }}
                </td>
                <td>
                  <!-- Type Description column -->
                  <a
                    v-if="element.imageId"
                    href="javascript:void(0)"
                    @click="props.openIndividualDocument(element)"
                  >
                    {{ element.documentTypeDescription }}
                  </a>
                  <span v-else>
                    {{ element.documentTypeDescription }}
                  </span>
                </td>
                <td>
                  <!-- Activity column -->
                  <v-chip-group>
                    <div
                      v-for="info in element.documentSupport"
                      :key="info.actCd"
                    >
                      <v-chip rounded="lg">{{ info.actCd }}</v-chip>
                    </div>
                  </v-chip-group>
                </td>
                <!-- Date Filed column -->
                <td>
                  {{ formatDateToDDMMMYYYY(element.filedDt) }}
                </td>
                <td>
                  <!-- Filed By column -->
                  <span v-for="(role, index) in element.filedBy" :key="index">
                    <span v-if="role.roleTypeCode">
                      <v-skeleton-loader
                        class="bg-transparent"
                        type="text"
                        :loading="props.rolesLoading"
                      >
                        {{
                          props.roles
                            ? getLookupShortDescription(
                                role.roleTypeCode,
                                props.roles
                              )
                            : ''
                        }}
                      </v-skeleton-loader>
                    </span>
                  </span>
                </td>
                <td>
                  <!-- Issues column -->
                  <LabelWithTooltip
                    v-if="element.issue?.length > 0"
                    :values="element.issue.map((issue) => issue.issueTypeDesc)"
                    :location="Anchor.Top"
                  />
                </td>
                <td>
                  <!-- Actions column -->
                  <EllipsesMenu :menuItems="removeFromBinder(element)" />
                </td>
              </tr>
            </template>
          </draggable>
        </template>
      </v-table>
    </div>
  </div>
</template>

<script setup lang="ts">
  import draggable from 'vuedraggable';
  import ConfirmButton from '@/components/shared/ConfirmButton.vue';
  import EllipsesMenu from '@/components/shared/EllipsesMenu.vue';
  import { civilDocumentType } from '@/types/civil/jsonTypes';
  import { Anchor, LookupCode } from '@/types/common';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { getLookupShortDescription } from '@/utils/utils';
  import { mdiDragVertical, mdiNotebookOutline } from '@mdi/js';
  import { watch, ref } from 'vue';

  const props = defineProps<{
    isBinderLoading: boolean;
    courtClassCdStyle: string;
    rolesLoading: boolean;
    roles: LookupCode[];
    baseHeaders: DataTableHeader[];
    binderDocuments: civilDocumentType[];
    selectedItems: civilDocumentType[];
    removeDocumentFromBinder: (documentId: string) => void;
    openIndividualDocument: (data: civilDocumentType) => void;
    deleteBinder: () => void;
  }>();

  const emit = defineEmits<
    (
      e: 'update:reordered',
      value: {
        oldIndex: number;
        newIndex: number;
        document: civilDocumentType;
      }
    ) => void
  >();

  const draggableItems = ref<civilDocumentType[]>([...props.binderDocuments]);
  const drag = ref(false);
  const dragOptions = {
    animation: 200,
    group: 'description',
    disabled: false,
    ghostClass: 'ghost',
  };

  watch(
    () => props.binderDocuments,
    (newVal) => {
      draggableItems.value = [...newVal];
    },
    { immediate: true, deep: true }
  );

  const headers = [
    {
      title: '',
      key: 'drag',
      align: 'start' as const,
      sortable: false,
    },
    ...props.baseHeaders,
  ];

  const removeFromBinder = (item: civilDocumentType) => 
    [
      {
        title: 'Remove from binder',
        action: () => props.removeDocumentFromBinder(item.civilDocumentId),
        enable: true,
      },
    ];

  const dropped = (event) =>
    emit('update:reordered', {
      oldIndex: event.moved.oldIndex,
      newIndex: event.moved.newIndex,
      document: event.moved.element,
    });
</script>
<style>
  .jb-table {
    max-height: 400px;
  }
</style>
