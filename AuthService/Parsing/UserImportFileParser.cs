using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using TFELibrary.Shared;

namespace AuthService.Parsing;

public sealed record UserImportRecord(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    RoleType Role);

public sealed record UserImportFileParseResult(
    bool IsValid,
    string? ErrorMessage,
    IReadOnlyList<UserImportRecord> Records);

/// <summary>
/// Parses a UTF-8 CSV (or TXT) file into a list of <see cref="UserImportRecord"/>.
///
/// Expected format (no header row — every line is a user record):
///   email,firstName,lastName,password,role
///
/// Rules:
///   - Separator: comma (,). Quoted fields containing commas are supported.
///   - BOM is detected and handled automatically.
///   - Empty lines are ignored.
///   - Allowed roles: Student, Teacher (case-insensitive). Admin is not permitted.
///   - Password must be at least 6 characters.
///   - Parsing stops at the first invalid row and returns an error.
/// </summary>
public static class UserImportFileParser
{
    private const int MinPasswordLength = 6;

    public static UserImportFileParseResult Parse(byte[] fileContent)
    {
        if (fileContent is null || fileContent.Length == 0)
            return Failure("The file is empty.");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = args =>
                throw new CsvHelperException(args.Context, "missing required field.")
        };

        using var stream = new MemoryStream(fileContent);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<UserImportRecordMap>();

        var records = new List<UserImportRecord>();

        while (csv.Read())
        {
            int row = csv.Context.Parser?.Row ?? 0;
            UserImportRecord record;

            try
            {
                record = csv.GetRecord<UserImportRecord>();
            }
            catch (TypeConverterException ex)
            {
                return Failure($"Row {ex.Context?.Parser?.Row}: {ex.Message}");
            }
            catch (CsvHelperException ex)
            {
                return Failure($"Row {ex.Context?.Parser?.Row}: {ex.Message}");
            }

            if (string.IsNullOrEmpty(record.Email) || !IsValidEmail(record.Email))
                return Failure($"Row {row}: '{record.Email}' is not a valid email address.");

            if (string.IsNullOrEmpty(record.FirstName))
                return Failure($"Row {row}: 'firstName' cannot be empty.");

            if (string.IsNullOrEmpty(record.LastName))
                return Failure($"Row {row}: 'lastName' cannot be empty.");

            if (record.Password.Length < MinPasswordLength)
                return Failure($"Row {row}: password must be at least {MinPasswordLength} characters.");

            records.Add(record);
        }

        if (records.Count == 0)
            return Failure("The file contains no user records.");

        return new UserImportFileParseResult(true, null, records);
    }

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email).Address == email; }
        catch { return false; }
    }

    private static UserImportFileParseResult Failure(string message)
        => new(false, message, Array.Empty<UserImportRecord>());
}

internal sealed class UserImportRecordMap : ClassMap<UserImportRecord>
{
    public UserImportRecordMap()
    {
        Parameter("Email").Index(0);
        Parameter("FirstName").Index(1);
        Parameter("LastName").Index(2);
        Parameter("Password").Index(3);
        Parameter("Role").Index(4).TypeConverter<ImportRoleTypeConverter>();
    }
}

internal sealed class ImportRoleTypeConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return text?.ToLower() switch
        {
            "student" => RoleType.Student,
            "teacher" => RoleType.Teacher,
            _ => throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"'{text}' is not a valid role. Allowed values: Student, Teacher.")
        };
    }
}
