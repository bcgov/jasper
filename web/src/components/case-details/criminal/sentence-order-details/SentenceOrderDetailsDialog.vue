<template>
  <v-dialog v-model="show">
    <v-card class="pa-3">
      <v-card-title
        ><div class="d-flex justify-space-between align-center w-100">
          <span>{{ title }}</span>
          <v-btn icon @click="closeClick">
            <v-icon :icon="mdiClose" size="32" />
          </v-btn>
        </div>
      </v-card-title>
      <v-card-text class="pl-3 py-0">
        <NameFilter
          class="accused mb-3"
          v-model="selectedAccused"
          :people="participants"
        />
        <p v-if="subtitle">
          <b>{{ subtitle }}</b>
        </p>
        <ul class="pl-3">
          <li v-for="comment in comments">
            <span>{{ comment }}</span>
          </li>
        </ul>
      </v-card-text>
      <v-card-actions class="justify-start">
        <v-btn-secondary text="Close" @click="closeClick" />
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
<script setup lang="ts">
  import NameFilter from '@/components/shared/Form/NameFilter.vue';
  import { criminalParticipantType } from '@/types/criminal/jsonTypes';
  import { formatFromFullname } from '@/utils/utils';
  import { mdiClose } from '@mdi/js';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    title: string;
    subtitle: string;
    targetProperty: string;
    participants: criminalParticipantType[];
  }>();

  const show = defineModel<boolean>({ type: Boolean, required: true });
  const selectedAccused = ref<string>();

  const comments = computed(() =>
    props.participants.filter(filterByAccused).reduce<string[]>((acc, p) => {
      const details = p.count.flatMap((c) =>
        Array.from(
          new Set(
            c.sentence
              .filter((s) => s[props.targetProperty])
              .map((s) => s[props.targetProperty])
          )
        )
      );
      return [...acc, ...details];
    }, [])
  );

  const filterByAccused = (item: criminalParticipantType) =>
    !selectedAccused.value ||
    (item.fullName &&
      formatFromFullname(item.fullName) === selectedAccused.value);

  const closeClick = () => {
    selectedAccused.value = '';
    show.value = false;
  };
</script>
<style scoped>
  .v-dialog {
    max-width: 850px;
  }

  .v-dialog .v-card {
    border-radius: 1rem;
  }
</style>
