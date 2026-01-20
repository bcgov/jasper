using System.Threading.Tasks;
using Bogus;
using FluentValidation.TestHelper;
using Scv.Api.Models.Order;
using Scv.Api.Validators.Order;
using Xunit;

namespace tests.api.Validators.Order;

public class OrderRequestDtoValidatorTests
{
    private readonly OrderRequestDtoValidator _validator;
    private readonly Faker _faker;

    public OrderRequestDtoValidatorTests()
    {
        _validator = new OrderRequestDtoValidator();
        _faker = new Faker();
    }

    #region CourtFile Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtFileIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = null,
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile)
            .WithErrorMessage("CourtFile is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenPhysicalFileIdIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = null,
                CourtDivisionCd = "R",
                CourtClassCd = "A"
            },
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.PhysicalFileId)
            .WithErrorMessage("PhysicalFileId is required.");
    }

    #endregion

    #region CourtDivisionCd Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtDivisionCdIsEmpty()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = "";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd)
            .WithErrorMessage("CourtDivisionCd is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtDivisionCdIsNull()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = null;

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd)
            .WithErrorMessage("CourtDivisionCd is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtDivisionCdIsWhitespace()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = "   ";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd)
            .WithErrorMessage("CourtDivisionCd is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtDivisionCdIsInvalid()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = "INVALID";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd)
            .WithErrorMessage("CourtDivisionCd is unsupported.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtDivisionCdIsF()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = "F";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd)
            .WithErrorMessage("CourtDivisionCd is unsupported.");
    }

    [Theory]
    [InlineData("R")]
    [InlineData("I")]
    public async Task Validate_ShouldNotHaveError_WhenCourtDivisionCdIsValid(string division)
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = division;

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd);
    }

    [Fact]
    public async Task Validate_ShouldNotValidateCourtDivisionCd_WhenCourtFileIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = null,
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile);
        result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd);
    }

    #endregion

    #region CourtClassCd Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtClassCdIsEmpty()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = "";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtClassCd)
            .WithErrorMessage("CourtClassCd is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtClassCdIsNull()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = null;

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtClassCd)
            .WithErrorMessage("CourtClassCd is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtClassCdIsWhitespace()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = "   ";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtClassCd)
            .WithErrorMessage("CourtClassCd is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenCourtClassCdIsInvalid()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = "Z";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtClassCd)
            .WithErrorMessage("CourtClassCd is unsupported.");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Y")]
    [InlineData("T")]
    [InlineData("C")]
    [InlineData("F")]
    [InlineData("L")]
    [InlineData("M")]
    public async Task Validate_ShouldNotHaveError_WhenCourtClassCdIsValid(string courtClass)
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = courtClass;

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtClassCd);
    }

    [Fact]
    public async Task Validate_ShouldNotValidateCourtClassCd_WhenCourtFileIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = null,
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile);
        result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtClassCd);
    }

    #endregion

    #region Referral Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenReferralIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 10000),
                CourtDivisionCd = "R",
                CourtClassCd = "A"
            },
            Referral = null,
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.Referral)
            .WithErrorMessage("Referral is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenSentToPartIdIsNull()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.Referral.SentToPartId = null;

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.Referral.SentToPartId)
            .WithErrorMessage("SentToPartId is required.");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenSentToPartIdIsValid()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.Referral.SentToPartId = _faker.Random.Int(1, 1000);

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.Referral.SentToPartId);
    }

    [Fact]
    public async Task Validate_ShouldNotValidateSentToPartId_WhenReferralIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 10000),
                CourtDivisionCd = "R",
                CourtClassCd = "A"
            },
            Referral = null,
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.Referral);
        result.ShouldNotHaveValidationErrorFor(o => o.Referral.SentToPartId);
    }

    #endregion

    #region PackageDocuments Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenPackageDocumentsIsNull()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 10000),
                CourtDivisionCd = "R",
                CourtClassCd = "A"
            },
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = null
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.PackageDocuments)
            .WithErrorMessage("PackageDocuments is required.");
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenPackageDocumentsIsEmpty()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 10000),
                CourtDivisionCd = "R",
                CourtClassCd = "A"
            },
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = []
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.PackageDocuments)
            .WithErrorMessage("PackageDocuments cannot be empty.");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenPackageDocumentsHasItems()
    {
        var orderDto = CreateValidOrderDto();

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.PackageDocuments);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenPackageDocumentsHasMultipleItems()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.PackageDocuments =
        [
            new() { DocumentId = _faker.Random.Int(1, 100) },
            new() { DocumentId = _faker.Random.Int(1, 100) },
            new() { DocumentId = _faker.Random.Int(1, 100) }
        ];

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.PackageDocuments);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Validate_ShouldNotHaveErrors_WhenAllFieldsAreValid()
    {
        var orderDto = CreateValidOrderDto();

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldHaveMultipleErrors_WhenMultipleFieldsAreInvalid()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = null,
            Referral = null,
            PackageDocuments = null
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile);
        result.ShouldHaveValidationErrorFor(o => o.Referral);
        result.ShouldHaveValidationErrorFor(o => o.PackageDocuments);
    }

    [Fact]
    public async Task Validate_ShouldHaveMultipleErrors_ForCourtFile()
    {
        var orderDto = new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = null,
                CourtDivisionCd = "",
                CourtClassCd = ""
            },
            Referral = new ReferralDto { SentToPartId = _faker.Random.Int(1, 1000) },
            PackageDocuments = [new()]
        };

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldHaveValidationErrorFor(o => o.CourtFile.PhysicalFileId);
        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd);
        result.ShouldHaveValidationErrorFor(o => o.CourtFile.CourtClassCd);
    }

    [Fact]
    public async Task Validate_ShouldValidateAllCriminalCourtClasses()
    {
        var criminalCourtClasses = new[] { "A", "Y", "T" };

        foreach (var courtClass in criminalCourtClasses)
        {
            var orderDto = CreateValidOrderDto();
            orderDto.CourtFile.CourtClassCd = courtClass;
            orderDto.CourtFile.CourtDivisionCd = "R";

            var result = await _validator.TestValidateAsync(orderDto);

            result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtClassCd);
        }
    }

    [Fact]
    public async Task Validate_ShouldValidateAllCivilCourtClasses()
    {
        var civilCourtClasses = new[] { "C", "F", "L", "M" };

        foreach (var courtClass in civilCourtClasses)
        {
            var orderDto = CreateValidOrderDto();
            orderDto.CourtFile.CourtClassCd = courtClass;
            orderDto.CourtFile.CourtDivisionCd = "I";

            var result = await _validator.TestValidateAsync(orderDto);

            result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtClassCd);
        }
    }

    [Fact]
    public async Task Validate_ShouldHandleCaseInsensitiveCourtClass()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = "a";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtClassCd);
    }

    [Fact]
    public async Task Validate_ShouldHandleCaseInsensitiveCourtDivision()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = "r";

        var result = await _validator.TestValidateAsync(orderDto);

        result.ShouldNotHaveValidationErrorFor(o => o.CourtFile.CourtDivisionCd);
    }

    #endregion

    #region Helper Methods

    private OrderRequestDto CreateValidOrderDto()
    {
        return new OrderRequestDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 10000),
                CourtDivisionCd = _faker.PickRandom("R", "I"),
                CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                CourtFileNo = _faker.Random.AlphaNumeric(10)
            },
            Referral = new ReferralDto
            {
                SentToPartId = _faker.Random.Int(1, 1000),
                ReferredDocumentId = _faker.Random.Int(1, 100)
            },
            PackageDocuments =
            [
                new()
                {
                    DocumentId = _faker.Random.Int(1, 100),
                    DocumentTypeCd = _faker.Lorem.Word()
                }
            ]
        };
    }

    #endregion
}