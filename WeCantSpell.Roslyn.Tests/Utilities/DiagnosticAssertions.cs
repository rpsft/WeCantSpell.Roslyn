using System.Globalization;
using AwesomeAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace WeCantSpell.Roslyn.Tests.Utilities
{
    [CustomAssertions]
    public class DiagnosticAssertions : ReferenceTypeAssertions<Diagnostic, DiagnosticAssertions>
    {
        private readonly AssertionChain _assertionChain;

        public DiagnosticAssertions(Diagnostic value, AssertionChain assertionChain)
            : base(value, assertionChain)
        {
            _assertionChain = assertionChain;
        }

        protected override string Identifier => "diagnostic";

        public AndConstraint<DiagnosticAssertions> HaveId(
            string expected,
            string because = "",
            params object[] becauseArgs
        )
        {
            var actual = Subject?.Id;

            _assertionChain
                .ForCondition(string.Equals(actual, expected, StringComparison.Ordinal))
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:id} to be {0}{reason}, but found {1}.", expected, actual);

            return new AndConstraint<DiagnosticAssertions>(this);
        }

        private AndConstraint<DiagnosticAssertions> BeFromFileName(
            string expected,
            string because = "",
            params object[] becauseArgs
        )
        {
            var actual = Subject?.Location.SourceTree?.FilePath;
            _assertionChain
                .ForCondition(string.Equals(actual, expected, StringComparison.Ordinal))
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:file} to be {0}{reason}, but found {1}.", expected, actual);

            return new AndConstraint<DiagnosticAssertions>(this);
        }

        private AndConstraint<DiagnosticAssertions> HaveLineSpan(
            int expectedLine,
            int expectedCharacter,
            int expectedLength,
            string because = "",
            params object[] becauseArgs
        )
        {
            var location = Subject?.Location;

            _assertionChain
                .ForCondition(location != null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:diagnostic} to have a location.");

            if (_assertionChain.Succeeded)
            {
                var actual = location!.GetLineSpan();

                _assertionChain
                    .ForCondition(
                        actual.StartLinePosition.Line == expectedLine - 1
                            && actual.StartLinePosition.Character == expectedCharacter - 1
                    )
                    .BecauseOf(because, becauseArgs)
                    .FailWith(
                        "Expected {context:span} to start at {0}:{1}{reason}, but found {2}:{3}.",
                        expectedLine,
                        expectedCharacter,
                        actual.StartLinePosition.Line + 1,
                        actual.StartLinePosition.Character + 1
                    );

                var actualLength = location.SourceSpan.Length;
                _assertionChain
                    .ForCondition(actualLength == expectedLength)
                    .BecauseOf(because, becauseArgs)
                    .FailWith(
                        "Expected {context:span} to have length {0}{reason}, but found {1}.",
                        expectedLength,
                        actualLength
                    );
            }

            return new AndConstraint<DiagnosticAssertions>(this);
        }

        public AndConstraint<DiagnosticAssertions> HaveLineLocation(
            int expectedLine,
            int expectedCharacter,
            int expectedLength,
            string expectedFileName,
            string because = "",
            params object[] becauseArgs
        ) =>
            HaveLineSpan(expectedLine, expectedCharacter, expectedLength, because, becauseArgs)
                .And.BeFromFileName(expectedFileName, because, becauseArgs);

        public AndConstraint<DiagnosticAssertions> HaveMessageContaining(
            string expectedSubstring,
            string because = "",
            params object[] becauseArgs
        )
        {
            var message = Subject?.GetMessage(CultureInfo.InvariantCulture);

            _assertionChain
                .ForCondition(message != null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:invariant message} to not be null.");

            if (_assertionChain.Succeeded)
            {
                _assertionChain
                    .ForCondition(message!.Contains(expectedSubstring))
                    .BecauseOf(because, becauseArgs)
                    .FailWith(
                        "Expected {context:invariant message} to contain {0}{reason}, but did not.",
                        expectedSubstring
                    );
            }

            return new AndConstraint<DiagnosticAssertions>(this);
        }
    }
}
