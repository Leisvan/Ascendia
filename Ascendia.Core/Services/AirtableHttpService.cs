using AirtableApiClient;
using Ascendia.Core.Extensions;
using Ascendia.Core.Records;

namespace Ascendia.Core.Services;

public class AirtableHttpService(string? airtableToken, string? baseId)
{
    private const string GuildSettingsTableName = "GuildSettings";
    private const string MembersTableName = "Members";
    private const int RecordsChunkSize = 10;
    private const string TeamsTableName = "Teams";
    private readonly string? _airtableToken = airtableToken;
    private readonly string? _baseId = baseId;

    private bool IsConfigured
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

    public async Task<bool> UpdateMultipleMemberAsync(MemberRecord[]? records)
    {
        if (!IsConfigured || records == null || records.Length == 0)
        {
            return false;
        }
        using var airtableBase = new AirtableBase(_airtableToken, _baseId);

        var airtableRecords = records.Select(r => r.ToAirtableRecord()).ToArray();

        return await UpdateRecordsAsync(MembersTableName, airtableRecords) > 0;
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
            }
            catch (Exception)
            {
                return updatedCount;
            }
        }
        return updatedCount;
    }
}