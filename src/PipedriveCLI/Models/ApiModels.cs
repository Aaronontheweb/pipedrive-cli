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

    [JsonPropertyName("cc_email")]
    public string? CcEmail { get; set; }

    [JsonPropertyName("is_archived")]
    public bool? IsArchived { get; set; }
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
/// Organization search result item (search endpoint)
/// </summary>
public sealed class OrganizationSearchItem
{
    [JsonPropertyName("result_score")]
    public decimal ResultScore { get; set; }

    [JsonPropertyName("item")]
    public OrganizationSearchResultOrg? Item { get; set; }
}

/// <summary>
/// Organization data from search results (may have different structure than standard Organization)
/// </summary>
public sealed class OrganizationSearchResultOrg
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("visible_to")]
    public int VisibleTo { get; set; }

    [JsonPropertyName("owner")]
    public SearchOwner? Owner { get; set; }

    /// <summary>
    /// Convert search result to standard Organization model
    /// </summary>
    public Organization ToOrganization()
    {
        return new Organization
        {
            Id = Id,
            Name = Name,
            Address = Address,
            OwnerId = Owner != null ? new Owner { Id = Owner.Id } : null
        };
    }
}

/// <summary>
/// Organization search data wrapper (search endpoint)
/// </summary>
public sealed class OrganizationSearchData
{
    [JsonPropertyName("items")]
    public List<OrganizationSearchItem>? Items { get; set; }
}

/// <summary>
/// Person search result item (search endpoint)
/// </summary>
public sealed class PersonSearchItem
{
    [JsonPropertyName("result_score")]
    public decimal ResultScore { get; set; }

    [JsonPropertyName("item")]
    public PersonSearchResultPerson? Item { get; set; }
}

/// <summary>
/// Person data from search results (may have different structure than standard Person)
/// </summary>
public sealed class PersonSearchResultPerson
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("emails")]
    public List<string>? Emails { get; set; }

    [JsonPropertyName("phones")]
    public List<string>? Phones { get; set; }

    [JsonPropertyName("visible_to")]
    public int VisibleTo { get; set; }

    [JsonPropertyName("owner")]
    public SearchOwner? Owner { get; set; }

    [JsonPropertyName("organization")]
    public SearchOrganizationRef? Organization { get; set; }

    /// <summary>
    /// Convert search result to standard Person model
    /// </summary>
    public Person ToPerson()
    {
        var person = new Person
        {
            Id = Id,
            Name = Name,
            OwnerId = Owner != null ? new Owner { Id = Owner.Id } : null,
            OrgId = Organization?.Id
        };

        // Convert simple email strings to Email objects
        if (Emails != null && Emails.Count > 0)
        {
            person.Email = Emails.Select((e, i) => new Email
            {
                Value = e,
                Primary = i == 0
            }).ToList();
        }

        // Convert simple phone strings to Phone objects
        if (Phones != null && Phones.Count > 0)
        {
            person.Phone = Phones.Select((p, i) => new Phone
            {
                Value = p,
                Primary = i == 0
            }).ToList();
        }

        return person;
    }
}

/// <summary>
/// Person search data wrapper (search endpoint)
/// </summary>
public sealed class PersonSearchData
{
    [JsonPropertyName("items")]
    public List<PersonSearchItem>? Items { get; set; }
}

/// <summary>
/// Owner reference in search results (simplified)
/// </summary>
public sealed class SearchOwner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

/// <summary>
/// Organization reference in person search results
/// </summary>
public sealed class SearchOrganizationRef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
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

    [JsonPropertyName("cc_email")]
    public string? CcEmail { get; set; }

    /// <summary>
    /// Custom fields - captured as extension data with hash keys
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CustomFields { get; set; }
}

/// <summary>
/// Pipedrive Deal Participant model - represents a person associated with a deal
/// </summary>
public sealed class DealParticipant
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("person_id")]
    [JsonConverter(typeof(PipedriveReferenceConverter))]
    public int? PersonId { get; set; }

    [JsonPropertyName("deal_id")]
    public int DealId { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("active_flag")]
    public bool ActiveFlag { get; set; }

    /// <summary>
    /// The related person object with full details
    /// </summary>
    [JsonPropertyName("person")]
    public Person? Person { get; set; }
}

/// <summary>
/// Request model for adding a participant to a deal
/// </summary>
public sealed class AddDealParticipantRequest
{
    [JsonPropertyName("person_id")]
    public int PersonId { get; set; }
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

    [JsonPropertyName("cc_email")]
    public string? CcEmail { get; set; }

    /// <summary>
    /// Custom fields - captured as extension data with hash keys
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CustomFields { get; set; }
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

    [JsonPropertyName("cc_email")]
    public string? CcEmail { get; set; }

    /// <summary>
    /// Custom fields - captured as extension data with hash keys
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CustomFields { get; set; }
}

