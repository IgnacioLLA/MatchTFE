using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using TFELibrary.Data;
using TFELibrary.Shared;
using UserService.Repositories;

namespace UserService.Service;

public class UserService : IUserService
{
    private readonly IUserProfileRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    private static readonly EmailAddressAttribute _emailValidator = new();

    public UserService(IUserProfileRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProfileResponse> GetProfileByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new ProfileResponse(new OperationResult(false, "User ID is required."));

        var entity = await _userRepository.GetByUserIdAsync(userId);

        if (entity == null)
            return new ProfileResponse(new OperationResult(false, "User not found.", "UserNotFound"));

        return new ProfileResponse(new OperationResult(true, string.Empty), GetProfileDto(entity));
    }

    public async Task<ProfileCreationResponse> CreateProfileAsync(ProfileCreationRequest request)
    {
        if (request == null)
            return new ProfileCreationResponse(new OperationResult(false, "Request payload cannot be null."));

        if (request.Profile == null)
            return new ProfileCreationResponse(new OperationResult(false, "Profile data is required."));

        var prof = request.Profile;

        if (string.IsNullOrWhiteSpace(prof.FirstName) || string.IsNullOrWhiteSpace(prof.LastName))
            return new ProfileCreationResponse(new OperationResult(false, "First name and last name are required."));

        if (string.IsNullOrWhiteSpace(prof.Email))
            return new ProfileCreationResponse(new OperationResult(false, "Email is required."));

        if (!_emailValidator.IsValid(prof.Email))
            return new ProfileCreationResponse(new OperationResult(false, "Invalid email format.", "InvalidEmail"));

        if (string.IsNullOrWhiteSpace(request.UserId))
            return new ProfileCreationResponse(new OperationResult(false, "User ID is required."));

        var newProfile = new UserProfile
        {
            UserId = request.UserId,
            FirstName = prof.FirstName,
            LastName = prof.LastName,
            Email = prof.Email
        };

        try
        {
            await _userRepository.CreateProfileAsync(newProfile);
        }
        catch (DbUpdateException ex) when (TryGetUniqueConstraintName(ex, out var constraintName))
        {
            if (IsEmailConstraint(constraintName))
            {
                return new ProfileCreationResponse(
                    new OperationResult(false, "A profile already exists with this email.", "DuplicateEmail")
                );
            }

            return new ProfileCreationResponse(
                new OperationResult(false, "A profile already exists for this user.", "DuplicateUserProfile")
            );
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while creating profile for user {UserId}.", request.UserId);
            return new ProfileCreationResponse(new OperationResult(false, "Could not create profile due to a database error.", "DatabaseError"));
        }

        return new ProfileCreationResponse(new OperationResult(true, "Profile created successfully."), newProfile.UserId);
    }

    public async Task<ProfileUpdateResponse> UpdateProfileAsync(string userId, ProfileUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new ProfileUpdateResponse(new OperationResult(false, "User ID is required."));

        if (request?.Profile == null)
            return new ProfileUpdateResponse(new OperationResult(false, "Profile data cannot be empty."));

        var entity = new UserProfile
        {
            UserId = userId,
            FirstName = request.Profile.FirstName,
            LastName = request.Profile.LastName,
            Bio = request.Profile.Bio,
            Role = request.Profile.Role,
            AcademicYear = request.Profile.AcademicYear,
            Department = request.Profile.Department,
            OfficeLocation = request.Profile.OfficeLocation,
            NotificationFrequency = request.Profile.NotificationFrequency
        };

        try
        {
            var isSaved = await _userRepository.UpdateUserProfileAsync(entity, request.Profile.Interests, request.Profile.Skills);

            if (isSaved)
                return new ProfileUpdateResponse(new OperationResult(true, "Profile updated successfully."), request.Profile);

            return new ProfileUpdateResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating profile for user {UserId}.", userId);
            return new ProfileUpdateResponse(new OperationResult(false, "Could not update profile due to an unexpected error.", "DatabaseError"));
        }
    }

    public async Task<ProfileByTfeInterestResponse> GetProfileByTfeInterestAsync(ProfileByTfeInterestRequest request)
    {
        if (request == null || request.TfeId <= 0)
            return new ProfileByTfeInterestResponse(new OperationResult(false, "TfeId is required."), new List<TfeCandidateDto>());

        var entities = await _userRepository.GetInterestedUsersByTfeIdInUserServiceAsync(request.TfeId);

        if (entities == null || !entities.Any())
            return new ProfileByTfeInterestResponse(new OperationResult(true, string.Empty), new List<TfeCandidateDto>());

        var dtos = entities.Select(user =>
        {
            var proposal = user.TfeProposals.FirstOrDefault(tp => tp.TfeId == request.TfeId);

            if (proposal == null)
            {
                _logger.LogWarning("User {UserId} returned for TFE {TfeId} but has no matching proposal.", user.UserId, request.TfeId);
                return null;
            }

            return new TfeCandidateDto
            {
                Profile = GetProfileDto(user),
                Status = proposal.Status
            };
        })
        .Where(dto => dto != null)
        .ToList();

        return new ProfileByTfeInterestResponse(new OperationResult(true, string.Empty), dtos!);
    }

