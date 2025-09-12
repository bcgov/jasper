using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents;
public class DocumentConverterTests : ServiceTestBase
{
    private readonly Faker _faker;
    private readonly IMapper _mapper;
    private readonly CachingService _cachingService;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<LookupService> _mockLookupService;

    public DocumentConverterTests()
    {
        _faker = new Faker();


        // Setup Cache
        _cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:LookupExpiryMinutes")).Returns(mockSection.Object);

        var config = new TypeAdapterConfig();
        config.Apply(new CalendarMapping());
        config.Apply(new LocationMapping());
        _mapper = new Mapper(config);

        var mockLookupCodeServicesClient = new Mock<LookupCodeServicesClient>(MockBehavior.Strict, this.HttpClient);

        var dcService = new Mock<IDocumentCategoryService>();
        dcService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        _mockLookupService = new Mock<LookupService>(
            MockBehavior.Strict,
            _mockConfig.Object,
            mockLookupCodeServicesClient.Object,
            _cachingService,
            dcService.Object);
    }

    [Fact]
    public async Task DocumentConverter_Should_GetCriminalDocuments()
    {
        var participantId = _faker.Random.AlphaNumeric(10);

        var mockAccussedFile = new CfcAccusedFile
        {
            PartId = participantId,
            Appearance = [new CfcAppearance { }],
            Document = [
                new CfcDocument
                {
                    DocmFormId = "1",
                    DocmClassification = "A",
                    DocmFormDsc = "Form A",
                    DocmId = "D1",
                    ImageId = "I1"
                },
                new CfcDocument
                {
                    DocmFormId = "2",
                    DocmClassification = "B",
                    DocmFormDsc = "Form B",
                    DocmId = "D2",
                    ImageId = "I2"
                }
            ],
        };

        var documentConverter = new DocumentConverter(_mapper, _mockLookupService.Object);
        var result = await documentConverter.GetCriminalDocuments(mockAccussedFile);

        Assert.Equal(3, result.Count);
        Assert.Equal(participantId, result.First().PartId);
        Assert.Equal("rop", result.First().Category);
        Assert.Equal("Record of Proceedings", result.First().DocumentTypeDescription);
    }
}
