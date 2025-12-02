using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Mail Messages and Mail Threads API serialization
/// </summary>
public class MailMessagesApiClientTests
{
    #region MailParty Tests

    /// <summary>
    /// Test that MailParty deserialization handles all fields correctly
    /// </summary>
    [Fact]
    public void MailParty_Deserialization_AllFields()
    {
        // Arrange
        var json = """
        {
            "id": 123,
            "email_address": "john@example.com",
            "name": "John Doe",
            "linked_person_id": 456,
            "linked_person_name": "John D.",
            "linked_organization_id": 789,
            "mail_message_party_id": 101112,
            "latest_sent": true,
            "message_time": 1699999999
        }
        """;

        // Act
        var party = JsonSerializer.Deserialize(json, ApiJsonContext.Default.MailParty);

        // Assert
        Assert.NotNull(party);
        Assert.Equal(123, party.Id);
        Assert.Equal("john@example.com", party.EmailAddress);
        Assert.Equal("John Doe", party.Name);
        Assert.Equal(456, party.LinkedPersonId);
        Assert.Equal("John D.", party.LinkedPersonName);
        Assert.Equal(789, party.LinkedOrganizationId);
        Assert.Equal(101112, party.MailMessagePartyId);
        Assert.True(party.LatestSent);
    }

