using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipedriveCLI.Models;

/// <summary>
/// Standard Pipedrive API response wrapper
/// </summary>
/// <typeparam name="T">The data type in the response</typeparam>
public sealed class PipedriveResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("additional_data")]
    public AdditionalData? AdditionalData { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_info")]
    public string? ErrorInfo { get; set; }
}

/// <summary>
/// Additional metadata in API responses (pagination, etc.)
/// </summary>
public sealed class AdditionalData
{
    [JsonPropertyName("pagination")]
    public Pagination? Pagination { get; set; }
}

/// <summary>
/// Pagination information
/// </summary>
public sealed class Pagination
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("more_items_in_collection")]
    public bool MoreItemsInCollection { get; set; }

    [JsonPropertyName("next_start")]
    public int? NextStart { get; set; }
}

/// <summary>
/// Custom JSON converter for Pipedrive reference fields that can be either int or object with "value" property
/// Used for org_id and person_id fields which return as objects in GET responses but accept ints in POST/PUT
/// </summary>
public sealed class PipedriveReferenceConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int? result = null;
            int depth = 1; // We're already at StartObject

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        depth++;
                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        depth--;
                        break;
                    case JsonTokenType.PropertyName when depth == 1:
                        var propertyName = reader.GetString();
                        if (propertyName == "value")
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.Number)
                            {
                                result = reader.GetInt32();
                            }
                        }
                        break;
                }

                // Exit the loop after processing the final EndObject, but before reading the next token
                if (depth == 0)
                {
                    break;
                }
            }

            return result;
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to int?");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Pipedrive Lead model
/// </summary>
public sealed class Lead
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("person_id")]
    public int? PersonId { get; set; }

    [JsonPropertyName("organization_id")]
    public int? OrganizationId { get; set; }

    [JsonPropertyName("owner_id")]
    public int? OwnerId { get; set; }

    [JsonPropertyName("value")]
    public LeadValue? Value { get; set; }

    [JsonPropertyName("expected_close_date")]
    public string? ExpectedCloseDate { get; set; }

    [JsonPropertyName("was_seen")]
    public bool WasSeen { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
}

/// <summary>
/// Lead model for search results (v2 API search endpoint)
/// Different from standard Lead - value/currency are separate fields
/// </summary>
public sealed class LeadSearchResultLead
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("person_id")]
    public int? PersonId { get; set; }

    [JsonPropertyName("organization_id")]
    public int? OrganizationId { get; set; }

    [JsonPropertyName("owner_id")]
    public int? OwnerId { get; set; }

    [JsonPropertyName("value")]
    public decimal? ValueAmount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("expected_close_date")]
    public string? ExpectedCloseDate { get; set; }

    [JsonPropertyName("was_seen")]
    public bool WasSeen { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// Convert search result lead to standard Lead model
    /// </summary>
    public Lead ToLead()
    {
        var lead = new Lead
        {
            Id = Id,
            Title = Title,
            PersonId = PersonId,
            OrganizationId = OrganizationId,
            OwnerId = OwnerId,
            ExpectedCloseDate = ExpectedCloseDate,
            WasSeen = WasSeen,
            AddTime = AddTime,
            UpdateTime = UpdateTime
        };

        if (ValueAmount.HasValue && !string.IsNullOrWhiteSpace(Currency))
        {
            lead.Value = new LeadValue
            {
                Amount = ValueAmount.Value,
                Currency = Currency
            };
        }

        return lead;
    }
}

/// <summary>
/// Lead search result item (v2 API search endpoint)
/// </summary>
public sealed class LeadSearchItem
{
    [JsonPropertyName("result_score")]
    public decimal ResultScore { get; set; }

    [JsonPropertyName("item")]
    public LeadSearchResultLead? Item { get; set; }
}

/// <summary>
/// Lead search data wrapper (v2 API search endpoint)
/// </summary>
public sealed class LeadSearchData
{
    [JsonPropertyName("items")]
    public List<LeadSearchItem>? Items { get; set; }
}

/// <summary>
/// Lead value with amount and currency
/// </summary>
public sealed class LeadValue
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

