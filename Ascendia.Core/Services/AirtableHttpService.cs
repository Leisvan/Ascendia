using AirtableApiClient;
using Ascendia.Core.Extensions;
using Ascendia.Core.Records;
using Ascendia.Core.Strings;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ascendia.Core.Services;

public class AirtableHttpService(string? airtableToken, string? baseId)
{
    private const string GuildSettingsTableName = "GuildSettings";
    private const string MembersTableName = "Members";
    private const int RecordsChunkSize = 10;
    private readonly string? _airtableToken = airtableToken;
    private readonly string? _baseId = baseId;

    public string AirtableUrl => IsConfigured ? $"https://airtable.com/{_baseId}" : "about:blank";

    public bool IsConfigured
        => !string.IsNullOrEmpty(_airtableToken) && !string.IsNullOrEmpty(_baseId);

    public async Task<bool> CreateOrEditMemberAsync(MemberRecord? record)
    {
        if (record == null || !IsConfigured)
        {
            return false;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);
        var airtableRecord = record.ToAirtableRecord();
        if (string.IsNullOrEmpty(airtableRecord.Id))
        {
            return await CreateRecordsAsync(MembersTableName, [airtableRecord]) > 0;
        }
        return await UpdateRecordsAsync(MembersTableName, [airtableRecord]) > 0;
    }

    public async Task<IEnumerable<DiscordBotGuildSettingsRecord>?> GetDiscordBotGuildsSettingsAsync()
    {
        var records = await GetRecordsAsync(GuildSettingsTableName);
        if (records == null)
        {
            return null;
        }
        return records.Select(r => r.ToDiscordBotSettings());
    }

    public async Task<IEnumerable<MemberRecord>?> GetMemberRecordsAsync()
    {
        if (!IsConfigured)
        {
            return default;
        }
        var records = await GetRecordsAsync(MembersTableName);
        if (records == null)
        {
            return null;
        }
        return records.Select(r => r.ToMemberRecord());
    }

    public async Task<bool> RemoveMemberAsync(string id)
    {
        if (!IsConfigured)
        {
            return default;
        }
        return await RemoveRecordAsync(MembersTableName, id);
    }

    public async Task<bool> UpdateMultipleMembersAsync(MemberRecord[]? records)
    {
        if (!IsConfigured || records == null || records.Length == 0)
        {
            return false;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);

        var airtableRecords = records.Select(r => r.ToAirtableRecord()).ToArray();

        return await UpdateRecordsAsync(MembersTableName, airtableRecords) > 0;
    }

    private static void LogResponse(AirtableApiResponse response, [CallerMemberName] string caller = "")
    {
        var logMessage = new StringBuilder();
        if (!response.Success)
        {
            logMessage.AppendLine(string.Format(Messages.HttpAirtableRequestErrorFormat, caller));
            if (response.AirtableApiError != null)
            {
                var error = response.AirtableApiError;
                logMessage.AppendLine($"[{error.ErrorCode}] - {error.ErrorName} - {error.ErrorMessage}");
            }
        }
        else
        {
            logMessage.AppendLine(string.Format(Messages.HttpAirtableRequestSuccessFormat, caller));
            if (response is AirtableCreateUpdateReplaceMultipleRecordsResponse multiple)
            {
                logMessage.AppendLine(string.Format(Messages.HttpAirtableRecordsUpdatedFormat, multiple.UpdatedRecords?.Length ?? 0));
                logMessage.AppendLine(string.Format(Messages.HttpAirtableRecordsCreatedFormat, multiple.CreatedRecords?.Length ?? 0));
            }
            else if (response is AirtableListRecordsResponse list)
            {
                logMessage.AppendLine(string.Format(Messages.HttpAirtableRecordsListFormat, list.Records?.Count() ?? 0));
            }
        }
        var log = logMessage.ToString();

        if (!string.IsNullOrEmpty(log))
        {
            if (response.Success)
            {
                CoreTelemetry.WriteLine(log);
            }
            else
            {
                CoreTelemetry.WriteErrorLine(log);
            }
        }
    }

    private async Task<int> CreateRecordsAsync(string tableName, AirtableRecord[] airTableRecords)
    {
        if (!IsConfigured || airTableRecords == null)
        {
            return 0;
        }
        if (airTableRecords.Length == 0)
        {
            return 0;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);
        var chunks = airTableRecords.Chunk(RecordsChunkSize);
        int updatedCount = 0;
        foreach (var item in chunks)
        {
            var results = await airtableBase.CreateMultipleRecords(tableName, item);
            if (results.Success)
            {
                updatedCount += results.Records.Length;
            }
            LogResponse(results);
        }
        return updatedCount;
    }

    private async Task<IEnumerable<AirtableRecord>> GetRecordsAsync(string table)
    {
        if (!IsConfigured)
        {
            return [];
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);
        string? offset = null;
        string? errorMessage = null;
        var records = new List<AirtableRecord>();
        do
        {
            var response = await airtableBase.ListRecords(table, offset);
            if (response.Success)
            {
                records.AddRange(response.Records);
            }
            else if (response.AirtableApiError is not null)
            {
                errorMessage = response.AirtableApiError.ErrorMessage;
                if (response.AirtableApiError is AirtableInvalidRequestException)
                {
                    errorMessage += "\nDetailed error message: ";
                    errorMessage += response.AirtableApiError.DetailedErrorMessage;
                }
                break;
            }
            else
            {
                errorMessage = "Unknown error";
                break;
            }

            LogResponse(response);

            offset = response.Offset;
        } while (offset != null);
        return records;
    }

    private async Task<AirtableRecord?> GetSingleRecordAsync(string table, string id)
    {
        if (!IsConfigured)
        {
            return default;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);
        string? errorMessage = null;

        var response = await airtableBase.RetrieveRecord(table, id);
        if (response.Success)
        {
            return response.Record;
        }
        else if (response.AirtableApiError is not null)
        {
            errorMessage = response.AirtableApiError.ErrorMessage;
            if (response.AirtableApiError is AirtableInvalidRequestException)
            {
                errorMessage += "\nDetailed error message: ";
                errorMessage += response.AirtableApiError.DetailedErrorMessage;
            }
        }
        else
        {
            errorMessage = "Unknown error";
        }
        return null;
    }

    private async Task<bool> RemoveRecordAsync(string tableName, string id)
    {
        if (!IsConfigured)
        {
            return false;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);
        var response = await airtableBase.DeleteRecord(tableName, id);
        LogResponse(response);
        return response.Success;
    }

    private async Task<int> UpdateRecordsAsync(string tableName, AirtableRecord[] airTableRecords)
    {
        if (!IsConfigured || airTableRecords == null)
        {
            return 0;
        }
        if (airTableRecords.Length == 0)
        {
            return 0;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);
        var chunks = airTableRecords.Chunk(RecordsChunkSize);
        int updatedCount = 0;
        foreach (var item in chunks)
        {
            try
            {
                var results = await airtableBase.UpdateMultipleRecords(tableName, item);

                if (results.Success)
                {
                    updatedCount += results.Records.Length;
                }

                LogResponse(results);
            }
            catch (Exception)
            {
                return updatedCount;
            }
        }
        return updatedCount;
    }
}