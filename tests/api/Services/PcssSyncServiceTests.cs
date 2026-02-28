using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Xunit;

namespace Scv.Api.Tests.Services
{
    public class PcssSyncServiceTests
    {
        private readonly Mock<IPcssAuthorizationService> _authorizationServiceMock;
        private readonly Mock<IGroupService> _groupServiceMock;
        private readonly Mock<IJudgeService> _judgeServiceMock;
        private readonly Mock<ILogger<PcssSyncService>> _loggerMock;
        private readonly PcssSyncService _pcssSyncService;

        public PcssSyncServiceTests()
        {
            _authorizationServiceMock = new Mock<IPcssAuthorizationService>();

            _groupServiceMock = new Mock<IGroupService>();
            _judgeServiceMock = new Mock<IJudgeService>();
            _loggerMock = new Mock<ILogger<PcssSyncService>>();

            _pcssSyncService = new PcssSyncService(
                _authorizationServiceMock.Object,
                _groupServiceMock.Object,
                _judgeServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenNoProvJudGuid()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = null };

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldCallGetUserByGuid_WithForceRefreshFalse_ByDefault()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };

            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", false), Times.Once);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid(It.IsAny<string>(), true), Times.Never);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldCallGetUserByGuid_WithForceRefreshTrue_WhenForceUpdateCacheIsTrue()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };

            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto, forceUpdateCache: true);

            // Assert
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", true), Times.Once);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid(It.IsAny<string>(), false), Times.Never);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenMatchingUserNotFound()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync((UserItem)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenMatchingUserNotFoundWithForceRefresh()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync((UserItem)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto, forceUpdateCache: true);

            // Assert
            Assert.False(result);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", true), Times.Once);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenGetPcssUserRoleNamesFails()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Failure("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenGetGroupsByAliasesFails()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Failure("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnTrue_WhenChangesDetected()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = null,
                IsActive = false,
                GroupIds = new List<string>()
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.Equal(456, userDto.JudgeId);
            Assert.True(userDto.IsActive);
            Assert.Contains("group1", userDto.GroupIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_WithForceRefresh_ShouldReturnTrue_WhenChangesDetected()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = null,
                IsActive = false,
                GroupIds = new List<string>()
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto, forceUpdateCache: true);

            // Assert
            Assert.True(result);
            Assert.Equal(456, userDto.JudgeId);
            Assert.True(userDto.IsActive);
            Assert.Contains("group1", userDto.GroupIds);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", true), Times.Once);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenNoChangesDetected()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = new List<string> { "group1" },
                FirstName = "John",
                LastName = "Doe"
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenExceptionThrown()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ThrowsAsync(new Exception("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_WithForceRefresh_ShouldReturnFalse_WhenExceptionThrown()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", true))
                .ThrowsAsync(new Exception("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto, forceUpdateCache: true);

            // Assert
            Assert.False(result);
            _authorizationServiceMock.Verify(x => x.GetUserByGuid("guid", true), Times.Once);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenMatchingUserHasNoUserId()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = null };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnTrue_WhenJudgeNotFound_And_JudgeIdChanged()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = false,
                GroupIds = new List<string>()
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto>();
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem>();
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.Null(userDto.JudgeId);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenGetPcssUserRoleNamesReturnsNull()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync((OperationResult<IEnumerable<string>>)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenGetGroupsByAliasesReturnsNull()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync((OperationResult<IEnumerable<GroupDto>>)null);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnTrue_WhenUserDtoGroupIdsIsNull()
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
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.NotNull(userDto.GroupIds);
            Assert.Contains("group1", userDto.GroupIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnTrue_WhenRolesExistButNoGroups_And_IsActiveChanges()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = new List<string> { "group1" }
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role1" }));

            var groups = new List<GroupDto>();
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.False(userDto.IsActive);
            Assert.Empty(userDto.GroupIds);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnTrue_WhenGroupIdsChange()
        {
            // Arrange
            var userDto = new UserDto
            {
                Email = "test@test.com",
                NativeGuid = "guid",
                JudgeId = 456,
                IsActive = true,
                GroupIds = new List<string> { "group1" }
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123, GivenName = "John", Surname = "Doe", Email = "test@test.com" };
            _authorizationServiceMock.Setup(x => x.GetUserByGuid("guid", false))
                .ReturnsAsync(pcssUser);

            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role2" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group2" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.Contains("group2", userDto.GroupIds);
            Assert.DoesNotContain("group1", userDto.GroupIds);
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
                GroupIds = new List<string>()
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
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 789 } };
            _judgeServiceMock.Setup(x => x.GetJudges(null, null))
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto, forceUpdateCache: true);

            // Assert
            Assert.True(result);
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
