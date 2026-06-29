# WeCantSpell.Roslyn

WeCantSpell.Roslyn is a spell check analyzer for Roslyn languages, such as C# and Visual Basic. It uses [WeCantSpell.Hunspell](https://www.nuget.org/packages/WeCantSpell.Hunspell/), a fully managed C# port of the Hunspell spell checker engine, to check and suggest words.

## Features

- Reads Hunspell DIC and AFF file formats
- Supports checking and suggesting words
- Can be queried concurrently
- Compatible with .NET Core, .NET Standard and multiple .NET framework versions

## Installation

You can install WeCantSpell.Roslyn as a NuGet package using the following command:

`dotnet add package WeCantSpell.Roslyn`

## Configuration

To use WeCantSpell.Roslyn, you need to configure the analyzer options in your project. There are two ways to do this:

- Using `.wecantspell` or `.wecantspell.json` files, located in the project root or inside nested directories. The structure of the file is as follows:

```json
{
    "isRoot" : true,
    "languages" : [
        "en-US",
        "ru-RU"
    ]
}
```

The `isRoot` property indicates whether the file is located in the solution root or not. The `languages` property specifies the list of languages to use for spell checking.

- Using additional project dictionaries, named one of: `.spelling.dic`, `.directory.dic`, `.wecantspell.dic`. These files can be located in the project root or inside nested directories. They contain custom words to add to the spell checker.

### Dictionary file format

Dictionary files use a simple, plain-text format: **one word per line**. Blank lines and lines starting with `#` are ignored (you can use `#` for comments). A leading all-digits Hunspell word-count header is also skipped for backward compatibility.

```
# My project's custom words
bazinga
froomplestoot
utc
```

#### Case sensitivity

Dictionary entries follow standard Hunspell casing rules:

- An **all-lowercase** entry (e.g. `utc`) matches all case forms: `utc`, `Utc`, `UTC`.
- An **initial-cap** entry (e.g. `Utc`) matches `Utc` and `UTC` but **not** `utc`.
- An **all-caps** entry (e.g. `UTC`) matches only `UTC`.

**Recommendation:** add the lowercase form of a word to cover all casings (e.g. add `utc`, not `Utc`).

### Automatic dictionary tracking (live reload without build-server restart)

Starting from version 2.0.8, the NuGet package ships MSBuild props that automatically add any dictionary and config files found in or above the project directory to Roslyn's `AdditionalFiles`. This makes dictionary edits tracked compilation inputs: editing a dictionary takes effect on the **next normal `dotnet build`** without needing `dotnet build-server shutdown`.

The auto-wiring is enabled by default. To opt out, set the `WeCantSpellAddAdditionalFiles` property to `false` in your project or `Directory.Build.props`:

```xml
<PropertyGroup>
  <WeCantSpellAddAdditionalFiles>false</WeCantSpellAddAdditionalFiles>
</PropertyGroup>
```

> **Note:** MSBuild evaluates globs at project load time, so a **newly created** dictionary file may require a one-time project reload (close and reopen the solution in your IDE) to be picked up. **Edits to existing** dictionary files take effect immediately on the next build.

You also need to set the severity of the analyzer diagnostics in your editorconfig file. For example:

```ini
root = true

[*.{cs,vb}]
# WeCantSpell properties
dotnet_diagnostic.SP3110.severity = warning
dotnet_diagnostic.SP3111.severity = suggestion
dotnet_diagnostic.SP3112.severity = suggestion
dotnet_diagnostic.SP3113.severity = suggestion
```

The analyzer will report spelling errors in your code according to the following error codes:

- SP3110: Identifier Spelling - Identifier name may contain a spelling error.
- SP3111: Text Literal Spelling - Text literal may contain a spelling error.
- SP3112: Comment Spelling - Comment may contain a spelling error.
- SP3113: Xml Documentation Spelling - Xml documentation may contain a spelling error.

For more details, please refer to the [source code](https://github.com/aarondandy/WeCantSpell.Roslyn) of the project.
