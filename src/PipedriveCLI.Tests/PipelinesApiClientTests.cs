using System.Text.Json;
using PipedriveCLI.Models;
using Xunit;

namespace PipedriveCLI.Tests;

/// <summary>
/// Tests for Pipelines API client methods and JSON serialization
/// </summary>
public class PipelinesApiClientTests
{
    /// <summary>
    /// Test that Pipeline deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Pipeline_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
        var json = """
        {
            "success": true,
            "data": {
                "id": 15,
                "name": "Phobos Sales (v2)",
                "order_nr": 1,
                "active": true,
                "deal_probability": true,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePipeline);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(15, response.Data.Id);
        Assert.Equal("Phobos Sales (v2)", response.Data.Name);
        Assert.Equal(1, response.Data.OrderNr);
        Assert.True(response.Data.Active);
        Assert.True(response.Data.DealProbability);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test deserializing a list of pipelines
    /// </summary>
    [Fact]
    public void PipelinesList_Deserialization_Success()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 15,
                    "name": "Phobos Sales (v2)",
                    "order_nr": 1,
                    "active": true,
                    "deal_probability": true,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 26,
                    "name": "Support Plan Sales (v2)",
                    "order_nr": 2,
                    "active": true,
                    "deal_probability": false,
                    "add_time": "2024-01-02T11:00:00Z",
                    "update_time": "2024-01-02T11:00:00Z"
                },
                {
                    "id": 1,
                    "name": "Services Pipeline (Archived)",
                    "order_nr": 3,
                    "active": false,
                    "deal_probability": true,
                    "add_time": "2023-01-01T00:00:00Z",
                    "update_time": "2024-05-15T12:00:00Z"
                }
            ]
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListPipeline);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(3, response.Data.Count);

        // First pipeline
        var pipeline1 = response.Data[0];
        Assert.Equal(15, pipeline1.Id);
        Assert.Equal("Phobos Sales (v2)", pipeline1.Name);
        Assert.Equal(1, pipeline1.OrderNr);
        Assert.True(pipeline1.Active);
        Assert.True(pipeline1.DealProbability);

        // Second pipeline
        var pipeline2 = response.Data[1];
        Assert.Equal(26, pipeline2.Id);
        Assert.Equal("Support Plan Sales (v2)", pipeline2.Name);
        Assert.Equal(2, pipeline2.OrderNr);
        Assert.True(pipeline2.Active);
        Assert.False(pipeline2.DealProbability);

        // Third pipeline (archived)
        var pipeline3 = response.Data[2];
        Assert.Equal(1, pipeline3.Id);
        Assert.Equal("Services Pipeline (Archived)", pipeline3.Name);
        Assert.Equal(3, pipeline3.OrderNr);
        Assert.False(pipeline3.Active);
    }

    /// <summary>
    /// Test handling API error responses for pipelines
    /// </summary>
    [Fact]
    public void Pipeline_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Pipeline not found",
            "error_info": "No pipeline found with ID: 999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponsePipeline);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Pipeline not found", response.Error);
        Assert.Equal("No pipeline found with ID: 999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test that Stage deserialization handles the full Pipedrive API response
    /// </summary>
    [Fact]
    public void Stage_Deserialization_HandlesFullApiResponse()
    {
        // Arrange - Full API response with all fields
        var json = """
        {
            "success": true,
            "data": {
                "id": 82,
                "name": "Qualified",
                "pipeline_id": 15,
                "order_nr": 1,
                "active_flag": true,
                "deal_probability": 10,
                "rotten_flag": true,
                "rotten_days": 30,
                "add_time": "2024-01-15T10:30:00Z",
                "update_time": "2024-01-16T14:20:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseStage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(82, response.Data.Id);
        Assert.Equal("Qualified", response.Data.Name);
        Assert.Equal(15, response.Data.PipelineId);
        Assert.Equal(1, response.Data.OrderNr);
        Assert.True(response.Data.ActiveFlag);
        Assert.Equal(10, response.Data.DealProbability);
        Assert.True(response.Data.RottenFlag);
        Assert.Equal(30, response.Data.RottenDays);
        Assert.Equal("2024-01-15T10:30:00Z", response.Data.AddTime);
        Assert.Equal("2024-01-16T14:20:00Z", response.Data.UpdateTime);
    }

    /// <summary>
    /// Test deserializing a list of stages
    /// </summary>
    [Fact]
    public void StagesList_Deserialization_Success()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": [
                {
                    "id": 82,
                    "name": "Qualified",
                    "pipeline_id": 15,
                    "order_nr": 1,
                    "active_flag": true,
                    "deal_probability": 10,
                    "rotten_flag": false,
                    "rotten_days": null,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 83,
                    "name": "Contact Made",
                    "pipeline_id": 15,
                    "order_nr": 2,
                    "active_flag": true,
                    "deal_probability": 25,
                    "rotten_flag": true,
                    "rotten_days": 14,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                },
                {
                    "id": 84,
                    "name": "Demo Scheduled",
                    "pipeline_id": 15,
                    "order_nr": 3,
                    "active_flag": true,
                    "deal_probability": 50,
                    "rotten_flag": null,
                    "rotten_days": null,
                    "add_time": "2024-01-01T10:00:00Z",
                    "update_time": "2024-01-01T10:00:00Z"
                }
            ]
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListStage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(3, response.Data.Count);

