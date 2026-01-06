<template>
  <v-banner :style="{ backgroundColor: bgColor }">
    <v-row class="my-1">
      <v-col cols="9">
        <h3 :style="{ color: color }">
          {{ title }}
          <slot name="left" />
        </h3>
      </v-col>
      <v-col>
        <slot name="right" />
      </v-col>
    </v-row>
  </v-banner>
</template>

<script setup lang="ts">
  defineProps({
    title: {
      type: String,
      default: '',
    },
    color: {
      type: String,
      required: true,
    },
    bgColor: {
      type: String,
      required: true,
    },
  });
</script>