/// <summary>
/// Pipedrive Deal model
/// </summary>
public sealed class Deal
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("person_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? PersonId { get; set; }

    [JsonPropertyName("org_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? OrgId { get; set; }

    [JsonPropertyName("stage_id")]
    public int? StageId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("probability")]
    public decimal? Probability { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("won_time")]
    public string? WonTime { get; set; }

    [JsonPropertyName("lost_time")]
    public string? LostTime { get; set; }

    [JsonPropertyName("expected_close_date")]
    public string? ExpectedCloseDate { get; set; }
}

/// <summary>
/// Pipedrive Person (contact) model
/// </summary>
public sealed class Person
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public List<Email>? Email { get; set; }

    [JsonPropertyName("phone")]
    public List<Phone>? Phone { get; set; }

    [JsonPropertyName("org_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? OrgId { get; set; }

    [JsonPropertyName("owner_id")]
    public Owner? OwnerId { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
}

/// <summary>
/// Email address entry
/// </summary>
public sealed class Email
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

/// <summary>
/// Phone number entry
/// </summary>
public sealed class Phone
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

/// <summary>
/// Pipedrive Owner/User reference
/// </summary>
public sealed class Owner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

/// <summary>
/// Pipedrive Organization model
/// </summary>
public sealed class Organization
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("people_count")]
    public int PeopleCount { get; set; }

    [JsonPropertyName("owner_id")]
    public Owner? OwnerId { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
}

/// <summary>
/// Pipedrive Activity model
/// </summary>
public sealed class Activity
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("due_time")]
    public string? DueTime { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("deal_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? DealId { get; set; }

    [JsonPropertyName("person_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? PersonId { get; set; }

    [JsonPropertyName("org_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? OrgId { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
}

/// <summary>
/// Merge request model for merging two entities
/// </summary>
public sealed class MergeRequest
{
    [JsonPropertyName("merge_with_id")]
    public int MergeWithId { get; set; }
}

/// <summary>
/// Pipedrive Note model
/// </summary>
public sealed class Note
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("active_flag")]
    public bool ActiveFlag { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("deal_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? DealId { get; set; }

    [JsonPropertyName("person_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? PersonId { get; set; }

    [JsonPropertyName("org_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? OrgId { get; set; }

    [JsonPropertyName("lead_id")]
    public string? LeadId { get; set; }

    [JsonPropertyName("project_id")]
    public int? ProjectId { get; set; }

    [JsonPropertyName("pinned_to_deal_flag")]
    public bool? PinnedToDealFlag { get; set; }

    [JsonPropertyName("pinned_to_person_flag")]
    public bool? PinnedToPersonFlag { get; set; }

    [JsonPropertyName("pinned_to_organization_flag")]
    public bool? PinnedToOrganizationFlag { get; set; }

    [JsonPropertyName("pinned_to_lead_flag")]
    public bool? PinnedToLeadFlag { get; set; }

    [JsonPropertyName("pinned_to_project_flag")]
    public bool? PinnedToProjectFlag { get; set; }
}

/// <summary>
/// JSON source generator context for API models (Native AOT compatibility)
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(PipedriveResponse<object>))]
[JsonSerializable(typeof(PipedriveResponse<Lead>))]
[JsonSerializable(typeof(PipedriveResponse<List<Lead>>))]
[JsonSerializable(typeof(PipedriveResponse<LeadSearchData>))]
[JsonSerializable(typeof(PipedriveResponse<Deal>))]
[JsonSerializable(typeof(PipedriveResponse<List<Deal>>))]
[JsonSerializable(typeof(PipedriveResponse<Person>))]
[JsonSerializable(typeof(PipedriveResponse<List<Person>>))]
[JsonSerializable(typeof(PipedriveResponse<Organization>))]
[JsonSerializable(typeof(PipedriveResponse<List<Organization>>))]
[JsonSerializable(typeof(PipedriveResponse<Activity>))]
[JsonSerializable(typeof(PipedriveResponse<List<Activity>>))]
[JsonSerializable(typeof(PipedriveResponse<Note>))]
[JsonSerializable(typeof(PipedriveResponse<List<Note>>))]
[JsonSerializable(typeof(Lead))]
[JsonSerializable(typeof(LeadSearchResultLead))]
[JsonSerializable(typeof(LeadSearchItem))]
[JsonSerializable(typeof(LeadSearchData))]
[JsonSerializable(typeof(Deal))]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(Activity))]
[JsonSerializable(typeof(Note))]
[JsonSerializable(typeof(MergeRequest))]
[JsonSerializable(typeof(List<Lead>))]
[JsonSerializable(typeof(List<LeadSearchItem>))]
[JsonSerializable(typeof(List<Deal>))]
[JsonSerializable(typeof(List<Person>))]
[JsonSerializable(typeof(List<Organization>))]
[JsonSerializable(typeof(List<Activity>))]
[JsonSerializable(typeof(List<Note>))]
[JsonSerializable(typeof(Owner))]
internal partial class ApiJsonContext : JsonSerializerContext
{
}
