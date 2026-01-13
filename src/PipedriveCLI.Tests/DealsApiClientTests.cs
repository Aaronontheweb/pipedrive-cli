using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Deals API client methods and JSON serialization
/// </summary>
public class DealsApiClientTests
{
    /// <summary>
    /// Test that Deal deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
        var json = """
        {
            "success": true,
            "data": {
                "id": 123,
                "title": "Enterprise Deal",
                "value": 50000.00,
                "currency": "USD",
                "person_id": 456,
                "org_id": 789,
                "stage_id": 1,
                "status": "open",
                "probability": 75.5,
                "expected_close_date": "2024-12-31",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z",
                "won_time": null,
                "lost_time": null,
                "pipeline_id": 1,
                "owner_id": null,
                "creator_user_id": 888
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(123, response.Data.Id);
        Assert.Equal("Enterprise Deal", response.Data.Title);
        Assert.Equal(50000.00m, response.Data.Value);
        Assert.Equal("USD", response.Data.Currency);
        Assert.Equal(456, response.Data.PersonId);
        Assert.Equal(789, response.Data.OrgId);
        Assert.Equal(1, response.Data.StageId);
        Assert.Equal("open", response.Data.Status);
        Assert.Equal(75.5m, response.Data.Probability);
        Assert.Equal("2024-12-31", response.Data.ExpectedCloseDate);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
        Assert.Null(response.Data.WonTime);
        Assert.Null(response.Data.LostTime);
    }

    /// <summary>
    /// Test deserializing a list of deals
    /// </summary>
    [Fact]
    public void DealsList_Deserialization_WithPagination()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 100,
                    "title": "Deal One",
                    "value": 10000.00,
                    "currency": "USD",
                    "person_id": 200,
                    "org_id": null,
                    "stage_id": 1,
                    "status": "open",
                    "probability": 50.0,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 101,
                    "title": "Deal Two",
                    "value": 25000.50,
                    "currency": "EUR",
                    "person_id": null,
                    "org_id": 300,
                    "stage_id": 2,
                    "status": "won",
                    "probability": 100.0,
                    "expected_close_date": "2024-06-30",
                    "won_time": "2024-05-15T12:00:00Z",
                    "add_time": "2024-01-02T11:00:00Z",
                    "update_time": "2024-05-15T12:00:00Z"
                }
            ],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 100,
                    "more_items_in_collection": false,
                    "next_start": null
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);

        // First deal
        var deal1 = response.Data[0];
        Assert.Equal(100, deal1.Id);
        Assert.Equal("Deal One", deal1.Title);
        Assert.Equal(10000.00m, deal1.Value);
        Assert.Equal("USD", deal1.Currency);
        Assert.Equal(200, deal1.PersonId);
        Assert.Null(deal1.OrgId);
        Assert.Equal("open", deal1.Status);

        // Second deal
        var deal2 = response.Data[1];
        Assert.Equal(101, deal2.Id);
        Assert.Equal("Deal Two", deal2.Title);
        Assert.Equal(25000.50m, deal2.Value);
        Assert.Equal("EUR", deal2.Currency);
        Assert.Null(deal2.PersonId);
        Assert.Equal(300, deal2.OrgId);
        Assert.Equal("won", deal2.Status);
        Assert.Equal("2024-05-15T12:00:00Z", deal2.WonTime);

        // Pagination
        Assert.NotNull(response.AdditionalData?.Pagination);
        Assert.Equal(0, response.AdditionalData.Pagination.Start);
        Assert.Equal(100, response.AdditionalData.Pagination.Limit);
        Assert.False(response.AdditionalData.Pagination.MoreItemsInCollection);
    }

    /// <summary>
    /// Test that Deal serialization works for create/update operations
    /// </summary>
    [Fact]
    public void Deal_Serialization_ForCreateUpdate()
    {
        // Arrange
        var deal = new Deal
        {
            Title = "New Deal",
            Value = 15000m,
            Currency = "USD",
            PersonId = 123,
            OrgId = null,
            StageId = 1,
            ExpectedCloseDate = "2024-12-31"
        };

        // Act
        var json = JsonSerializer.Serialize(deal, ApiJsonContext.Default.Deal);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.Deal);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("New Deal", deserialized.Title);
        Assert.Equal(15000m, deserialized.Value);
        Assert.Equal("USD", deserialized.Currency);
        Assert.Equal(123, deserialized.PersonId);
        Assert.Null(deserialized.OrgId);
        Assert.Equal(1, deserialized.StageId);
        Assert.Equal("2024-12-31", deserialized.ExpectedCloseDate);
    }

    /// <summary>
    /// Test handling API error responses
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Deal not found",
            "error_info": "No deal found with ID: 999999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Deal not found", response.Error);
        Assert.Equal("No deal found with ID: 999999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test deal with minimal required fields
    /// </summary>
    [Fact]
    public void Deal_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 500,
                "title": "Minimal Deal",
                "value": 0,
                "currency": "USD",
                "person_id": null,
                "org_id": null,
                "stage_id": null,
                "status": "open",
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(500, response.Data.Id);
        Assert.Equal("Minimal Deal", response.Data.Title);
        Assert.Equal(0m, response.Data.Value);
        Assert.Equal("USD", response.Data.Currency);
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
    }

    /// <summary>
    /// Test deal with won status and won_time
    /// </summary>
    [Fact]
    public void Deal_Deserialization_WonDeal()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 600,
                "title": "Won Deal",
                "value": 75000.00,
                "currency": "USD",
                "person_id": 111,
                "org_id": 222,
                "status": "won",
                "probability": 100.0,
                "won_time": "2024-03-15T16:30:00Z",
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-03-15T16:30:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("won", response.Data.Status);
        Assert.Equal(100.0m, response.Data.Probability);
        Assert.Equal("2024-03-15T16:30:00Z", response.Data.WonTime);
        Assert.Null(response.Data.LostTime);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void DealsList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [],
            "additional_data": {
                "pagination": {
                    "start": 0,
                    "limit": 100,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test that Deal deserialization handles reference fields as objects (from actual API GET responses)
    /// The PipedriveReferenceConverter should extract the "value" property from nested objects
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesReferenceFieldsAsObjects()
    {
        // Arrange - API response with reference fields as complex objects (actual API format)
        var json = """
        {
            "success": true,
            "data": {
                "id": 888,
                "title": "Enterprise Software Deal",
                "value": 100000.00,
                "currency": "USD",
                "person_id": {
                    "active_flag": true,
                    "name": "Sarah Johnson",
                    "email": [
                        {
                            "value": "sarah@enterprise.com",
                            "primary": true,
                            "label": "work"
                        }
                    ],
                    "phone": [
                        {
                            "value": "+1-555-7890",
                            "primary": true
                        }
                    ],
                    "owner_id": 23920819,
                    "company_id": 999,
                    "value": 11111
                },
                "org_id": {
                    "name": "Enterprise Solutions Inc",
                    "people_count": 150,
                    "owner_id": 23920819,
                    "address": "456 Corporate Drive, Seattle, WA",
                    "active_flag": true,
                    "cc_email": "enterprise@pipedrive.com",
                    "value": 22222
                },
                "stage_id": 2,
                "status": "open",
                "probability": 85.5,
                "expected_close_date": "2024-12-15",
                "add_time": "2024-10-01T09:00:00Z",
                "update_time": "2024-11-01T15:30:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(888, response.Data.Id);
        Assert.Equal("Enterprise Software Deal", response.Data.Title);
        Assert.Equal(100000.00m, response.Data.Value);
        Assert.Equal("USD", response.Data.Currency);

        // Verify that the PipedriveReferenceConverter correctly extracted the "value" property from each object
        Assert.Equal(11111, response.Data.PersonId);
        Assert.Equal(22222, response.Data.OrgId);

        Assert.Equal(2, response.Data.StageId);
        Assert.Equal("open", response.Data.Status);
        Assert.Equal(85.5m, response.Data.Probability);
        Assert.Equal("2024-12-15", response.Data.ExpectedCloseDate);
        Assert.Equal("2024-10-01T09:00:00Z", response.Data.AddTime);
        Assert.Equal("2024-11-01T15:30:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test that PipedriveReferenceConverter returns null when object doesn't have "value" property
    /// This is an edge case to ensure robustness
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesObjectWithoutValueProperty()
    {
        // Arrange - API response with person_id as object but without "value" property
        var json = """
        {
            "success": true,
            "data": {
                "id": 999,
                "title": "Edge Case Deal",
                "value": 5000.00,
                "currency": "USD",
                "person_id": {
                    "name": "Some Person",
                    "active_flag": true
                },
                "org_id": null,
                "stage_id": 1,
                "status": "open",
                "add_time": "2024-11-04T10:00:00Z",
                "update_time": "2024-11-04T10:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(999, response.Data.Id);
        Assert.Equal("Edge Case Deal", response.Data.Title);

        // Verify that the converter returns null when no "value" property is found
        Assert.Null(response.Data.PersonId);
        Assert.Null(response.Data.OrgId);
    }

    /// <summary>
    /// Test that Deal deserialization correctly handles the cc_email field (Smart BCC)
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesCcEmail()
    {
        // Arrange - Full API response with cc_email field
        var json = """
        {
            "success": true,
            "data": {
                "id": 835,
                "title": "Enterprise Deal with Email",
                "value": 50000.00,
                "currency": "USD",
                "person_id": 456,
                "org_id": 789,
                "stage_id": 1,
                "status": "open",
                "probability": 75.5,
                "expected_close_date": "2024-12-31",
                "cc_email": "petabridgellc-baa75d+deal835@pipedrivemail.com",
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(835, response.Data.Id);
        Assert.Equal("Enterprise Deal with Email", response.Data.Title);
        Assert.Equal("petabridgellc-baa75d+deal835@pipedrivemail.com", response.Data.CcEmail);
    }

    /// <summary>
    /// Test that Deal deserialization handles null cc_email field
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesNullCcEmail()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 500,
                "title": "Deal Without CC Email",
                "value": 0,
                "currency": "USD",
                "person_id": null,
                "org_id": null,
                "stage_id": null,
                "status": "open",
                "cc_email": null,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.CcEmail);
    }

    /// <summary>
    /// Test that Deal deserialization handles missing cc_email field
    /// </summary>
    [Fact]
    public void Deal_Deserialization_HandlesMissingCcEmail()
    {
        // Arrange - API response without cc_email field at all
        var json = """
        {
            "success": true,
            "data": {
                "id": 501,
                "title": "Deal Without CC Email Field",
                "value": 1000,
                "currency": "USD",
                "status": "open",
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDeal);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Data.CcEmail);
    }

    /// <summary>
    /// Test that Deal update with clear fields generates correct JSON with explicit nulls
    /// </summary>
    [Fact]
    public void Deal_UpdateWithClearFields_GeneratesCorrectJson()
    {
        // Arrange - A deal with some fields set
        var deal = new Deal
        {
            Title = "Updated Title"
        };

        var clearFields = new List<string> { "expected_close_date" };

        // Act - Simulate what the API client does to build JSON with clear fields
        var dealJson = JsonSerializer.Serialize(deal, ApiJsonContext.Default.Deal);
        using var dealDoc = JsonDocument.Parse(dealJson);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        foreach (var property in dealDoc.RootElement.EnumerateObject())
        {
            property.WriteTo(writer);
        }
        foreach (var field in clearFields)
        {
            if (!dealDoc.RootElement.TryGetProperty(field, out _))
            {
                writer.WriteNull(field);
            }
        }
        writer.WriteEndObject();
        writer.Flush();

        var resultJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        using var resultDoc = JsonDocument.Parse(resultJson);

        // Assert - The JSON should contain the title and an explicit null for expected_close_date
        Assert.True(resultDoc.RootElement.TryGetProperty("title", out var titleProp));
        Assert.Equal("Updated Title", titleProp.GetString());

        Assert.True(resultDoc.RootElement.TryGetProperty("expected_close_date", out var dateProp));
        Assert.Equal(JsonValueKind.Null, dateProp.ValueKind);
    }

    /// <summary>
    /// Test that DealProduct deserialization handles the full API response
    /// </summary>
    [Fact]
    public void DealProduct_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response from GET /deals/{id}/products
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 547,
                    "deal_id": 1497,
                    "product_id": 5,
                    "product_variation_id": null,
                    "name": "Consulting Call",
                    "order_nr": 0,
                    "item_price": 300,
                    "quantity": 1,
                    "sum": 300,
                    "currency": "USD",
                    "active_flag": true,
                    "enabled_flag": true,
                    "add_time": "2026-01-13 16:55:45",
                    "last_edit": "2026-01-13 16:55:45",
                    "comments": null,
                    "tax": 0,
                    "discount": 0,
                    "discount_type": "percentage",
                    "billing_frequency": "one-time",
                    "billing_frequency_cycles": null,
                    "billing_start_date": null
                }
            ],
            "additional_data": {
                "products_quantity_total": 1,
                "products_sum_total": 300
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListDealProduct);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);

        var product = response.Data[0];
        Assert.Equal(547, product.Id);
        Assert.Equal(1497, product.DealId);
        Assert.Equal(5, product.ProductId);
        Assert.Null(product.ProductVariationId);
        Assert.Equal("Consulting Call", product.Name);
        Assert.Equal(0, product.OrderNr);
        Assert.Equal(300m, product.ItemPrice);
        Assert.Equal(1, product.Quantity);
        Assert.Equal(300m, product.Sum);
        Assert.Equal("USD", product.Currency);
        Assert.True(product.ActiveFlag);
        Assert.True(product.EnabledFlag);
        Assert.Equal("2026-01-13 16:55:45", product.AddTime);
        Assert.Null(product.Comments);
        Assert.Equal(0m, product.Tax);
        Assert.Equal(0m, product.Discount);
        Assert.Equal("percentage", product.DiscountType);
        Assert.Equal("one-time", product.BillingFrequency);
    }

    /// <summary>
    /// Test single DealProduct deserialization from POST /deals/{id}/products
    /// </summary>
    [Fact]
    public void DealProduct_Deserialization_SingleProduct()
    {
        // Arrange - Response from adding a product to a deal
        var json = """
        {
            "success": true,
            "data": {
                "id": 548,
                "company_id": 6850536,
                "deal_id": 1497,
                "product_id": 5,
                "product_variation_id": null,
                "name": "Consulting Call",
                "order_nr": 0,
                "item_price": 300,
                "quantity": 2,
                "sum": 600,
                "currency": "USD",
                "active_flag": true,
                "enabled_flag": true,
                "add_time": "2026-01-13 16:55:45",
                "last_edit": "2026-01-13 16:55:45",
                "comments": null,
                "tax": 0,
                "discount": 10,
                "discount_type": "percentage",
                "billing_frequency": "one-time",
                "billing_frequency_cycles": null,
                "billing_start_date": null
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseDealProduct);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(548, response.Data.Id);
        Assert.Equal(1497, response.Data.DealId);
        Assert.Equal(5, response.Data.ProductId);
        Assert.Equal("Consulting Call", response.Data.Name);
        Assert.Equal(2, response.Data.Quantity);
        Assert.Equal(600m, response.Data.Sum);
        Assert.Equal(10m, response.Data.Discount);
        Assert.Equal("percentage", response.Data.DiscountType);
    }

    /// <summary>
    /// Test AddDealProductRequest serialization for create operations
    /// </summary>
    [Fact]
    public void AddDealProductRequest_Serialization()
    {
        // Arrange
        var request = new AddDealProductRequest
        {
            ProductId = 5,
            ItemPrice = 300m,
            Quantity = 2,
            Discount = 10m,
            DiscountType = "percentage",
            Comments = "Test product"
        };

        // Act
        var json = JsonSerializer.Serialize(request, ApiJsonContext.Default.AddDealProductRequest);
        var deserialized = JsonSerializer.Deserialize(json, ApiJsonContext.Default.AddDealProductRequest);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(5, deserialized.ProductId);
        Assert.Equal(300m, deserialized.ItemPrice);
        Assert.Equal(2, deserialized.Quantity);
        Assert.Equal(10m, deserialized.Discount);
        Assert.Equal("percentage", deserialized.DiscountType);
        Assert.Equal("Test product", deserialized.Comments);
    }

    /// <summary>
    /// Test DealProduct list with empty data
    /// </summary>
    [Fact]
    public void DealProductList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": null,
            "additional_data": {
                "products_quantity_total": 0,
                "products_sum_total": 0,
                "products_quantity_total_formatted": "0",
                "products_sum_total_formatted": "$0",
                "pagination": {
                    "start": 0,
                    "limit": 100,
                    "more_items_in_collection": false
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListDealProduct);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Null(response.Data); // API returns null for no products
    }
}