/// <summary>
/// Activity participant - represents a person associated with an activity
/// </summary>
public sealed class ActivityParticipant
{
    [JsonPropertyName("person_id")]
    public int PersonId { get; set; }

    [JsonPropertyName("primary_flag")]
    public bool PrimaryFlag { get; set; }
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

    [JsonPropertyName("lead_id")]
    public string? LeadId { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("participants")]
    public List<ActivityParticipant>? Participants { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("cc_email")]
    public string? CcEmail { get; set; }
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
/// Pipedrive Pipeline model
/// </summary>
public sealed class Pipeline
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("order_nr")]
    public int OrderNr { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("deal_probability")]
    public bool DealProbability { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
}

/// <summary>
/// Pipedrive Stage model
/// </summary>
public sealed class Stage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pipeline_id")]
    public int PipelineId { get; set; }

    [JsonPropertyName("order_nr")]
    public int OrderNr { get; set; }

    [JsonPropertyName("active_flag")]
    public bool ActiveFlag { get; set; }

    [JsonPropertyName("deal_probability")]
    public int? DealProbability { get; set; }

    [JsonPropertyName("rotten_flag")]
    public bool? RottenFlag { get; set; }

    [JsonPropertyName("rotten_days")]
    public int? RottenDays { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }
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
/// Email message party (sender/recipient) information
/// </summary>
public sealed class MailParty
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("linked_person_id")]
    public int? LinkedPersonId { get; set; }

    [JsonPropertyName("linked_person_name")]
    public string? LinkedPersonName { get; set; }

    [JsonPropertyName("linked_organization_id")]
    public int? LinkedOrganizationId { get; set; }

    [JsonPropertyName("mail_message_party_id")]
    public int? MailMessagePartyId { get; set; }

    [JsonPropertyName("latest_sent")]
    public bool? LatestSent { get; set; }

    [JsonPropertyName("message_time")]
    public object? MessageTime { get; set; }
}

/// <summary>
/// Pipedrive Mail Message model for email conversations
/// </summary>
public sealed class MailMessage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("from")]
    public List<MailParty>? From { get; set; }

    [JsonPropertyName("to")]
    public List<MailParty>? To { get; set; }

    [JsonPropertyName("cc")]
    public List<MailParty>? Cc { get; set; }

    [JsonPropertyName("bcc")]
    public List<MailParty>? Bcc { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("body_url")]
    public string? BodyUrl { get; set; }

    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("mail_thread_id")]
    public int? MailThreadId { get; set; }

    [JsonPropertyName("mail_tracking_status")]
    public string? MailTrackingStatus { get; set; }

    [JsonPropertyName("mail_link_tracking_enabled_flag")]
    public int? MailLinkTrackingEnabledFlag { get; set; }

    [JsonPropertyName("read_flag")]
    public int? ReadFlag { get; set; }

    [JsonPropertyName("draft_flag")]
    public int? DraftFlag { get; set; }

    [JsonPropertyName("synced_flag")]
    public int? SyncedFlag { get; set; }

    [JsonPropertyName("deleted_flag")]
    public int? DeletedFlag { get; set; }

    [JsonPropertyName("has_body_flag")]
    public int? HasBodyFlag { get; set; }

    [JsonPropertyName("sent_flag")]
    public int? SentFlag { get; set; }

    [JsonPropertyName("sent_from_pipedrive_flag")]
    public int? SentFromPipedriveFlag { get; set; }

    [JsonPropertyName("smart_bcc_flag")]
    public int? SmartBccFlag { get; set; }

    [JsonPropertyName("message_time")]
    public string? MessageTime { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("has_attachments_flag")]
    public int? HasAttachmentsFlag { get; set; }

    [JsonPropertyName("has_inline_attachments_flag")]
    public int? HasInlineAttachmentsFlag { get; set; }

    [JsonPropertyName("has_real_attachments_flag")]
    public int? HasRealAttachmentsFlag { get; set; }

    [JsonPropertyName("deal_id")]
    public int? DealId { get; set; }

    [JsonPropertyName("lead_id")]
    public string? LeadId { get; set; }
}

/// <summary>
/// Wrapper object returned by deals/{id}/mailMessages and persons/{id}/mailMessages endpoints.
/// These endpoints return mail messages in a nested structure with object type and timestamp.
/// </summary>
public sealed class MailMessageWrapper
{
    [JsonPropertyName("object")]
    public string? ObjectType { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("data")]
    public MailMessage? Data { get; set; }
}

/// <summary>
/// Parties information for a mail thread
/// </summary>
public sealed class MailThreadParties
{
    [JsonPropertyName("to")]
    public List<MailParty>? To { get; set; }

    [JsonPropertyName("from")]
    public List<MailParty>? From { get; set; }
}

