using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly Mock<IAuthorizationServicesClient> _pcssAuthorizationServiceClientMock;
        private readonly Mock<AuthorizationService> _authorizationServiceMock;
        private readonly Mock<IGroupService> _groupServiceMock;
        private readonly Mock<IDashboardService> _dashboardServiceMock;
        private readonly Mock<ILogger<PcssSyncService>> _loggerMock;
        private readonly PcssSyncService _pcssSyncService;

        public PcssSyncServiceTests()
        {
            _pcssAuthorizationServiceClientMock = new Mock<IAuthorizationServicesClient>();
            
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            
            var configSectionMock = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
            configSectionMock.Setup(x => x.Value).Returns("60");
            configMock.Setup(x => x.GetSection("Caching:UserExpiryMinutes")).Returns(configSectionMock.Object);
            
            var cacheMock = new Mock<LazyCache.IAppCache>();
            var cachePolicy = new LazyCache.CacheDefaults();
            cacheMock.Setup(x => x.DefaultCachePolicy).Returns(cachePolicy);

            var loggerMock = new Mock<ILogger<AuthorizationService>>();
            
            _authorizationServiceMock = new Mock<AuthorizationService>(
                configMock.Object, 
                _pcssAuthorizationServiceClientMock.Object, 
                cacheMock.Object, 
                loggerMock.Object);
            
            _groupServiceMock = new Mock<IGroupService>();
            _dashboardServiceMock = new Mock<IDashboardService>();
            _loggerMock = new Mock<ILogger<PcssSyncService>>();
            
            _pcssSyncService = new PcssSyncService(
                _pcssAuthorizationServiceClientMock.Object,
                _authorizationServiceMock.Object,
                _groupServiceMock.Object,
                _dashboardServiceMock.Object,
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
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenMatchingUserNotFound()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem>());

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenGetPcssUserRoleNamesFails()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
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
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
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
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _dashboardServiceMock.Setup(x => x.GetJudges())
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
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenNoChangesDetected()
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
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _dashboardServiceMock.Setup(x => x.GetJudges())
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
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenMatchingUserHasNoUserId()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = null }; // UserId is null
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });

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
                JudgeId = 456, // Currently has a judge ID
                IsActive = false,
                GroupIds = new List<string>()
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto>(); // No groups
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem>(); // No judges found
            _dashboardServiceMock.Setup(x => x.GetJudges())
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.Null(userDto.JudgeId); // Should be updated to null
        }

        [Fact]
        public async Task UpdateUserFromPcss_ShouldReturnFalse_WhenGetPcssUserRoleNamesReturnsNull()
        {
            // Arrange
            var userDto = new UserDto { Email = "test@test.com", NativeGuid = "guid" };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
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
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
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
                GroupIds = null // Null group IDs
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group1" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _dashboardServiceMock.Setup(x => x.GetJudges())
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
                IsActive = true, // Currently active
                GroupIds = new List<string> { "group1" }
            };
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role1" }));

            var groups = new List<GroupDto>(); // No groups mapped
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _dashboardServiceMock.Setup(x => x.GetJudges())
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.False(userDto.IsActive); // Should become inactive
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
            var pcssUser = new UserItem { GUID = "guid", UserId = 123 };
            _pcssAuthorizationServiceClientMock.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserItem> { pcssUser });
            
            _authorizationServiceMock.Setup(x => x.GetPcssUserRoleNames(123))
                .ReturnsAsync(OperationResult<IEnumerable<string>>.Success(new List<string> { "role2" }));

            var groups = new List<GroupDto> { new GroupDto { Id = "group2" } };
            _groupServiceMock.Setup(x => x.GetGroupsByAliases(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success(groups));

            var judges = new List<PersonSearchItem> { new PersonSearchItem { UserId = 123, PersonId = 456 } };
            _dashboardServiceMock.Setup(x => x.GetJudges())
                .ReturnsAsync(judges);

            // Act
            var result = await _pcssSyncService.UpdateUserFromPcss(userDto);

            // Assert
            Assert.True(result);
            Assert.Contains("group2", userDto.GroupIds);
            Assert.DoesNotContain("group1", userDto.GroupIds);
        }
    }
}
