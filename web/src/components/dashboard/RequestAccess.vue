<template>
  <v-container class="home" fluid>
    <v-row justify="center">
      <v-col cols="12" md="8" lg="4">
        <v-card
          class="text-center access-body"
          elevation="2"
          style="padding: 1rem"
        >
          <img class="logo" :src="logo" alt="logo" width="63" />
          <v-row justify="center">
            <h2 class="mt-4">Request Access To JASPER:</h2>
          </v-row>
          <v-row justify="center">
            <v-col cols="12" sm="10" md="8" xs="10">
              <v-text-field
                id="email"
                :model-value="selectedEmail"
                label="Email"
                :disabled="true"
                persistent-hint
                hint="This email is associated to the account you logged in with, it will be used to create your account."
                type="email"
                class="email-input"
              >
              </v-text-field>
            </v-col>
          </v-row>
          <v-row justify="center">
            <v-btn
              color="primary"
              class="py-2 px-5"
              @click="requestAccess"
              :disabled="!canSubmit"
            >
              {{ isSubmitted ? 'Submitted' : 'Submit Your Request' }}
            </v-btn>
          </v-row>
          <v-row justify="center" class="my-4">
            <v-col cols="12" xs="12">
              <v-alert
                v-if="isSubmitted"
                border="start"
                type="success"
                class="shared-badge px-5 mx-auto"
              >
                Your request has been submitted!<br />
                We will get back to you soon.
              </v-alert>
              <v-alert
                v-if="isUserDisabled"
                border="start"
                type="warning"
                class="shared-badge px-5 mx-auto"
              >
                Your user has been disabled.<br />
                Please contact the JASPER admin if you require access.
              </v-alert>
              <v-alert
                v-if="isUserInvalid"
                border="start"
                type="warning"
                class="shared-badge px-5 mx-auto"
              >
                Warning, you do not have valid access to JASPER.<br />
                Please contact the JASPER admin to correct your account.
              </v-alert>
            </v-col>
          </v-row>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import logo from '@/assets/jasper-logo.svg?url';
  import { SESSION_STATES } from '@/constants/sessionStates';
  import { UserService } from '@/services/UserService';
  import { useCommonStore } from '@/stores';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { getApiErrorResponse } from '@/types/ApiResponse';
  import { useSessionInitializer } from '@/utils/utils';
  import { computed, inject, ref } from 'vue';
  import { useRouter } from 'vue-router';

  const commonStore = useCommonStore();
  const snackBarStore = useSnackbarStore();
  const selectedEmail = computed(() => commonStore.userInfo?.email ?? '');
  const isUserDisabled = computed(
    () =>
      commonStore.userInfo?.isActive === false &&
      commonStore.userInfo.roles.length > 0
  );
  const isUserInvalid = computed(() => {
    const user = commonStore.userInfo;
    return (
      !user ||
      (user.isActive && user.roles.length === 0) ||
      (!isUserDisabled.value && !selectedEmail.value.trim())
    );
  });
  const isSubmitted = ref(false);
  const isSubmitting = ref(false);
  const canSubmit = computed(
    () =>
      commonStore.sessionState === SESSION_STATES.ACCESS_REQUIRED &&
      !isSubmitted.value &&
      !isSubmitting.value &&
      !isUserInvalid.value &&
      !isUserDisabled.value
  );
  const userService = inject<UserService>('userService');
  const initializeSessionSettings = useSessionInitializer();
  const router = useRouter();

  function errorMessage(data: unknown): string {
    if (typeof data === 'string') return data.trim();
    if (Array.isArray(data)) {
      return data
        .filter((item) => typeof item === 'string')
        .join(' ')
        .trim();
    }
    if (data && typeof data === 'object') {
      const { error, errors, message, detail, title } = data as Record<
        string,
        unknown
      >;
      const validationErrors =
        errors && typeof errors === 'object' && !Array.isArray(errors)
          ? Object.values(errors).flat()
          : errors;
      return (
        [error, validationErrors, message, detail, title]
          .filter((value) => typeof value === 'string' || Array.isArray(value))
          .map(errorMessage)
          .find(Boolean) ?? ''
      );
    }
    return '';
  }

  const requestAccess = async (): Promise<void> => {
    if (!canSubmit.value) return;
    isSubmitting.value = true;

    try {
      const response = await userService?.requestAccess();
      if (!response || typeof response !== 'object') {
        throw new Error('Access request response is unavailable.');
      }
      if (
        'alreadyAuthorized' in response &&
        response.alreadyAuthorized === true
      ) {
        await initializeSessionSettings();
        if (commonStore.sessionState === SESSION_STATES.AUTHORIZED) {
          await router.replace({ name: 'DashboardView' });
        } else if (
          commonStore.sessionState === SESSION_STATES.ACCESS_REQUIRED
        ) {
          throw new Error('JASPER access could not be confirmed.');
        }
      } else if (
        'id' in response &&
        response.id === commonStore.userInfo?.userId
      ) {
        isSubmitted.value = true;
      } else {
        throw new Error('Access request response is incomplete.');
      }
    } catch (error) {
      snackBarStore.showSnackbar(
        errorMessage(getApiErrorResponse(error)?.data) ||
          'Please try again or contact your JASPER administrator.',
        'error',
        'Unable to submit access request'
      );
    } finally {
      isSubmitting.value = false;
    }
  };
</script>

<style scoped>
  .card {
    border: white;
  }

  .btn.disabled {
    cursor: default;
  }

  .shared-badge {
    font-size: 16px;
    font-weight: unset;
    white-space: wrap;
  }

  .access-body {
    background-color: var(--bg-blue-100);
  }

  .email-input :deep() input {
    text-align: center;
  }
</style>
