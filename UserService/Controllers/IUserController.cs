using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace UserService.Controllers
{
    public interface IUserController
    {
        Task<ActionResult<ProfileResponse>> GetCurrentProfile();
        Task<ActionResult> CreateInitialProfile(ProfileCreationRequest newProfile);
        Task<IActionResult> UpdateProfile(ProfileUpdateRequest request);
    }
}