    public async Task<RoleUpdateResponse> UpdateUserRoleAsync(string userId, RoleType newRole)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new RoleUpdateResponse(new OperationResult(false, "User ID is required."));

        try
        {
            var isUpdated = await _userRepository.UpdateUserRoleAsync(userId, newRole);

            if (isUpdated)
                return new RoleUpdateResponse(new OperationResult(true, "User role updated successfully."));

            return new RoleUpdateResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating role for user {UserId}.", userId);
            return new RoleUpdateResponse(new OperationResult(false, "Could not update role due to an unexpected error.", "DatabaseError"));
        }
    }

    public async Task<SuspensionUpdateResponse> UpdateUserSuspensionAsync(string userId, bool isSuspended)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new SuspensionUpdateResponse(new OperationResult(false, "User ID is required."));

        try
        {
            var isUpdated = await _userRepository.UpdateUserSuspensionAsync(userId, isSuspended);

            if (isUpdated)
                return new SuspensionUpdateResponse(new OperationResult(true, "User suspension status updated successfully."));

            return new SuspensionUpdateResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating suspension for user {UserId}.", userId);
            return new SuspensionUpdateResponse(new OperationResult(false, "Could not update suspension status due to an unexpected error.", "DatabaseError"));
        }
    }

    public async Task<GetAllProfilesResponse> GetAllProfilesAsync(GetAllProfilesRequest request)
    {
        try
        {
            var entities = await _userRepository.GetAllProfilesAsync();
            var dtos = entities?.Select(GetProfileDto).ToList() ?? new List<ProfileDto>();
            return new GetAllProfilesResponse(new OperationResult(true, string.Empty), dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving all profiles.");
            return new GetAllProfilesResponse(new OperationResult(false, "Could not retrieve profiles.", "DatabaseError"), new List<ProfileDto>());
        }
    }

    public async Task<PendingNotificationsResponse> GetUsersForNotificationAsync()
    {
        try
        {
            var entities = await _userRepository.GetUsersForNotificationAsync();
            var dtos = entities.Select(u => new UserNotificationDto
            {
                UserId = u.UserId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                NotificationFrequency = u.NotificationFrequency,
                LastNotificationSentAt = u.LastNotificationSentAt
            }).ToList();

            return new PendingNotificationsResponse
            {
                Error = new OperationResult(true, string.Empty),
                Users = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving users for notification.");
            return new PendingNotificationsResponse
            {
                Error = new OperationResult(false, "Could not retrieve users for notification.", "DatabaseError")
            };
        }
    }

    public async Task MarkNotificationSentAsync(List<string> userIds)
    {
        try
        {
            await _userRepository.MarkNotificationSentAsync(userIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while marking notifications as sent.");
        }
    }

    public async Task<DeleteProfileResponse> DeleteProfileAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new DeleteProfileResponse(new OperationResult(false, "User ID is required."));

        try
        {
            var isDeleted = await _userRepository.DeleteProfileAsync(userId);

            if (isDeleted)
                return new DeleteProfileResponse(new OperationResult(true, "User profile deleted successfully."));

            return new DeleteProfileResponse(new OperationResult(false, "User not found.", "UserNotFound"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting profile for user {UserId}.", userId);
            return new DeleteProfileResponse(new OperationResult(false, "Could not delete profile due to an unexpected error.", "DatabaseError"));
        }
    }

    // --------------------------------------------------

    private ProfileDto GetProfileDto(UserProfile profile)
    {
        return new ProfileDto
        {
            Id = profile.UserId,
            FirstName = profile.FirstName ?? string.Empty,
            LastName = profile.LastName ?? string.Empty,
            Email = profile.Email ?? string.Empty,
            Bio = profile.Bio ?? string.Empty,
            IsSuspended = profile.IsSuspended,
            Interests = profile.UserInterests?.Select(ui => ui.Tag.Name).ToList() ?? new List<string>(),
            Role = profile.Role,
            AcademicYear = profile.AcademicYear ?? string.Empty,
            Skills = profile.StudentSkills?.Select(s => new SkillDto
            {
                Tag = s.Tag.Name,
                Level = s.Level
            }).ToList() ?? new List<SkillDto>(),
            Department = profile.Department ?? string.Empty,
            OfficeLocation = profile.OfficeLocation ?? string.Empty,
            NotificationFrequency = profile.NotificationFrequency
        };
    }

    private static bool TryGetUniqueConstraintName(DbUpdateException exception, out string? constraintName)
    {
        constraintName = null;

        if (exception.InnerException is not PostgresException postgresException ||
            postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        constraintName = postgresException.ConstraintName;
        return true;
    }

    private static bool IsEmailConstraint(string? constraintName)
    {
        return constraintName?.Contains("Email", StringComparison.OrdinalIgnoreCase) == true;
    }
}
