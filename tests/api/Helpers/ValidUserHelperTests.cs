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
        var person = new Person
        {
            Statuses = null
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusesIsEmpty()
    {
        var person = new Person
        {
            Statuses = []
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusDescriptionIsNotInactive()
    {
        var person = new Person
        {
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "Active",
                    EffDate = DateTime.Now.AddDays(-1)
                }
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusDescriptionIsInactiveButEffDateIsInFuture()
    {
        var person = new Person
        {
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now.AddDays(1)
                }
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnFalse_WhenStatusDescriptionIsInactiveAndEffDateIsPast()
    {
        var person = new Person
        {
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now.AddDays(-1)
                }
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.False(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnFalse_WhenStatusDescriptionIsInactiveAndEffDateIsToday()
    {
        var person = new Person
        {
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now
                }
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.False(result);
    }

    [Fact]
    public void IsPersonActive_ShouldCheckFirstStatusOnly()
    {
        var person = new Person
        {
            Statuses =
            [
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
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldReturnTrue_WhenStatusDescriptionIsNull()
    {
        var person = new Person
        {
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = null,
                    EffDate = DateTime.Now.AddDays(-1)
                }
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result);
    }

    [Fact]
    public void IsPersonActive_ShouldBeCaseSensitive()
    {
        var person = new Person
        {
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "inactive", // lowercase
                    EffDate = DateTime.Now.AddDays(-1)
                }
            ]
        };

        var result = ValidUserHelper.IsPersonActive(person);

        Assert.True(result); // Should return true because "inactive" != "Inactive"
    }
}
