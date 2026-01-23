using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Models.Order;
using Xunit;

namespace tests.Api.Helpers;

public class FlexibleNamingJsonConverterTests
{
    private JsonSerializerSettings SettingsWithConverter()
    {
        return new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            Converters = { new FlexibleNamingJsonConverter<OrderDto>() },
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    [Fact]
    public void Deserialize_SnakeCase_TopLevel_And_Nested_Works()
    {
        var json = @"{
              ""court_file"": {
                ""court_class_cd"": ""A"",
                ""physical_file_id"": 123
              },
              ""referral"": {
                ""sent_to_part_id"": 456
              },
              ""package_documents"": [
                { ""document_id"": 789, ""document_type_cd"": ""ORD"", ""document_type_desc"": ""Order"" }
              ],
              ""relevant_ceis_documents"": [
                { ""civil_document_id"": 1011, ""document_type_cd"": ""CSR"", ""document_type_desc"": ""Summary"" }
              ],
              ""created_date"": ""2026-01-12T10:00:00Z""
            }";

        var dto = JsonConvert.DeserializeObject<OrderRequestDto>(json, SettingsWithConverter());

        Assert.NotNull(dto);
        Assert.NotNull(dto.CourtFile);
        Assert.Equal("A", dto.CourtFile.CourtClassCd);
        Assert.Equal(123, dto.CourtFile.PhysicalFileId);
        Assert.NotNull(dto.Referral);
        Assert.Equal(456, dto.Referral.SentToPartId);
        Assert.NotNull(dto.PackageDocuments);
        Assert.Single(dto.PackageDocuments);
        Assert.Equal(789, dto.PackageDocuments[0].DocumentId);
        Assert.NotNull(dto.RelevantCeisDocuments);
        Assert.Single(dto.RelevantCeisDocuments);
        Assert.Equal(1011, dto.RelevantCeisDocuments[0].CivilDocumentId);
    }

    [Fact]
    public void Deserialize_CamelCase_Works()
    {
        var json = @"{
              ""courtFile"": { ""courtClassCd"": ""C"", ""physicalFileId"": 55 },
              ""referral"": { ""sentToPartId"": 999 }
            }";

        var dto = JsonConvert.DeserializeObject<OrderRequestDto>(json, SettingsWithConverter());

        Assert.NotNull(dto);
        Assert.Equal("C", dto.CourtFile.CourtClassCd);
        Assert.Equal(55, dto.CourtFile.PhysicalFileId);
        Assert.Equal(999, dto.Referral.SentToPartId);
    }

    [Fact]
    public void Deserialize_PascalCase_Works()
    {
        var json = @"{
              ""CourtFile"": { ""CourtClassCd"": ""F"", ""PhysicalFileId"": 777 },
              ""Referral"": { ""SentToPartId"": 42 }
            }";

        var dto = JsonConvert.DeserializeObject<OrderRequestDto>(json, SettingsWithConverter());

        Assert.NotNull(dto);
        Assert.Equal("F", dto.CourtFile.CourtClassCd);
        Assert.Equal(777, dto.CourtFile.PhysicalFileId);
        Assert.Equal(42, dto.Referral.SentToPartId);
    }

    [Fact]
    public void Deserialize_Nested_Array_Items_With_SnakeCase_Works()
    {
        var json = @"{
              ""court_file"": { ""court_class_cd"": ""A"", ""physical_file_id"": 1 },
              ""package_documents"": [
                { ""document_id"": 10, ""document_type_cd"": ""ORD"", ""document_type_desc"": ""Order"", ""order"": true, ""referred_document"": false },
                { ""document_id"": 11, ""document_type_cd"": ""MOT"", ""document_type_desc"": ""Motion"", ""order"": false, ""referred_document"": true }
              ],
              ""relevant_ceis_documents"": [
                { ""civil_document_id"": 20, ""document_type_cd"": ""CSR"", ""document_type_desc"": ""Summary"" }
              ]
            }";

        var dto = JsonConvert.DeserializeObject<OrderRequestDto>(json, SettingsWithConverter());

        Assert.NotNull(dto);
        Assert.Equal("A", dto.CourtFile.CourtClassCd);
        Assert.Equal(1, dto.CourtFile.PhysicalFileId);
        Assert.Equal(2, dto.PackageDocuments.Count);
        Assert.Equal(10, dto.PackageDocuments[0].DocumentId);
        Assert.True(dto.PackageDocuments[0].Order);
        Assert.False(dto.PackageDocuments[0].ReferredDocument);
        Assert.Equal(11, dto.PackageDocuments[1].DocumentId);
        Assert.False(dto.PackageDocuments[1].Order);
        Assert.True(dto.PackageDocuments[1].ReferredDocument);
        Assert.Single(dto.RelevantCeisDocuments);
        Assert.Equal(20, dto.RelevantCeisDocuments[0].CivilDocumentId);
    }

    [Fact]
    public void Deserialize_Null_Token_Returns_Null()
    {
        var dto = JsonConvert.DeserializeObject<OrderRequestDto>("null", SettingsWithConverter());
        Assert.Null(dto);
    }

    [Fact]
    public void Deserialize_Ignores_Unknown_Properties()
    {
        var json = @"{
              ""court_file"": { ""court_class_cd"": ""Y"", ""physical_file_id"": 999, ""unknown_prop"": ""ignored"" },
              ""referral"": { ""sent_to_part_id"": 123, ""extra"": ""ignored"" },
              ""unknown_root"": ""ignored""
            }";

        var dto = JsonConvert.DeserializeObject<OrderRequestDto>(json, SettingsWithConverter());

        Assert.NotNull(dto);
        Assert.Equal("Y", dto.CourtFile.CourtClassCd);
        Assert.Equal(999, dto.CourtFile.PhysicalFileId);
        Assert.Equal(123, dto.Referral.SentToPartId);
    }
}