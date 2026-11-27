using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using PCSSCommon.Models;
using Scv.Api.Services;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;
using Xunit;
using Role = Scv.Db.Models.Role;

namespace Scv.Api.Tests.Services
{
    public class PcssSyncServiceTests
    {
        private readonly Mock<IPcssAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IGroupService> _groupServiceMock;
        private readonly Mock<IJudgeService> _judgeServiceMock;
        private readonly Mock<IRepositoryBase<Group>> _groupRepoMock;
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly Mock<IRepositoryBase<Role>> _roleRepoMock;
        private readonly Mock<ILogger<PcssSyncService>> _loggerMock;
        private readonly PcssSyncService _pcssSyncService;

        public PcssSyncServiceTests()
        {
            _authorizationServiceMock = new Mock<IPcssAuthorizationService>();

            _groupServiceMock = new Mock<IGroupService>();
            _judgeServiceMock = new Mock<IJudgeService>();
            _roleServiceMock = new Mock<IRoleService>();
            _roleRepoMock = new Mock<IRepositoryBase<Role>>();
            _loggerMock = new Mock<ILogger<PcssSyncService>>();
            _groupRepoMock = new Mock<IRepositoryBase<Group>>();

            _pcssSyncService = new PcssSyncService(
                _authorizationServiceMock.Object,
                _groupServiceMock.Object,
                _judgeServiceMock.Object,
                _groupRepoMock.Object,
                _roleRepoMock.Object,
                _roleServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnNoAccess_WhenNoProvJudGuid()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", IsActive = true, GroupIds = ["manual"], RoleIds = ["admin"] };

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess), result);
            Assert.True(userDto.IsActive);
            Assert.Equal("manual", Assert.Single(userDto.GroupIds));
            Assert.Equal("admin", Assert.Single(userDto.RoleIds));
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldConfirmCacheMissWithPcss()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };

            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto>();
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new() { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", true), Times.Once);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid(It.IsAny<string>(), false), Times.Never);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnNoAccess_WhenMatchingUserNotFound()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com", NativeGuid = "guid", IsActive = true,
                GroupIds = ["pcss-group"], RoleIds = ["pcss-role"], IsPendingRegistration = true, JudgeId = 456
            };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync((UserItem)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess, true), result);
            Assert.False(userDto.IsActive);
            Assert.Empty(userDto.GroupIds);
            Assert.Empty(userDto.RoleIds);
            Assert.True(userDto.IsPendingRegistration);
            Assert.Equal(456, userDto.JudgeId);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenGetPcssUserRoleNamesFails()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Failure("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenGetGroupsByAliasesFails()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com", NativeGuid = "guid", IsActive = true, IsPendingRegistration = true,
                GroupIds = ["existing-group"], RoleIds = ["existing-role"]
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Failure("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
            Assert.Equal("existing-group", Assert.Single(userDto.GroupIds));
            Assert.Equal("existing-role", Assert.Single(userDto.RoleIds));
            Assert.True(userDto.IsActive);
            Assert.True(userDto.IsPendingRegistration);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenGetRolesByAliasesFails()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Failure("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenGetRolesByAliasesReturnsNull()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync((OperationResult<IEnumerable<RoleDto>>)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReportChanges_WhenChangesDetected()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = null,
                IsActive = false,
                GroupIds = []
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto> { new() { Id = "role1" } };
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new() { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.Equal(456, userDto.JudgeId);
            Assert.True(userDto.IsActive);
            Assert.Contains("group1", userDto.GroupIds);
            Assert.Contains("role1", userDto.RoleIds);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, true)]
        public async Task UpdateUserFromPcss_ShouldDetectPendingRegistrationAsTheOnlyChange(bool? pending, bool expectedChanges)
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                IsPendingRegistration = pending,
                GroupIds = ["group1"],
                RoleIds = [],
                FirstName = "John",
                LastName = "Doe"
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto>();
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new() { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, expectedChanges), result);
            Assert.False(userDto.IsPendingRegistration);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenLookupThrows(bool timedOut)
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid", IsPendingRegistration = true, IsActive = true, GroupIds = ["old-group"], RoleIds = ["old-role"] };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ThrowsAsync(timedOut ? new TimeoutException() : new Exception("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
            Assert.True(userDto.IsPendingRegistration);
            Assert.True(userDto.IsActive);
            Assert.Equal("old-group", Assert.Single(userDto.GroupIds));
            Assert.Equal("old-role", Assert.Single(userDto.RoleIds));
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenMatchingUserHasNoUserId()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = null };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReportChanges_WhenJudgeNotFound_And_JudgeIdChanged()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = false,
                GroupIds = []
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto>();
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto>();
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem>();
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.Null(userDto.JudgeId);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenGetPcssUserRoleNamesReturnsNull()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync((OperationResult<IEnumerable<string>>)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFailed_WhenGetGroupsByAliasesReturnsNull()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync((OperationResult<IEnumerable<GroupDto>>)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Failed), result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReportChanges_WhenUserDtoGroupIdsIsNull()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = null
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto>();
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.NotNull(userDto.GroupIds);
            Assert.Contains("group1", userDto.GroupIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnNoAccess_WhenNoGroupsMapToPcssRoles()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = ["group1"],
                RoleIds = ["role1"],
                IsPendingRegistration = true
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role1"]));

            var groups = new List<GroupDto>();
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success([new RoleDto { Id = "role1" }]));
            _judgeServiceMock.Setup(x => x.GetJudges(null, null)).ReturnsAsync([]);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess, true), result);
            Assert.False(userDto.IsActive);
            Assert.False(userDto.IsPendingRegistration);
            Assert.Empty(userDto.GroupIds);
            Assert.Empty(userDto.RoleIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnNoAccess_WhenPcssConfirmsNoActiveRoles()
        {
            var userDto = new UserDto
            {
                NativeGuid = "guid", IsPendingRegistration = true, IsActive = true,
                GroupIds = ["old-group"], RoleIds = ["old-role"]
            };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(new UserItem { UserId = 123 });
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success([]));
            _judgeServiceMock.Setup(x => x.GetJudges(null, null)).ReturnsAsync([]);

            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess, true), result);
            Assert.False(userDto.IsPendingRegistration);
            Assert.False(userDto.IsActive);
            Assert.Empty(userDto.GroupIds);
            Assert.Empty(userDto.RoleIds);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UpdateUserFromPcss_PreservesOnlyTestingAndDeveloper_WhenPcssGrantsNoAccess(bool hasMatchingUser)
        {
            var user = new UserDto { NativeGuid = "guid", IsActive = true, GroupIds = ["testing", "old-group"], RoleIds = ["old-role"] };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(hasMatchingUser ? new UserItem { UserId = 123 } : null);
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success([]));
            _judgeServiceMock.Setup(x => x.GetJudges(null, null)).ReturnsAsync([]);
            _groupRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
                .ReturnsAsync([new Group { Id = "testing", Name = Group.TESTING, Description = "Testing" }]);
            _roleRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
                .ReturnsAsync([new Role { Id = "developer", Name = Role.DEVELOPER, Description = "Developer" }]);

            var result = await _pcssSyncService.UpdateUserFromPcss(user);

            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess, true), result);
            Assert.True(user.IsActive);
            Assert.Equal("testing", Assert.Single(user.GroupIds));
            Assert.Equal("developer", Assert.Single(user.RoleIds));
            Assert.Equal(hasMatchingUser ? false : (bool?)null, user.IsPendingRegistration);
        }

        [Fact]
        public async Task UpdateUserFromPcss_LeavesRolesInherited_WhenTestingAccompaniesAGroupWithoutDirectRoles()
        {
            var user = new UserDto { NativeGuid = "guid", IsActive = true, GroupIds = ["testing"], RoleIds = ["developer"] };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true)).ReturnsAsync(new UserItem { UserId = 123 });
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["pcss-role"]));
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success([new GroupDto { Id = "judiciary" }]));
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success([]));
            _judgeServiceMock.Setup(x => x.GetJudges(null, null)).ReturnsAsync([]);
            _groupRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Group, bool>>>())).ReturnsAsync([
                new Group { Id = "testing", Name = Group.TESTING, Description = "Testing", RoleIds = ["developer"] }]);
            _roleRepoMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Role, bool>>>())).ReturnsAsync([
                new Role { Id = "developer", Name = Role.DEVELOPER, Description = "Developer" }]);

            var result = await _pcssSyncService.UpdateUserFromPcss(user);

            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.Equal(new[] { "judiciary", "testing" }, user.GroupIds);
            Assert.Empty(user.RoleIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ReportsOnlyPendingChangeOnce_WhenUserHasNoApplicableAccess()
        {
            var user = new UserDto { NativeGuid = "guid", IsPendingRegistration = null };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true)).ReturnsAsync(new UserItem { UserId = 123 });
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success([]));
            _judgeServiceMock.Setup(x => x.GetJudges(null, null)).ReturnsAsync([]);

            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess, true), await _pcssSyncService.UpdateUserFromPcss(user));
            Assert.False(user.IsPendingRegistration);
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.NoAccess), await _pcssSyncService.UpdateUserFromPcss(user));
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReportChanges_WhenGroupIdsChange()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = ["group1"]
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role2"]));

            var groups = new List<GroupDto> { new() { Id = "group2" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto> { new() { Id = "role1" } };
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.Contains("group2", userDto.GroupIds);
            Assert.DoesNotContain("group1", userDto.GroupIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReportChanges_WhenRoleIdsChange()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = [],
                RoleIds = ["role1"]
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role2" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto> { new() { Id = "role2" } };
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.Contains("role2", userDto.RoleIds);
            Assert.DoesNotContain("role1", userDto.RoleIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldUpdateUserProperties_WhenForceRefreshUsed()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "old@test.com",
                NativeGuid = "guid",
                FirstName = "OldName",
                LastName = "OldLastName",
                JudgeId = null,
                IsActive = false,
                GroupIds = []
            };
            var pcssUser = new UserItem
            {
                GUID = "guid",
                UserId = 123,
                GivenName = "NewName",
                Surname = "NewSurname",
                Email = "new@test.com"
            };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(["role"]));

            var groups = new List<GroupDto> { new() { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var roles = new List<RoleDto>();
            _roleServiceMock.Setup(x => x.GetRolesByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success(roles));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 789 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.Equal(new PcssSyncResult(PcssSyncOutcome.Synchronized, true), result);
            Assert.Equal("NewName", userDto.FirstName);
            Assert.Equal("NewSurname", userDto.LastName);
            Assert.Equal("new@test.com", userDto.Email);
            Assert.Equal(789, userDto.JudgeId);
            Assert.True(userDto.IsActive);
            Assert.Contains("group1", userDto.GroupIds);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", true), Times.Once);
        }
    }
}
