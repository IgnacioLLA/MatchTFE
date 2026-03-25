using Microsoft.AspNetCore.Mvc;
using TFELibrary.Shared;

namespace UserService.Controllers
{
    public interface IUserController
    {
        Task<ActionResult<ProfileDto>> GetCurrentProfile();
    }
}
