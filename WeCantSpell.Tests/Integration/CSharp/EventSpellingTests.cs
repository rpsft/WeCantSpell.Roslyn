﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using WeCantSpell.Tests.Utilities;
using Xunit;

namespace WeCantSpell.Tests.Integration.CSharp
{
    public class EventSpellingTests : CSharpTestBase
    {
        public static IEnumerable<object[]> can_find_mistakes_in_various_fields_data
        {
            get
            {
                yield return new object[] { "Do", 143 };
                yield return new object[] { "The", 145 };
                yield return new object[] { "Thing", 148 };
                yield return new object[] { "Click", 193 };
                yield return new object[] { "Clack", 198 };
            }
        }

        [Theory, MemberData(nameof(can_find_mistakes_in_various_fields_data))]
        public async Task can_find_mistakes_in_various_fields(string expectedWord, int expectedStart)
        {
            var expectedEnd = expectedStart + expectedWord.Length;

            var analyzer = new SpellingAnalyzerCSharp(new WrongWordChecker(expectedWord));
            var project = await ReadCodeFileAsProjectAsync("Events.SimpleExamples.cs");

            var diagnostics = await GetDiagnosticsAsync(project, analyzer);

            diagnostics.Should().ContainSingle()
                .Subject.Should()
                .HaveId("SP3110")
                .And.HaveLocation(expectedStart, expectedEnd, "Events.SimpleExamples.cs")
                .And.HaveMessageContaining(expectedWord);
        }
    }
}