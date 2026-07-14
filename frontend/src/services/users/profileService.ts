import { apiClient } from "@/lib/common/apiClient";
import { routes } from "@/lib/common/routes";
import { MyProfile, UpdateMyProfileRequest } from "@/types/users/user.types";

export const profileService = {
  /**
   * Get the current user's profile
   */
  getMyProfile: (): Promise<MyProfile> => {
    return apiClient.get<MyProfile>(routes.users.me);
  },

  /**
   * Update the current user's profile
   */
  updateMyProfile: (request: UpdateMyProfileRequest): Promise<void> => {
    return apiClient.put<void>(routes.users.me, request);
  },
};