/// <summary>
/// Pipedrive Mail Thread model for email thread management
/// </summary>
public sealed class MailThread
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }

    [JsonPropertyName("snippet_draft")]
    public string? SnippetDraft { get; set; }

    [JsonPropertyName("snippet_sent")]
    public string? SnippetSent { get; set; }

    [JsonPropertyName("read_flag")]
    public int? ReadFlag { get; set; }

    [JsonPropertyName("mail_tracking_status")]
    public string? MailTrackingStatus { get; set; }

    [JsonPropertyName("has_attachments_flag")]
    public int? HasAttachmentsFlag { get; set; }

    [JsonPropertyName("has_inline_attachments_flag")]
    public int? HasInlineAttachmentsFlag { get; set; }

    [JsonPropertyName("has_real_attachments_flag")]
    public int? HasRealAttachmentsFlag { get; set; }

    [JsonPropertyName("deleted_flag")]
    public int? DeletedFlag { get; set; }

    [JsonPropertyName("synced_flag")]
    public int? SyncedFlag { get; set; }

    [JsonPropertyName("smart_bcc_flag")]
    public int? SmartBccFlag { get; set; }

    [JsonPropertyName("mail_link_tracking_enabled_flag")]
    public int? MailLinkTrackingEnabledFlag { get; set; }

    [JsonPropertyName("parties")]
    public MailThreadParties? Parties { get; set; }

    [JsonPropertyName("folders")]
    public List<string>? Folders { get; set; }

    [JsonPropertyName("version")]
    public long? Version { get; set; }

    [JsonPropertyName("message_count")]
    public int? MessageCount { get; set; }

    [JsonPropertyName("has_draft_flag")]
    public int? HasDraftFlag { get; set; }

    [JsonPropertyName("has_sent_flag")]
    public int? HasSentFlag { get; set; }

    [JsonPropertyName("archived_flag")]
    public int? ArchivedFlag { get; set; }

    [JsonPropertyName("shared_flag")]
    public int? SharedFlag { get; set; }

    [JsonPropertyName("external_deleted_flag")]
    public int? ExternalDeletedFlag { get; set; }

    [JsonPropertyName("first_message_to_me_flag")]
    public int? FirstMessageToMeFlag { get; set; }

    [JsonPropertyName("all_messages_sent_flag")]
    public int? AllMessagesSentFlag { get; set; }

    [JsonPropertyName("last_message_timestamp")]
    public string? LastMessageTimestamp { get; set; }

    [JsonPropertyName("first_message_timestamp")]
    public string? FirstMessageTimestamp { get; set; }

    [JsonPropertyName("last_message_sent_timestamp")]
    public string? LastMessageSentTimestamp { get; set; }

    [JsonPropertyName("last_message_received_timestamp")]
    public string? LastMessageReceivedTimestamp { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("deal_id")]
    public int? DealId { get; set; }

    [JsonPropertyName("deal_status")]
    public string? DealStatus { get; set; }

    [JsonPropertyName("lead_id")]
    public string? LeadId { get; set; }
}

/// <summary>
/// Pipedrive Email Template model
/// </summary>
public sealed class EmailTemplate
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("add_time")]
    public string? AddTime { get; set; }

    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    [JsonPropertyName("shared_flag")]
    public int? SharedFlag { get; set; }

    [JsonPropertyName("deleted_flag")]
    public int? DeletedFlag { get; set; }

    [JsonPropertyName("visible_flag")]
    public int? VisibleFlag { get; set; }

    [JsonPropertyName("order_nr")]
    public int? OrderNr { get; set; }

    [JsonPropertyName("has_real_attachments_flag")]
    public bool? HasRealAttachmentsFlag { get; set; }
}

/// <summary>
/// Field definition option for select/multi-select fields
/// </summary>
public sealed class FieldOption
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(StringOrIntIdConverter))]
    public string? Id { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

/// <summary>
/// Converter that handles JSON values that can be either a string, integer, or boolean and returns them as string
/// </summary>
public sealed class StringOrIntIdConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value != null)
            writer.WriteStringValue(value);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Base field definition model for custom fields
/// </summary>
public abstract class BaseField
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("field_type")]
    public string? FieldType { get; set; }

    [JsonPropertyName("edit_flag")]
    public bool? EditFlag { get; set; }

    [JsonPropertyName("mandatory_flag")]
    [JsonConverter(typeof(BoolOrObjectConverter))]
    public bool? MandatoryFlag { get; set; }

    [JsonPropertyName("options")]
    public List<FieldOption>? Options { get; set; }
}

/// <summary>
/// Converter that handles JSON values that can be either a boolean or an object (treats objects as true)
/// </summary>
public sealed class BoolOrObjectConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartObject:
                // When mandatory_flag is an object (conditional requirement), treat it as true
                reader.Skip();
                return true;
            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteBooleanValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Deal field definition
