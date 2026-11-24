import { defineStore } from 'pinia'
import { computed, shallowRef } from 'vue'

import { aiApi, type AiProfiles } from '@/api/ai'
import type { ApiResult } from '@/api/types/commandResult'

export const useAiProfilesStore = defineStore('aiProfiles', () => {
  const profiles = shallowRef<AiProfiles | null>(null)
  let loading: Promise<ApiResult<AiProfiles>> | null = null

  const selectable = computed(
    () => profiles.value?.chatProfiles.filter((profile) => profile.selectable) ?? [],
  )
  const defaultProfile = computed(() => profiles.value?.defaultChatProfile ?? null)

  function load(): Promise<ApiResult<AiProfiles>> {
    loading ??= aiApi.profiles().then((result) => {
      if (result.success && result.data) {
        profiles.value = result.data
      } else {
        loading = null
      }

      return result
    })

    return loading
  }

  return { profiles, selectable, defaultProfile, load }
})
