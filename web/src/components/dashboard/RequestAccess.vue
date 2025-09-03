<template>
  <v-container class="home" fluid>
    <v-row justify="center">
      <v-col cols="12" md="8" lg="4">
        <v-card
          class="text-center access-body"
          elevation="2"
          style="padding: 1rem"
        >
          <img
            class="logo"
            src="../../assets/jasper-logo.svg"
            alt="logo"
            width="63"
          />
          <v-row justify="center">
            <h2 class="mt-4">Request Access To JASPER:</h2>
          </v-row>
          <v-row justify="center">
            <v-col cols="12" sm="10" md="8" xs="10">
              <v-text-field
                id="email"
                v-model="selectedEmail"
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
              :disabled="isSubmitted || isUserInvalid || isUserDisabled"
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
  import { UserService } from '@/services/UserService';
  import { useCommonStore } from '@/stores';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { isPositiveInteger } from '@/utils/utils';
  import axios, { AxiosError } from 'axios';
  import _ from 'underscore';
  import { inject, onMounted, ref } from 'vue';
  import { CustomAPIError } from '@/types/ApiResponse';

  const commonStore = useCommonStore();
  const snackBarStore = useSnackbarStore();
  const selectedEmail = ref<string>(commonStore.userInfo?.email ?? '');
  const isUserDisabled = ref(false);
  const isUserInvalid = ref(false);
  const isSubmitted = ref(false);
  const userService = inject<UserService>('userService');

  const requestAccess = async (): Promise<void> => {
    if (!_.isEmpty(selectedEmail.value)) {
      try {
        const accessRequest = await userService?.requestAccess(
          selectedEmail.value
        );
        if (accessRequest?.email === selectedEmail.value) {
          isSubmitted.value = true;
        } else {
          throw Error();
        }
      } catch (error) {
        if (
          error instanceof CustomAPIError &&
          axios.isAxiosError(
            (error as CustomAPIError<AxiosError>).originalError
          )
        ) {
          snackBarStore.showSnackbar(
            `${(error as CustomAPIError<AxiosError<[]>>)?.originalError?.response?.data?.join(' ') ?? ''}`,
            '#b84157',
            'Unable to submit access request'
          );
        }
      }
    }
  };

  const checkForUser = async (): Promise<void> => {
    const user = await userService?.getMyUser();

    if (!user?.isActive && isPositiveInteger(user?.roles?.length)) {
      isUserDisabled.value = true;
    } else if (user?.isPendingRegistration) {
      isSubmitted.value = true;
    } else if (
      (user?.isActive && !isPositiveInteger(user?.roles?.length)) ||
      !user
    ) {
      isUserInvalid.value = true;
    }
  };

  onMounted(async () => {
    await checkForUser();
  });
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
