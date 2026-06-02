using System;
using System.Collections.Generic;
using System.Text;

namespace TFELibrary.Shared
{
    public record ProfileResponse(
        ErrorRecord Error,
        ProfileDto? Profile = null
    );

    public record ProfileCreationRequest(
        string UserId,
        ProfileDto Profile
    );

    public record ProfileCreationResponse(
        ErrorRecord Error,
        string? UserId = null
    );

    public record ProfileUpdateRequest(
        ProfileDto Profile
    );

    public record ProfileUpdateResponse(
        ErrorRecord Error,
        ProfileDto? UpdatedProfile = null
    );

    public record ProfileLogoutRequest(

    );

    public record ProfileByTfeInterestRequest(
        int TfeId
    );

    public record ProfileByTfeInterestResponse(
        ErrorRecord Error,
        List<TfeCandidateDto> Interested
    );

    public record ChangeRoleRequest(
        RoleType NewRole
    );

    public record GetAllProfilesRequest();

    public record GetAllProfilesResponse(
        ErrorRecord Error,
        List<ProfileDto> Profiles
    );

    public record RoleUpdateResponse(ErrorRecord Error);

}
