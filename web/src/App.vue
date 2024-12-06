<template>
  <div class="app-outer fill-body" id="app">
    <navigation-topbar />
    <router-view />
    <navigation-footer id="footer" />
  </div>
</template>

<script lang="ts">
  import NavigationFooter from '@components/NavigationFooter.vue';
  import NavigationTopbar from '@components/NavigationTopbar.vue';
  import { defineComponent } from 'vue';

  export default defineComponent({
    name: 'App',
    components: {
      NavigationTopbar,
      NavigationFooter,
    },
  });
</script>
