using Xunit;

namespace tests.api.Fixtures;

[CollectionDefinition("ServiceFixture")]
public class ServiceFixtureCollection :
    ICollectionFixture<CourtListServiceFixture>,
    ICollectionFixture<FilesServiceFixture>,
    ICollectionFixture<LocationServiceFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}