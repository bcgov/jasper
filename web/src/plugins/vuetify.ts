import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import { VBtn } from 'vuetify/components';
import { VDateInput } from 'vuetify/components/VDateInput';
import { VFileUpload } from 'vuetify/components/VFileUpload';
import * as directives from 'vuetify/directives';
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg';
import 'vuetify/styles';

// https://vuetifyjs.com/en/introduction/why-vuetify/#feature-guides

export default createVuetify({
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: {
      mdi,
    },
  },
  components: {
    ...components,
    VDateInput,
    VFileUpload,
  },
  directives,
  aliases: {
    VBtnSecondary: VBtn,
    VBtnTertiary: VBtn,
  },
  defaults: {
    VContainer: {
      fluid: true,
    },
    VBtn: {
      rounded: 'pill',
      variant: 'flat',
      class: 'text-none',
    },
    VBtnSecondary: {
      rounded: 'pill',
      variant: 'outlined',
      class: 'text-none',
    },
    VBtnTertiary: {
      rounded: 'pill',
      class: 'text-none text-white',
      baseColor: '#183a4a',
    },
    VSelect: {
      bgColor: '#dae5f5',
      rounded: true,
      variant: 'solo',
      clearable: true,
      density: 'comfortable',
    },
    VTextField: {
      rounded: true,
      dense: true,
      variant: 'outlined',
    },
    VDateInput: {
      density: 'comfortable',
      bgColor: '#dae5f5',
      variant: 'solo',
      label: 'Date',
      clearable: false,
    },
    VDataTable: {
      hover: true,
      showSelect: true,
      returnObject: true,
      hideDefaultFooter: true,
    },
    VDataTableVirtual: {
      hover: true,
    },
    VTab: {
      color: '#000',
      rounded: false,
    },
    VChip: {},
  },
});