    /// <summary>
    /// Test that MailParty handles minimal fields
    /// </summary>
    [Fact]
    public void MailParty_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "id": 100,
            "email_address": "minimal@test.com",
            "name": null
        }
        """;

        // Act
        var party = JsonSerializer.Deserialize(json, ApiJsonContext.Default.MailParty);

        // Assert
        Assert.NotNull(party);
        Assert.Equal(100, party.Id);
        Assert.Equal("minimal@test.com", party.EmailAddress);
        Assert.Null(party.Name);
        Assert.Null(party.LinkedPersonId);
    }

    #endregion

    #region MailMessage Tests

    /// <summary>
    /// Test that MailMessage deserialization handles full API response
    /// </summary>
    [Fact]
    public void MailMessage_Deserialization_FullApiResponse()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 12345,
                "from": [
                    {
                        "id": 1,
                        "email_address": "sender@company.com",
                        "name": "Sales Rep",
                        "linked_person_id": 100,
                        "linked_person_name": "Sales Representative"
                    }
                ],
                "to": [
                    {
                        "id": 2,
                        "email_address": "prospect@customer.com",
                        "name": "John Customer",
                        "linked_person_id": 200,
                        "linked_person_name": "John Customer"
                    }
                ],
                "cc": [
                    {
                        "id": 3,
                        "email_address": "manager@company.com",
                        "name": "Manager"
                    }
                ],
                "bcc": [],
                "subject": "Follow-up on our discussion",
                "snippet": "Hi John, I wanted to follow up on our conversation yesterday...",
                "body": "<html><body><p>Hi John, I wanted to follow up on our conversation yesterday...</p></body></html>",
                "body_url": "https://api.pipedrive.com/v1/mailbox/mailMessages/12345/body",
                "account_id": "abc123",
                "user_id": 555,
                "mail_thread_id": 999,
                "mail_tracking_status": "opened",
                "mail_link_tracking_enabled_flag": 1,
                "read_flag": 1,
                "draft_flag": 0,
                "synced_flag": 1,
                "deleted_flag": 0,
                "has_body_flag": 1,
                "sent_flag": 1,
                "sent_from_pipedrive_flag": 1,
                "smart_bcc_flag": 0,
                "message_time": "2024-11-15T10:30:00Z",
                "add_time": "2024-11-15T10:30:05Z",
                "update_time": "2024-11-15T11:00:00Z",
                "has_attachments_flag": 0,
                "has_inline_attachments_flag": 0,
                "has_real_attachments_flag": 0,
                "deal_id": 835,
                "lead_id": null
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailMessage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var message = response.Data;
        Assert.Equal(12345, message.Id);
        Assert.Equal("Follow-up on our discussion", message.Subject);
        Assert.Equal("Hi John, I wanted to follow up on our conversation yesterday...", message.Snippet);
        Assert.Contains("Hi John", message.Body);
        Assert.Equal("abc123", message.AccountId);
        Assert.Equal(555, message.UserId);
        Assert.Equal(999, message.MailThreadId);
        Assert.Equal("opened", message.MailTrackingStatus);
        Assert.Equal(1, message.MailLinkTrackingEnabledFlag);
        Assert.Equal(1, message.ReadFlag);
        Assert.Equal(0, message.DraftFlag);
        Assert.Equal(1, message.SyncedFlag);
        Assert.Equal(0, message.DeletedFlag);
        Assert.Equal(1, message.HasBodyFlag);
        Assert.Equal(1, message.SentFlag);
        Assert.Equal(1, message.SentFromPipedriveFlag);
        Assert.Equal(0, message.SmartBccFlag);
        Assert.Equal("2024-11-15T10:30:00Z", message.MessageTime);
        Assert.Equal(0, message.HasAttachmentsFlag);
        Assert.Equal(835, message.DealId);
        Assert.Null(message.LeadId);

        // Check from/to parties
        Assert.NotNull(message.From);
        Assert.Single(message.From);
        Assert.Equal("sender@company.com", message.From[0].EmailAddress);
        Assert.Equal("Sales Rep", message.From[0].Name);

        Assert.NotNull(message.To);
        Assert.Single(message.To);
        Assert.Equal("prospect@customer.com", message.To[0].EmailAddress);

        Assert.NotNull(message.Cc);
        Assert.Single(message.Cc);
        Assert.Equal("manager@company.com", message.Cc[0].EmailAddress);
    }

    /// <summary>
    /// Test deserializing a list of mail messages (for deal/person endpoints)
    /// </summary>
    [Fact]
    public void MailMessageList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 100,
                    "from": [{"id": 1, "email_address": "sales@company.com", "name": "Sales"}],
                    "to": [{"id": 2, "email_address": "customer@client.com", "name": "Customer"}],
                    "subject": "First email",
                    "snippet": "First message content...",
                    "message_time": "2024-11-01T10:00:00Z",
                    "read_flag": 1,
                    "sent_flag": 1,
                    "has_attachments_flag": 0
                },
                {
                    "id": 101,
                    "from": [{"id": 2, "email_address": "customer@client.com", "name": "Customer"}],
                    "to": [{"id": 1, "email_address": "sales@company.com", "name": "Sales"}],
                    "subject": "Re: First email",
                    "snippet": "Reply content...",
                    "message_time": "2024-11-02T14:00:00Z",
                    "read_flag": 1,
                    "sent_flag": 0,
                    "has_attachments_flag": 1
                }
            ],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 50,
                    "more_items_in_collection": true,
                    "next_start": 50
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailMessage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First message (sent)
        var msg1 = response.Data[0];
        Assert.Equal(100, msg1.Id);
        Assert.Equal("First email", msg1.Subject);
        Assert.Equal(1, msg1.SentFlag);
        Assert.Equal(0, msg1.HasAttachmentsFlag);

        // Second message (received)
        var msg2 = response.Data[1];
        Assert.Equal(101, msg2.Id);
        Assert.Equal("Re: First email", msg2.Subject);
        Assert.Equal(0, msg2.SentFlag);
        Assert.Equal(1, msg2.HasAttachmentsFlag);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(50, response.AdditionalData.Pagination.Limit);
        Assert.True(response.AdditionalData.Pagination.MoreItemsInCollection);
        Assert.Equal(50, response.AdditionalData.Pagination.NextStart);
    }

    /// <summary>
    /// Test handling empty mail message list
    /// </summary>
    [Fact]
    public void MailMessageList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 50,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailMessage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test handling API error response
    /// </summary>
    [Fact]
    public void MailMessage_Deserialization_ErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Mail message not found",
            "error_info": "No mail message with ID 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailMessage);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Mail message not found", response.Error);
        Assert.Equal("No mail message with ID 999999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    #endregion

    #region MailThread Tests

    /// <summary>
    /// Test that MailThread deserialization handles full API response
    /// </summary>
    [Fact]
    public void MailThread_Deserialization_FullApiResponse()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 55555,
                "account_id": "account-xyz",
                "user_id": 100,
                "subject": "Product Demo Discussion",
                "snippet": "Thanks for the demo, I have a few questions...",
                "snippet_draft": null,
                "snippet_sent": "Looking forward to our call next week.",
                "read_flag": 1,
                "mail_tracking_status": "clicked",
                "has_attachments_flag": 1,
                "has_inline_attachments_flag": 0,
                "has_real_attachments_flag": 1,
                "deleted_flag": 0,
                "synced_flag": 1,
                "smart_bcc_flag": 1,
                "mail_link_tracking_enabled_flag": 1,
                "parties": {
                    "from": [
                        {
                            "id": 1,
                            "email_address": "sales@company.com",
                            "name": "Sales Team",
                            "linked_person_id": 500
                        }
                    ],
                    "to": [
                        {
                            "id": 2,
                            "email_address": "buyer@enterprise.com",
                            "name": "Enterprise Buyer",
                            "linked_person_id": 600
                        }
                    ]
                },
                "folders": ["inbox", "important"],
                "version": 12345678901,
                "message_count": 8,
                "has_draft_flag": 0,
                "has_sent_flag": 1,
                "archived_flag": 0,
                "shared_flag": 1,
                "external_deleted_flag": 0,
                "first_message_to_me_flag": 0,
                "all_messages_sent_flag": 0,
                "last_message_timestamp": "2024-11-20T16:45:00Z",
                "first_message_timestamp": "2024-11-10T09:00:00Z",
                "last_message_sent_timestamp": "2024-11-19T14:30:00Z",
                "last_message_received_timestamp": "2024-11-20T16:45:00Z",
                "add_time": "2024-11-10T09:00:00Z",
                "update_time": "2024-11-20T16:45:00Z",
                "deal_id": 835,
                "deal_status": "open",
                "lead_id": null
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var thread = response.Data;
        Assert.Equal(55555, thread.Id);
        Assert.Equal("account-xyz", thread.AccountId);
        Assert.Equal(100, thread.UserId);
        Assert.Equal("Product Demo Discussion", thread.Subject);
        Assert.Equal("Thanks for the demo, I have a few questions...", thread.Snippet);
        Assert.Null(thread.SnippetDraft);
        Assert.Equal("Looking forward to our call next week.", thread.SnippetSent);
        Assert.Equal(1, thread.ReadFlag);
        Assert.Equal("clicked", thread.MailTrackingStatus);
        Assert.Equal(1, thread.HasAttachmentsFlag);
        Assert.Equal(1, thread.HasRealAttachmentsFlag);
        Assert.Equal(0, thread.DeletedFlag);
        Assert.Equal(1, thread.SyncedFlag);
        Assert.Equal(1, thread.SmartBccFlag);
        Assert.Equal(8, thread.MessageCount);
        Assert.Equal(0, thread.HasDraftFlag);
        Assert.Equal(1, thread.HasSentFlag);
        Assert.Equal(0, thread.ArchivedFlag);
        Assert.Equal(1, thread.SharedFlag);
        Assert.Equal("2024-11-20T16:45:00Z", thread.LastMessageTimestamp);
        Assert.Equal("2024-11-10T09:00:00Z", thread.FirstMessageTimestamp);
        Assert.Equal(835, thread.DealId);
        Assert.Equal("open", thread.DealStatus);
        Assert.Null(thread.LeadId);

        // Check parties
        Assert.NotNull(thread.Parties);
        Assert.NotNull(thread.Parties.From);
        Assert.Single(thread.Parties.From);
        Assert.Equal("sales@company.com", thread.Parties.From[0].EmailAddress);
        Assert.NotNull(thread.Parties.To);
        Assert.Single(thread.Parties.To);
        Assert.Equal("buyer@enterprise.com", thread.Parties.To[0].EmailAddress);

        // Check folders
        Assert.NotNull(thread.Folders);
        Assert.Equal(2, thread.Folders.Count);
        Assert.Contains("inbox", thread.Folders);
        Assert.Contains("important", thread.Folders);
    }

    /// <summary>
    /// Test deserializing a list of mail threads
    /// </summary>
    [Fact]
    public void MailThreadList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 1001,
                    "subject": "Initial Inquiry",
                    "snippet": "I'm interested in your product...",
                    "message_count": 3,
                    "read_flag": 1,
                    "has_attachments_flag": 0,
                    "folders": ["inbox"],
                    "last_message_timestamp": "2024-11-15T10:00:00Z",
                    "parties": {
                        "from": [{"email_address": "lead@prospect.com", "name": "Lead"}],
                        "to": [{"email_address": "sales@company.com", "name": "Sales"}]
                    },
                    "deal_id": 100
                },
                {
                    "id": 1002,
                    "subject": "Partnership Proposal",
                    "snippet": "We'd like to discuss a partnership...",
                    "message_count": 5,
                    "read_flag": 0,
                    "has_attachments_flag": 1,
                    "folders": ["inbox", "starred"],
                    "last_message_timestamp": "2024-11-18T14:30:00Z",
                    "parties": {
                        "from": [{"email_address": "partner@partner.com", "name": "Partner"}],
                        "to": [{"email_address": "sales@company.com", "name": "Sales"}]
                    },
                    "deal_id": null
                }
            ],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 50,
                    "more_items_in_collection": true,
                    "next_start": 50
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First thread
        var thread1 = response.Data[0];
        Assert.Equal(1001, thread1.Id);
        Assert.Equal("Initial Inquiry", thread1.Subject);
        Assert.Equal(3, thread1.MessageCount);
        Assert.Equal(1, thread1.ReadFlag);
        Assert.Equal(0, thread1.HasAttachmentsFlag);
        Assert.Equal(100, thread1.DealId);

        // Second thread (unread, with attachments)
        var thread2 = response.Data[1];
        Assert.Equal(1002, thread2.Id);
        Assert.Equal("Partnership Proposal", thread2.Subject);
        Assert.Equal(5, thread2.MessageCount);
        Assert.Equal(0, thread2.ReadFlag);
        Assert.Equal(1, thread2.HasAttachmentsFlag);
        Assert.Null(thread2.DealId);
        Assert.NotNull(thread2.Folders);
        Assert.Contains("starred", thread2.Folders);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.True(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test handling empty thread list
    /// </summary>
    [Fact]
    public void MailThreadList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 50,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test thread with archived flag
    /// </summary>
    [Fact]
    public void MailThread_Deserialization_ArchivedThread()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 9999,
                "subject": "Old Conversation",
                "snippet": "This conversation is archived",
                "message_count": 10,
                "read_flag": 1,
                "archived_flag": 1,
                "folders": ["archive"],
                "last_message_timestamp": "2024-01-01T10:00:00Z",
                "deal_id": null
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(9999, response.Data.Id);
        Assert.Equal(1, response.Data.ArchivedFlag);
        Assert.NotNull(response.Data.Folders);
        Assert.Contains("archive", response.Data.Folders);
    }

    /// <summary>
    /// Test thread associated with a lead
    /// </summary>
    [Fact]
    public void MailThread_Deserialization_WithLeadAssociation()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 7777,
                "subject": "New Lead Inquiry",
                "snippet": "I found your website and I'm interested...",
                "message_count": 2,
                "read_flag": 0,
                "folders": ["inbox"],
                "last_message_timestamp": "2024-11-20T09:00:00Z",
                "deal_id": null,
                "lead_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(7777, response.Data.Id);
        Assert.Null(response.Data.DealId);
        Assert.Equal("a1b2c3d4-e5f6-7890-abcd-ef1234567890", response.Data.LeadId);
    }

    /// <summary>
    /// Test thread with draft
    /// </summary>
    [Fact]
    public void MailThread_Deserialization_WithDraft()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 6666,
                "subject": "Proposal Draft",
                "snippet": "Latest message...",
                "snippet_draft": "I'm working on a proposal that includes...",
                "message_count": 4,
                "has_draft_flag": 1,
                "read_flag": 1,
                "folders": ["drafts", "inbox"],
                "last_message_timestamp": "2024-11-19T15:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(6666, response.Data.Id);
        Assert.Equal(1, response.Data.HasDraftFlag);
        Assert.Equal("I'm working on a proposal that includes...", response.Data.SnippetDraft);
        Assert.NotNull(response.Data.Folders);
        Assert.Contains("drafts", response.Data.Folders);
    }

    #endregion

    #region MailThreadParties Tests

    /// <summary>
    /// Test MailThreadParties deserialization with multiple parties
    /// </summary>
    [Fact]
    public void MailThreadParties_Deserialization_MultipleParties()
    {
        // Arrange
        var json = """
        {
            "from": [
                {"email_address": "sender1@company.com", "name": "Sender 1"},
                {"email_address": "sender2@company.com", "name": "Sender 2"}
            ],
            "to": [
                {"email_address": "recipient1@client.com", "name": "Recipient 1"},
                {"email_address": "recipient2@client.com", "name": "Recipient 2"},
                {"email_address": "recipient3@client.com", "name": "Recipient 3"}
            ]
        }
        """;

        // Act
        var parties = JsonSerializer.Deserialize(json, ApiJsonContext.Default.MailThreadParties);

        // Assert
        Assert.NotNull(parties);
        Assert.NotNull(parties.From);
        Assert.Equal(2, parties.From.Count);
        Assert.NotNull(parties.To);
        Assert.Equal(3, parties.To.Count);
    }

    /// <summary>
    /// Test MailThreadParties with empty lists
    /// </summary>
    [Fact]
    public void MailThreadParties_Deserialization_EmptyParties()
    {
        // Arrange
        var json = """
        {
            "from": [],
            "to": []
        }
        """;

        // Act
        var parties = JsonSerializer.Deserialize(json, ApiJsonContext.Default.MailThreadParties);

        // Assert
        Assert.NotNull(parties);
        Assert.NotNull(parties.From);
        Assert.Empty(parties.From);
        Assert.NotNull(parties.To);
        Assert.Empty(parties.To);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Test mail message with no body (body_url only)
    /// </summary>
    [Fact]
    public void MailMessage_Deserialization_NoBodyContent()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 88888,
                "subject": "Quick question",
                "snippet": "Do you have time for a call?",
                "body": null,
                "body_url": "https://api.pipedrive.com/v1/mailbox/mailMessages/88888/body",
                "has_body_flag": 1,
                "from": [{"email_address": "sender@test.com"}],
                "to": [{"email_address": "recipient@test.com"}]
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailMessage);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.Body);
        Assert.NotNull(response.Data.BodyUrl);
        Assert.Equal(1, response.Data.HasBodyFlag);
    }

    /// <summary>
    /// Test mail message that was sent via Smart BCC
    /// </summary>
    [Fact]
    public void MailMessage_Deserialization_SmartBcc()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 77777,
                "subject": "External Email via BCC",
                "snippet": "This email was tracked via Smart BCC",
                "smart_bcc_flag": 1,
                "sent_from_pipedrive_flag": 0,
                "sent_flag": 1,
                "from": [{"email_address": "sales@company.com"}],
                "to": [{"email_address": "customer@external.com"}]
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailMessage);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal(1, response.Data.SmartBccFlag);
        Assert.Equal(0, response.Data.SentFromPipedriveFlag);
        Assert.Equal(1, response.Data.SentFlag);
    }

    /// <summary>
    /// Test thread error response
    /// </summary>
    [Fact]
    public void MailThread_Deserialization_ErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Thread not found",
            "error_info": "No mail thread with ID 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseMailThread);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Thread not found", response.Error);
        Assert.Null(response.Data);
    }

    #endregion

    #region MailMessageWrapper Tests (Deal/Person Mail Messages)

    /// <summary>
    /// Test that MailMessageWrapper correctly deserializes the nested structure
    /// returned by deals/{id}/mailMessages and persons/{id}/mailMessages endpoints
    /// </summary>
    [Fact]
    public void MailMessageWrapper_Deserialization_DealMailMessages()
    {
        // Arrange - This is the actual structure returned by deals/{id}/mailMessages
        var json = """
        {
            "success": true,
            "data": [
                {
                    "object": "mailMessage",
                    "timestamp": "2025-12-02 17:16:53",
                    "data": {
                        "id": 637668,
                        "from": [
                            {
                                "id": 28420,
                                "email_address": "gregorius@example.com",
                                "name": "Gregorius Soedharmo",
                                "linked_person_id": 1146,
                                "linked_person_name": "Gregorius Soedharmo"
                            }
                        ],
                        "to": [
                            {
                                "id": 8629,
                                "email_address": "jay@example.com",
                                "name": "Jay DeBoer",
                                "linked_person_id": 110,
                                "linked_person_name": "Jay DeBoer"
                            }
                        ],
                        "cc": [],
                        "bcc": [],
                        "subject": "Discord discussion follow-up",
                        "snippet": "Hi Jay, I'm Gregorius, part of the team...",
                        "mail_thread_id": 119048,
                        "read_flag": 1,
                        "sent_flag": 0,
                        "message_time": "2025-12-02T17:16:53.000Z",
                        "has_attachments_flag": 0,
                        "deal_id": 1604
                    }
                },
                {
                    "object": "mailMessage",
                    "timestamp": "2025-09-22 11:19:00",
                    "data": {
                        "id": 633160,
                        "from": [
                            {
                                "id": 5,
                                "email_address": "aaron@example.com",
                                "name": "Aaron Stannard"
                            }
                        ],
                        "to": [
                            {
                                "id": 8629,
                                "email_address": "nate@example.com",
                                "name": "Nate Dahlin"
                            }
                        ],
                        "subject": "Re: Can we help you with your project?",
                        "snippet": "Hi Nate, Yeah this makes sense to me...",
                        "mail_thread_id": 115000,
                        "read_flag": 1,
                        "sent_flag": 1,
                        "message_time": "2025-09-22T11:19:00.000Z",
                        "has_attachments_flag": 0,
                        "deal_id": 1604
                    }
                }
            ],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 2,
                    "more_items_in_collection": true,
                    "next_start": 2
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailMessageWrapper);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First wrapper
        var wrapper1 = response.Data[0];
        Assert.Equal("mailMessage", wrapper1.ObjectType);
        Assert.Equal("2025-12-02 17:16:53", wrapper1.Timestamp);
        Assert.NotNull(wrapper1.Data);
        Assert.Equal(637668, wrapper1.Data.Id);
        Assert.Equal("Discord discussion follow-up", wrapper1.Data.Subject);
        Assert.NotNull(wrapper1.Data.From);
        Assert.Single(wrapper1.Data.From);
        Assert.Equal("gregorius@example.com", wrapper1.Data.From[0].EmailAddress);
        Assert.Equal("Gregorius Soedharmo", wrapper1.Data.From[0].Name);
        Assert.Equal(1604, wrapper1.Data.DealId);

        // Second wrapper
        var wrapper2 = response.Data[1];
        Assert.Equal("mailMessage", wrapper2.ObjectType);
        Assert.NotNull(wrapper2.Data);
        Assert.Equal(633160, wrapper2.Data.Id);
        Assert.Equal("Re: Can we help you with your project?", wrapper2.Data.Subject);
        Assert.Equal(1, wrapper2.Data.SentFlag);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(2, response.AdditionalData.Pagination.Limit);
        Assert.True(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test extracting MailMessage objects from wrapper list (simulates what EmailsCommands does)
    /// </summary>
    [Fact]
    public void MailMessageWrapper_ExtractInnerMessages()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "object": "mailMessage",
                    "timestamp": "2025-12-02 10:00:00",
                    "data": {
                        "id": 100,
                        "subject": "First email",
                        "snippet": "Content 1..."
                    }
                },
                {
                    "object": "mailMessage",
                    "timestamp": "2025-12-02 11:00:00",
                    "data": {
                        "id": 101,
                        "subject": "Second email",
                        "snippet": "Content 2..."
                    }
                }
            ]
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailMessageWrapper);
        var messages = response!.Data!
            .Where(w => w.Data != null)
            .Select(w => w.Data!)
            .ToList();

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.Equal(100, messages[0].Id);
        Assert.Equal("First email", messages[0].Subject);
        Assert.Equal(101, messages[1].Id);
        Assert.Equal("Second email", messages[1].Subject);
    }

    /// <summary>
    /// Test handling empty wrapper list
    /// </summary>
    [Fact]
    public void MailMessageWrapper_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 50,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListMailMessageWrapper);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    #endregion
}
