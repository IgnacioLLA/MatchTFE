using System;
using System.Collections.Generic;
using System.Text;

namespace TFELibrary.Shared
{
    public record SkillResponse(string Tag, int Level);

    public record ProfileResponse(
        string RoleType,
        string FirstName,
        string LastName,
        string Email,
        string Bio,
        List<string> Interests,
        string? AcademicYear,
        List<SkillResponse>? Skills,
        string? Department,
        string? OfficeLocation
    );
}
