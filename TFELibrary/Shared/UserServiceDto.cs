namespace TFELibrary.Shared;

public record ProfileResponse(
    OperationResult Error,
    ProfileDto? Profile = null
);

public record ProfileCreationRequest(
    string UserId,
    ProfileDto Profile
);

public record ProfileCreationResponse(
    OperationResult Error,
    string? UserId = null
);

public record ProfileUpdateRequest(
    ProfileDto Profile
);

public record ProfileUpdateResponse(
    OperationResult Error,
    ProfileDto? UpdatedProfile = null
);

public record ProfileLogoutRequest();

public record ProfileByTfeInterestRequest(
    int TfeId
);

public record ProfileByTfeInterestResponse(
    OperationResult Error,
    List<TfeCandidateDto> Interested
);

public record ChangeRoleRequest(
    RoleType NewRole
);

public record GetAllProfilesRequest();

public record GetAllProfilesResponse(
    OperationResult Error,
    List<ProfileDto> Profiles
);

public record RoleUpdateResponse(OperationResult Error);

public record SuspensionUpdateRequest(bool IsSuspended);

public record SuspensionUpdateResponse(OperationResult Error);

public record DeleteProfileResponse(OperationResult Error);
