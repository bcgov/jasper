using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class EmailTemplateServiceTests
{
    private readonly Mock<IRepositoryBase<EmailTemplate>> _mockEmailTemplateRepo;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailTemplateService>> _mockLogger;
    private readonly EmailTemplateService _emailTemplateService;
    private readonly Faker _faker;

    public EmailTemplateServiceTests()
    {
        _mockEmailTemplateRepo = new Mock<IRepositoryBase<EmailTemplate>>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailTemplateService>>();
        _faker = new Faker();

        var mockServiceAccountSection = new Mock<IConfigurationSection>();
        mockServiceAccountSection.Setup(s => s.Value).Returns(_faker.Internet.Email());
        _mockConfiguration.Setup(c => c.GetSection("AZURE:SERVICE_ACCOUNT")).Returns(mockServiceAccountSection.Object);

        _emailTemplateService = new EmailTemplateService(
            _mockEmailTemplateRepo.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendEmailTemplateAsync_WithValidTemplate_SendsEmail()
    {
        var templateName = EmailTemplate.ORDER_RECEIVED;
        var recipient = _faker.Internet.Email();
        var caseFileNumber = $"{_faker.Random.Number(1000, 9999)}";
        var lastName = _faker.Name.LastName();
        var data = new { CaseFileNumber = caseFileNumber, LastName = lastName };

        var emailTemplate = new EmailTemplate
        {
            TemplateName = templateName,
            Subject = "Order Received for {{case_file_number}}",
            Body = "Dear Judge {{last_name}}, Your order for case {{case_file_number}} has been received."
        };

        _mockEmailTemplateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync([emailTemplate]);

        await _emailTemplateService.SendEmailTemplateAsync(templateName, recipient, data);

        _mockEmailService.Verify(s => s.SendEmailAsync(
            It.IsAny<string>(),
            recipient,
            It.Is<string>(subj => subj.Contains(caseFileNumber)),
            It.Is<string>(body => body.Contains(lastName) && body.Contains(caseFileNumber)),
            null,
            null,
            null),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailTemplateAsync_WithNonExistentTemplate_LogsWarningAndDoesNotSendEmail()
    {
        var templateName = _faker.Lorem.Sentence();
        var recipient = _faker.Internet.Email();
        var caseFileNumber = $"{_faker.Random.Number(1000, 9999)}";
        var data = new { CaseFileNumber = caseFileNumber };

        _mockEmailTemplateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync([]);

        await _emailTemplateService.SendEmailTemplateAsync(templateName, recipient, data);

        _mockEmailService.Verify(s => s.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            null,
            null,
            null),
            Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailTemplateAsync_WithNullTemplateResult_LogsWarningAndDoesNotSendEmail()
    {
        var templateName = _faker.Lorem.Word();
        var recipient = _faker.Internet.Email();
        var caseFileNumber = $"F-{_faker.Random.Number(1000, 9999)}";
        var data = new { CaseFileNumber = caseFileNumber };

        _mockEmailTemplateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync((IEnumerable<EmailTemplate>)null);

        await _emailTemplateService.SendEmailTemplateAsync(templateName, recipient, data);

        _mockEmailService.Verify(s => s.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            null,
            null,
            null),
            Times.Never);
    }

    [Fact]
    public async Task SendEmailTemplateAsync_WithEmailServiceException_LogsError()
    {
        var templateName = _faker.Lorem.Word();

        var emailTemplate = new EmailTemplate
        {
            TemplateName = templateName,
            Subject = _faker.Lorem.Sentence(),
            Body = _faker.Lorem.Paragraph()
        };

        _mockEmailTemplateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync([emailTemplate]);

        _mockEmailService
            .Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                null))
            .ThrowsAsync(new Exception("Email send failed"));

        await _emailTemplateService.SendEmailTemplateAsync(templateName, null, null);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailTemplateAsync_WithComplexTemplate_RendersAllVariables()
    {
        var templateName = _faker.Lorem.Word();
        var recipient = _faker.Internet.Email();
        var caseFileNumber = $"{_faker.Random.Number(1000, 9999)}";
        var lastName = _faker.Name.LastName();
        var firstName = _faker.Name.FirstName();
        var courtLocation = _faker.Address.City();
        var data = new
        {
            CaseFileNumber = caseFileNumber,
            LastName = lastName,
            FirstName = firstName,
            CourtLocation = courtLocation
        };

        var emailTemplate = new EmailTemplate
        {
            TemplateName = templateName,
            Subject = "Case {{case_file_number}} - {{last_name}}",
            Body = "Hello {{first_name}} {{last_name}}, Your case {{case_file_number}} at {{court_location}} is ready."
        };

        _mockEmailTemplateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync([emailTemplate]);

        await _emailTemplateService.SendEmailTemplateAsync(templateName, recipient, data);

        _mockEmailService.Verify(s => s.SendEmailAsync(
            It.IsAny<string>(),
            recipient,
            It.Is<string>(subj => subj.Contains(caseFileNumber) && subj.Contains(lastName)),
            It.Is<string>(body => body.Contains(firstName) && body.Contains(lastName) && body.Contains(caseFileNumber) && body.Contains(courtLocation)),
            null,
            null,
            null),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("sent to")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailTemplateAsync_WithEmptyData_RendersTemplateWithoutVariables()
    {
        var templateName = _faker.Lorem.Word();
        var recipient = _faker.Internet.Email();
        var data = new { };
        var staticSubject = _faker.Lorem.Sentence();
        var staticBody = _faker.Lorem.Paragraph();

        var emailTemplate = new EmailTemplate
        {
            TemplateName = templateName,
            Subject = staticSubject,
            Body = staticBody
        };

        _mockEmailTemplateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailTemplate, bool>>>()))
            .ReturnsAsync([emailTemplate]);

        await _emailTemplateService.SendEmailTemplateAsync(templateName, recipient, data);

        _mockEmailService.Verify(s => s.SendEmailAsync(
            It.IsAny<string>(),
            recipient,
            staticSubject,
            staticBody,
            null,
            null,
            null),
            Times.Once);
    }
}