        // First stage
        var stage1 = response.Data[0];
        Assert.Equal(82, stage1.Id);
        Assert.Equal("Qualified", stage1.Name);
        Assert.Equal(15, stage1.PipelineId);
        Assert.Equal(1, stage1.OrderNr);
        Assert.True(stage1.ActiveFlag);
        Assert.Equal(10, stage1.DealProbability);
        Assert.False(stage1.RottenFlag);
        Assert.Null(stage1.RottenDays);

        // Second stage
        var stage2 = response.Data[1];
        Assert.Equal(83, stage2.Id);
        Assert.Equal("Contact Made", stage2.Name);
        Assert.Equal(15, stage2.PipelineId);
        Assert.Equal(2, stage2.OrderNr);
        Assert.True(stage2.ActiveFlag);
        Assert.Equal(25, stage2.DealProbability);
        Assert.True(stage2.RottenFlag);
        Assert.Equal(14, stage2.RottenDays);

        // Third stage
        var stage3 = response.Data[2];
        Assert.Equal(84, stage3.Id);
        Assert.Equal("Demo Scheduled", stage3.Name);
        Assert.Equal(50, stage3.DealProbability);
        Assert.Null(stage3.RottenFlag);
    }

    /// <summary>
    /// Test handling API error responses for stages
    /// </summary>
    [Fact]
    public void Stage_Deserialization_HandlesErrorResponse()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": "Stage not found",
            "error_info": "No stage found with ID: 999",
            "data": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseStage);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Stage not found", response.Error);
        Assert.Equal("No stage found with ID: 999", response.ErrorInfo);
        Assert.Null(response.Data);
    }

    /// <summary>
    /// Test that empty list response deserializes correctly
    /// </summary>
    [Fact]
    public void PipelinesList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": []
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListPipeline);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test that empty stages list response deserializes correctly
    /// </summary>
    [Fact]
    public void StagesList_Deserialization_EmptyList()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": []
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseListStage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
    }

    /// <summary>
    /// Test stage with minimal fields (nullable fields not set)
    /// </summary>
    [Fact]
    public void Stage_Deserialization_MinimalFields()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "data": {
                "id": 100,
                "name": "New Stage",
                "pipeline_id": 20,
                "order_nr": 5,
                "active_flag": true,
                "deal_probability": null,
                "rotten_flag": null,
                "rotten_days": null,
                "add_time": "2024-01-01T00:00:00Z",
                "update_time": "2024-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, ApiJsonContext.Default.PipedriveResponseStage);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(100, response.Data.Id);
        Assert.Equal("New Stage", response.Data.Name);
        Assert.Equal(20, response.Data.PipelineId);
        Assert.Equal(5, response.Data.OrderNr);
        Assert.True(response.Data.ActiveFlag);
        Assert.Null(response.Data.DealProbability);
        Assert.Null(response.Data.RottenFlag);
        Assert.Null(response.Data.RottenDays);
    }
}
