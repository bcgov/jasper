﻿using JCCommon.Clients.LookupServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Controllers;
using Scv.Api.Services;
using tests.api.Helpers;
using Xunit;

namespace tests.api.Controllers
{
    /// <summary>
    /// These tests, ensure Api.LookupControllers and JC-Client-Interface.LookupServiceClient work correctly. 
    /// Credit to DARS for most of these tests. 
    /// </summary>
    public class LookupControllerTests
    {
        #region Variables
        private readonly LookupController _controller;
        #endregion


        #region Constructor
        public LookupControllerTests()
        {
            var preTest = new EnvironmentBuilder("LookupServicesClient:Username", "LookupServicesClient:Password", typeof(FilesController));
            var lookupService = new LookupService(preTest.Configuration, new LookupServiceClient(preTest.HttpClient), new CachingService());
            _controller = new LookupController(preTest.Configuration, preTest.LogFactory.CreateLogger<LookupController>(), lookupService, new Mapper());
        }
        #endregion


        #region Tests
        [Fact]
        public async void Document_Codes()
        {
            var actionResult = await _controller.GetDocumentCodes();

            var lookupCodes = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Contains(lookupCodes, fd => fd.CodeType == "DOCUMENT_TYPES");
        }

        #endregion
    }
}