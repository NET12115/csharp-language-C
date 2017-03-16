﻿using System;
using FluentAssertions;
using Lsp.Models;
using Newtonsoft.Json;
using Xunit;

namespace Lsp.Tests.Models
{
    public class ApplyWorkspaceEditResponseTests
    {
        [Theory, JsonFixture]
        public void SimpleTest(string expected)
        {
            var model = new ApplyWorkspaceEditResponse() {
                Applied = true,
            };
            var result = Fixture.SerializeObject(model);
            
            result.Should().Be(expected);

            var deresult = JsonConvert.DeserializeObject<ApplyWorkspaceEditResponse>(expected);
            deresult.ShouldBeEquivalentTo(model);
        }
    }
}
