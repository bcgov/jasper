using System;
using System.Collections.Generic;
using Scv.Api.Helpers;
using Scv.Api.Models;
using Xunit;

namespace tests.api.Helpers;

public class ValidUserHelperTests
{
    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusesIsNull()
    {
        // Arrange
        var person = new Person
        {
            Statuses = null
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusesIsEmpty()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>()
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusDescriptionIsNotInactive()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = "Active",
                    EffDate = DateTime.Now.AddDays(-1)
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusDescriptionIsInactiveButEffDateIsInFuture()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now.AddDays(1)
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnFalse_WhenStatusDescriptionIsInactiveAndEffDateIsPast()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now.AddDays(-1)
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnFalse_WhenStatusDescriptionIsInactiveAndEffDateIsToday()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPersonActive_ShouldCheckFirstStatusOnly()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = "Active",
                    EffDate = DateTime.Now.AddDays(-1)
                },
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now.AddDays(-10)
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusDescriptionIsNull()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = null,
                    EffDate = DateTime.Now.AddDays(-1)
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldBeCaseSensitive()
    {
        // Arrange
        var person = new Person
        {
            Statuses = new List<PersonStatus>
            {
                new PersonStatus
                {
                    StatusDescription = "inactive", // lowercase
                    EffDate = DateTime.Now.AddDays(-1)
                }
            }
        };

        // Act
        var result = ValidUserHelper.IsPersonActive(person);

        // Assert
        Assert.True(result); // Should return true because "inactive" != "Inactive"
    }
}