/// </summary>
public sealed class DealField : BaseField
{
}

/// <summary>
/// Person field definition
/// </summary>
public sealed class PersonField : BaseField
{
}

/// <summary>
/// Organization field definition
/// </summary>
public sealed class OrganizationField : BaseField
{
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
[JsonSerializable(typeof(PipedriveResponse<OrganizationSearchData>))]
[JsonSerializable(typeof(PipedriveResponse<PersonSearchData>))]
[JsonSerializable(typeof(PipedriveResponse<Deal>))]
[JsonSerializable(typeof(PipedriveResponse<List<Deal>>))]
[JsonSerializable(typeof(PipedriveResponse<DealParticipant>))]
[JsonSerializable(typeof(PipedriveResponse<List<DealParticipant>>))]
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
[JsonSerializable(typeof(OrganizationSearchResultOrg))]
[JsonSerializable(typeof(OrganizationSearchItem))]
[JsonSerializable(typeof(OrganizationSearchData))]
[JsonSerializable(typeof(PersonSearchResultPerson))]
[JsonSerializable(typeof(PersonSearchItem))]
[JsonSerializable(typeof(PersonSearchData))]
[JsonSerializable(typeof(SearchOwner))]
[JsonSerializable(typeof(SearchOrganizationRef))]
[JsonSerializable(typeof(Deal))]
[JsonSerializable(typeof(DealParticipant))]
[JsonSerializable(typeof(AddDealParticipantRequest))]
[JsonSerializable(typeof(List<DealParticipant>))]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(Activity))]
[JsonSerializable(typeof(ActivityParticipant))]
[JsonSerializable(typeof(List<ActivityParticipant>))]
[JsonSerializable(typeof(Note))]
[JsonSerializable(typeof(MergeRequest))]
[JsonSerializable(typeof(List<Lead>))]
[JsonSerializable(typeof(List<LeadSearchItem>))]
[JsonSerializable(typeof(List<OrganizationSearchItem>))]
[JsonSerializable(typeof(List<PersonSearchItem>))]
[JsonSerializable(typeof(List<Deal>))]
[JsonSerializable(typeof(List<Person>))]
[JsonSerializable(typeof(List<Organization>))]
[JsonSerializable(typeof(List<Activity>))]
[JsonSerializable(typeof(List<Note>))]
[JsonSerializable(typeof(Owner))]
[JsonSerializable(typeof(DealField))]
[JsonSerializable(typeof(PersonField))]
[JsonSerializable(typeof(OrganizationField))]
[JsonSerializable(typeof(List<DealField>))]
[JsonSerializable(typeof(List<PersonField>))]
[JsonSerializable(typeof(List<OrganizationField>))]
[JsonSerializable(typeof(PipedriveResponse<List<DealField>>))]
[JsonSerializable(typeof(PipedriveResponse<List<PersonField>>))]
[JsonSerializable(typeof(PipedriveResponse<List<OrganizationField>>))]
[JsonSerializable(typeof(Pipeline))]
[JsonSerializable(typeof(Stage))]
[JsonSerializable(typeof(List<Pipeline>))]
[JsonSerializable(typeof(List<Stage>))]
[JsonSerializable(typeof(PipedriveResponse<Pipeline>))]
[JsonSerializable(typeof(PipedriveResponse<Stage>))]
[JsonSerializable(typeof(PipedriveResponse<List<Pipeline>>))]
[JsonSerializable(typeof(PipedriveResponse<List<Stage>>))]
[JsonSerializable(typeof(EmailTemplate))]
[JsonSerializable(typeof(List<EmailTemplate>))]
[JsonSerializable(typeof(PipedriveResponse<EmailTemplate>))]
[JsonSerializable(typeof(PipedriveResponse<List<EmailTemplate>>))]
[JsonSerializable(typeof(MailParty))]
[JsonSerializable(typeof(List<MailParty>))]
[JsonSerializable(typeof(MailMessage))]
[JsonSerializable(typeof(List<MailMessage>))]
[JsonSerializable(typeof(PipedriveResponse<MailMessage>))]
[JsonSerializable(typeof(PipedriveResponse<List<MailMessage>>))]
[JsonSerializable(typeof(MailMessageWrapper))]
[JsonSerializable(typeof(List<MailMessageWrapper>))]
[JsonSerializable(typeof(PipedriveResponse<List<MailMessageWrapper>>))]
[JsonSerializable(typeof(MailThreadParties))]
[JsonSerializable(typeof(MailThread))]
[JsonSerializable(typeof(List<MailThread>))]
[JsonSerializable(typeof(PipedriveResponse<MailThread>))]
[JsonSerializable(typeof(PipedriveResponse<List<MailThread>>))]
internal partial class ApiJsonContext : JsonSerializerContext
{
}
