using System;
using System.Collections.Generic;
using System.Text;

namespace TFELibrary.Shared
{
    public record ProfileResponse(
        ProfileDto Profile
    );

    public record ProfileCreationRequest(
        string UserId,
        ProfileDto Profile
    );

    public record ProfileUpdateRequest(
        ProfileDto Profile
    );

    public record ProfileUpdateResponse(
        bool Success,
        string Message,
        ProfileDto? UpdatedProfile = null
    );

    public record ProfileByTfeInterestRequest(
        int TfeId
    );

    public record ProfileByTfeInterestResponse(
        List<ProfileDto> Interested
    );
}
